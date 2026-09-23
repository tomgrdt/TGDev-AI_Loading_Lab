using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.ServiceModel.Syndication;
using System.Xml;

using TGDev.Step01.NewsRetrieval.Models;


namespace TGDev.Step01.NewsRetrieval.Services;

public class FeedFetcherService : IFeedFetcherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly FeedFetcherOptions _options;
    private readonly ILogger<FeedFetcherService> _logger;

    public FeedFetcherService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<FeedFetcherOptions> options,
        ILogger<FeedFetcherService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }
    public async Task<IEnumerable<FeedItemModel>> GetFeedFetcherAsync(CancellationToken cancellationToken = default)
    {
        // Implementation of the method to fetch feeds

        //TODO: Implementer la logique de gestion du cache ici si mise en place d'une authentification.
        //if (_cache.TryGetValue(CacheKey, out IReadOnlyList<FeedItemModel>? cached) && cached is not null)
        //{
        //    return cached;
        //}

        IEnumerable<FeedItemModel> categoriesMerged = [];

        foreach (var category in _options.Categories)
        {
            if (category.Feeds is null || !category.Feeds.Any())
            {
                _logger.LogWarning("La catégorie {CategoryName} n'a pas de flux configurés.", category.Name);
                continue;
            }
            var fetchTasks = category.Feeds.Select(feedUrl => FetchFeedSafeAsync(feedUrl, category.Name, cancellationToken));

            var results = await Task.WhenAll(fetchTasks);

            var merged = results
            .SelectMany(items => items)
            .Where(item => !string.IsNullOrEmpty(item.Summary))
            .OrderByDescending(item => item.PublishedAt)
            .ToList();

            categoriesMerged = categoriesMerged.Concat(merged);

        }

        //TODO: Implementer la logique de gestion du cache ici si mise en place d'une authentification.
        //_cache.Set(CacheKey, categoriesMerged, TimeSpan.FromMinutes(_options.CacheDurationMinutes));

        return categoriesMerged;
    }

    /// <summary>
    /// Récupère un flux unique. Toute erreur (réseau, parsing, timeout) est journalisée
    /// et retourne une liste vide plutôt que de faire échouer l'agrégation complète.
    /// </summary>
    private async Task<IReadOnlyList<FeedItemModel>> FetchFeedSafeAsync(string feedUrl, string category, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient(nameof(FeedFetcherService));
            await using var stream = await httpClient.GetStreamAsync(feedUrl, cancellationToken);
            using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Ignore });

            var syndicationFeed = SyndicationFeed.Load(xmlReader)
                ?? throw new InvalidOperationException("Flux vide ou illisible.");

            var sourceName = string.IsNullOrWhiteSpace(syndicationFeed.Title?.Text)
                ? new Uri(feedUrl).Host
                : syndicationFeed.Title.Text;

            return syndicationFeed.Items
                .Take(_options.MaxItemsPerFeed)
                .Select(item => MapToModel(item, sourceName, feedUrl, category))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Échec de la récupération du flux {FeedUrl}", feedUrl);
            return Array.Empty<FeedItemModel>();
        }
    }

    private static FeedItemModel MapToModel(SyndicationItem item, string sourceName, string sourceUrl, string category)
    {
        var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? string.Empty;
        var publishedAt = item.PublishDate != default ? item.PublishDate : item.LastUpdatedTime;

        return new FeedItemModel
        {
            Title = item.Title?.Text ?? "(sans titre)",
            Link = link,
            Category = category,
            Summary = item.Summary?.Text,
            PublishedAt = publishedAt,
            SourceName = sourceName,
            SourceUrl = sourceUrl
        };
    }
}
