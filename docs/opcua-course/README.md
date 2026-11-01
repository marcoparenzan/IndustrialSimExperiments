# OPC UA in C#: from signal to information model

A progressive lab with a simulated OPC UA server and a Blazor Server application (.NET 11, Interactive Server rendering). The first synoptic is based on the water plant of the screen provided: the diagram is redrawn in SVG, without menus, dates, static values or frame. The second one explores a reactor, methods and recipes.

The labels receive DataValues through MonitoredItems and Publish. The buttons run Write and Call; events arrive through a subscription to BaseEventType. The browser talks to Blazor over SignalR: it is the ASP.NET process that speaks OPC UA.

The course lives in the ForgeIndustrial repository (`src/OpcUaCourse`) and can also be pointed at the `ForgeSimApp` simulator, which exposes its own OPC UA server on port 4840 ([lesson 19](lessons/19-forgesim-vfd.md)). The course server uses port **4841** so that both can run at the same time.

## Quick start

Prerequisite: the .NET 11 SDK. From the repository root:

```powershell
dotnet restore src/ForgeIndustrial.slnx
dotnet build src/ForgeIndustrial.slnx --no-restore
dotnet run --project src/OpcUaCourse/UaCourse --no-build -- server
```

In a second terminal, **always from the same root**:

```powershell
dotnet run --project src/OpcUaCourse/UaCourse.Web --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080. UA endpoint: `opc.tcp://localhost:4841/UaCourse`. Stop with Ctrl+C. The server and the web app are separate processes, with two independent sessions if you also start the console client.

Certificates (including private keys), the exported NodeSet2 and the ASP.NET keys are written outside the repository: to `%LOCALAPPDATA%\UaCourse`, or to the folder named by the `UACOURSE_ARTIFACTS_ROOT` environment variable. Set the same value for every process of the course.

The home page is a neutral index of the dashboards, with static previews and links. The UA Lab template uses a dark sidebar, light panels and compact navigation on mobile. The dashboards have their own URLs: [water plant](http://localhost:5080/dashboards/water), [reactor and recipes](http://localhost:5080/dashboards/reactor) and [VFD drive](http://localhost:5080/dashboards/vfd), whose values come from `ForgeSimApp` (start it with `dotnet run --project src/ForgeSimApp -- --protocols OpcUa`; the other two dashboards do not need it). The catalog generates both the home cards and the navigation links automatically.

## The path

[Lesson index](lessons/README.md), [usage and troubleshooting](USAGE.md), [architecture](ARCHITECTURE.md), [roadmap](ROADMAP.md), [open points](OPEN-POINTS.md), [changelog](CHANGELOG.md).

The path distinguishes executable labs from design deep-dives. Implemented: browse and continuation points, browse paths, batch reads and attributes, validated writes, ObjectTypes, standard and custom structures, arrays, methods with a structured input, data changes, events, an encrypted channel and Blazor reconnection. RecipeDataType publishes DataTypeDefinition and HasEncoding: an independent client decodes it from the metadata. ModelCompiler generation, Alarms & Conditions, history, PubSub, roles and GDS remain extensions in the roadmap.

## Automatic verification

With no server running on port 4841:

```powershell
dotnet run --project src/OpcUaCourse/UaCourse -- verify
```

It starts a real TCP server, opens a SignAndEncrypt session and runs thirteen labs with assertions. A failure produces exit code 1. No PLC is needed. The labs are separate files in `src/OpcUaCourse/UaCourse/Labs`; they include partial arrays, model verification, method preconditions and custom recipes.

With the server running, try the [executable lesson 09](lessons/09-custom-types.md):

```powershell
dotnet run --project src/OpcUaCourse/UaCourse -- client custom-types
dotnet run --project src/OpcUaCourse/UaCourse.DynamicClient
```

The second project references neither the server DLL nor RecipeData: it uses the official ComplexTypes component to discover the type. The server exports the NodeSet2 fragment of the recipe to `<artifacts root>/models/Recipe.NodeSet2.xml`. The Blazor panel allows loading and reading the custom recipe.

## Extending the screens

Define a `ScreenDefinition` in `src/OpcUaCourse/UaCourse.Web/Modules/ScreenCatalog.cs`, add a background in `wwwroot/screens`, declare the labels with percentage coordinates and publish the matching nodes on the server. `Synoptic` renders every screen and `UaConnection` automatically subscribes to the distinct nodes of the catalog.

## References

Official SDK [OPC Foundation UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard), package version 1.5.376.244 (the same one `ForgeSimApp` uses), pinned to make the lab's APIs reproducible; this is not a claim that it is the latest version. [Part 3: Address Space](https://reference.opcfoundation.org/Core/Part3/), [Part 4: Services](https://reference.opcfoundation.org/Core/Part4/), [Part 5: Information Model](https://reference.opcfoundation.org/Core/Part5/), [official samples](https://github.com/OPCFoundation/UA-.NETStandard-Samples). The APIs of the current branch may differ from those of the pinned package.
