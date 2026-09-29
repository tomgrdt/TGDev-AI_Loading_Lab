using TGDev.Step04.LocalLLM.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Local LLM API",
        Version = "v1",
        Description = "Provides local language model capabilities.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TGDev - AI Loading Lab",
            Email = "your.email@example.com"
        }
    });
    //TODO : Ajouter l'authentification
});

builder.Services.AddHttpClient(nameof(LocalLLMService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "TGDev.LocalLLM/1.0");
});

builder.Services.AddScoped<ILocalLLMService, LocalLLMService>();

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
