# Discovery, endpoints, SecureChannel and Session

## Concepts

Discovery describes endpoints, security policies, modes and user tokens. The SecureChannel protects the messages; the Session keeps the application context. A Subscription belongs to a Session but has a lifecycle of its own. If TCP drops, the session is not necessarily lost immediately: the timeout and the recovery strategies matter.

## Experiment

Run client read. Observe the SessionId and Basic256Sha256. Start a second client and compare the sessions; both see the same server.

## In the C# code

Configuration.CreateAsync creates the application certificate. CoreClientUtils.SelectEndpoint selects a secure endpoint. Session.Create uses an AnonymousIdentityToken: the application has a certificate, the user remains anonymous.

## Exercise and verification

Record the selected endpoint, the security mode and the token policy. Try a port with no server and tell a discovery error from an authentication error.

## Mistake to avoid

Do not choose a None endpoint as a silent fallback when the configuration requires security.

## Goals and prerequisites

Estimated time: 75 minutes. After lesson 01, you learn to follow the cycle discovery → endpoint → channel → session → subscription and to identify the level of an error. Open [Configuration.cs](../../../src/OpcUaCourse/UaCourse/Configuration.cs) and the client branch of [Program.cs](../../../src/OpcUaCourse/UaCourse/Program.cs).

## Four levels not to be confused

| Level | State it owns | Example of failure |
|---|---|---|
| TCP connection | Transport of the bytes | Server unreachable |
| SecureChannel | Protection and token of the channel | Certificate rejected |
| Session | Context and user identity | Session expired |
| Subscription | Items and sequence of the notifications | Lifetime exceeded |

A channel can be replaced without every application state having to be recreated. However, the sample adopts a simpler strategy: on a failed KeepAlive it recreates everything. This is a choice of our client, not a mandatory property of the protocol.

## Opening the session in C#

This is the path used by the program; the parameters are APIs of the version pinned in the project:

```csharp
var app = await Configuration.CreateAsync(false);
var description = CoreClientUtils.SelectEndpoint(
    app.ApplicationConfiguration, Configuration.Endpoint, true);
var endpoint = new ConfiguredEndpoint(
    null, description,
    EndpointConfiguration.Create(app.ApplicationConfiguration));

using var session = await Session.Create(
    app.ApplicationConfiguration, endpoint, false,
    "Teaching client", 60_000,
    new UserIdentity(new AnonymousIdentityToken()), null);
```

The Boolean true of SelectEndpoint requests secure selection; the Boolean false of this Session.Create overload concerns updateBeforeConnect. Do not copy them as generic security flags. To understand their meaning, consult the signature of the overload in the installed SDK version.

60_000 is a session-timeout request, not the periodicity of the reads. The server may revise the parameters. The discovery address can also return endpoints with a different host: in an industrial network check reachability, host name and certificate instead of replacing the name arbitrarily.

## Application identity and user identity

The UA certificate of the client proves the identity of the application in the channel. AnonymousIdentityToken means that the user supplies no personal credentials. There is no contradiction between an encrypted channel and an anonymous user: transport protection and authorization answer different questions.

Configuration generates separate stores for server and client. The lab trust compares the exact DER of the local peer only when the error is BadCertificateUntrusted. It does not make expiry, host or algorithm errors acceptable.

## Lab with expected results

1. Start the server and client read: note the SessionId and the security policy.
2. Start another client read console: the SessionId must differ, the source of the values stays the same.
3. Stop only the console: the server and the browser must keep running.
4. For the exercise, use an endpoint with a different port in a local copy of the configuration: discovery fails before the read.
5. Restore the port and repeat: now check the trust before investigating the NodeIds.

## Solved exercise: classifying the errors

You have to choose between retry and fixing the configuration:

| Observed error | Initial hypothesis | Reasoned action |
|---|---|---|
| BadNotConnected during discovery | Transport unavailable | Check process and port, then a limited retry |
| BadCertificateUntrusted | Trust missing | Approve/configure the expected peer |
| BadIdentityTokenRejected | User identity rejected | Fix token and policy |
| BadNodeIdUnknown during Read | Wrong contract or mapping | Check namespace and browse |

**Solution.** Do not resolve every exception by selecting a less secure endpoint. The problem must be identified at the level where it happens. A retry can fix a server that is temporarily off; it does not fix a password or a wrong namespace.

**Self-check.** Why does closing the channel not necessarily mean losing every subscription? Because the resources belong to levels with their own times and services. Which resources must be released anyway when the client ends? Subscription, session and the SDK objects owned.

Reference: [Services, Part 4](https://reference.opcfoundation.org/Core/Part4/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
