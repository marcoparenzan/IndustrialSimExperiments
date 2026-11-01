# Roadmap

## Available in the first version

- Server and console client, modular water-plant/reactor synoptics, subscriptions, Write and Call.
- Address space, ObjectTypes, attributes, namespace URIs, arrays and the standard UA structure.
- Simple events, encrypted channel, service errors, reconnection by recreating the session.
- Teaching path with automatic labs and deep-dive projects.
- Neutral home page and dashboards with dedicated URLs; responsive UA Lab template with navigation generated from the catalog.

## Evolution of the model

1. Extract protocol and contracts into a library; separate the physical simulation from the NodeManager mapping.
2. Complete ReactorType and create PumpType with individual objects and methods. Criterion: two instances with the same relative BrowseNames and independent behavior.
3. RecipeDataType with StructureDefinition, encoding, an array of steps, a method and independent decoding is available. Extend it with enums/optional fields and a reproducible ModelCompiler generation; the current NodeSet2 is a partial runtime export.
4. Extend the declarative commands already available to the methods of the individual pumps, with arguments and feedback per hotspot. Criterion: no UA logic in the graphic component and bindings validated at startup.

## Advanced services

5. AlarmConditionType with Active/Acked/Confirmed, ConditionRefresh and retained branches. Criterion: recovery of the active alarms after a reconnect.
6. History with raw HistoryRead, continuation points and a persistent archive. Criterion: pagination and ordered samples with quality and timestamp.
7. FileType to transfer recipes, StateMachineType for the cycle, and modeling with an appropriate companion specification.
8. PubSub MQTT/JSON and UDP/UADP, using a separate lab and an explicit broker. Criterion: correlation of the fields and the DataSet metadata.

## Robustness and management

9. User identity, roles and audit; web HTTPS/authentication; a policy for every command.
10. Reconnect with backoff and jitter, optional reactivation/transfer of the subscriptions, loss counters and diagnostics.
11. External endpoint/PKI configuration, administrative trust, certificate renewal and a GDS lab.
12. Automated browser tests, server restart, queue overflow, load and interoperability with third-party clients.

The course can be extended by modules: today the deep-dives describe contracts, experiments and completion criteria, but they do not promise support that this server already implements.
