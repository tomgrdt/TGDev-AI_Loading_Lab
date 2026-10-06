using Microsoft.Azure.Cosmos.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TGDev.StepS01.Shared.Models;

namespace TGDev.StepS01.Shared.Database
{
    public class NewsItemDbContext:DbContext
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
            modelBuilder.Entity<NewsItemModel>(entity =>
            {
                entity.ToContainer("NewsItems");

                entity.HasKey(n => n.Id);

                entity.HasPartitionKey(n => n.PartitionKeyId);
            });

            var converter = new ValueConverter<ReadOnlyMemory<float>, float[]>(
                v => v.ToArray(),
                v => new ReadOnlyMemory<float>(v)
            );

            var comparer = new ValueComparer<ReadOnlyMemory<float>>(
                (c1, c2) => CompareMemory(c1, c2),
                c => GetHashCodeForMemory(c),
                c => new ReadOnlyMemory<float>(c.ToArray())
            );

            modelBuilder.Entity<NewsItemModel>()
                .Property(n => n.DescriptionEmbedding)
                .HasConversion(converter, comparer);
        }

        // 1. Méthode pour comparer le contenu sans bloquer l'arbre d'expression EF Core
        private static bool CompareMemory(ReadOnlyMemory<float> m1, ReadOnlyMemory<float> m2)
        {
            // Ici, nous ne sommes pas dans une expression lambda EF, l'usage de .Span est autorisé !
            return MemoryExtensions.SequenceEqual<float>(m1.Span, m2.Span);
        }

        // 2. Méthode pour calculer le HashCode (déjà fournie précédemment, indispensable)
        private static int GetHashCodeForMemory(ReadOnlyMemory<float> memory)
        {
            var hashCode = new HashCode();
            var span = memory.Span;
            for (int i = 0; i < span.Length; i++)
            {
                hashCode.Add(span[i]);
            }
            return hashCode.ToHashCode();
        }
    }
}
