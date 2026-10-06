using System.Net.Http.Headers;
using System.Text.Json;

namespace TGDev.StepS01.Shared.Services
{
    public class N8nWorkflowClient: IN8nWorkflowClient
    {
        private readonly HttpClient _httpClient;
        private readonly string? _baseUrl;
        private readonly string? _apiKey;
        private readonly string? _webhookPath;

        public bool EstConfigure => !string.IsNullOrWhiteSpace(_webhookPath);

        public N8nWorkflowClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["N8n:BaseUrl"];
            _apiKey = configuration["N8n:ApiKey"];
            _webhookPath = configuration["N8n:WebhookUrl"];
        }

        public async Task<N8nDeclenchementResult> DeclencherWorkflowAsync(CancellationToken ct)
        {
            if (!EstConfigure)
            {
                return new N8nDeclenchementResult
                {
                    Succes = false,
                    Message = "N8n:WebhookUrl n'est pas configuré dans appsettings.json."
                };
            }

            try
            {
                // TODO : adapte le corps envoyé au webhook selon ce qu'attend ton
                // workflow n8n (ici, on transmet juste un identifiant de run côté Host).
                var payload = new { source = "TGDev-AI-Loading-Lab", declencheLe = DateTime.UtcNow };

                using var response = await _httpClient.PostAsJsonAsync(_webhookPath, payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return new N8nDeclenchementResult
                    {
                        Succes = false,
                        Message = $"n8n a répondu {(int)response.StatusCode} ({response.ReasonPhrase})."
                    };
                }

                // TODO : adapte le nom du champ si ton nœud "Respond to Webhook"
                // renvoie l'identifiant d'exécution sous une autre clé.
                string? executionId = null;
                try
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (doc.RootElement.TryGetProperty("executionId", out var idProp))
                    {
                        executionId = idProp.GetString();
                    }
                }
                catch (JsonException)
                {
                    // Réponse non-JSON ou sans executionId : le déclenchement a réussi
                    // mais le suivi de statut ne sera pas disponible.
                }

                return new N8nDeclenchementResult
                {
                    Succes = true,
                    ExecutionId = executionId,
                    Message = executionId is not null
                        ? "Workflow déclenché."
                        : "Workflow déclenché, mais aucun executionId renvoyé — suivi de statut indisponible."
                };
            }
            catch (HttpRequestException ex)
            {
                return new N8nDeclenchementResult { Succes = false, Message = $"Échec d'appel à n8n : {ex.Message}" };
            }
        }

        public async Task<N8nStatutExecution> ObtenirStatutAsync(string executionId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_apiKey))
            {
                return N8nStatutExecution.Inconnu;
            }

            try
            {
                // TODO : vérifie ce chemin contre la version de ton instance n8n
                // (API REST n8n : GET /api/v1/executions/{id}).
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_baseUrl.TrimEnd('/')}/api/v1/executions/{executionId}");
                request.Headers.Add("X-N8N-API-KEY", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode) return N8nStatutExecution.Inconnu;

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var root = doc.RootElement;

                var finished = root.TryGetProperty("finished", out var finishedProp) && finishedProp.GetBoolean();
                if (!finished) return N8nStatutExecution.EnCours;

                // TODO : n8n expose le détail du succès/échec dans root.data selon
                // la version — à affiner si besoin d'un statut plus précis qu'ici.
                return N8nStatutExecution.Succes;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                return N8nStatutExecution.Inconnu;
            }
        }
    }
}
