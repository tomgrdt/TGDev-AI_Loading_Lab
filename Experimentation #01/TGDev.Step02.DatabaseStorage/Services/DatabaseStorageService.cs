using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TGDev.Step02.DatabaseStorage.Models;
using TGDev.Step02.DatabaseStorage.Context;
using Microsoft.EntityFrameworkCore;

namespace TGDev.Step02.DatabaseStorage.Services;

public class DatabaseStorageService : IDatabaseStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DatabaseStorageService> _logger;

    public DatabaseStorageService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<DatabaseStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }
    public async Task<IEnumerable<NewsItemModel>> PostDatabaseStorageAsync(List<NewsItemModel> listNewsItems, CancellationToken cancellationToken = default)
    {
        // Implementation of the method to fetch database items

        List<NewsItemModel> newsItemsAdded = new List<NewsItemModel>();

        //TODO: Implementer la logique de gestion du cache ici si mise en place d'une authentification.
        //if (_cache.TryGetValue(CacheKey, out IReadOnlyList<NewsItemModel>? cached) && cached is not null)
        //{
        //    return cached;
        //}

        // AJOUT: Logique d'ajout des éléments dans la base de données
        var context = new NewsItemDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var newsItem in listNewsItems)
        {
            if(context.NewsItems.Where(n => n.Link == newsItem.Link).Count() > 0)
            {
                _logger.LogInformation("News item with title {Title} already exists in the database.", newsItem.Title);
                continue; // Skip adding this item if it already exists
            }
            newsItem.Id = new Guid();
            context.NewsItems.Add(newsItem);
            newsItemsAdded.Add(newsItem);
        }

        await context.SaveChangesAsync(cancellationToken);
        
        return newsItemsAdded;

        //TODO: Implementer la logique de gestion du cache ici si mise en place d'une authentification.
        //_cache.Set(CacheKey, categoriesMerged, TimeSpan.FromMinutes(_options.CacheDurationMinutes));

    }
}
