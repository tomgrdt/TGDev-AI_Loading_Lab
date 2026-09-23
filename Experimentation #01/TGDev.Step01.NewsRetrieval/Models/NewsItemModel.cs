namespace TGDev.Step01.NewsRetrieval.Models;

public sealed class NewsItemModel
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Link { get; init; }
    public required string Category { get; init; }
    public string? Summary { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public required string SourceName { get; init; }
    public required string SourceUrl { get; init; }
}
