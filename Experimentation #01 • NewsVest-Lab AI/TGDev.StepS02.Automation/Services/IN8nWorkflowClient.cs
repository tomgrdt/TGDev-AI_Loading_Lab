namespace TGDev.StepS02.Automation.Services;

public class N8nDeclenchementResult
{
    public bool Succes { get; init; }
    /// <summary>Identifiant d'exécution renvoyé par n8n (si disponible) pour le suivi.</summary>
    public string? ExecutionId { get; init; }
    public string? Message { get; init; }
}

public enum N8nStatutExecution
{
    Inconnu,
    EnCours,
    Succes,
    Echec
}

/// <summary>
/// Contrat d'intégration avec un workflow n8n qui orchestrerait l'exécution
/// des 7 étapes côté n8n (plutôt que localement en C#). Pensé pour un workflow
/// n8n qui appelle, dans l'ordre, les mêmes endpoints que ceux exposés par les
/// modules (FeedFetcher.Api, etc.), puis notifie son avancement.
/// </summary>
public interface IN8nWorkflowClient
{
    bool EstConfigure { get; }

    Task<N8nDeclenchementResult> DeclencherWorkflowAsync(CancellationToken ct);

    Task<N8nStatutExecution> ObtenirStatutAsync(string executionId, CancellationToken ct);
}
