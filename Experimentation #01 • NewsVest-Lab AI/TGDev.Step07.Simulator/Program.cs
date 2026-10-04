using TGDev.Step07.Simulator.Services;
using TGDev.StepS01.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMemoryCache();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Vectorization API",
        Version = "v1",
        Description = "Vectorizes news articles for semantic search.",
        Contact = new Microsoft.OpenApi.OpenApiContact
        {
            Name = "TGDev - AI Loading Lab",
            Email = "your.email@example.com"
        }
    });
    //TODO : Ajouter l'authentification
});

builder.Services.AddHttpClient(nameof(SimulatorService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "TGDev.Simulator/1.0");
});

builder.Services.AddScoped<ISimulatorService, SimulatorService>();
builder.Services.AddScoped<ISharedService, SharedService>();

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
