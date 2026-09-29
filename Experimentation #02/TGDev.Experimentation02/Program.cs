using TGDev.Experimentation02.Creances;
using TGDev.Experimentation02.SupplierEmails;
using TGDev.Experimentation02.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IComptesService, ComptesDemo>();
builder.Services.AddScoped<IRedacteurRelance, RedacteurModele>();
builder.Services.AddScoped<ISupplierEmailService, DemoSupplierEmails>();
builder.Services.AddScoped<IErpService, DemoErpService>();
builder.Services.AddScoped<IEmailAnalyzer, DemoEmailAnalyzer>();
builder.Services.AddScoped<IReplyWriter, TemplateReplyWriter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
