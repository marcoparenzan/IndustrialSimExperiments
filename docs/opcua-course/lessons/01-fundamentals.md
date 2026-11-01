# From the tag to the information model

## Concepts

An OPC UA datum is not just a number. A DataValue associates Value, StatusCode, SourceTimestamp and ServerTimestamp. A Variable exposes type and access attributes; an Object groups components; a Method describes a call. The client can discover this information without knowing a register map. Our cell uses Reactor1 as the object, Temperature as the variable and Start as the method.

## Experiment

Open the two synoptics, start Reactor1, watch the temperature and the totalizer. The simulation runs in the server even without a browser; opening a second window does not create a second plant.

## In the C# code

Follow Program.cs from the server command to PlantNodeManager.CreateAddressSpace; compare it with UaConnection.ExecuteAsync. The browser uses SignalR, not UA TCP.

## Exercise and verification

Draw a motor with speed, current, state and Start. Which are values, which properties and which operations?

## Mistake to avoid

A read can succeed with Uncertain or Bad quality; do not treat a successful transport response as confirmation of Good quality.

## Goals and lab setup

Estimated time: 60–90 minutes. You need the basics of C#, but no OPC UA experience. At the end you must be able to tell apart the identity of a node, the type of its value, the quality of the data and an application operation.

Open [PlantServer.cs](../../../src/OpcUaCourse/UaCourse/PlantServer.cs), [Program.cs](../../../src/OpcUaCourse/UaCourse/Program.cs) and [Synoptic.razor](../../../src/OpcUaCourse/UaCourse.Web/Components/Synoptic.razor). Start the server from the repository root first and then the client:

```powershell
dotnet run --project src/OpcUaCourse/UaCourse -- server
# Second terminal
dotnet run --project src/OpcUaCourse/UaCourse -- client read
```

The client does not create a sensor: it asks the server for the attributes of a node that already exists. The browser does not create the industrial process: it represents the state that the server publishes. This separation allows the simulator to be replaced by a real source while keeping, if the contract stays compatible, the same client.

## From the C# class to the UA contract

Imagine this domain class:

```csharp
public sealed class Reactor
{
    public double Temperature { get; private set; }
    public double Setpoint { get; set; }
    public bool Running { get; private set; }
    public void Start() => Running = true;
}
```

The class is not automatically an OPC UA model. It does not describe a stable identity of the members, engineering units, quality, remote permissions or navigable references. An explicit mapping is needed:

| C# domain | UA contract | What a client discovers |
|---|---|---|
| Reactor instance | Object Reactor1 | Components and type of the object |
| Temperature | Variable of DataType Double | Value, access and rank |
| Temperature unit | Property EngineeringUnits | The unit, without naming conventions |
| Start | Method with OutputArguments | Signature and result of the call |
| Reactor model | ObjectType ReactorType | Contract shared by the instances |

ObjectType and DataType do not mean the same thing. The first describes the structure of an object in the address space; the second describes the representation and the meaning of a value.

## Reading a value without losing its semantics

Snippet to insert in a lab with a Session already open:

```csharp
var id = Lessons.Id(session, "Reactor1.Temperature");
DataValue reading = session.ReadValue(id);
if (!StatusCode.IsGood(reading.StatusCode))
    throw new InvalidOperationException($"Quality: {reading.StatusCode}");

Console.WriteLine($"Value={reading.Value}");
Console.WriteLine($"Source={reading.SourceTimestamp:O}");
Console.WriteLine($"Server={reading.ServerTimestamp:O}");
```

In the sample ReadValue is a helper that can throw ServiceResultException for Bad results; in batch Read calls you instead receive the individual DataValues and can classify each result. Do not treat the two APIs as uniform by assuming that they always return an object even on error.

The number can stay identical while the quality changes: a disconnected sensor might keep its last value. A user interface that shows only Value loses this distinction. Our renderer adds quality and timestamp to the tooltip and also considers the availability of the connection.

## Guided experiment and solution

1. Read Temperature before Start: after settling it is about 20 °C.
2. Run client methods: the target is set to 70 and Running becomes true.
3. Repeat client read: the value grows gradually.
4. Close the browser and keep reading from the console: the process goes on.
5. Open two browsers: both observe the same reactor.

**Reasoned solution.** The PlantNodeManager timer updates the process every 500 ms. The rule is `current + (target-current)*0.08`: the model does not reach the target instantly. The state is owned by the server; browser and console are observers and can send commands.

**Self-check.** If you change the color of a label, does the temperature change? No. If the NamespaceIndex changes, does the sensor change? No, as long as URI and identifier stay the same. If Read returns Good, is the datum automatically recent? No: quality and freshness are distinct properties that must be checked.

Reference: [Address Space Model, Part 3](https://reference.opcfoundation.org/Core/Part3/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
