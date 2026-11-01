using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using UaCourse;

try
{
    var app = await Configuration.CreateAsync(false);
    var description = CoreClientUtils.SelectEndpoint(app.ApplicationConfiguration, Configuration.Endpoint, true);
    using var session = await Session.Create(app.ApplicationConfiguration,
        new ConfiguredEndpoint(null, description, EndpointConfiguration.Create(app.ApplicationConfiguration)),
        false, "Dynamic recipe client", 60_000, new UserIdentity(new AnonymousIdentityToken()), null);
    var index = session.NamespaceUris.GetIndex("urn:uacourse:plant");
    if (index < 0) throw new InvalidOperationException("Application namespace not found");
    var types = new ComplexTypeSystem(session);
    if (!await types.Load()) throw new InvalidOperationException("Dynamic type loading failed");
    var value = session.ReadValue(new NodeId("Reactor1.BatchRecipe", (ushort)index));
    var decoded = ExtensionObject.ToEncodeable(value.Value as ExtensionObject)
        ?? throw new InvalidOperationException("Structure not decoded");
    var fields = decoded as IComplexTypeProperties
        ?? throw new InvalidOperationException("The type does not come from the dynamic decoder");
    Console.WriteLine($"Generated CLR type: {decoded.GetType().FullName}");
    foreach (var name in fields.GetPropertyNames())
    {
        var field = fields[name];
        Console.WriteLine($"{name} = {(field is System.Collections.IEnumerable array and not string ? string.Join(", ", array.Cast<object>()) : field)}");
    }
    if (!fields.GetPropertyNames().SequenceEqual(new[] { "Name", "Revision", "Target", "Steps" }))
        throw new InvalidOperationException("Dynamic fields differ from the contract");
    if (fields["Name"] is not string { Length: > 0 } || fields["Target"] is not double target || !double.IsFinite(target))
        throw new InvalidOperationException("Dynamic values are invalid");
    Console.WriteLine("OK: recipe decoded without the server assembly and without RecipeData");
    session.Close();
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
