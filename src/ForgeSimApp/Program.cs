using ForgeSimApp.Cli;
using Spectre.Console.Cli;

var app = new CommandApp<SimulationCommand>();
app.Configure(config => config.SetApplicationName("ForgeSimApp"));

return await app.RunAsync(args);
