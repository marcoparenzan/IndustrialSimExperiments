using ForgeDataGatewayCli.Cli;
using Spectre.Console.Cli;

var app = new CommandApp<GatewayCommand>();
app.Configure(config => config.SetApplicationName("ForgeDataGatewayCli"));

return await app.RunAsync(args);
