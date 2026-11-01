# Address space, references and namespaces

## Concepts

The address space is a graph. Organizes expresses organization, HasComponent composition, HasProperty metadata, HasTypeDefinition the type of the instance and HasSubtype inheritance. BrowseName is a QualifiedName used in paths; DisplayName is localizable and serves presentation. NodeId identifies a node in the context of the server.

## Experiment

Run client browse. The server limits the first result to two references: BrowseNext walks the continuation points until they are exhausted.

## In the C# code

Lessons.Id consults NamespaceUris using urn:uacourse:plant. Do not store an ns=2 index as a portable identifier: it can change after modifications or a reconnect. Store the namespace URI and the identifier.

## Exercise and verification

Add Reactor2 with the same type and the same relative names. Implement a recursive browse with a visited set, a depth limit and the release of the continuation points if the user interrupts.

## Mistake to avoid

A graph can have several parents and cycles. Do not assume that Browse returns a tree or all the references in one response.

## Goals and starting point

Estimated time: 90 minutes. You will learn to read a graph of nodes, handle pagination and resolve relative paths. Labs: [RunBrowse.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunBrowse.cs) and [RunPaths.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunPaths.cs).

```powershell
dotnet run --project src/OpcUaCourse/UaCourse -- client browse
dotnet run --project src/OpcUaCourse/UaCourse -- client paths
```

## Identity, name and presentation

A NodeId combines a namespace index and an identifier, which can be numeric, string, Guid or ByteString. The NamespaceArray of the session links the index to its URI. An ExpandedNodeId can carry further information: do not convert it to a NodeId without considering URI and server index.

BrowseName is a QualifiedName. The same string Temperature in two namespaces can identify different qualified names. DisplayName is instead a LocalizedText intended for the user: translating it must not break an application path.

In the course we use readable string identifiers, such as Reactor1.Temperature. The dot does not impose a hierarchy on the protocol: it is a convention of our identifier. The real hierarchy depends on the references.

## Browse step by step

The lab asks for forward hierarchical references and a maximum of two results per response:

```csharp
session.Browse(null, null, Lessons.Id(session, "Reactor1"), 2,
    BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
    true, 0, out var continuationPoint, out var references);
```

HierarchicalReferences is a family of types; includeSubtypes=true includes the derived references. NodeClassMask=0 does not restrict the results to a single class. It does not mean that every node is a Variable: you will also find the methods.

The response contains references, not current values. For each ReferenceDescription you can observe ReferenceTypeId, IsForward, BrowseName, NodeClass and TypeDefinition. To read Temperature a subsequent Read of the node found is needed.

The continuation point is an opaque token of the server. When it is present, call BrowseNext, accumulate the references and continue until it is no longer returned. If you interrupt the exploration before the end, release the token with releaseContinuationPoints=true. In our complete path the tokens are consumed until exhaustion.

## Relative paths

RunPaths builds two RelativePathElements: Reactor1 and Temperature, both qualified in the application namespace. The StartingNode is Plant. The expected result is a single target and status Good:

```text
Plant → Reactor1 → Temperature
Translate: Good, targets=1
ns=<index of the session>;s=Reactor1.Temperature
```

The service can return several targets or a path that is not completely resolved. In a general client also check RemainingPathIndex and reject remote targets if your application does not handle them. The single assertion of the lab is enough for this known model, not for any address space.

## Solved exercise: exploration without cycles

Design a recursive browse. Do not use depth as the only protection: two objects can refer to each other.

```csharp
var visited = new HashSet<string>();
bool FirstVisit(ExpandedNodeId id)
    => visited.Add(id.ToString());
```

This snippet illustrates the check; in a multi-server application keep a normalized identity with namespace URI and server. Add a depth limit, a maximum number of nodes and a CancellationToken. Every Browse call must manage its own continuation points.

**Solution.** Visit each node only once for the required traversal, but keep the edges separately: skipping a repeated visit does not authorize you to delete a significant reference from the graph shown.

**Diagnostics.** BadNoMatch during Translate suggests a wrong BrowseName, namespace, direction or reference filter. Do not automatically fall back to a Read with a NodeId built by guesswork.

References: [Address Space Model](https://reference.opcfoundation.org/Core/Part3/), [View Service Set](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.9).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
