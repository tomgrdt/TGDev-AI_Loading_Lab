using TGDev.Step03.Vectorization.Utils;

namespace TGDev.Step03.Vectorization.Services;

public class Indexer(LocalQdrantClient qdrantClient, EmbeddingClient embeddingClient)
{
    public async Task IndexDocumentAsync(string docId, string text)
    {
        var chunks = Ingest.Chunk(text).ToList();
        Console.WriteLine($"OK - Document '{docId}' split into {chunks.Count} chunks");

        int chunkIndex = 0;
        foreach (var chunk in chunks)
        {
            try
            {
                var embedding = await embeddingClient.EmbedAsync(chunk);

                var payload = new Dictionary<string, object>
                {
                    ["content"] = chunk,
                    ["docId"] = docId,
                    ["chunkIndex"] = chunkIndex
                };

                var id = Guid.NewGuid();
                await qdrantClient.UpsertAsync(id, embedding, payload);
                chunkIndex++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error - Indexing chunk {chunkIndex}: {ex.Message}");
            }
        }

        Console.WriteLine($"OK - Indexed {chunkIndex} chunks for '{docId}'\n");
    }


    public async Task IndexDatabaseAsync(string title, string summary, string link, string category, DateTimeOffset publishedAt, string sourceName, string sourceUrl)
    {

        var chunks = Ingest.Chunk(summary).ToList();
        Console.WriteLine($"OK - L'article '{title}' split into {chunks.Count} chunks");

        int chunkIndex = 0;
        foreach (var chunk in chunks)
        {
            try
            {
                var embedding = await embeddingClient.EmbedAsync(chunk);

                var payload = new Dictionary<string, object>
                {
                    ["category"] = category,
                    ["title"] = title,
                    ["chunkIndex"] = chunkIndex,
                    ["content"] = chunk,
                    ["publishedAt"] = publishedAt.ToString("f"),
                    ["link"] = link,
                    ["sourceName"] = sourceName,
                    ["sourceUrl"] = sourceUrl
                };

                var id = Guid.NewGuid();
                await qdrantClient.UpsertAsync(id, embedding, payload);
                chunkIndex++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error - Indexing chunk {chunkIndex}: {ex.Message}");
            }
        }

        Console.WriteLine($"OK - Indexed {chunkIndex} chunks for '{title}'\n");
    }
}
