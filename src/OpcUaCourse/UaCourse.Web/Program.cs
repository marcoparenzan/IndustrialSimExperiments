using UaCourse.Web.Components;
using UaCourse.Web.Modules;
using UaCourse.Web.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
Opc.Ua.EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(UaCourse.Models.RecipeData));
// Anche dotnet run --no-launch-profile (Production) deve risolvere gli asset
// Blazor e gli script collocati nei componenti dal manifest di build.
builder.WebHost.UseStaticWebAssets();
// Store del laboratorio isolato dal profilo Windows e dagli altri progetti.
builder.Services.AddDataProtection().PersistKeysToFileSystem(
    new DirectoryInfo(Path.Combine(UaCourse.Configuration.WorkspaceRoot, ".artifacts", "aspnet-keys")));
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSingleton<ScreenCatalog>();
builder.Services.AddSingleton<UaConnection>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<UaConnection>());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
