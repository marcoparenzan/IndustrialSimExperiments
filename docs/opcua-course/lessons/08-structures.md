# Variant, arrays, ExtensionObject and standard structures

## Concepts

Variant carries UA built-in types, scalars and arrays. ValueRank describes whether a value is a scalar or an array and how many dimensions it has. ExtensionObject encapsulates a structure with its encoding identifier and either an encoded or an already decoded representation. Not every ExtensionObject can be cast right away to a known C# class.

## Experiment

Run client types and the Read structure and array button. Recipe contains the standard Range Low=20, High=90; Profile is a Double[3]. EngineeringUnits uses EUInformation, another standard structure.

## In the C# code

The RunTypes lab uses ExtensionObject.ToEncodeable and verifies Range. The Recipe value is a demonstration of a standard structure, not a custom RecipeDataType. The array has DataType Double and ValueRank OneDimension, not a DataType called DoubleArray.

## Exercise and verification

Read a subset of Profile with IndexRange 1:2. Add a matrix and check ValueRank/ArrayDimensions. Handle an ExtensionObject with an unrecognized binary Body without losing the payload.

## Mistake to avoid

A JSON string is not an interoperable UA complex type. Keep the hierarchy of variables and the single structured value separate: they have different semantics and notification granularity.

## Goals and lab

Estimated time: 90 minutes. Read [RunTypes.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunTypes.cs) and use client types. This lesson really runs arrays and **standard** structures. The custom RecipeDataType is the next step, in lesson 09.

## Scalar-ness and type of the value

A Double DataType does not say whether the value is a scalar or an array. ValueRank describes this dimension of the contract. Temperature uses Scalar; Profile uses OneDimension. ArrayDimensions can add information about the dimensions, but the effective dimension must still be considered at runtime.

Variant keeps the information needed for the UA encoding. Avoid converting everything to strings in the communication layer: you would lose the differences between Int32, Double, Boolean, arrays, DateTime and structures.

## Decoding a known structure

The server assigns Recipe an ExtensionObject with a Range body:

```csharp
var encoded = new ExtensionObject(
    new Opc.Ua.Range { Low = 20, High = 90 });
```

The client of the lab does this:

```csharp
DataValue value = session.ReadValue(
    Lessons.Id(session, "Reactor1.Recipe"));
var extension = value.Value as ExtensionObject;
var decoded = ExtensionObject.ToEncodeable(extension);
if (decoded is not Opc.Ua.Range range)
    throw new InvalidOperationException("Range not recognized");
Console.WriteLine($"{range.Low}..{range.High}");
```

It is not an arbitrary cast of bytes to a class. Range is a standard type known to the SDK. For an unknown type the Body can remain encoded: you must load/register the contract or handle the payload explicitly.

On the wire, the identifier field of a structured ExtensionObject identifies the encoding of the structure. Do not confuse it with the NodeId of the DataType: this distinction becomes essential for custom types.

## Partial read of an array

Snippet to try in a lab with an open session:

```csharp
var request = new ReadValueId
{
    NodeId = Lessons.Id(session, "Reactor1.Profile"),
    AttributeId = Attributes.Value,
    IndexRange = "1:2"
};
session.Read(null, 0, TimestampsToReturn.Both,
    new ReadValueIdCollection { request }, out var values, out _);
if (StatusCode.IsBad(values[0].StatusCode))
    throw new ServiceResultException(values[0].StatusCode);
Console.WriteLine(string.Join(", ", (double[])values[0].Value));
```

The indices are zero-based and the end of the range is inclusive. For [20,40,60] the expected result is [40,60]. A range outside the dimensions can return a specific error; do not read an empty array as an automatic confirmation of success.

## Hierarchy or structure?

A Recipe object with three child variables allows access and notifications per field. A Variable with a structured value instead represents a composite value; the notification concerns that value. The choice depends on the contract: static metadata, update granularity, snapshots and the services required.

Neither choice makes different operations automatically atomic. If you need to update a recipe and activate it as a unit, use an explicit commit semantics.

## Solved exercise

The client receives an unknown ExtensionObject. Can it replace it with a JSON string built from the bytes?

**Solution.** No: the bytes have an encoding determined by the contract. Keep the encoding identifier and the body, report the type as unavailable and load the correct model/factory. Only after decoding can you project the datum into JSON for a view or an API.

**Self-check.** Why does Profile have DataType Double? Because the elementary type is Double and the rank describes the array. Why is Recipe not a real application recipe? Because it publishes only a Range, with no name, steps or version.

References: [ExtensionObject, Part 6](https://reference.opcfoundation.org/Core/Part6/v105/docs/5.2.2.15), [DataTypes, Part 3](https://reference.opcfoundation.org/Core/Part3/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
