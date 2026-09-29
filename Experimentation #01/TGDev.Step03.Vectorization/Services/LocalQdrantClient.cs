using Qdrant.Client.Grpc;
using TGDev.Step03.Vectorization.Models;

namespace TGDev.Step03.Vectorization.Services;

public class LocalQdrantClient(string host= "localhost", int port= 6334, string collection = "TGDev.Experimentation01.News")
{
    private readonly global::Qdrant.Client.QdrantClient _client = new(host, port);

    public async Task UpsertAsync(Guid id, float[] vector, Dictionary<string, object> payload)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = id.ToString() },
            Vectors = vector,
            Payload = { }
        };

        foreach (var kvp in payload)
        {
            point.Payload[kvp.Key] = kvp.Value switch
            {
                string s => s,
                int i => i,
                long l => l,
                double d => d,
                bool b => b,
                _ => kvp.Value.ToString() ?? string.Empty
            };
        }

        await _client.UpsertAsync(collection, [point]);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(float[] vector, int k = 4)
    {
        var results = await _client.SearchAsync(
            collectionName: collection,
            vector: vector,
            limit: (ulong)k,
            payloadSelector: true
        );

        return [.. results.Select(r => new SearchHit(
            Id: Guid.Parse(r.Id.Uuid),
            Score: r.Score,
            Payload: r.Payload.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertValue(kvp.Value)
            )
        ))];
    }

    private static object ConvertValue(Value value)
    {
        return value.KindCase switch
        {
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.IntegerValue => value.IntegerValue,
            Value.KindOneofCase.DoubleValue => value.DoubleValue,
            Value.KindOneofCase.BoolValue => value.BoolValue,
            _ => value.ToString()
        };
    }
}
