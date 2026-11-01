# State machines, FileType and process contracts

## Concepts

Design deep-dive. StateMachineType makes states and transitions explicit; CurrentState and LastTransition are navigable data. FileType exposes Open/Read/Write/Close with a handle, Size and permissions. These contracts avoid invented methods and blobs without shared semantics.

## Experiment

Extend the reactor cycle into Idle, Heating, Holding, Completed and Fault. Define which commands are legal and which transitions generate events. The current version uses a Running Boolean.

## In the C# code

For the recipe file use a FileType object and server limits on size and number of handles. Separate loading, validation of the content and activation of the recipe. The parser must reject an incomplete recipe before activating it.

## Exercise and verification

Design a chunked download and the cleanup of the handles when the session ends. Write a transition table with preconditions and outputs; generate an event for every transition.

## Mistake to avoid

A file transferred correctly is not necessarily a valid recipe. Do not confuse the transfer with the commit of the process configuration.

## Goals and integrated scenario

Estimated time: 2–3 hours. State machine and FileType are extensions to be implemented; the current server uses Running and does not publish a file object. The scenario is: transfer a recipe, validate it, make it active and start a cycle of states.

## Define the states before the buttons

| State | Start | Stop | Event or exit condition |
|---|---|---|---|
| Idle | Accepts a valid recipe | No effect, or a documented outcome | Start → Heating |
| Heating | Rejects | Returns to Idle | Target reached → Holding |
| Holding | Rejects | Returns to Idle | Hold time → Completed |
| Completed | New cycle according to policy | Returns to Idle | Reset or new start |
| Fault | Rejects | Safe-stop policy | Authorized reset after removing the cause |

The table is a proposed application contract. It must be mapped to an appropriate StateMachineType/FiniteStateMachineType with states, transitions and identifiers. The readable CurrentState and its Id cannot be replaced by a free string that changes with the language.

For the process of the lab, avoid transitions caused by a single noisy reading: define a tolerance, the quality required and possibly a stability time.

## Separate the process model from the NodeManager

A domain state engine can expose the current state and a transition function; the NodeManager translates the effects into node updates and events. Do not scatter ifs on Running in every callback.

**Solution of the design exercise.** Centralize the transitions, validate command and state, update the state as a coherent operation and notify the change. The domain test verifies the table; the UA test verifies that CurrentState, the output of Call and the event represent the same transition.

For an error do not limit yourself to throwing an exception from the timer: the process must enter an observable state and, where appropriate, activate a condition.

## FileType: transfer protocol

FileType exposes standard methods based on a file handle. Open requires a mode, Read uses the handle and a length, Close releases the resource. The node exposes information such as Size and write capability.

Client flow to adapt to a future fileNode and to MethodIds resolved through browse; these nodes are not present today:

```csharp
uint handle = (uint)session.Call(fileNode, openMethod, (byte)1)[0];
try
{
    byte[] chunk = (byte[])session.Call(
        fileNode, readMethod, handle, 4096)[0];
    // Accumulate the chunk and continue until the end, respecting limits and state.
}
finally
{
    session.Call(fileNode, closeMethod, handle);
}
```

The byte literal of the mode and the UInt32 of the handle must respect the signature. The finally block is essential, but a remote Close can fail: the server must have a handle cleanup policy when the session expires.

## Transfer does not mean activation

To load a recipe use staging: transfer the content, close the file, validate schema, values and version, then call an activation command. Do not change the target as soon as the first bytes arrive.

Define the maximum size, a checksum if required, permissions and handle limits. A file name supplied by the client must not become an arbitrary path of the host filesystem.

## Solved exercise: interrupted upload

The client drops after half of the file. The current recipe must stay active and intact; the partial content remains in staging or is deleted according to policy.

**Verification.** The checksum of the active recipe does not change; no transition starts from the incomplete file; the handle does not stay open indefinitely; a retry can create a new staging without corrupting the one already validated.

References: [Information Model, Part 5](https://reference.opcfoundation.org/Core/Part5/), [File Transfer, Part 20](https://reference.opcfoundation.org/Core/Part20/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
