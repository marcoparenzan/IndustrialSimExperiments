# Teaching path

Lessons 01–12 have labs on the available code; lesson 13 combines the current security with the design of authorizations. Lessons 14–18 are deep-dives with extensions still to be implemented. Lesson 19 connects the course client to the `ForgeSimApp` simulator of this repository and has an executable lab. Lesson 09 includes a custom DataType, a structured method, a subscription and an independent dynamic client. Every lesson connects concepts, code, experiments and verifications.

1. [From the tag to the information model](01-fundamentals.md)
2. [Discovery, endpoints, SecureChannel and Session](02-sessions.md)
3. [Address space, references and namespaces](03-address-space.md)
4. [Read, Write, quality, timestamps and batches](04-read-write.md)
5. [ObjectType, VariableType and modeling rules](05-modeling.md)
6. [Methods, arguments and the semantics of commands](06-methods.md)
7. [Subscriptions, MonitoredItems, Publish and deadband](07-subscription.md)
8. [Variant, arrays, ExtensionObject and standard structures](08-structures.md)
9. [Custom types and NodeSet2](09-custom-types.md)
10. [EventType, EventNotifier and filters](10-events.md)
11. [Modular synoptics and the Blazor lifecycle](11-blazor.md)
12. [Reconnection and data continuity](12-recovery.md)
13. [Certificates, identity, authorization and audit](13-security.md)
14. [Alarms & Conditions: state, acknowledge and refresh](14-alarms.md)
15. [Historical Access and trends](15-history.md)
16. [State machines, FileType and process contracts](16-state-files.md)
17. [PubSub, DataSets and transports](17-pubsub.md)
18. [NodeSet, companion specifications, diagnostics and the final project](18-interoperability.md)
19. [A real simulator: browsing ForgeSim and modeling a VfdType](19-forgesim-vfd.md)

The `verify` command covers thirteen client/server labs, not the unimplemented extensions and not the ForgeSim lab (it needs ForgeSimApp running). For the complete commands see [USAGE](../USAGE.md).

## How to follow the course

Every chapter contains an initial summary, goals and prerequisites, an explanation of the UA contract, a guided C# path, expected results, errors and a reasoned solution of the exercises. The project snippets declared as such must not be confused with features already available in the simulator.

Recommended path: lessons 01–08 for the core of the protocol; 10–13 for events, UI, recovery and security; then 09 and 14–18 to design interoperable extensions. The chapter times are indicative and do not include the full development of the extensions.

## Map of the labs and solutions

Paths are relative to the repository root: the code lives under `src/OpcUaCourse/`.

| Chapter | Command or exercise | Code to study | Criterion |
|---|---|---|---|
| 01–02 | `client read` | [Program](../../../src/OpcUaCourse/UaCourse/Program.cs), [Configuration](../../../src/OpcUaCourse/UaCourse/Configuration.cs) | Secure session and a measurement with quality |
| 03 | `client browse`, `client paths` | [Browse](../../../src/OpcUaCourse/UaCourse/Labs/RunBrowse.cs), [Paths](../../../src/OpcUaCourse/UaCourse/Labs/RunPaths.cs) | Pages consumed and path resolved |
| 04 | `client read`, `client write`, `client errors` | [Read](../../../src/OpcUaCourse/UaCourse/Labs/RunRead.cs), [Write](../../../src/OpcUaCourse/UaCourse/Labs/RunWrite.cs), [Errors](../../../src/OpcUaCourse/UaCourse/Labs/RunErrors.cs) | Distinct results and readback |
| 05 | `client model` | [Model](../../../src/OpcUaCourse/UaCourse/Labs/RunModel.cs) | Consistent type, Mandatory and DataType |
| 06 | `client methods`, `client method-errors` | [Methods](../../../src/OpcUaCourse/UaCourse/Labs/RunMethods.cs), [MethodErrors](../../../src/OpcUaCourse/UaCourse/Labs/RunMethodErrors.cs) | Output, preconditions, state after a rejection |
| 07 | `client subscribe` | [Notifications](../../../src/OpcUaCourse/UaCourse/Labs/ObserveNotifications.cs) | Notification and item state |
| 08 | `client types`, `client ranges` | [Types](../../../src/OpcUaCourse/UaCourse/Labs/RunTypes.cs), [Ranges](../../../src/OpcUaCourse/UaCourse/Labs/RunRanges.cs) | Structure, array and IndexRange |
| 09 | `client custom-types` and `UaCourse.DynamicClient` | [RecipeData](../../../src/OpcUaCourse/UaCourse/Models/RecipeData.cs), [RecipeNodeModel](../../../src/OpcUaCourse/UaCourse/Models/RecipeNodeModel.cs), [lab](../../../src/OpcUaCourse/UaCourse/Labs/RunCustomTypes.cs) | Read/Call/Publish and decoding without the server DLL |
| 10 | `client events` | [Notifications](../../../src/OpcUaCourse/UaCourse/Labs/ObserveNotifications.cs) | EventFieldList according to the SelectClauses |
| 11 | Browser and layout runner | [UaConnection](../../../src/OpcUaCourse/UaCourse.Web/Services/UaConnection.cs), [runner](../../../src/OpcUaCourse/scripts/check-layout.cjs) | Labels, screen change and commands |
| 12 | Server stop/restart | [UaConnection](../../../src/OpcUaCourse/UaCourse.Web/Services/UaConnection.cs) | Cache invalidated and new subscriptions |
| 13 | Current trust + roles design | [Configuration](../../../src/OpcUaCourse/UaCourse/Configuration.cs) | Distinguish channel, user and authorization |
| 14–18 | Advanced projects | Contracts and exercises in the chapters | Expected/actual criteria for each extension |
| 19 | `client forgesim` (needs ForgeSimApp on port 4840) | [ForgeSim lab](../../../src/OpcUaCourse/UaCourse/Labs/RunForgeSim.cs) | Namespace by URI prefix, browse paths, read-only tags, subscription |

The implementations are separate in `src/OpcUaCourse/UaCourse/Labs`; `Lessons.cs` keeps the dispatch and the helpers. The custom type is available with manual encodeable code and a partial NodeSet2 export; automatic generation remains future work. A&C, history, FileType and PubSub still require dedicated modules.
