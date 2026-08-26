using IndustrialSimApp.Cli;
using Spectre.Console.Cli;

var app = new CommandApp<SimulationCommand>();
app.Configure(config => config.SetApplicationName("IndustrialSimApp"));

return await app.RunAsync(args);
