# PubSub, DataSets and transports

## Concepts

Design deep-dive. Client/server subscriptions use Session, MonitoredItem and Publish. OPC UA PubSub uses Publisher, DataSetWriter, WriterGroup and Subscriber with messages and metadata. UDP/UADP and MQTT/JSON are possible combinations of transport and encoding: they are not a flag change of the Session.

## Experiment

Create a separate lab with an explicit MQTT broker and publish temperatures and levels. The current server does not publish DataSetMessages and Blazor uses client/server.

## In the C# code

Define PublishedDataSet and DataSetMetaData, versions, publisher/writer identifiers and the mapping of the fields. Handle security and broker credentials separately from the UA TCP channel.

## Exercise and verification

Compare fan-out with ten consumers, end-to-end delay, message loss and metadata update. Try a subscriber without a shared C# model.

## Mistake to avoid

An MQTT JSON message with values does not automatically become OPC UA PubSub. Verify the format and metadata required by the standard.

## Goals and required infrastructure

Estimated time: 2–3 hours. A lab separate from the current client/server is needed. For MQTT an explicit broker is necessary; for UDP you need a network where the configured traffic is allowed. The sample does not yet generate PubSub NetworkMessages.

The goal is to understand the configuration of the DataSet, the identity of the publisher, the metadata and the behavior of the consumer, not to call a generic Publish function with a JSON string.

## Comparison of communication models

| Aspect | Client/server of the course | PubSub |
|---|---|---|
| Observation resource | MonitoredItem in the Session | Published DataSet and reader |
| Discovery of the data | Browse and attributes of the node | Metadata/configuration and the related model |
| Sending | Publish of the UA service | NetworkMessage on the chosen transport |
| Read/Write/Call commands | Services available | Not implicit in the DataSet flow |
| Multiple consumers | Sessions/subscriptions | Fan-out according to network or broker |

PubSub can coexist with a client/server server on the same device. Using PubSub for telemetry does not mean giving up an address space for configuration and commands.

## Configuration objects

PublishedDataSet defines which fields to publish. DataSetWriter produces the messages of the dataset; WriterGroup coordinates sending and transport settings; a reader identifies what must be consumed. Identifiers such as PublisherId and DataSetWriterId must be consistent among the participants.

DataSetMetaData describes the fields and their types. The version of the metadata and the order of the fields are part of the contract: the consumer must not attribute the name Pressure to the field that previously represented Temperature just because it occupies the same index.

UDP/UADP and MQTT/JSON are frequent combinations; choose those supported by your SDK and profile. MQTT QoS is a property of the transport, not proof that the source process produced or kept every sample.

## Guided lab

1. Define a DataSet with Temperature, Setpoint and Running.
2. Fix the publisher and writer identifiers, an interval and the mapping of the fields.
3. Configure metadata and transport of the publisher.
4. Configure a distinct subscriber with no references to the classes of the simulator.
5. Compare the value received with a simultaneous client/server Read.
6. Introduce a compatible/incompatible change of the model and observe the version check.
7. Stop the consumer and restart: document what is recovered from the broker and what remains lost.

Use the PubSub library and the samples of the chosen release of the [official SDK](https://github.com/OPCFoundation/UA-.NETStandard). Do not embed APIs of the master branch in the current project without verifying compatibility and dependencies.

## Solved exercise: unknown metadata

The subscriber receives a message with a metadata version it does not know. Must it interpret it anyway with the previous table?

**Solution.** No: keep or discard according to an explicit policy, report the mismatch and recover the necessary contract through the mechanism foreseen by the deployment. An equal number of fields does not prove that types, names and meaning are unchanged.

You can make the check explicit in your domain adapter:

```csharp
static void RequireVersion(uint expectedMajor, uint receivedMajor)
{
    if (receivedMajor != expectedMajor)
        throw new InvalidOperationException("Incompatible metadata");
}
```

This snippet illustrates an application policy; it does not implement the encoding of the OPC UA metadata nor their whole version semantics.

## Security and verification

Broker credentials, TLS and the security of the PubSub messages must be designed with the chosen profile. A certificate trusted by the UA TCP server does not automatically configure an MQTT broker.

Measure latency with the source timestamp, the number of messages, sequences and gaps where available. Verify key frame/delta frame if enabled and the behavior of a subscriber that joins late.

**Completion criterion.** Two independent consumers interpret the same fields, reject an incompatible contract and declare what the system does not recover after an interruption.

Reference: [PubSub, Part 14](https://reference.opcfoundation.org/Core/Part14/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
