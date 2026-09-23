using TGDev.StepS01.Shared.Services;
using System.Text;
using TGDev.StepS01.Shared.Models;

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
    private readonly Dictionary<int, CancellationTokenSource> _executionsEnCours = new();

    private ISharedService _sharedService;
    private HttpClient _httpClient;

    public List<PipelineStep> Etapes { get; }
    
    /// <summary>Étape actuellement affichée dans le dashboard (pilotée par la barre d'onglets).</summary>
    public int EtapeSelectionnee { get; set; } = 1;

    public event Action? OnChange;

    public PipelineService(IConfiguration configuration)
    {
        _sharedService = new SharedService();
        _httpClient = new HttpClient();

        var newsRetrieverUrl = configuration["NewsRetrieval:BaseUrl"] ?? "https://localhost:7056";
        var newsRetrieverSwaggerUrl = string.IsNullOrWhiteSpace(newsRetrieverUrl) ? null : $"{newsRetrieverUrl.TrimEnd('/')}/swagger/index.html";

        var databaseStorageUrl = configuration["DatabaseStorage:BaseUrl"] ?? "https://localhost:7170";
        var databaseStorageSwaggerUrl = string.IsNullOrWhiteSpace(databaseStorageUrl) ? null : $"{databaseStorageUrl.TrimEnd('/')}/swagger/index.html";

        Etapes = new List<PipelineStep>
        {
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
        };
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

            _sharedService.FeedFetcherJsonResult = await response.Content.ReadAsStringAsync(ct);
            total = _sharedService.FeedFetcherJsonResult.Split("\"title\"").Count() - 1; // Compte le nombre d'articles récupérés

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
        step.DetailProgression = $"{_sharedService.FeedFetcherJsonResult.Split("\"title\"").Count() - 1} articles à enregistrer";

        var url = "https://localhost:7170/api/databasestorage";

        var response = await _httpClient.PostAsync(url, new StringContent(_sharedService.FeedFetcherJsonResult, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            // Cela affichera le rapport d'erreur d'ASP.NET Core (souvent un objet ValidationProblemDetails)
            string errorDetails = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Détails de l'erreur 400 : {errorDetails}");
        }

        response.EnsureSuccessStatusCode();

        _sharedService.DatabaseStorageResult = await response.Content.ReadFromJsonAsync<IEnumerable<NewsItemModel>>(ct);

        int total = _sharedService.DatabaseStorageResult!.Count();

        step.Resume = $"{total} enregistrements";
        step.Indicateurs.Add(new KpiItem { Icone = "bi-database-check", Valeur = total.ToString(), Label = "Enregistrements insérés" });
        Log(step, NiveauJournal.Success, $"{total} articles insérés dans la base de connaissances.");
    }

    private async Task Etape3_VectoriserAsync(PipelineStep etape, CancellationToken ct)
    {
        await Task.Delay(700, ct); // TODO : appeler un modèle d'embeddings et stocker dans une base vectorielle (Qdrant, pgvector, etc.)
        Log(etape, NiveauJournal.Success, "Vectorisation terminée (dimension 1536, 42 vecteurs indexés).");
    }

    private async Task Etape4_EnrichirLLMAsync(PipelineStep etape, CancellationToken ct)
    {
        await Task.Delay(900, ct); // TODO : brancher un LLM local (Ollama, LM Studio...) en logique RAG sur la base vectorielle
        Log(etape, NiveauJournal.Success, "Contexte RAG chargé dans le LLM local.");
    }

    private async Task Etape5_UniversInvestissementAsync(PipelineStep etape, CancellationToken ct)
    {
        string[] titres = { "CAC 40", "S&P 500", "MSCI World ETF", "Nasdaq 100 ETF" };
        foreach (var titre in titres)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(300, ct); // TODO : appeler une API de données de marché (ex. données boursières)
            Log(etape, NiveauJournal.Success, $"Ajouté à l'univers : {titre}");
        }
    }

    private async Task Etape6_AnalyseLLMAsync(PipelineStep etape, CancellationToken ct)
    {
        await Task.Delay(1000, ct); // TODO : envoyer l'univers + contexte RAG au LLM enrichi pour analyse
        Log(etape, NiveauJournal.Success, "Titres à privilégier identifiés avec échéancier associé.");
    }

    private async Task Etape7_SimulateurAsync(PipelineStep etape, CancellationToken ct)
    {
        await Task.Delay(800, ct); // TODO : brancher un simulateur de placements (paper trading) et comparer aux prédictions
        Log(etape, NiveauJournal.Success, "Simulation exécutée : prédictions confrontées aux résultats obtenus.");
    }
}
