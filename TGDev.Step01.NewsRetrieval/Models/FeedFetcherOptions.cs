namespace TGDev.Step01.NewsRetrieval.Models;

/// <summary>
/// Options mappées depuis la section "FeedFetcher" d'appsettings.json.
/// </summary>
public sealed class FeedFetcherOptions
{
    public const string SectionName = "FeedFetcher";

    public List<Category> Categories { get; init; } = new();
    public int MaxItemsPerFeed { get; init; } = 20;
    public int CacheDurationMinutes { get; init; } = 10;
}

public sealed class Category
{
    public required string Name { get; init; }
    public required List<string> Feeds { get; init; }
}
