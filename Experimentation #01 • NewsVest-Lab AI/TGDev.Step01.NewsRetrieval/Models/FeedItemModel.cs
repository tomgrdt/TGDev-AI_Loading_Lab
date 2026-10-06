using TGDev.StepS01.Shared.Models;

namespace TGDev.Step01.NewsRetrieval.Models;

/// <summary>
/// Représente un article normalisé, quel que soit le flux d'origine (RSS ou Atom).
/// </summary>
public sealed class FeedItemModel
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Link { get; init; }
    public required string Category { get; init; }
    public string? Summary { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public required string SourceName { get; init; }
    public required string SourceUrl { get; init; }

    public NewsItemModel Convert()
    {
        return new NewsItemModel
        {
            PartitionKeyId = Id,
            Title = this.Title,
            Link = this.Link,
            Category = this.Category,
            Summary = this.Summary,
            PublishedAt = this.PublishedAt,
            SourceName = this.SourceName,
            SourceUrl = this.SourceUrl
        };
    }
}
