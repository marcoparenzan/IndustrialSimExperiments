using ForgeDataGatewayApp.Components;
using ForgeDataGatewayApp.Services;
using ForgeDataGatewayCore.Config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string configPath = builder.Configuration["Gateway:ConfigPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "gateway-config.json");
builder.Services.AddSingleton<IGatewayConfigStore>(new JsonFileGatewayConfigStore(configPath));
builder.Services.AddSingleton<GatewayService>();

var app = builder.Build();

await app.Services.GetRequiredService<GatewayService>().InitializeAsync();

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
