using Opc.Ua;
using Opc.Ua.Client;
using UaCourse;
EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(UaCourse.Models.RecipeData));

var command = args.FirstOrDefault() ?? "help";
if (command == "help")
{
    Console.WriteLine("UaCourse: server | client <browse|read|write|methods|subscribe|events|types|custom-types|paths|errors|ranges|model|method-errors|all> | client forgesim [endpoint] | verify");
    return;
}
try
{
    if (command == "server")
    {
        var app = await Configuration.CreateAsync(true);
        using var server = new PlantServer();
        await app.Start(server);
        Console.WriteLine($"Server: {Configuration.Endpoint}. Press Ctrl+C to stop.");
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopped.TrySetResult(); };
        await stopped.Task;
        server.Stop();
    }
    else if (command == "client" && args.ElementAtOrDefault(1) == "forgesim")
    {
        // Needs ForgeSimApp running (default opc.tcp://localhost:4840/ForgeSim); not part of `all` or `verify`.
        await Lessons.RunForgeSimAsync(args.ElementAtOrDefault(2) ?? Lessons.ForgeSimEndpoint);
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
            Console.WriteLine($"Session: {session.SessionId}, security: {endpoint.SecurityPolicyUri}");
            await Lessons.RunAsync(session, command == "verify" ? "all" : args.ElementAtOrDefault(1) ?? "browse");
            session.Close();
        }
        finally { server?.Stop(); server?.Dispose(); }
    }
    else throw new ArgumentException($"Unknown command: {command}");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
