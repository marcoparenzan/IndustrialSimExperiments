# ObjectType, VariableType and modeling rules

## Concepts

An ObjectType is a navigable contract, different from a C# class. ReactorType derives from BaseObjectType, declares Temperature and marks it Mandatory. A property of the instance that is not declared in the type can be an extension; a conforming instance must materialize the mandatory children. ModellingRule is not a simple application attribute: it is a reference to a standard object.

## Experiment

Use client browse for the instance and a generic client for ReactorType. Compare HasTypeDefinition, HasSubtype and HasModellingRule. EngineeringUnits is a Property distinct from the Value of the temperature.

## In the C# code

CreateAddressSpace creates the type and then the instance. The manual construction makes the details visible; the initial type declares only Temperature. The next step is to complete all the declarations of the contract.

## Exercise and verification

Declare Setpoint, Running and the methods in the type. Create two independent instances. Define what is Mandatory and what is Optional; document the compatibility when you add children.

## Mistake to avoid

A folder with well-ordered names does not replace a typed model. The visual hierarchy alone does not express units, meaning and behavior.

## Goals and prerequisites

Estimated time: 90–120 minutes. After browse and Read, you learn to design a reusable contract. The lab consists in comparing type and instance in [PlantServer.cs](../../../src/OpcUaCourse/UaCourse/PlantServer.cs); there is not yet a dedicated command that certifies the complete conformity of the model.

## Two type systems, two roles

A CLR class determines layout and behavior in your process. A UA ObjectType is a node that an independent client can navigate. Having a Reactor class with ten properties does not publish those properties in the UA contract; conversely, a UA instance can be built from NodeState without an equivalent CLR class.

An ObjectType can declare Variables, Objects and Methods. A VariableType defines characteristics of a variable; the DataType of its value remains a distinct concept. EngineeringUnits is a Property of type EUInformation, not the type of Temperature.

## Building and linking the type

Excerpt of the construction of our type:

```csharp
var reactorType = new BaseObjectTypeState
{
    NodeId = new NodeId("ReactorType", NamespaceIndex),
    BrowseName = new QualifiedName("ReactorType", NamespaceIndex),
    DisplayName = "ReactorType",
    IsAbstract = false,
    SuperTypeId = ObjectTypeIds.BaseObjectType
};
reactorType.AddReference(
    ReferenceTypeIds.HasSubtype, true, ObjectTypeIds.BaseObjectType);
```

This snippet belongs in the context of the NodeManager: NamespaceIndex and AddPredefinedNode are services of the manager, not global variables. The server must also make the link from the supertype to the subtype navigable. For references to nodes managed by another manager, the sample uses externalReferences.

Assigning SuperTypeId alone does not replace every model registration operation. Likewise, assigning TypeDefinitionId to the instance does not automatically create all the declared children.

## Mandatory and Optional

The Temperature declaration uses ModellingRule_Mandatory. It means that conforming instances must have the corresponding component. It does not mean that the sensor cannot fail, that the value is always Good or that the component is writable.

The distinction concerns the structure of the instance. Optional allows a capability that some instances have and others do not. For an adaptive client it is better to discover the existence of the capability than to assume that every device exposes it.

In the current type only Temperature is declared. Setpoint, Running and the methods exist in the instance as extensions: do not pretend that the initial type is already the complete industrial contract.

## Guided lab

1. Navigate from BaseObjectType to the ReactorType node.
2. Navigate the components of ReactorType: observe Temperature.
3. Read DataType and ValueRank of the declaration.
4. Navigate HasModellingRule and verify Mandatory.
5. Navigate Reactor1 and compare its Temperature.
6. Verify the HasTypeDefinition of Reactor1.

Conformity is not limited to equality of the name: check the qualified name, NodeClass, type, rank and the rule required by the contract.

## Solved exercise: two reactors

Design Reactor2. The Temperature of the two instances must have the same relative BrowseName, but a distinct NodeId. The Start method must act on the object passed in the Call and on the state of the correct instance.

**Architectural solution.** Extract an instance builder that receives an identifier and the domain state. Create nodes prefixed with Reactor1 or Reactor2; keep the per-instance variables in a dictionary. Avoid a single shared running field, which would start both reactors together. Register both instances with the same TypeDefinitionId.

**Completion criterion.** Start Reactor1 and verify Reactor2.Running=false; then repeat swapping the objects. A client must resolve Temperature with the same relative path on both instances.

References: [Part 3](https://reference.opcfoundation.org/Core/Part3/), [Information Model, Part 5](https://reference.opcfoundation.org/Core/Part5/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
