# Reconnection and data continuity

## Concepts

TCP, SecureChannel, Session and Subscription have different times and recoveries. Recreating a session is simple but loses the previous context. Reactivating a session or transferring subscriptions can keep more continuity if server and identity allow it. Republish can recover messages still in the buffer, not create samples that were never kept.

## Experiment

With the web app open, stop the server, wait for the failed KeepAlive, restart. The labels become unavailable and are repopulated by new subscriptions. No command is queued automatically to be executed after the reconnection.

## In the C# code

UaConnection sets KeepAliveInterval=2000 and rebuilds session and items after 3 s. Snapshot emptied, warning logged, commands protected by a SemaphoreSlim. It implements no SessionReconnectHandler, no transfer and no manual Republish.

## Exercise and verification

Make the retry exponential with jitter and a limit, add reconnect counters and the timestamp of the last data. Simulate an interrupted network without stopping the server and compare with a server restart.

## Mistake to avoid

Do not show the last value as Good just because the renderer is still alive. For lost events a persistent log or ConditionRefresh is needed, depending on the type of information.

## Goals, prerequisites and expected result

Estimated time: 120 minutes. You must have understood lessons 02, 07 and 11. The goal is not only to make Connected=true come back: it is to decide what happens to quality, commands in progress, events and the identity of the nodes during a failure.

Open [UaConnection.cs](../../../src/OpcUaCourse/UaCourse.Web/Services/UaConnection.cs). The sample implements **recreation of session and subscription**. It does not transfer subscriptions and does not promise the absence of data loss.

## The actual strategy, step by step

1. The service opens a Session and configures KeepAliveInterval=2000.
2. A KeepAlive handler signals a Bad result through a TaskCompletionSource.
3. The loop waits for that signal or for the cancellation of the service.
4. The finally block acquires the command gate, removes the active session and empties the snapshot.
5. It attempts to close the subscription/session and Dispose.
6. After three seconds the loop resolves namespace, nodes and items again.

The two-second delay is not a guarantee that the interface becomes offline within two seconds: request timeouts, the scheduler and network conditions matter. During teardown the remote close services can fail; the local cleanup must continue anyway.

A Bad KeepAlive signal followed by a normal exit from lost.Task does not necessarily enter the catch: the Status description can remain the previous one while Connected is false. The renderer must consider Connected, not interpret a string as the authoritative state.

## Which state can be recovered?

| Event | Simple choice of the sample | Possible more advanced strategy |
|---|---|---|
| Channel interrupted, server alive | Recreate the session | Reactivate the existing one |
| Session available, client replaced | New subscriptions | TransferSubscriptions if authorized/supported |
| Publish message lost | No explicit application recovery | Republish from the available buffer |
| Server restarted | Recreate everything | New discovery and model rebuild |
| Active alarm after reconnect | Not available in the sample | ConditionRefresh |
| Transient event during an outage | Can be lost | Persistent log and application recovery |

Do not overlap these strategies. ConditionRefresh rebuilds retained conditions, not a complete history of events.

## Reproducible lab

1. Open the web app and note a source timestamp.
2. Stop the server process, leaving ASP.NET running.
3. Wait for the unavailability: the labels show — and the buttons are disabled.
4. Restart the server.
5. Wait for new labels with Good quality.
6. Read a structure and use Stop to verify that the command path has been rebuilt too.

Repeat with a freshly opened console client and compare the NamespaceArray. The client must resolve the URI after every new session, not reuse a NodeId with an old namespace index.

## Solved exercise: retry with backoff and jitter

C# snippet to integrate in place of the fixed delay, not already present in the service:

```csharp
static TimeSpan RetryDelay(int failures)
{
    int attempt = Math.Clamp(failures, 0, 10);
    double ceilingMs = Math.Min(30_000, 1_000 * Math.Pow(2, attempt));
    double delayMs = ceilingMs * (0.5 + Random.Shared.NextDouble() * 0.5);
    return TimeSpan.FromMilliseconds(delayMs);
}
```

**Solution.** Increment failures on failures, reset it only after a connection considered stable and use Task.Delay(delay, stoppingToken). If you reset the counter as soon as Session.Create succeeds, an endpoint that loses the connection immediately can cause attempts that are too frequent.

## Concurrency and data from a previous session

A callback already in progress can finish during the cleanup. For a robust client assign a generation id to every connection and accept updates only from the active generation. The sample does not implement this check yet: emptying the dictionary alone does not prove that no old callback can write after the emptying.

Do not automatically queue Start when offline. A command may already have had an effect before the timeout; repeating it must follow an idempotent contract or a correlation id.

**Completion criterion of the exercise.** No command uses a disposed session, no old update is shown as current, the retry is cancellable and the client declares any loss of events.

References: [Session Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.7), [Subscription Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.14).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
