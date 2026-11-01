# Usage

## 1. Installation

Requirements:

- **.NET 11 SDK** (developed with `11.0.100-rc.3`). Every project targets `net11.0` only.
- **A NuGet source with `PySharp.Interpreter 2.6.1-net11-preview.2`.** The package is not on nuget.org: it comes from the local feed configured in [NuGet.config](../NuGet.config) (`D:\Dev\NuGetLocalFeed`). Without it, `ConveyorSimLib` does not restore.
- Windows (the commands below were run on Windows 11; nothing in the code is Windows-only, but nothing else was tried).
- Optional: the Tailwind standalone CLI for the Gateway UI styling, see [FORGE-DATA-GATEWAY.md](../FORGE-DATA-GATEWAY.md). Without it the build works and the Gateway UI renders unstyled.

Build and test:

```powershell
dotnet restore src/ForgeIndustrial.slnx
dotnet build src/ForgeIndustrial.slnx
dotnet test src/ForgeIndustrial.slnx
```

**Where the output goes.** [Directory.Build.props](../Directory.Build.props) sets `ArtifactsPath` to `D:\Dev\BuildArtifacts\IndustrialSimExperiments\build`, so nothing is built inside the repository. Executables are in `build\bin\<Project>\debug\` (for example `build\bin\ForgeSimApp\debug\ForgeSimApp.exe`). Override with `-p:ArtifactsPath=<folder>`.

## 2. Ports

| Port | Who | What |
|---|---|---|
| 4840 | ForgeSimApp | OPC UA server `opc.tcp://localhost:4840/ForgeSim` (SecurityMode None) |
| 1883 | ForgeSimApp | Embedded MQTT broker, topics `ForgeSim/<machine>/...` |
| 4841 | OPC UA course server | `opc.tcp://localhost:4841/UaCourse` (SignAndEncrypt) |
| 5080 | OPC UA course web app | Blazor Server, started with `--urls http://localhost:5080` |
| 5289 (http), 7064 (https) | ForgeDataGatewayApp | Gateway configuration UI (from its launch profile) |

The course server deliberately uses 4841 so that it can run next to ForgeSimApp.

## 3. Running things

### The simulator

```powershell
dotnet run --project src/ForgeSimApp                                        # all protocols, live console dashboard
dotnet run --project src/ForgeSimApp -- --protocols OpcUa --protocols Mqtt  # no console dashboard
dotnet run --project src/ForgeSimApp -- --machine Conveyor --duration 10    # C# conveyor instead of the Python one, 10 s
dotnet run --project src/ForgeSimApp -- --help
```

Machine and protocol options, configuration and the exposed tags are in the [README](../README.md) and in [CONVEYOR.md](../CONVEYOR.md).

### The data gateway

```powershell
dotnet run --project src/ForgeDataGatewayApp                  # configuration UI
dotnet run --project src/ForgeDataGatewayCli -- --help        # headless
```

Point it at ForgeSimApp's OPC UA endpoint (above) to republish the tags to MQTT as a Unified Namespace. See [FORGE-DATA-GATEWAY.md](../FORGE-DATA-GATEWAY.md).

### The OPC UA course

Full instructions, in English: [docs/opcua-course/README.md](opcua-course/README.md) and [USAGE](opcua-course/USAGE.md). The short version:

```powershell
$env:UACOURSE_ARTIFACTS_ROOT = 'D:\Dev\BuildArtifacts\IndustrialSimExperiments\opcua-course'   # optional, see below
dotnet run --project src/OpcUaCourse/UaCourse -- server
dotnet run --project src/OpcUaCourse/UaCourse.Web --no-launch-profile --urls http://localhost:5080
dotnet run --project src/OpcUaCourse/UaCourse -- verify        # needs port 4841 free; runs 13 labs
```

Certificates (including private keys), the exported NodeSet2 and the ASP.NET keys go to `UACOURSE_ARTIFACTS_ROOT`, or to `%LOCALAPPDATA%\UaCourse` when it is not set; never into the repository. Every process of the course must see the same value.

### A VFD, live, in the browser

1. `dotnet run --project src/ForgeSimApp -- --protocols OpcUa`
2. `dotnet run --project src/OpcUaCourse/UaCourse -- server`
3. `dotnet run --project src/OpcUaCourse/UaCourse.Web --no-launch-profile --urls http://localhost:5080`
4. Open http://localhost:5080/dashboards/vfd

To read the same tags from a console client instead: `dotnet run --project src/OpcUaCourse/UaCourse -- client forgesim`.

## 4. Where generated files go

| What | Where |
|---|---|
| Build output | `D:\Dev\BuildArtifacts\IndustrialSimExperiments\build` (`Directory.Build.props`) |
| ForgeSim / Gateway OPC UA certificates (`pki`) | next to the executable, so under the build output |
| Course certificates, NodeSet2, ASP.NET keys | `UACOURSE_ARTIFACTS_ROOT` or `%LOCALAPPDATA%\UaCourse` |
| Logs, reports, screenshots of this work | `D:\Dev\BuildArtifacts\IndustrialSimExperiments` |
| VFD reference infographics (third-party, not committed) | `docs/images/vfd/*.png`, index in `docs/images/vfd/SOURCES.md` |

## 5. Troubleshooting

| Symptom | Check |
|---|---|
| `NU1103` for `PySharp.Interpreter` | The local feed in `NuGet.config` must contain `2.6.1-net11-preview.2` |
| Port 4840 or 4841 already in use | Another ForgeSimApp or course server is running |
| The console dashboard hangs | It needs an interactive terminal; disable it with `--protocols OpcUa --protocols Mqtt` |
| VFD dashboard shows "ForgeSimApp unavailable" | Start ForgeSimApp with `--protocols OpcUa` first |
| Course `BadCertificateUntrusted` | Processes started with different `UACOURSE_ARTIFACTS_ROOT` values |
| Gateway UI unstyled | The Tailwind executable is missing (see FORGE-DATA-GATEWAY.md) |
