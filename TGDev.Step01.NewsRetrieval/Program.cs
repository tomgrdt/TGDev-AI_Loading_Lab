using System.Reflection.Metadata.Ecma335;
using TGDev.Step01.NewsRetrieval.Models;
using TGDev.Step01.NewsRetrieval.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "News Retrieval API",
        Version = "v1",
        Description = "Aggregates multiple RSS/Atom feeds and exposes the merged articles in JSON.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TGDev - AI Loading Lab",
            Email = "your.email@example.com"
        }
    });
    //TODO : Ajouter l'authentification
});

builder.Services.AddHttpClient(nameof(FeedFetcherService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "TGDev.NewsRetrieval/1.0");
});

builder.Services.Configure<FeedFetcherOptions>(builder.Configuration.GetSection(FeedFetcherOptions.SectionName));

builder.Services.AddScoped<IFeedFetcherService, FeedFetcherService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
