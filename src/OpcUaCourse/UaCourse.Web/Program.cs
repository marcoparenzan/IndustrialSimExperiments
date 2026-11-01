using UaCourse.Web.Components;
using UaCourse.Web.Modules;
using UaCourse.Web.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
Opc.Ua.EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(UaCourse.Models.RecipeData));
// dotnet run --no-launch-profile (Production) must also resolve the Blazor assets
// and the scripts placed in the components by the build manifest.
builder.WebHost.UseStaticWebAssets();
// Lab store isolated from the Windows profile and from other projects.
builder.Services.AddDataProtection().PersistKeysToFileSystem(
    new DirectoryInfo(Path.Combine(UaCourse.Configuration.ArtifactsRoot, "aspnet-keys")));
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSingleton<ScreenCatalog>();
builder.Services.AddSingleton<UaConnection>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<UaConnection>());
builder.Services.AddSingleton<ForgeSimConnection>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ForgeSimConnection>());
builder.Services.AddSingleton<SignalSources>();

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
