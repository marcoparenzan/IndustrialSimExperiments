# Certificates, identity, authorization and audit

## Concepts

The application certificate identifies the peer; UserIdentity identifies the user. SecurityPolicy and MessageSecurityMode protect the channel. AccessLevel is a capability of the node, UserAccessLevel depends on the user. A role can authorize Read but forbid Write and Call. Audit and diagnostic logs have different purposes.

## Experiment

Inspect the own/trusted/issuers/rejected stores under `<artifacts root>/pki`. The lab uses only SignAndEncrypt and local trust by DER comparison; it has no None endpoint. Anonymous remains an identity with teaching permissions.

## In the C# code

Configuration.CertificateValidation accepts only the BadCertificateUntrusted of the exact local peer. UaConnection is shared and all browsers use the same identity. The server does not yet implement roles or a web login.

## Exercise and verification

In a separate module configure explicit trust lists, a viewer and an operator user. Verify that the viewer can neither write nor call Start, even bypassing the UI. Publish audit events for the commands.

## Mistake to avoid

Disabling a button is not authorization. Do not expose this lab on the network as an operational service without web authentication, trust configuration and server policy.

## Goals and state of the sample

Estimated time: 120 minutes. This lesson combines the observation of features that are present with the design of authorizations not yet implemented. The encrypted channel is present; roles, web login and a complete application audit are not.

Read [Configuration.cs](../../../src/OpcUaCourse/UaCourse/Configuration.cs) and [UaConnection.cs](../../../src/OpcUaCourse/UaCourse.Web/Services/UaConnection.cs). Identify who owns the certificates, who chooses the UserIdentity and which node authorizes Write.

## The questions each mechanism answers

| Mechanism | Question |
|---|---|
| Application certificate | Which application is the peer? |
| Trust list and validation | Do I trust that certificate in this context? |
| SignAndEncrypt | Is the message protected in transit? |
| UserIdentity | Which user does the session represent? |
| Roles and permissions | Which operations can the user perform? |
| Audit | Who requested which operation and with what outcome? |

A valid but untrusted certificate and an expired certificate are distinct problems. The callback of the lab accepts only the exact DER of the local peer and only BadCertificateUntrusted. It must not turn into an indiscriminate e.Accept=true for any error.

## Actual stores and configuration

Every application has own, trusted, issuers and rejected under `<artifacts root>/pki`. The artifacts root is the `UACOURSE_ARTIFACTS_ROOT` environment variable or, by default, `%LOCALAPPDATA%\UaCourse`; both applications must see the same value, and nothing is written inside the repository.

The server exposes SignAndEncrypt/Basic256Sha256 and an Anonymous user token. The selection helper of the client chooses a secure endpoint. The convention of the lab depends on local files accessible to both applications: for separate hosts it must be replaced with an appropriate provisioning of trust.

The ASP.NET DataProtection keys are another family of keys, not the UA certificates. They protect web features; the store of the lab is isolated in `<artifacts root>/aspnet-keys`, with no encryption-at-rest configuration.

## Server-side authorization

AccessLevel describes general capabilities of the node, UserAccessLevel those of the user in the context of the request. For Methods, Executable/UserExecutable and the actual checks of the service are relevant. Do not set UI permissions and presume that they protect the server.

The UA Session of the web app is shared: all browsers use the anonymous identity of the same application. Adding a Blazor login does not automatically transfer the personal identity to the UA session. You must decide whether to use sessions per identity, an authorized service identity, or a gateway that enforces application policies and audit.

## Solved exercise: viewer and operator

Design two roles:

| Operation | Viewer | Operator |
|---|---|---|
| Browse/Read | Allowed | Allowed |
| Subscription | Allowed | Allowed |
| Write Setpoint | Forbidden | Allowed, with range |
| Call Start/Stop | Forbidden | Allowed |
| Manage trust and users | Forbidden | Forbidden, separate administrative role |

**Solution.** Implement the checks in the server requests using the identity/roles of the session. The UI can disable controls for clarity, but the decisive test is a direct request from a viewer client: it must fail even without a browser.

Do not use a fictitious C# scheme as a supposed authorization API of the SDK: the callbacks and role permissions available depend on the release. Start from the implementation and the samples of the pinned version, then verify every service.

## Audit and renewal

A process event "Reactor started" does not necessarily contain the identity of the requester, the outcome of the validation and a correlation id. Record both successful and rejected commands with timestamp and context, avoiding publishing passwords or private keys.

Try the certificate renewal in a lab environment: the fingerprint changes, so the trust by exact DER must be updated in a controlled way. GDS/CertificateManager and automated management are a future module with their own infrastructure.

**Self-check.** Does browser HTTPS make UA TCP automatically secure? No, they are distinct channels. Does Anonymous mean an unencrypted channel? No. Does a hidden button forbid Call? No.

References: [Security Model, Part 2](https://reference.opcfoundation.org/Core/Part2/), [Services, Part 4](https://reference.opcfoundation.org/Core/Part4/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
