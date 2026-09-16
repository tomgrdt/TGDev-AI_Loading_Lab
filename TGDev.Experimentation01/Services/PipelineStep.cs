namespace TGDev.Experimentation01.Services;

public enum StepStatus
{
    EnAttente,
    EnCours,
    Termine,
    Erreur
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
    public List<string> Journal { get; } = new();

    // Fonction métier exécutée lorsqu'on déclenche l'étape.
    // Chaque étape reçoit un CancellationToken pour pouvoir être annulée indépendamment des autres.
    public Func<PipelineStep, CancellationToken, Task> Executer { get; init; }
        = (_, _) => Task.CompletedTask;
}
