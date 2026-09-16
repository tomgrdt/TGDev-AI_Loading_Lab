using TGDev.Step02.DatabaseStorage.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace TGDev.Step02.DatabaseStorage.Context;

public class NewsItemDbContext : DbContext
{
    public DbSet<NewsItemModel> NewsItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseCosmos(
            accountEndpoint: "https://localhost:8081",
            accountKey: "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            databaseName: "TGDev.Experimentation01",
            cosmosOptionsAction: options =>
            {
                options.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }));

                options.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
            }
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NewsItemModel>()
            .ToContainer("NewsItems")
            .HasPartitionKey(n => n.Id);
    }
}
