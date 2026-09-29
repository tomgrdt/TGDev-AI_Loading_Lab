using TGDev.Step03.Vectorization.Models;

namespace TGDev.Step03.Vectorization.Services;

public class EmbeddingClient
{
    public async Task<float[]> EmbedAsync(string text, string model = "nomic-embed-text")
    {
        try
        {
            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            var request = new EmbedRequest(text, model);
            var response = await httpClient.PostAsJsonAsync("/v1/embeddings", request);
            response.EnsureSuccessStatusCode();

            var embedResponse = await response.Content.ReadFromJsonAsync<EmbedResponse>();
            return embedResponse?.Data?.FirstOrDefault()?.Embedding ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Embedding error: {ex.Message}");
            throw;
        }
    }
}
