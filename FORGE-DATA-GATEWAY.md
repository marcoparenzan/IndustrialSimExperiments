# ForgeDataGateway

An edge-collector: polls industrial protocols (OPC UA today; Modbus/S7 are documented extension points), buffers samples in memory, and republishes them to MQTT as a [Unified Namespace](https://www.hivemq.com/solutions/unified-namespace/) (UNS) — a retained, hierarchical MQTT topic tree that always reflects the current value of every tag. Core logic is protocol/UI-agnostic and shared by two hosts: a Blazor Server UI for configuring sources/tags interactively, and a Spectre.Console.Cli console app for running headless.

Pairs naturally with this repo's simulator: point a source at [`ForgeSimApp`](README.md)'s OPC UA endpoint (`opc.tcp://localhost:4840/ForgeSim`) to try the whole pipeline end to end without any real hardware.

## Solution map

```text
src/
  ForgeDataGatewayCore/   Engine: ISourceClient/OpcUaSourceClient, SamplingEngine (in-memory queue
                          buffer + latest-value snapshot), MqttUnsPublisher, GatewayConfig +
                          JsonFileGatewayConfigStore. No UI, no protocol/host-specific dependencies.
  ForgeDataGatewayApp/    Blazor Server UI (Tailwind CSS): configure sources/tags, browse an OPC UA
                          address space, watch live sampled values, edit the UNS/MQTT settings.
  ForgeDataGatewayCli/    Spectre.Console.Cli host: runs the same engine headless, no web server.
  ForgeDataGatewayTests/  xUnit tests for the engine (sampling, topic templating, config round-trip).
```

Both hosts depend only on `ForgeDataGatewayCore` — neither the engine nor `ISourceClient` implementations know anything about Blazor, Spectre, or each other. See ["Adding a new ISourceClient"](#adding-a-new-isourceclient-forgedatagatewaycore) below for how a Modbus or S7 client would slot in.

## Architecture

```csharp
public interface ISourceClient : IAsyncDisposable
{
    string Protocol { get; }
    Task ConnectAsync(SourceDefinition source, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrowseNode>> BrowseAsync(string? nodeId, CancellationToken cancellationToken = default);
    Task<SampledValue> ReadAsync(TagDefinition tag, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
```

- **`SamplingEngine`** connects one `ISourceClient` per enabled `SourceDefinition` and runs one `PeriodicTimer` loop per enabled `TagDefinition` (its own interval, or the source's default — floored at 1ms, the same `PeriodicTimer` lesson learned in `ForgeSimApp/SimulationWorker.cs`). Every sample updates a non-destructive `LatestValues` snapshot (what the UI reads) and is written into a bounded `Channel<SampledValue>` — the in-memory queue buffer — so a slow or disconnected MQTT broker never blocks polling.
- **`MqttUnsPublisher`** drains that channel and publishes each value as a retained, JSON-payload MQTT message on a topic built from `NamespaceOptions.TopicTemplate` (default `{Enterprise}/{Site}/{Area}/{Source}/{Tag}`), mirroring `MqttProtocolAdapter`'s retain/JSON approach from the simulator side.
- **`OpcUaSourceClient`** mirrors `OpcUaServerLib/MyOpcUaServerHost.cs`'s `ApplicationInstance`/certificate bootstrap (client cert instead of server cert) and uses `Opc.Ua.Client.Session` to connect, browse, and read. Pinned to the same OPC UA SDK version (`1.5.376.244`) as `OpcUaServerLib` rather than the newer client-only package, whose async-first API diverged too far to be worth the risk here.
- **Configuration** is a single JSON file (`IGatewayConfigStore`/`JsonFileGatewayConfigStore`, default `gateway-config.json` next to the executable) — no database. Both hosts load/save the same shape; the Blazor UI reloads (stop → save → restart) the engine on every edit so changes take effect immediately.

## Running the Blazor UI

```bash
dotnet run --project src/ForgeDataGatewayApp
```

Opens with whatever `gateway-config.json` already exists next to the app (the repo ships one pre-wired to `ForgeSimApp`'s default endpoint, under `src/ForgeDataGatewayApp/gateway-config.json` — run `ForgeSimApp` first and the gateway starts sampling and publishing automatically). Pages: **Home** (status + start/stop), **Sources** (add/remove protocol endpoints), **Browse** (walk a connected source's address space and add a node as a tag), **Tags** (live value/quality/timestamp per tag), **Namespace & MQTT** (UNS hierarchy + broker settings, with a live topic preview).

### Tailwind CSS setup (one-time)

The UI is styled with Tailwind CSS v4 via its standalone CLI (no Node/npm). The binary is a downloaded platform executable, not a NuGet package, so it isn't committed to git:

```bash
curl -sL -o src/ForgeDataGatewayApp/tools/tailwindcss.exe https://github.com/tailwindlabs/tailwindcss/releases/download/v4.3.3/tailwindcss-windows-x64.exe
```

(swap the asset name for your platform — see the [releases page](https://github.com/tailwindlabs/tailwindcss/releases)). Once present, an MSBuild target (`TailwindBuild` in `ForgeDataGatewayApp.csproj`) compiles `Styles/tailwind.css` → `wwwroot/css/tailwind.css` on every build automatically; without it, the build still succeeds (with a warning) but the UI renders unstyled.

## Running the CLI

```bash
dotnet run --project src/ForgeDataGatewayCli -- --config gateway-config.json --dashboard
dotnet run --project src/ForgeDataGatewayCli -- --help
```

- `-c`/`--config <PATH>`: gateway config JSON (default: `gateway-config.json` next to the executable).
- `--mqtt-host` / `--mqtt-port`: override the config file's broker.
- `-d`/`--duration <SECONDS>`: stop after this long (`0` = run until Ctrl+C).
- `--dashboard`: a live Spectre.Console table of the latest sampled values — interactive terminals only. Like `ConsoleDashboardProtocolAdapter` in the simulator, it checks `AnsiConsole.Profile.Capabilities.Interactive` and silently no-ops when stdout is redirected/piped, since Spectre's live display can hang against a non-interactive stream otherwise.

## Config file shape

```json
{
  "Sources": [
    { "Id": "...", "Name": "ForgeSim", "Protocol": "OpcUa", "Endpoint": "opc.tcp://localhost:4840/ForgeSim", "DefaultSamplingIntervalMs": 500, "Enabled": true }
  ],
  "Tags": [
    { "Id": "...", "SourceId": "...", "NodeId": "ns=2;s=Conveyor.Supply.LineLineVoltage", "Alias": "LineVoltage", "Enabled": true }
  ],
  "Namespace": { "Enterprise": "Forge", "Site": "Demo", "Area": "Line1", "Line": "Cell1", "TopicTemplate": "{Enterprise}/{Site}/{Area}/{Source}/{Tag}" },
  "Mqtt": { "Host": "localhost", "Port": 1883, "ClientId": "ForgeDataGateway" }
}
```

`NodeId` values are OPC UA node identifiers as returned by `BrowseAsync` (e.g. `ns=2;s=Conveyor.Segments.Segment0.Motor.SpeedRpm` when pointed at `ForgeSimApp`) — use the Blazor UI's Browse page to discover them rather than guessing the format.

## Unified Namespace / MQTT

- Every published message is **retained**, so a client subscribing mid-run immediately gets the latest value of every tag instead of waiting for the next sample.
- Payload is JSON: `{"value": ..., "timestamp": "...", "quality": "Good"|"Uncertain"|"Bad"}`.
- Topic = `NamespaceOptions.TopicTemplate` with `{Enterprise}`, `{Site}`, `{Area}`, `{Line}`, `{Source}` (the tag's source name), `{Tag}` (the tag's alias) substituted — edit the template on the Namespace page or in the config file to match your own ISA-95/UNS convention.

## Verified end to end

This pipeline was proven against the real (simulated) stack in this repo, not mocked: `ForgeSimApp`'s OPC UA server → `OpcUaSourceClient` browsing and reading real NodeIds → `SamplingEngine`'s buffer → `MqttUnsPublisher` → a real MQTT subscriber receiving live, retained, UNS-topic values — both through `ForgeDataGatewayCli` and through `ForgeDataGatewayApp`. Along the way this caught and fixed a real bug in `OpcUaServerLib/NodeExtension.cs`: nested folder NodeIds were built from the immediate parent's short name instead of its full accumulated identifier, so any NodeId more than one folder deep was wrong for *any* OPC UA client (not just this gateway).

## Adding a new ISourceClient (ForgeDataGatewayCore)

1. Implement `ISourceClient` in a new project (e.g. `src/ModbusSourceLib`), referencing only `ForgeDataGatewayCore`. `OpcUaSourceClient` (`src/ForgeDataGatewayCore/OpcUa/OpcUaSourceClient.cs`) is the reference implementation. If the protocol has no browsable address space, throw `NotSupportedException` from `BrowseAsync` (the Blazor Browse page and the CLI simply don't call it for that protocol).
2. Reference the new project from `ForgeDataGatewayCli` and `ForgeDataGatewayApp`, and add a case to each host's `ResolveClient(string protocol)` switch (`Cli/GatewayCommand.cs` and `Services/GatewayService.cs`).
3. Add `"Modbus"` (or whatever `Protocol` string you chose) as a selectable option in `Sources.razor`'s protocol `<select>` (currently only `OpcUa` is enabled there, with Modbus/S7 shown disabled).
4. Set a `SourceDefinition.Protocol` of that value in a config file, or add it through the Sources page once step 3 is done.
