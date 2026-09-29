using TGDev.Step03.Vectorization.Models;

namespace TGDev.Step03.Vectorization.Services;

public class RagService(LocalQdrantClient qdrantClient, EmbeddingClient embeddingClient)
{
    private readonly LocalQdrantClient _qdrantClient = qdrantClient;
    private readonly EmbeddingClient _embeddingClient = embeddingClient;

    public async Task<string> AskAsync(string question)
    {
        try
        {
            HttpClient httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            Console.WriteLine($"🔍 Question: {question}");

            // Step 1: Embed the question
            var questionVector = await _embeddingClient.EmbedAsync(question);
            Console.WriteLine("✓ Question embedded");

            // Step 2: Retrieve relevant chunks
            var hits = await _qdrantClient.SearchAsync(questionVector, 4);
            Console.WriteLine($"✓ Retrieved {hits.Count} chunks");

            if (hits.Count == 0)
            {
                return "⚠️ No relevant documents found in the knowledge base.";
            }

            // Display retrieved chunks
            var context = new System.Text.StringBuilder();
            int i = 1;
            foreach (var hit in hits)
            {
                var content = hit.Payload["content"]?.ToString() ?? string.Empty;
                Console.WriteLine($"  [{i}] (Score: {hit.Score:F4}) {content[..Math.Min(80, content.Length)]}...");
                context.AppendLine(content);
                i++;
            }

            // Step 3: Generate response with context
            var messages = new List<ChatMessage>
            {
                new("system", "You are a helpful assistant. Answer questions based on the provided context. Always cite the context."),
                new("user", $"Context:\n{context}\n\nQuestion: {question}")
            };

            var chatRequest = new ChatRequest("llama3.1", messages, 0.2);
            var response = await httpClient.PostAsJsonAsync("http://localhost:11434/v1/chat/completions", chatRequest);
            response.EnsureSuccessStatusCode();

            var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
            var answer = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response generated.";

            Console.WriteLine($"\n💡 Answer: {answer}\n");
            return answer;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in RAG query: {ex.Message}");
            throw;
        }
    }
}
