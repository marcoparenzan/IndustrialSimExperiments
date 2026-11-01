# Read, Write, quality, timestamps and batches

## Concepts

Read can request several attributes of several nodes in the same call. Every result has its own StatusCode. MaxAge is a freshness request, not a polling rate. SourceTimestamp indicates the time attributed to the datum by the source; ServerTimestamp the time associated by the server with handling the datum. Non-Value attributes can have different or empty timestamps.

## Experiment

Run client read, write and errors. Writing Setpoint=65 uses Double; 999 produces BadOutOfRange, a string produces BadTypeMismatch and Temperature produces BadNotWritable.

## In the C# code

PlantNodeManager sets AccessLevel and UserAccessLevel and validates Write in the OnWriteValue callback. Lessons.Write checks the StatusCode of the individual result. The UI shows the outcome and receives the new value through the subscription.

## Exercise and verification

Add a batch write of two nodes with one valid and one invalid value. Verify both results and the readback, without presuming atomicity.

## Mistake to avoid

A batch Write is not a transaction. Do not use double.NaN as a normal setpoint: the course server explicitly rejects it.

## Goals and labs

Estimated time: 90 minutes. You will learn to distinguish the service result from the operation results, validate values and verify the readback. Read [RunRead.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunRead.cs), [RunWrite.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunWrite.cs) and [RunErrors.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunErrors.cs).

## A batch read is a list of operations

ReadValueId does not only indicate a node: it specifies AttributeId, an optional IndexRange and DataEncoding. RunRead reads four attributes of the same Variable. The list of results keeps the correspondence with the order of the requests.

```csharp
var requests = new ReadValueIdCollection
{
    new() { NodeId = Lessons.Id(session, "Reactor1.Temperature"),
            AttributeId = Attributes.Value },
    new() { NodeId = Lessons.Id(session, "Reactor1.Setpoint"),
            AttributeId = Attributes.Value }
};
session.Read(null, 0, TimestampsToReturn.Both, requests,
    out var results, out var diagnostics);
for (var i = 0; i < results.Count; i++)
    Console.WriteLine($"{requests[i].NodeId}: {results[i].StatusCode}");
```

A service error can prevent a valid response; with a valid response one of the operations can still fail. Showing only the first StatusCode or catching only exceptions loses this granularity.

MaxAge=0 requests an attempt to obtain a current value according to the behavior of the server; it does not certify synchronous access to a physical sensor. Here the value is kept in memory by the simulator. TimestampsToReturn.Both requests both timestamps, but the non-Value attributes must not be interpreted as physical measurements.

## Writing and server-side validation

The request uses Double explicitly:

```csharp
var status = Lessons.Write(session,
    Lessons.Id(session, "Reactor1.Setpoint"), 65.0);
if (StatusCode.IsBad(status))
    throw new ServiceResultException(status);
```

65 and 65.0 are not equivalent from the point of view of the Variant: the first C# literal is Int32, the second Double. Do not assume universal automatic conversions by the server.

PlantNodeManager combines the permissions of the node with a validation callback: it rejects wrong types, non-finite numbers and targets outside 20..90. The UI has min/max, but this server-side validation remains indispensable because other clients exist.

The readback confirms the published state; it does not prove that the physical process has reached 65 °C. Setpoint is the desired value, Temperature is the dynamic response. Keeping these two indicators separate is part of the application semantics.

## Solved exercise: a partially successful write

Build a WriteValueCollection with Setpoint=64.0 and Temperature=50.0. For the first node expect Good; for the second BadNotWritable. After the call read both.

**Solution.** The setpoint must be 64. The temperature keeps depending on the simulation. Do not perform an invented rollback on the server: if the domain requires atomicity, expose an application Method that validates the whole operation and defines the commit behavior.

| Case | Outcome of the sample |
|---|---|
| Setpoint=65.0 | Good |
| Setpoint=999.0 | BadOutOfRange |
| Setpoint="wrong" | BadTypeMismatch |
| Temperature=50.0 | BadNotWritable |
| Nonexistent node | BadNodeIdUnknown |

The errors lab also verifies the exception raised by the ReadValue helper for a nonexistent node. Do not attribute this handling style to all Read overloads.

**Self-check.** Must a Bad value always be replaced by zero? No: zero is a plausible process value and would confuse missing data with a real measurement. Keep the quality separate and display the unavailability.

Reference: [Attribute Service Set](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.11).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
