# Usage

## Walkthrough

1. Start `server` from the repository root (see the [README](README.md#quick-start)).
2. Start the web app from the same root. Certificate stores are resolved from `UACOURSE_ARTIFACTS_ROOT`, or `%LOCALAPPDATA%\UaCourse` when it is not set: use the same value for every process.
3. Open `http://localhost:5080`: the home page lists the available dashboards. Select Water plant (`/dashboards/water`) and check the connected state.
4. Start the reactor: the totalizer increases. Use the navigation to move to the Reactor and recipes synoptic (`/dashboards/reactor`): the temperature rises towards the target.
5. Write a setpoint. Then apply a recipe with Call: observe the method output and the event.
6. Read structure and array: a Range and a Double[] are decoded.
7. Stop and restart the server: the UI reports unavailability and recreates the subscriptions automatically.

The Start/Stop commands are also available as hotspots on the background, described in the catalog. The panel adds Write, ApplyRecipe and reading of the structures. All of them belong to Reactor1. The pumps in the drawing are static symbols: they are not individual actuation buttons.

The home page is also available at `/dashboards` and contains no live values or commands. Every dashboard can be opened directly or reloaded. An unknown identifier shows "Dashboard not found" with a link back to the home page. On mobile the sidebar becomes a top navigation; the synoptic keeps its internal horizontal scrolling to preserve legibility.

## VFD drive screen (ForgeSimApp)

`/dashboards/vfd` is fed by a second, read-only connection to the OPC UA server of `ForgeSimApp` (default `opc.tcp://localhost:4840/ForgeSim`; change it with the `ForgeSim:Endpoint` setting, for example `--ForgeSim:Endpoint=opc.tcp://host:4840/ForgeSim`). It shows the supply, DC bus, inverter output, heatsink, motor and relay tags of conveyor segment 0. With the Python conveyor (default) it also shows the active trip; with the C# conveyor that label stays at "—" because `Machine.ActiveTrip` exists only in the Python machine.

If ForgeSimApp is not running, the chip in the header says so and the labels show "—"; the other dashboards keep working. The screen has no commands: ForgeSim's variables are read-only. See [lesson 19](lessons/19-forgesim-vfd.md).

## Console labs

With the server already running:

```powershell
dotnet run --project src/OpcUaCourse/UaCourse -- client browse
dotnet run --project src/OpcUaCourse/UaCourse -- client paths
dotnet run --project src/OpcUaCourse/UaCourse -- client read
dotnet run --project src/OpcUaCourse/UaCourse -- client write
dotnet run --project src/OpcUaCourse/UaCourse -- client types
dotnet run --project src/OpcUaCourse/UaCourse -- client custom-types
dotnet run --project src/OpcUaCourse/UaCourse -- client ranges
dotnet run --project src/OpcUaCourse/UaCourse -- client model
dotnet run --project src/OpcUaCourse/UaCourse -- client method-errors
dotnet run --project src/OpcUaCourse/UaCourse -- client methods
dotnet run --project src/OpcUaCourse/UaCourse -- client errors
dotnet run --project src/OpcUaCourse/UaCourse -- client subscribe
dotnet run --project src/OpcUaCourse/UaCourse -- client events
```

`dotnet run --project src/OpcUaCourse/UaCourse -- client forgesim [endpoint]` is different: it connects to the `ForgeSimApp` simulator (default `opc.tcp://localhost:4840/ForgeSim`, start it with `dotnet run --project src/ForgeSimApp -- --protocols OpcUa`) and belongs to [lesson 19](lessons/19-forgesim-vfd.md). It is not part of `all` or `verify`.

`client all` runs the thirteen course labs in sequence. `verify` starts its own server: stop the external server first. Subscribe and events end when a notification arrives; they time out after 10 s if none does.

`ranges` verifies `Profile[1:2] = [40,60]` and a range beyond the data. `model` verifies HasTypeDefinition, the Mandatory rule and the DataType of the declaration/instance. `method-errors` verifies BadInvalidState on the second Start and the integrity of the setpoint after a rejected Call; it stops the reactor in its cleanup. The individual labs are separate files in `src/OpcUaCourse/UaCourse/Labs` and correspond to the solutions of lessons 05, 06 and 08.

`custom-types` verifies the RecipeDataType metadata, a Call with a structured input, readback, Publish and the rejection of an invalid recipe. It restores the previous recipe. With the server running, `dotnet run --project src/OpcUaCourse/UaCourse.DynamicClient` decodes the fields without the server DLL. The web app adds name/revision inputs and the Load RecipeDataType and Read custom recipe buttons. The type model is exported at startup to `<artifacts root>/models/Recipe.NodeSet2.xml`.

## Adding a synoptic

```csharp
new ScreenDefinition("line2", "Line 2", "Description", "/screens/line2.svg",
    [new SignalLabel("Line2.Speed", "Speed", "rpm", 50, 30)]);
```

Publish `Line2.Speed` in the NodeManager, add the entry to the Screens property and the background to `wwwroot/screens`. X and Y are percentages of the canvas, not browser pixels. An SVG with a viewBox keeps its aspect ratio; avoid values already printed in the background. To use other raster images, save a clean background and keep the ratio and the positions in the catalog consistent. There is no need to change Synoptic or open new sessions.

## Troubleshooting

| Symptom | Check |
|---|---|
| Connection permanently offline | Is the server running? Is port 4841 free? What does the web app log say? |
| BadCertificateUntrusted | Were the processes started with the same `UACOURSE_ARTIFACTS_ROOT`? Are the `pki` stores of both present? |
| BadCertificateHostNameInvalid | Use localhost as in the commands; the sample does not handle remote hosts |
| BadNodeIdUnknown | Do the node name and the namespace in the catalog match the server? |
| No updates | Item.Status.Error, MonitoringMode, PublishingInterval and the quality of the DataValue |
| BadInvalidState on Start | The reactor is already running; run Stop |
| BadOutOfRange | The target must be finite and between 20 and 90 |
| Port in use during verify | Stop `server` before starting the verification |
| Port 4841 in use by something else | Another OPC UA server? `ForgeSimApp` uses 4840, not 4841 |
| VFD screen says "ForgeSimApp unavailable" | Start `ForgeSimApp` with `--protocols OpcUa`; check that port 4840 is free and that the endpoint matches `ForgeSim:Endpoint` |

`dotnet build src/ForgeIndustrial.slnx --no-restore` builds the whole repository, course included. Private keys and build outputs live outside the repository (artifacts root and the central build output), so Git never sees them. Do not delete the PKI folder while the applications are running.
