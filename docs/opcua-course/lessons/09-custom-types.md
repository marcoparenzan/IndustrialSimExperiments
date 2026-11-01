# Custom types: RecipeDataType, encoding and a dynamic client

This lesson implements a custom UA type, a structured Variable, a Method that receives the structure and a subscription on the composite value. It includes two clients: one knows the C# class; the other builds the type from the metadata of the server, without depending on the simulator's DLL.

## Goals, prerequisites and code

Estimated time: 2–3 hours. Lessons 03, 06 and 08 are needed. At the end you must be able to tell a DataTypeId from an encoding id, keep definition and encoding consistent, and verify that a rejected recipe does not change the state.

| Artifact | Responsibility |
|---|---|
| [RecipeData.cs](../../../src/OpcUaCourse/UaCourse/Models/RecipeData.cs) | Encodeable class, fields, Clone and validation |
| [RecipeNodeModel.cs](../../../src/OpcUaCourse/UaCourse/Models/RecipeNodeModel.cs) | DataType, StructureDefinition, encoding and LoadRecipe |
| [RunCustomTypes.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunCustomTypes.cs) | Client assertions, Read, Call and Publish |
| [DynamicClient](../../../src/OpcUaCourse/UaCourse.DynamicClient/Program.cs) | Decoding from metadata without RecipeData |
| [UaConnection](../../../src/OpcUaCourse/UaCourse.Web/Services/UaConnection.cs) | Commands and reads of the Blazor panel |

The encodeable code is written by hand to make the contract visible. Generation with ModelCompiler remains an evolution, not a step needed to start this lab.

## Preparation and execution

From the repository root, restore the dependencies and build. Stop the processes that use the same build outputs before rebuilding on Windows.

```powershell
dotnet restore src/ForgeIndustrial.slnx
dotnet build src/ForgeIndustrial.slnx --no-restore
dotnet run --project src/OpcUaCourse/UaCourse --no-build -- server
```

In a second terminal:

```powershell
dotnet run --project src/OpcUaCourse/UaCourse --no-build -- client custom-types
dotnet run --project src/OpcUaCourse/UaCourse.DynamicClient --no-build
```

The first command verifies the behavior and restores the previous recipe. The second uses only the SDK client and ComplexTypes: its project does not reference UaCourse and does not compile RecipeData.

With no server running, `verify` starts its own server and runs thirteen labs, including this one.

## 1. Defining a precise contract

| Field | UA DataType | ValueRank | Rule |
|---|---|---|---|
| Name | String | Scalar | Not empty, at most 128 characters |
| Revision | UInt32 | Scalar | Greater than zero |
| Target | Double | Scalar | Finite, between 20 and 90 |
| Steps | Double | OneDimension | From 1 to 64 elements, all finite and between 20 and 90 |

Steps is a target profile that is carried and stored. The current simulator **does not run a timed sequence of the steps**: LoadRecipe sets Setpoint to the Target field. Do not confuse the transport of the recipe with the implementation of its process cycle.

The Variable `Reactor1.BatchRecipe` uses the new DataType. The old `Reactor1.Recipe` stays the standard Range of lesson 08, so the two examples are comparable.

## 2. Publishing the DataType and the encoding

The application identifiers are distinct:

```text
urn:uacourse:plant / s=RecipeDataType
urn:uacourse:plant / s=RecipeDataType.Binary
```

The first identifies the type of the value; the second its binary encoding. The client resolves the namespace index in every session. The numeric index is not part of the stable contract.

RecipeNodeModel creates a DataTypeState with SuperTypeId=Structure and a DataTypeDefinition that contains a StructureDefinition. DefaultEncodingId points to the Default Binary node; the fields are declared in the order Name, Revision, Target, Steps.

HasEncoding is an explicit reference from the DataType to the encoding object. It is not enough to treat the encoding as a normal hierarchical child: the lab checks that HasEncoding is navigable and the dynamic client uses it to rebuild the contract.

## 3. Encoding and decoding in the same order

Excerpt of the class actually used:

```csharp
encoder.WriteString(nameof(Name), Name);
encoder.WriteUInt32(nameof(Revision), Revision);
encoder.WriteDouble(nameof(Target), Target);
encoder.WriteDoubleArray(nameof(Steps), Steps);
```

Decode reads the same types in the same order. Do not reorder these fields to improve the look of the class: the order is part of the binary representation. The PushNamespace/PopNamespace functions delimit the encoding context.

TypeId and BinaryEncodingId are ExpandedNodeIds with a stable URI. This version exposes only the custom binary encoding; XmlEncodingId is null. It does not declare custom XML or JSON encodings that it does not implement in the model.

The class implements IEncodeable and Clone also copies the array. A shallow clone would share Steps, allowing a local change to alter a recipe already stored in memory. Decode normalizes a null array to an empty array, which validation then rejects.

## 4. Receiving the structure in a Method

The method `LoadRecipe(Recipe: RecipeDataType) → AcceptedRevision: UInt32` exposes its signature in the InputArguments and OutputArguments properties.

The static client builds a recipe:

```csharp
var recipe = new UaCourse.Models.RecipeData
{
    Name = "Lab09",
    Revision = 42,
    Target = 66,
    Steps = [20, 35, 50, 66]
};
var output = session.Call(
    Lessons.Id(session, "Reactor1"),
    Lessons.Id(session, "Reactor1.LoadRecipe"),
    new Opc.Ua.ExtensionObject(recipe));
```

The callback accepts the encodeable body, checks all the rules before changing the state, keeps a clone, updates Setpoint, returns the Revision and emits an event. All of this happens inside the Lock of the NodeManager.

BadTypeMismatch indicates that the body is not the expected type; BadOutOfRange indicates a recipe that violates the application rules. Do not describe an empty Name as a partial success: the whole operation is rejected.

## 5. Verifying the subscription too

The lab creates a MonitoredItem on BatchRecipe before the Call. It ignores the initial notification and waits for a recipe with the name and revision of the request sent. Then it compares all the fields, including the array.

This proves more than a local cast: the value goes through the TCP server and Publish. However, the static client still knows the class and could mask missing metadata. That is why the second client is needed.

## 6. Independent decoding

DynamicClient uses `ComplexTypeSystem(session).Load()` to load the available definitions. It does not register RecipeData in the factory. The CLR type printed is generated at runtime:

```text
Generated CLR type: Opc.Ua.ComplexTypes.plant.<index>.RecipeDataType
Name = Standard
Revision = 1
Target = 60
Steps = 20, 40, 60
OK: recipe decoded without the server assembly and without RecipeData
```

The name of the generated type and the index can change; the values reflect the current recipe. The client uses IComplexTypeProperties to read the fields, not a cast to the class of the server.

The certificate configuration is shared as a neutral source file: it neither contains nor registers the recipe class. The dynamic process and its factory are independent of the server process.

## 7. NodeSet2 and the Blazor synoptic

At startup the server exports `<artifacts root>/models/Recipe.NodeSet2.xml` from the same definition used at runtime. It is a **fragment of the model** containing the type and the encoding; it is not the whole address space and it is not yet a generated/versioned model with all the RequiredModel dependencies.

In the file the namespace can be ns=1 while in the server it is ns=2: the respective NamespaceUris explain the difference. An importer must remap the indices.

The Blazor panel adds name and revision, `Load RecipeDataType · Call` and `Read custom recipe`. The target of the panel is put in the Target field and in the profile [20,40,target]. The responses report the accepted revision or the structure read.

## Exercises with solution and verification

**Invalid recipe.** Insert a NaN step. The Call must fail with BadOutOfRange and the readback must equal the previous recipe. RunCustomTypes performs this test.

**Clone.** Clone a recipe and change Steps[0] in the copy. The original must not change. The solution is the copy of the array in Clone, not MemberwiseClone alone.

**Unknown type.** Remove the static registration from the client of a copy of the project: a body that is not decoded must not be turned into JSON by guesswork. The available solution is the dynamic client that loads the contract before the Read.

**Evolution.** Add a field to the model in an exercise branch. You must update the definition, Encode, Decode and the tests; consider new identifiers when the layout is incompatible. Do not keep the same ids while changing the meaning of the fields and presume compatibility.

## Limits and next increment

The lab verifies static/dynamic decoding, metadata, Call, Read and subscription. It does not yet include optional fields, unions, custom enums, import of the NodeSet into the server or ModelCompiler generation. The next phase is to make the generation reproducible and to verify compatibility with a previous version of the model.

References: [DataTypeDefinition, Part 3](https://reference.opcfoundation.org/Core/Part3/), [ExtensionObject, Part 6](https://reference.opcfoundation.org/Core/Part6/v105/docs/5.2.2.15), [official SDK](https://github.com/OPCFoundation/UA-.NETStandard) (package version 1.5.376.244).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
