# Methods, arguments and the semantics of commands

## Concepts

Call identifies an ObjectId and a MethodId: the method belongs to an object. InputArguments and OutputArguments describe names, DataType, ValueRank and description. A command can have process preconditions in addition to type validation. BadInvalidState indicates that the command is not valid in the current state.

## Experiment

Run client methods: Stop, ApplyRecipe(70), Start. In the browser apply a recipe and observe the response and the event. Invoke Start twice: the second call is rejected.

## In the C# code

MethodState.OnCallMethod implements the behavior and fills the output list. The server checks the number and types of the arguments, the callback checks range and state. The ApplyRecipe method sets the target; it does not yet update a complex persistent recipe.

## Exercise and verification

Add ResetTotalizer() with a precondition of plant stopped. For long operations return a job identifier and publish state and completion.

## Mistake to avoid

A Call timeout does not prove that the command was not executed. Avoid blind retries of non-idempotent operations; design a correlation id and a queryable result.

## Goals and lab

Estimated time: 90 minutes. Read [RunMethods.cs](../../../src/OpcUaCourse/UaCourse/Labs/RunMethods.cs) and the MethodState callbacks in [PlantServer.cs](../../../src/OpcUaCourse/UaCourse/PlantServer.cs). Run client methods with the server running and observe the new events in the web app.

## A remote call has a public contract

ObjectId identifies the object to operate on. MethodId identifies the method. They are not interchangeable, even though in the sample both contain the string Reactor1.

The signature is not a naming convention: InputArguments and OutputArguments are UA properties that contain an array of Argument. Each Argument describes Name, DataType, ValueRank and Description. A generic client can read the signature before invoking it.

| Method | Input | Output | Rule of the sample |
|---|---|---|---|
| Start | None | Accepted: Boolean | Rejects if already running |
| Stop | None | None | Repeatable stop |
| ApplyRecipe | Target: Double | Target: Double | Finite target, 20..90 |

Stop is idempotent for the Running value, but it still emits a new event at every call: idempotence of the state and equivalence of all the effects are not necessarily the same thing.

## Client: invocation and result

```csharp
NodeId reactor = Lessons.Id(session, "Reactor1");
NodeId apply = Lessons.Id(session, "Reactor1.ApplyRecipe");
IList<object> output = session.Call(reactor, apply, 70.0);
double acceptedTarget = (double)output[0];
Console.WriteLine(acceptedTarget);
```

The Call helper used here returns the outputs and can throw ServiceResultException. For several calls in the same request or for fine-grained diagnostics use the Call service and its CallMethodResults: the status of the call, the results of the individual arguments and the outputs are distinct levels.

A UI should not conclude that the process has reached the target just because acceptedTarget is 70. The method confirms the action on the contract of the server; Temperature is the indicator of the process.

## Server: validation and effect

ApplyRecipe reads input[0], checks finiteness and range, updates Setpoint through Set, fills output[0] and emits an event. The Lock coordinates this behavior with the simulation timer. The SDK checks the compatibility of the request with the signature; the domain checks the legality of the command.

The output array is prepared for the signature: the callback assigns the expected positions. Returning Good without filling a required output produces an ambiguous or invalid contract.

## Timeouts and long commands

The critical case is: the server executed the command, but the response does not arrive. A client timeout does not prove that the action did not happen. Repeating a dosing command can duplicate the operation.

For a long command design StartBatch(CommandId, Recipe) → JobId. The server keeps the CommandId and the result; a retry with the same identifier retrieves the existing job. State, progress and completion become nodes or events. This pattern is an application design, not an SDK API already implemented in the course.

## Solved exercise

Call Stop, Start, Start. The second Start must fail with BadInvalidState, while Running stays true. Then try ApplyRecipe(999.0): the previous target must remain unchanged.

**Solution.** Read the state before and after, catch the ServiceResultException for the rejected command and verify the readback. Do not use the absence of an event as the only confirmation: a subscription can lose notifications.

**Diagnostics.** BadArgumentsMissing suggests a signature that was not respected; BadTypeMismatch indicates an incompatible type; BadInvalidState indicates an application precondition. A disabled button helps the user, but does not replace the server check.

Reference: [Method Service Set](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.12).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
