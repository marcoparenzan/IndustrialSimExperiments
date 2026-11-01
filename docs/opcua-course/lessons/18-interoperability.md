# NodeSet, companion specifications, diagnostics and the final project

## Concepts

A shared model describes meaning and contracts beyond the transport. NodeSet2 allows nodes and dependencies to be distributed; companion specifications model specific domains. Before inventing a PumpType check the official models suited to the domain. Conformance and interoperability require independent clients and behavior tests.

## Experiment

Final project: two lines, controllable pumps, a structured recipe, a retained alarm, history and three synoptics. The initial implementation of the repository is the executable base, the extensions are tracked in the roadmap.

## In the C# code

Version the namespace, ModelVersion, PublicationDate, NodeIds and RequiredModel dependencies. Measure the number of sessions, items, queue overflow, latency, reconnects and service errors. GDS and certificate renewal require a dedicated infrastructure lab.

## Exercise and verification

Verify with a generic client: type discovery, browse, units, Call, custom decoding, conditions and history. Repeat with a different namespace index, a restart and two users. Record expected and actual without depending on the UI.

## Mistake to avoid

A C# client that shares all the classes of the server does not prove interoperability. A successful compilation does not prove conformance to the specifications nor certification.

## Goals and final project

Estimated time: at least a day for the design, then implementation by increments. Bring the lessons together in a cell with two reactors and independent pumps, custom recipes, conditions, history and modular synoptics. Do not declare the project complete just because the two C# processes compile together.

## Distributing the model

NodeSet2 describes nodes, references, attributes and dependencies of the model. Keep the source of the model and a reproducible generation. Namespace URI, ModelVersion, PublicationDate and RequiredModel help identify the contract and its dependencies.

Publishing the model is not the same as the server behavior: a Method declared in the NodeSet still requires an implementation; a historized Variable requires a provider; a condition requires a coherent state cycle.

Avoid changing identifiers at every build. The model must survive the recreation of the process. Random ids are useful for ephemeral resources, not for contract nodes that clients must find again.

## Using a shared domain model

Before inventing your own PumpType, evaluate companion specifications suited to the use case, for example the relevant device-integration and machinery models. The choice depends on the domain: a similar name does not prove that the model is right for that plant.

Import the required dependencies and keep the official namespaces without arbitrarily modifying them. Your extensions belong to the application namespace. Do not label an object as conforming if part of the mandatory contract is missing.

## An interoperability matrix

| Test | Static C# client | Client without the server DLL |
|---|---|---|
| Browse and paths | Required | Required |
| Types and units | Required | Required |
| Signature and Call | Required | Required |
| RecipeDataType decoding | Can use a factory | Must discover/load the contract |
| Acknowledge and refresh | Required | Required if supported |
| Paginated HistoryRead | Required | Required |
| Changed namespace index | Required | Required |

The static client is useful for development and performance; the independent client discovers gaps that a shared DLL can mask. The presence of a model does not guarantee all the capabilities of the server: verify the services required.

## Diagnostics that help to decide

Record counters of sessions, subscriptions and items, reconnects, Bad results per service and discarded callbacks. Measure latency without mixing source time, server reception, client reception and rendering.

A domain freshness diagnostic could be:

```csharp
static bool IsFresh(DateTime sourceUtc, DateTime nowUtc,
    TimeSpan allowedAge)
{
    var age = nowUtc - sourceUtc;
    return age >= TimeSpan.Zero && age <= allowedAge;
}
```

This check presupposes consistent clocks and meaningful timestamps. A future timestamp must not be treated as fresh data indefinitely. For nodes that change rarely, the freshness policy does not necessarily coincide with the time since the last data-change notification.

## Certificates and GDS as a dedicated lab

To manage several devices define enrollment, trust, renewal, revocation and recovery. GDS and CertificateManager require their own roles, services and configuration; the local DER trust of the course is not an implementation of them.

The management exercise must include renewal before expiry, a peer still holding the old certificate, trust synchronization and an administrative audit. Verify the failure mode before automating the rotation.

## Solved exercise: delivery criteria

Organize four increments:

1. Model and two instances: same structure, independent state.
2. Custom recipe: independent decoding and versioned compatibility.
3. Advanced services: retained conditions and persistent history.
4. Robustness: recovery, identity and UI on several circuits.

**Verification solution.** For every increment produce an expected/actual table and at least one failure test. For the model change the namespace index; for the recipes use an invalid payload; for the alarms reconnect after the return to normal; for the history paginate and restart; for authorizations send requests directly without the UI.

The thirteen labs cover the client/server core and the custom recipe type; an independent dynamic client verifies the metadata. The other extensions of the final project require dedicated server implementations. Formal conformance and certification are goals beyond our tests.

References: [OPC Foundation models](https://github.com/OPCFoundation/UA-Nodeset), [NodeSet and models, Part 6](https://reference.opcfoundation.org/Core/Part6/), [Discovery, Part 12](https://reference.opcfoundation.org/Core/Part12/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
