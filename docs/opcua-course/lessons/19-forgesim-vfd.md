# A real simulator: browsing ForgeSim and modeling a VfdType

## Concepts

So far the server was a small simulator written for the course, with an ObjectType, units, methods and events. `ForgeSimApp` is different: it is a generic host that exposes **any** machine module through **any** protocol, so its OPC UA adapter cannot know what a VFD is. It receives a flat list of tags such as `Segments.Segment0.Vfd.BusVoltage` and turns every path into folders and a read-only variable. This lesson connects the course client to that server, reads and subscribes to the VFD of a conveyor segment, and asks you to design the better model.

## Experiment

Start ForgeSimApp (it listens on port 4840) and run the lab:

```powershell
dotnet run --project src/ForgeSimApp -- --protocols OpcUa
# Second terminal
dotnet run --project src/OpcUaCourse/UaCourse -- client forgesim
```

The course's own server uses port 4841, so the two can run together. The lab is not part of `all` or `verify`: it needs ForgeSimApp running. To use another endpoint pass it as the last argument: `client forgesim opc.tcp://host:4840/ForgeSim`.

## In the C# code

[RunForgeSim.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunForgeSim.cs) does six things:

1. **Endpoint.** ForgeSim publishes only a `SecurityMode=None` endpoint, so the client asks for it with `useSecurity: false`. Lesson 02 warned against silent fallbacks to None: here it is a deliberate choice for a local simulator, printed on the first line.
2. **Namespace.** The URI is `urn:IndustrialSim:<machine name>` (for the default machine, `urn:IndustrialSim:Conveyor (PySharp)`). The lab finds it by prefix and resolves the index, as lesson 03 requires.
3. **Machine folder.** One folder under `Objects`, in that namespace.
4. **Browse paths.** `Segments/Segment0/Vfd/BusVoltage` and similar are resolved with TranslateBrowsePathsToNodeIds. No NodeId is built by hand, so nothing depends on how the server composes its identifiers.
5. **Read and a rejected Write.** All reads must be Good. A Write to `BusVoltage` must fail: the adapter publishes only `CurrentRead` variables (`BadNotWritable`).
6. **Subscription.** Two MonitoredItems for five seconds. `OutputFrequency` sits at its target after the ramp, so it reports only the initial value (1 notification); `BusVoltage` moves with the motor load and reports dozens. A notification is sent when the value changes, so the count tells you about the signal, not about the simulation rate.

Typical output (values change on every run):

```text
Endpoint: opc.tcp://localhost:4840/ForgeSim, mode: None, policy: http://opcfoundation.org/UA/SecurityPolicy#None
OK: ForgeSim namespace found (urn:IndustrialSim:Conveyor (PySharp))
OK: Machine folder under Objects: Conveyor (PySharp)
Vfd: Variable OutputFrequency
Vfd: Variable OutputVoltage
Vfd: Variable BusVoltage
...
OK: Read Segments/Segment0/Vfd/OutputFrequency = 30
OK: Read Segments/Segment0/Vfd/BusVoltage = 564,1
OK: Write to a ForgeSim tag is rejected (BadNotWritable)
OK: Segments/Segment0/Vfd/BusVoltage: 27 notifications in 5 s
```

The VFD tags themselves (bus voltage and its nominal value, brake chopper state, relays, heatsink temperature) are described in [docs/vfd/VFD.md](../../vfd/VFD.md) and in [CONVEYOR.md](../../../CONVEYOR.md).

## Two modeling styles side by side

| Aspect | Course server (`PlantServer`) | ForgeSim adapter |
|---|---|---|
| Structure | `Reactor1` is an Object of `ReactorType` | Folders `Segments/Segment0/Vfd` containing variables |
| Type information | HasTypeDefinition, Mandatory rule | Every variable is `BaseDataVariableType`; folders are `FolderType` |
| Units | `EngineeringUnits` property (`EUInformation`) | None: the unit is only in the documentation |
| Access | `Setpoint` writable, `Temperature` read-only | Everything read-only |
| Methods | `Start`, `Stop`, `ApplyRecipe`, `LoadRecipe` | None |
| Events | `BaseEventType` from `Reactor1` | None |
| Identity | Stable string identifiers, namespace `urn:uacourse:plant` | Identifiers derived from the tag path, namespace `urn:IndustrialSim:<machine>` |
| Reuse | One server, one purpose | Any machine module, any number of tags |

Neither is wrong. The generic mapping gives every machine a browsable tree with no protocol-specific code, which is why a new machine needs no OPC UA work. The price is that a client cannot discover that `Segment0/Vfd` *is* a drive, that `BusVoltage` is in volts, or that a drive can be reset.

## Exercise: design a VfdType

Design (on paper first, then in code in a copy of the course server) a `VfdType` that fits what ForgeSim publishes:

1. Declare the variables as components of the type. Decide which are Mandatory (output frequency, bus voltage, heatsink temperature) and which Optional (brake chopper state exists only when a braking resistor is fitted).
2. Give every variable its `EngineeringUnits`: Hz, V, °C, W. Which numbers in the table of [docs/vfd/VFD.md](../../vfd/VFD.md) must become `EURange`?
3. Decide how the relays and the analog output map to the type: Boolean variables, or a dedicated structure?
4. Decide whether a fault reset should be a Method. What precondition makes sense? (Compare lesson 06 and the way the simulator resets a trip.)
5. Create two instances (two segments) with the same relative BrowseNames and independent values (lesson 05, exercise "two reactors").

**Solution outline.** A `VfdType` deriving from `BaseObjectType`, with the mandatory components declared in the type and the optional ones added only to instances that have them. Units go in `EngineeringUnits` properties, ranges in `EURange` (output frequency 0..`MaxOutputFrequency`; bus voltage up to `OverVoltPUNomDC` × nominal). Relays are Boolean variables; the analog output is a Double with unit V and range 0..10. The reset is a Method with no input and a Boolean output, rejected with `BadInvalidState` when no trip is active.

**How it could reach ForgeSim.** The adapter would need a mapping provider: the machine module (or its configuration) would tell the adapter that the tags under `Segments.Segment{i}.Vfd` form an instance of a `VfdType`, instead of the adapter inferring structure from dots. That is an open design point of the repository, not something this lesson implements.

**Acceptance criteria.** A generic client with no knowledge of the simulator browses to a segment, finds `HasTypeDefinition` = `VfdType`, reads the units and calls the reset only when a trip is active. Repeat with a changed namespace index.

## Diagnostics

| Symptom | Check |
|---|---|
| `SelectEndpoint` fails | Is ForgeSimApp running with `--protocols OpcUa`? Is the endpoint `opc.tcp://localhost:4840/ForgeSim`? |
| Namespace not found | The default machine name appears in the URI; another machine gives another URI. The lab searches by prefix. |
| `Browse path not resolved` | Tags are exposed under the machine folder; a segment index above `SegmentCount - 1` does not exist. |
| Only one notification for a signal | Normal for a constant value such as `OutputFrequency` at its target; change `TargetFrequencyHz` or stop the belt and watch it move. |
| Port 4840 busy | Another OPC UA server, or a second ForgeSimApp. The course server uses 4841. |

References: [Address Space Model, Part 3](https://reference.opcfoundation.org/Core/Part3/), [Device Integration (DI) companion specification](https://reference.opcfoundation.org/DI/), [Information Model, Part 5](https://reference.opcfoundation.org/Core/Part5/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
