using TGDev.StepS01.Shared.Services;
using TGDev.Experimentation01.Services;

var builder = WebApplication.CreateBuilder(args);

// Services Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Service métier : exécute chacune des 7 étapes indépendamment.
// Enregistré en Scoped car chaque session Blazor doit avoir son propre état de pipeline.
builder.Services.AddScoped<PipelineService>();
builder.Services.AddScoped<ISharedService, SharedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
