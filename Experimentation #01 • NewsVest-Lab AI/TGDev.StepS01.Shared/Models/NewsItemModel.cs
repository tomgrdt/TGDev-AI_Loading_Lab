using Microsoft.Extensions.VectorData;
using System.Text.Json.Serialization;

namespace TGDev.StepS01.Shared.Models;

public class NewsItemModel
{
    public string? Id { get; set; }
    [VectorStoreKey]
    public Guid PartitionKeyId { get; set; }
    [VectorStoreData]
    public string? Title { get; set; }
    [VectorStoreData]
    public string? Link { get; set; }
    [VectorStoreData]
    public string? Category { get; set; }
    [VectorStoreData]
    public string? Summary { get; set; }
    [VectorStoreData]
    public DateTimeOffset PublishedAt { get; set; }
    [VectorStoreData]
    public string? SourceName { get; set; }
    [VectorStoreData]
    public string? SourceUrl { get; set; }
    public bool IsVectorized { get; set; } = false;

    [JsonIgnore, VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}