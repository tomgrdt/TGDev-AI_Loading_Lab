namespace TGDev.Experimentation01.Services;

public enum StepStatus
{
    EnAttente,
    EnCours,
    Termine,
    Erreur
}

public enum NiveauJournal
{
    Info,
    Success,
    Warn,
    Error
}

public class JournalEntry
{
    public DateTime Horodatage { get; init; } = DateTime.Now;
    public NiveauJournal Niveau { get; init; } = NiveauJournal.Info;
    public string Message { get; init; } = "";
}

/// <summary>
/// Un indicateur affiché dans le panneau "Indicateurs de l'étape"
/// (ex : icône bi-file-earmark-text, "542", "Enregistrements trouvés").
/// </summary>
public class KpiItem
{
    public string Icone { get; init; } = "bi-info-circle";
    public string Valeur { get; init; } = "";
    public string Label { get; init; } = "";
}

public class PipelineStep
{
    public int Numero { get; init; }
    public string Titre { get; init; } = "";
    public string Description { get; init; } = "";
    public string Phase { get; init; } = "";
    public string Icone { get; init; } = "bi-circle";

    public StepStatus Statut { get; set; } = StepStatus.EnAttente;
    public DateTime? DerniereExecution { get; set; }
    public TimeSpan? Duree { get; set; }

    /// <summary>Sous-libellé affiché sous le titre dans le tracker (ex. "542 articles").</summary>
    public string Resume { get; set; } = "En attente";

    /// <summary>Texte affiché dans la carte de progression pendant l'exécution (ex. "Génération des embeddings").</summary>
    public string? SousTitreProgression { get; set; }

    public int Progression { get; set; }        // 0-100
    public int Traites { get; set; }
    public int Restants { get; set; }
    public string? Vitesse { get; set; }
    public string? DetailProgression { get; set; } // ex. "542 documents à vectoriser"

    public List<JournalEntry> Journal { get; } = new();
    public List<KpiItem> Indicateurs { get; } = new();
    public int NombreErreurs { get; set; }

    public string? SwaggerUrl { get; init; }

    // Fonction métier exécutée lorsqu'on déclenche l'étape.
    public Func<PipelineStep, CancellationToken, Task> Executer { get; init; }
        = (_, _) => Task.CompletedTask;
}
