using TGDev.StepS01.Shared.Services;
using System.Text;
using TGDev.StepS01.Shared.Models;
using Microsoft.AspNetCore.WebUtilities;


namespace TGDev.Experimentation01.Services;

/// <summary>
/// Orchestrateur des 7 étapes de l'expérimentation IA (de l'actualité au portefeuille).
/// Chaque étape est indépendante : elle peut être déclenchée seule, dans n'importe quel ordre,
/// sans dépendre de l'exécution préalable des autres. C'est l'UI (Steps.razor) qui décide
/// quand appeler ExecuterEtapeAsync pour une étape donnée.
///
/// NOTE : le contenu de chaque étape est ici SIMULÉ (Task.Delay + données factices) afin que
/// la page fonctionne immédiatement "out of the box". Chaque méthode privée EtapeX(...)
/// est le point où brancher la vraie logique (appel API news, EF Core, base vectorielle,
/// LLM local, broker de marché, etc.) — voir les commentaires TODO.
/// </summary>
public class PipelineService
{
    private readonly Dictionary<int, CancellationTokenSource> _executionsEnCours = [];

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ISharedService SharedService;
    public List<PipelineStep> Etapes { get; }

    /// <summary>Étape actuellement affichée dans le dashboard (pilotée par la barre d'onglets).</summary>
    public int EtapeSelectionnee { get; set; } = 1;

    /// <summary>
    /// SIMULATOR
    /// </summary>
    public bool SimulateurOuvert { get; set; }
    public double CalledUpCapital { get; set; } = 10000;

    /// <summary>
    /// AUTOMATION
    /// </summary>

    public bool OpenedAutomation { get; set; }
    public bool AutomationInProgress { get; set; }
    /// <summary>Journal propre à l'automatisation (distinct du journal de chaque étape).</summary>
    public List<JournalEntry> JournalAutomatisation { get; } = [];

    public string? DerniereExecutionN8nId { get; private set; }
    public N8nStatutExecution DernierStatutN8n { get; private set; } = N8nStatutExecution.Inconnu;

    /// <summary>
    /// Exécute les 7 étapes dans l'ordre, l'une après l'autre, exactement
    /// comme si l'utilisateur avait cliqué sur chaque bouton "Exécuter" à
    /// la suite. Réutilise ExecuterEtapeAsync : chaque étape garde donc son
    /// propre journal, ses indicateurs, etc. — rien de spécifique à dupliquer.
    /// </summary>
    public async Task ExecuterToutesLesEtapesAsync()
    {
        if (AutomationInProgress) return;

        AutomationInProgress = true;
        JournalAutomatisation.Clear();
        LogAutomatisation(NiveauJournal.Info, "Démarrage de l'automatisation (exécution locale séquentielle).");
        NotifyChange();

        foreach (var etape in Etapes.OrderBy(e => e.Numero).Take(4))
        {
            LogAutomatisation(NiveauJournal.Info, $"Étape {etape.Numero} — {etape.Titre} : démarrage.");
            await ExecuterEtapeAsync(etape.Numero);

            var niveau = etape.Statut == StepStatus.Erreur ? NiveauJournal.Warn : NiveauJournal.Success;
            LogAutomatisation(niveau, $"Étape {etape.Numero} — {etape.Titre} : {(etape.Statut == StepStatus.Erreur ? "terminée en erreur" : "terminée")}.");
        }

        LogAutomatisation(NiveauJournal.Success, "Automatisation terminée — les 7 étapes ont été exécutées.");
        AutomationInProgress = false;
        NotifyChange();
    }

    /// <summary>
    /// Déclenche l'automatisation via un workflow n8n externe plutôt qu'en
    /// local. Le pipeline C# ne fait ici qu'appeler le webhook et suivre le
    /// statut — c'est le workflow n8n qui est responsable d'appeler, dans
    /// l'ordre, les endpoints des modules (FeedFetcher.Api, etc.).
    /// </summary>
    public async Task LancerAutomatisationN8nAsync(IN8nWorkflowClient n8nClient)
    {
        if (AutomationInProgress) return;

        AutomationInProgress = true;
        JournalAutomatisation.Clear();
        DerniereExecutionN8nId = null;
        DernierStatutN8n = N8nStatutExecution.Inconnu;
        LogAutomatisation(NiveauJournal.Info, "Déclenchement du workflow n8n...");
        NotifyChange();

        var declenchement = await n8nClient.DeclencherWorkflowAsync(CancellationToken.None);

        if (!declenchement.Succes)
        {
            LogAutomatisation(NiveauJournal.Error, declenchement.Message ?? "Échec du déclenchement du workflow n8n.");
            AutomationInProgress = false;
            NotifyChange();
            return;
        }

        LogAutomatisation(NiveauJournal.Success, declenchement.Message ?? "Workflow déclenché.");
        DerniereExecutionN8nId = declenchement.ExecutionId;

        // Suivi de statut par polling, uniquement si n8n a renvoyé un executionId
        // et que l'API REST n8n est configurée (N8n:BaseUrl + N8n:ApiKey).
        if (declenchement.ExecutionId is not null)
        {
            for (int tentative = 0; tentative < 30; tentative++) // ~1 min max (30 x 2s)
            {
                await Task.Delay(2000);
                var statut = await n8nClient.ObtenirStatutAsync(declenchement.ExecutionId, CancellationToken.None);
                DernierStatutN8n = statut;
                NotifyChange();

                if (statut is N8nStatutExecution.Succes or N8nStatutExecution.Echec) break;
            }

            LogAutomatisation(
                DernierStatutN8n == N8nStatutExecution.Succes ? NiveauJournal.Success : NiveauJournal.Warn,
                $"Statut final du workflow n8n : {DernierStatutN8n}.");
        }

        AutomationInProgress = false;
        NotifyChange();
    }

    private void LogAutomatisation(NiveauJournal niveau, string message)
    {
        JournalAutomatisation.Add(new JournalEntry { Niveau = niveau, Message = message });
        NotifyChange();
    }

    public event Action? OnChange;

    public PipelineService(IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();

        SharedService = new SharedService(configuration);

        var newsRetrieverUrl = configuration["NewsRetrieval:BaseUrl"] ?? "https://localhost:7056";
        var newsRetrieverSwaggerUrl = string.IsNullOrWhiteSpace(newsRetrieverUrl) ? null : $"{newsRetrieverUrl.TrimEnd('/')}/swagger/index.html";

        var databaseStorageUrl = configuration["DatabaseStorage:BaseUrl"] ?? "https://localhost:7170";
        var databaseStorageSwaggerUrl = string.IsNullOrWhiteSpace(databaseStorageUrl) ? null : $"{databaseStorageUrl.TrimEnd('/')}/swagger/index.html";

        var vectorizationUrl = configuration["Vectorization:BaseUrl"] ?? "https://localhost:7001";
        var vectorizationSwaggerUrl = string.IsNullOrWhiteSpace(vectorizationUrl) ? null : $"{vectorizationUrl.TrimEnd('/')}/swagger/index.html";

        Etapes = [
            new()
            {
                Numero = 1,
                Phase = "Phase 1 : Ingestion et intelligence locale",
                Titre = "Collecter l'information",
                Description = "Récupère les dernières actualités : géopolitique, économie & finance, banques & marchés, nouvelles technologies.",
                Icone = "bi-newspaper",
                SwaggerUrl = newsRetrieverSwaggerUrl,
                Executer = Step1_NewsFetcherAsync
            },
            new()
            {
                Numero = 2,
                Phase = "Phase 1 : Ingestion et intelligence locale",
                Titre = "Constituer une base de connaissances",
                Description = "Stocke les actualités collectées dans une base de données structurée.",
                Icone = "bi-database",
                SwaggerUrl = databaseStorageSwaggerUrl,
                Executer = Etape2_ConstituerBaseAsync
            },
            new()
            {
                Numero = 3,
                Phase = "Phase 1 : Ingestion et intelligence locale",
                Titre = "Vectoriser les informations",
                Description = "Transforme les données en vecteurs pour la recherche sémantique par similarité.",
                Icone = "bi-diagram-3",
                SwaggerUrl = vectorizationSwaggerUrl,
                Executer = Etape3_VectoriserAsync
            },
            new()
            {
                Numero = 4,
                Phase = "Phase 1 : Ingestion et intelligence locale",
                Titre = "Enrichir un LLM local (RAG)",
                Description = "Alimente un LLM local avec la base de connaissances pour un raisonnement contextualisé.",
                Icone = "bi-cpu",
                Executer = Etape4_EnrichirLLMAsync
            },
            new()
            {
                Numero = 5,
                Phase = "Phase 2 : Analyse et validation du marché",
                Titre = "Constituer un univers d'investissement",
                Description = "Récupère une liste d'actions et d'ETF représentatifs de la diversité du marché.",
                Icone = "bi-pie-chart",
                Executer = Etape5_UniversInvestissementAsync
            },
            new()
            {
                Numero = 6,
                Phase = "Phase 2 : Analyse et validation du marché",
                Titre = "Faire analyser cet univers par le LLM",
                Description = "Le modèle enrichi identifie les titres à privilégier avec un échéancier basé sur l'actualité.",
                Icone = "bi-search",
                Executer = Etape6_AnalyseLLMAsync
            },
            new()
            {
                Numero = 7,
                Phase = "Phase 2 : Analyse et validation du marché",
                Titre = "Simulateur de placements",
                Description = "Automatise un simulateur pour confronter les prédictions du modèle aux résultats obtenus.",
                Icone = "bi-briefcase",
                Executer = Etape7_SimulateurAsync
            }
        ];
    }

    /// <summary>
    /// Déclenche une étape précise, indépendamment des autres.
    /// Si l'étape est déjà en cours, l'appel est ignoré.
    /// </summary>
    public async Task ExecuterEtapeAsync(int numeroEtape)
    {
        var etape = Etapes.FirstOrDefault(e => e.Numero == numeroEtape);
        if (etape is null || etape.Statut == StepStatus.EnCours) return;

        var cts = new CancellationTokenSource();
        _executionsEnCours[numeroEtape] = cts;

        etape.Statut = StepStatus.EnCours;
        etape.Journal.Clear();
        etape.Indicateurs.Clear();
        etape.Progression = 0;
        etape.NombreErreurs = 0;
        var chrono = System.Diagnostics.Stopwatch.StartNew();

        Log(etape, NiveauJournal.Info, $"Démarrage de l'expérimentation — Étape {etape.Numero} : {etape.Titre}");
        NotifyChange();

        try
        {
            await etape.Executer(etape, cts.Token);
            etape.Statut = StepStatus.Termine;
            etape.DerniereExecution = DateTime.Now;
            etape.Progression = 100;
            Log(etape, NiveauJournal.Success, "Étape terminée avec succès.");
        }
        catch (OperationCanceledException)
        {
            etape.Statut = StepStatus.EnAttente;
            Log(etape, NiveauJournal.Warn, "Étape annulée.");
        }
        catch (Exception ex)
        {
            etape.Statut = StepStatus.Erreur;
            etape.NombreErreurs++;
            Log(etape, NiveauJournal.Error, $"Erreur : {ex.Message}");
        }
        finally
        {
            chrono.Stop();
            etape.Duree = chrono.Elapsed;
            _executionsEnCours.Remove(numeroEtape);
            NotifyChange();
        }
    }

    public void SelectionnerEtape(int numeroEtape) => EtapeSelectionnee = numeroEtape;

    public void AnnulerEtape(int numeroEtape)
    {
        if (_executionsEnCours.TryGetValue(numeroEtape, out var cts))
        {
            cts.Cancel();
        }
    }

    public void OuvrirSimulateur()
    {
        SimulateurOuvert = true;
        NotifyChange();
    }

    public void FermerSimulateur()
    {
        SimulateurOuvert = false;
        NotifyChange();
    }

    public void OpenAutomation()
    {
        OpenedAutomation = true;
        NotifyChange();
    }

    public void CloseAutomation()
    {
        OpenedAutomation = false;
        NotifyChange();
    }

    private void Log(PipelineStep etape, NiveauJournal niveau, string message)
    {
        etape.Journal.Add(new JournalEntry { Niveau = niveau, Message = message });
        NotifyChange();
    }

    private void NotifyChange() => OnChange?.Invoke();

    // ---------------------------------------------------------------
    // Implémentations de chaque étape (à remplacer par la vraie logique)
    // ---------------------------------------------------------------

    private async Task Step1_NewsFetcherAsync(PipelineStep step, CancellationToken ct)
    {

        ct.ThrowIfCancellationRequested();

        var url = "https://localhost:7056/api/feedfetcher";
        int total = 0;

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            SharedService.FeedFetcherJsonResult = await response.Content.ReadAsStringAsync(ct);
            total = SharedService.FeedFetcherJsonResult.Split("\"title\"").Length - 1; // Compte le nombre d'articles récupérés

            Log(step, NiveauJournal.Success, $"{total} articles récupérés sur le web.");
        }
        catch (HttpRequestException ex)
        {
            step.NombreErreurs++;
            Log(step, NiveauJournal.Error, $"Echec FeedFetcher — {ex.Message}");
        }

        step.Progression = 100;
        step.Resume = $"{total} articles";
        step.Indicateurs.Add(new KpiItem { Icone = "bi-file-earmark-text", Valeur = total.ToString(), Label = "Articles trouvés" });
        Log(step, NiveauJournal.Info, $"Total : {total} article(s) collecté(s).");
    }

    private async Task Etape2_ConstituerBaseAsync(PipelineStep step, CancellationToken ct)
    {
        step.SousTitreProgression = "Insertion en base de données";
        step.DetailProgression = $"{SharedService.FeedFetcherJsonResult.Split("\"title\"").Length - 1} articles à enregistrer";

        var url = "https://localhost:7170/api/databasestorage";

        var response = await _httpClient.PostAsync(url, new StringContent(SharedService.FeedFetcherJsonResult, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            // Cela affichera le rapport d'erreur d'ASP.NET Core (souvent un objet ValidationProblemDetails)
            string errorDetails = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Détails de l'erreur 400 : {errorDetails}");
        }

        response.EnsureSuccessStatusCode();

        SharedService.DatabaseStorageResult = await response.Content.ReadFromJsonAsync<IEnumerable<NewsItemModel>>(ct) ?? [];

        int total = SharedService.DatabaseStorageResult!.Count();

        SharedService.NoIndexedNewsItems = [..SharedService.DatabaseStorageResult!.Where(n => !n.IsVectorized)];

        step.Resume = $"{total} enregistrements";
        step.Indicateurs.Add(new KpiItem { Icone = "bi-database-check", Valeur = total.ToString(), Label = "Enregistrements insérés" });
        Log(step, NiveauJournal.Success, $"{total} articles insérés dans la base de connaissances.");
    }

    private async Task Etape3_VectoriserAsync(PipelineStep step, CancellationToken ct)
    {
        step.SousTitreProgression = "Indexation des nouvelles actualités";
        step.DetailProgression = $"{SharedService.NoIndexedNewsItems.Count()} actualités à vectoriser";

        var url = "https://localhost:7001/api/vectorization";
        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            // Cela affichera le rapport d'erreur d'ASP.NET Core (souvent un objet ValidationProblemDetails)
            string errorDetails = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Détails de l'erreur 400 : {errorDetails}");
        }
        response.EnsureSuccessStatusCode();

        var total = SharedService.NoIndexedNewsItems.Count();
        step.Resume = $"{total} enregistrements";
        step.Indicateurs.Add(new KpiItem { Icone = "bi-diagram-3-check", Valeur = total.ToString(), Label = "Enregistrements insérés" });
        Log(step, NiveauJournal.Success, $"Vectorisation terminée : • Collection {SharedService.KernelModel.QdrantCollectionName} • {total} vecteurs indexés");
    }

    private async Task Etape4_EnrichirLLMAsync(PipelineStep step, CancellationToken ct)
    {
        step.SousTitreProgression = "Chargement du contexte RAG dans le LLM local";
        step.DetailProgression = "Recherche des actualités vectorisées";

        var url = "https://localhost:7294/api/localllm";
        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            // Cela affichera le rapport d'erreur d'ASP.NET Core (souvent un objet ValidationProblemDetails)
            string errorDetails = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Détails de l'erreur 400 : {errorDetails}");
        }
        response.EnsureSuccessStatusCode();

        //var total = SharedService.NoIndexedNewsItems.Count();
        //step.Resume = $"{total} enregistrements";
        //step.Indicateurs.Add(new KpiItem { Icone = "bi-diagram-3-check", Valeur = total.ToString(), Label = "Enregistrements insérés" });
        //Log(step, NiveauJournal.Success, $"Vectorisation terminée : • Collection {SharedService.KernelModel.QdrantCollectionName} • {total} vecteurs indexés");
        //await Task.Delay(900, ct); // TODO : brancher un LLM local (Ollama, LM Studio...) en logique RAG sur la base vectorielle
        Log(step, NiveauJournal.Success, "Contexte RAG chargé dans le LLM local.");
    }

    private async Task Etape5_UniversInvestissementAsync(PipelineStep etape, CancellationToken ct)
    {
        string[] titres = [ "CAC 40", "S&P 500", "MSCI World ETF", "Nasdaq 100 ETF" ];
        foreach (var titre in titres)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(300, ct); // TODO : appeler une API de données de marché (ex. données boursières)
            Log(etape, NiveauJournal.Success, $"Ajouté à l'univers : {titre}");
        }
    }

    private async Task Etape6_AnalyseLLMAsync(PipelineStep etape, CancellationToken ct)
    {
        // TODO : remplacer par l'appel réel au LLM enrichi (étape 4) sur l'univers
        // d'investissement réel (étape 5 côté données de marché). Les poids,
        // confiances et justifications ci-dessous sont générés pour la démo.
        (string Titre, ActionRecommandee Action, double Poids, double Confiance, string Justification)[] analyse =
        [
            ("CAC 40", ActionRecommandee.Achat, 30, 72, "Contexte macro européen stabilisé selon les dernières actualités collectées."),
            ("S&P 500", ActionRecommandee.Achat, 35, 81, "Momentum technologique positif identifié dans le flux \"Nouvelles technologies\"."),
            ("MSCI World ETF", ActionRecommandee.Conserver, 20, 64, "Exposition diversifiée, pas de signal fort détecté sur la période."),
            ("Nasdaq 100 ETF", ActionRecommandee.Vente, 15, 58, "Valorisations tendues relevées dans les actualités \"Banques & marchés\".")
        ];

        etape.SousTitreProgression = "Analyse de l'univers d'investissement";
        SharedService.Recommendations.Clear();

        for (int i = 0; i < analyse.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(300, ct); // TODO : appeler l'API de données de marché puis le LLM d'analyse
            var (titre, action, poids, confiance, justification) = analyse[i];

            SharedService.Recommendations.Add(new InvestmentRecommendationModel
            {
                Stock = titre,
                Action = action,
                Weight = poids,
                Confidence = confiance,
                Justification = justification
            });

            Log(etape, NiveauJournal.Info, $"Analysé : {titre} → {LibelleAction(action)} ({poids}% du portefeuille, confiance {confiance}%).");
            etape.Progression = (i + 1) * 100 / analyse.Length;
            etape.Traites = i + 1;
            etape.Restants = analyse.Length - (i + 1);
            NotifyChange();
        }

        etape.Resume = $"{analyse.Length} titres analysés";
        etape.Indicateurs.Add(new KpiItem { Icone = "bi-graph-up", Valeur = analyse.Length.ToString(), Label = "Titres analysés" });
        etape.Indicateurs.Add(new KpiItem { Icone = "bi-bullseye", Valeur = $"{SharedService.Recommendations.Average(r => r.Confidence):0}%", Label = "Confiance moyenne" });
        Log(etape, NiveauJournal.Success, "Titres à privilégier identifiés avec échéancier associé — recommandations prêtes pour le simulateur.");
    }

    private static string LibelleAction(ActionRecommandee action) => action switch
    {
        ActionRecommandee.Achat => "Achat",
        ActionRecommandee.Vente => "Vente",
        _ => "Conserver"
    };

    private async Task Etape7_SimulateurAsync(PipelineStep etape, CancellationToken ct)
    {
        etape.SousTitreProgression = "Simulation des placements";

        Log(etape, NiveauJournal.Info, $"Démarrage de la simulation avec un capital de {CalledUpCapital:N0} €.");

        if (SharedService.Recommendations.Count == 0)
        {
            Log(etape, NiveauJournal.Warn, "Aucune recommandation de l'étape 5 trouvée — simulation lancée sur un portefeuille générique.");
        }

        var url = "https://localhost:7077/api/simulator";
        Dictionary<string, string?> queryParams = new()
        {
            { "capitalInitial", CalledUpCapital.ToString() }
        };
        string urlWithQuery = QueryHelpers.AddQueryString(url, queryParams);
        var response = await _httpClient.GetAsync(urlWithQuery, ct);

        SharedService.LastSimulationResults = await response.Content.ReadFromJsonAsync<SimulatorResultModel>(ct);
        if (SharedService.LastSimulationResults is null)
        {
            Log(etape, NiveauJournal.Error, "Erreur : les résultats de la simulation sont null.");
            return;
        }

        var globalPerformance = SharedService.LastSimulationResults.PercentGlobalPerformance;

        etape.Resume = $"{(globalPerformance >= 0 ? "+" : "")}{globalPerformance:0.0}%";
        etape.Indicateurs.Add(new KpiItem { Icone = "bi-cash-coin", Valeur = $"{SharedService.LastSimulationResults.FinalCapital:N0} €", Label = "Capital final simulé" });
        etape.Indicateurs.Add(new KpiItem { Icone = "bi-graph-up-arrow", Valeur = $"{(globalPerformance >= 0 ? "+" : "")}{globalPerformance:0.0}%", Label = "Performance globale" });
        Log(etape, NiveauJournal.Success, $"Simulation terminée : {SharedService.LastSimulationResults.InitialCapital:N0} € → {SharedService.LastSimulationResults.FinalCapital:N0} € ({(globalPerformance >= 0 ? "+" : "")}{globalPerformance:0.0}%).");
    }
}
