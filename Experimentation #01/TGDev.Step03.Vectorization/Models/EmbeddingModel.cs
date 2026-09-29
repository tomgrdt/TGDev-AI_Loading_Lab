namespace TGDev.Step03.Vectorization.Models;

public record EmbedRequest(string Input, string Model);
public record EmbedResponse(List<EmbedData> Data);
public record EmbedData(float[] Embedding);
