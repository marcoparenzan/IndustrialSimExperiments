using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// ForgeSim lab: browses and subscribes to the OPC UA server of ForgeSimApp (src/ForgeSimApp), not to the course server.
// It is NOT part of `all` / `verify` because it needs ForgeSimApp running (default endpoint below).
public static partial class Lessons
{
    public const string ForgeSimEndpoint = "opc.tcp://localhost:4840/ForgeSim";
    private const string ForgeSimNamespacePrefix = "urn:IndustrialSim:";

    public static async Task RunForgeSimAsync(string endpointUrl)
    {
        var app = await Configuration.CreateAsync(false);
        // ForgeSimApp's server publishes a SecurityMode=None endpoint only, so security is not requested (useSecurity: false).
        var description = CoreClientUtils.SelectEndpoint(app.ApplicationConfiguration, endpointUrl, false);
        Console.WriteLine($"Endpoint: {description.EndpointUrl}, mode: {description.SecurityMode}, policy: {description.SecurityPolicyUri}");
        var configured = new ConfiguredEndpoint(null, description, EndpointConfiguration.Create(app.ApplicationConfiguration));
        using var session = await Session.Create(app.ApplicationConfiguration, configured, false,
            "UaCourse.ForgeSim", 60_000, new UserIdentity(new AnonymousIdentityToken()), null);
        try { await RunForgeSimAsync(session); }
        finally { session.Close(); }
    }

    private static async Task RunForgeSimAsync(Session session)
    {
        // 1. The namespace URI is urn:IndustrialSim:<machine name>; never assume the index (lesson 03).
        var namespaceUri = session.NamespaceUris.ToArray().FirstOrDefault(uri => uri.StartsWith(ForgeSimNamespacePrefix, StringComparison.Ordinal));
        Require(namespaceUri is not null, $"ForgeSim namespace found ({namespaceUri})");
        var ns = (ushort)session.NamespaceUris.GetIndex(namespaceUri);

        // 2. The machine is one folder under Objects, in that namespace.
        session.Browse(null, null, ObjectIds.ObjectsFolder, 0, BrowseDirection.Forward,
            ReferenceTypeIds.HierarchicalReferences, true, 0, out _, out var objects);
        var machine = objects.FirstOrDefault(reference => reference.BrowseName.NamespaceIndex == ns);
        Require(machine is not null, $"Machine folder under Objects: {machine?.DisplayName}");
        var machineId = ExpandedNodeId.ToNodeId(machine!.NodeId, session.NamespaceUris);

        // 3. Every simulation tag path "A.B.C" became folders A/B and a variable C.
        var vfdFolder = ResolveForgeSimPath(session, machineId, ns, "Segments/Segment0/Vfd");
        session.Browse(null, null, vfdFolder, 0, BrowseDirection.Forward,
            ReferenceTypeIds.HierarchicalReferences, true, 0, out _, out var vfdChildren);
        foreach (var child in vfdChildren) Console.WriteLine($"Vfd: {child.NodeClass} {child.BrowseName.Name}");
        Require(vfdChildren.Any(c => c.BrowseName.Name == "BusVoltage"), "Vfd folder contains BusVoltage");

        // 4. Read through browse paths: no NodeId is built by hand.
        var paths = new[]
        {
            "Segments/Segment0/Vfd/OutputFrequency", "Segments/Segment0/Vfd/BusVoltage", "Segments/Segment0/Vfd/BusVoltageNominal",
            "Segments/Segment0/Motor/SpeedRpm", "Segments/Segment0/Motor/PhaseCurrent",
        };
        var ids = paths.ToDictionary(path => path, path => ResolveForgeSimPath(session, machineId, ns, path));
        foreach (var (path, id) in ids)
        {
            var value = session.ReadValue(id);
            Require(StatusCode.IsGood(value.StatusCode), $"Read {path} = {value.Value}");
        }

        // 5. ForgeSim publishes read-only variables: compare with the course server, where Setpoint is writable (lesson 04).
        var status = Write(session, ids["Segments/Segment0/Vfd/BusVoltage"], 0.0);
        Require(StatusCode.IsBad(status), $"Write to a ForgeSim tag is rejected ({status})");

        // 6. A short subscription: the values change on every simulation tick.
        using var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 250 };
        session.AddSubscription(subscription); subscription.Create();
        var counts = new Dictionary<string, int>();
        var items = new List<MonitoredItem>();
        foreach (var path in new[] { "Segments/Segment0/Vfd/OutputFrequency", "Segments/Segment0/Vfd/BusVoltage" })
        {
            counts[path] = 0;
            var item = new MonitoredItem(subscription.DefaultItem)
            { StartNodeId = ids[path], AttributeId = Attributes.Value, SamplingInterval = 100, QueueSize = 20, DiscardOldest = true };
            var captured = path;
            item.Notification += (monitored, _) => { lock (counts) counts[captured] += ((MonitoredItem)monitored).DequeueValues().Count; };
            items.Add(item);
        }
        try
        {
            subscription.AddItems(items); subscription.ApplyChanges();
            Require(items.All(item => !ServiceResult.IsBad(item.Status.Error)), "MonitoredItems created");
            await Task.Delay(TimeSpan.FromSeconds(5));
            lock (counts)
                foreach (var (path, count) in counts)
                    Require(count > 0, $"{path}: {count} notifications in 5 s");
        }
        finally { subscription.Delete(true); session.RemoveSubscription(subscription); }

        Console.WriteLine("ForgeSim publishes plain variables under folders: no ObjectType, no EngineeringUnits, no methods.");
        Console.WriteLine("Compare with the course server (lesson 05) and try the VfdType exercise of lesson 19.");
    }

    private static NodeId ResolveForgeSimPath(Session session, NodeId start, ushort ns, string path)
    {
        var relative = new RelativePath();
        foreach (var name in path.Split('/'))
            relative.Elements.Add(new RelativePathElement
            { ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences, IsInverse = false, IncludeSubtypes = true, TargetName = new QualifiedName(name, ns) });
        session.TranslateBrowsePathsToNodeIds(null, new BrowsePathCollection { new BrowsePath { StartingNode = start, RelativePath = relative } },
            out var results, out _);
        if (StatusCode.IsBad(results[0].StatusCode) || results[0].Targets.Count != 1)
            throw new InvalidOperationException($"Browse path not resolved: {path} ({results[0].StatusCode})");
        return ExpandedNodeId.ToNodeId(results[0].Targets[0].TargetId, session.NamespaceUris);
    }
}
