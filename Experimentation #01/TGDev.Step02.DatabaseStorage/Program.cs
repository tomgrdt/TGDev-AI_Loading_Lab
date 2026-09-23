using TGDev.Step02.DatabaseStorage.Services;

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
        Title = "Database Storage API",
        Version = "v1",
        Description = "Stores and retrieves news articles in a Cosmos database.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TGDev - AI Loading Lab",
            Email = "your.email@example.com"
        }
    });
    //TODO : Ajouter l'authentification
});

builder.Services.AddHttpClient(nameof(DatabaseStorageService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "TGDev.DatabaseStorage/1.0");
});

builder.Services.AddScoped<IDatabaseStorageService, DatabaseStorageService>();

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
