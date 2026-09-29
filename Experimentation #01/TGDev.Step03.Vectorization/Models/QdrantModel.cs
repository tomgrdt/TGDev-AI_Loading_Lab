namespace TGDev.Step03.Vectorization.Models;


public record QdrantPoint(string Id, float[] Vector, Dictionary<string, object> Payload);
public record UpsertRequest(List<QdrantPoint> Points);
public record SearchRequest(float[] Vector, int Limit);
public record SearchResponse(List<SearchHit> Result);
public record SearchHit(Guid Id, float Score, Dictionary<string, object> Payload);
