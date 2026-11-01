using Opc.Ua;
using Opc.Ua.Client;
using UaCourse;
EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(UaCourse.Models.RecipeData));

var command = args.FirstOrDefault() ?? "help";
if (command == "help")
{
    Console.WriteLine("UaCourse: server | client <browse|read|write|methods|subscribe|events|types|custom-types|paths|errors|ranges|model|method-errors|all> | verify");
    return;
}
try
{
    if (command == "server")
    {
        var app = await Configuration.CreateAsync(true);
        using var server = new PlantServer();
        await app.Start(server);
        Console.WriteLine($"Server: {Configuration.Endpoint}. Ctrl+C per terminare.");
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopped.TrySetResult(); };
        await stopped.Task;
        server.Stop();
    }
    else if (command is "client" or "verify")
    {
        PlantServer? server = null;
        try
        {
            if (command == "verify")
            {
                server = new PlantServer();
                await (await Configuration.CreateAsync(true)).Start(server);
            }
            var app = await Configuration.CreateAsync(false);
            var endpoint = CoreClientUtils.SelectEndpoint(app.ApplicationConfiguration, Configuration.Endpoint, true);
            var configured = new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(app.ApplicationConfiguration));
            using var session = await Session.Create(app.ApplicationConfiguration, configured, false,
                "UaCourse", 60_000, new UserIdentity(new AnonymousIdentityToken()), null);
            Console.WriteLine($"Sessione: {session.SessionId}, sicurezza: {endpoint.SecurityPolicyUri}");
            await Lessons.RunAsync(session, command == "verify" ? "all" : args.ElementAtOrDefault(1) ?? "browse");
            session.Close();
        }
        finally { server?.Stop(); server?.Dispose(); }
    }
    else throw new ArgumentException($"Comando sconosciuto: {command}");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
