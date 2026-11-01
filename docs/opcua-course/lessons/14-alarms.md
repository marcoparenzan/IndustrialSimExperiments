# Alarms & Conditions: state, acknowledge and refresh

## Concepts

Design deep-dive. ConditionType represents a significant state; AlarmConditionType adds ActiveState and alarm semantics. AckedState and ConfirmedState are distinct actions. Retain indicates that the condition must be presented to the client. Branches preserve previous states that still require attention.

## Experiment

Design a low-level alarm for Well1 with a threshold and hysteresis. The current server emits only BaseEventType: there is no Acknowledge, no Confirm and no ConditionRefresh.

## In the C# code

Extend the NodeManager with a standard alarm type and standard methods. The client selects ConditionId, EventId, Retain, ActiveState/Id and AckedState/Id. The alarm view must keep a list per condition/branch, different from the chronological log.

## Exercise and verification

Activate the alarm, acknowledge, return to normal, reconnect and ConditionRefresh. Verify the retained state after reconnect and an acknowledge with an expired EventId. Distinguish the physical return to normal from the acknowledgement.

## Mistake to avoid

Do not implement acknowledge by writing a random Boolean. The services and methods of the conditions have specific state rules and identifiers.

## Goals and boundary of the lab

Estimated time: 2–3 hours. This lesson does not use the BaseEventType log as if it were already an alarm system. The current server **does not implement Alarms & Conditions**. The goal is to define and verify the contract before adding the corresponding NodeStates and standard methods.

Start from Well1: generate an alarm when the level falls below a threshold and define a hysteresis to avoid continuous oscillations. The measured value and the state of the alarm are different pieces of information.

## Physical state, acknowledgement and persistence

| Field | Question |
|---|---|
| ActiveState | Is the alarm situation currently active? |
| AckedState | Has it been taken over/acknowledged? |
| ConfirmedState | Has it been confirmed, if the model requires it? |
| EnabledState | Is the condition enabled? |
| Retain | Must it stay in the client's current list? |
| BranchId | Which state/branch does the notification belong to? |

Acknowledge must not stop a pump or fix the fault by definition. The physical return to normal is not automatically equivalent to an acknowledge. The Retain policy depends on the type and on the states the server keeps: do not always fix it equal to ActiveState.

A ConditionId identifies the condition; an EventId identifies an occurrence/state notified. A server can keep previous branches when a past state still needs to be acknowledged. Using a single Boolean in the browser loses this distinction.

## Designing the detection with hysteresis

This code is domain logic, **not an implementation of the A&C protocol**:

```csharp
static bool NextActive(bool wasActive, double level,
    double lowLimit, double hysteresis)
{
    if (!double.IsFinite(level))
        throw new ArgumentOutOfRangeException(nameof(level));
    return wasActive
        ? level < lowLimit + hysteresis
        : level < lowLimit;
}
```

With lowLimit=5 and hysteresis=0.3, a measurement of 4.9 activates the alarm; 5.1 does not yet deactivate it; 5.3 allows the return to normal. The choice of strictly-less thresholds is explicit: write tests on the values exactly equal to the boundaries.

A Bad quality level requires a separate decision: do not make the alarm return to normal by using zero or a missing value. You can keep the last state and signal uncertain quality, or expose a faulty-sensor condition.

## Guided implementation

1. Choose an appropriate standard type, for example a non-exclusive limit for the level.
2. Create the condition instance and link the SourceNode and the notifying source.
3. Update state fields, quality, severity and timestamp consistently.
4. Implement the standard methods and the checks on EventId/branch.
5. In the client select ConditionId, EventId, BranchId, Retain and the Ids of the states.
6. Keep a dictionary of conditions/branches, distinct from the chronological log.
7. After a reconnect run ConditionRefresh and handle the refresh start/end events correctly.

The updates during the refresh must not be mistaken for new occurrences all to be added to the log. The current table is reconciled with the server state.

## Solved exercise: outage and return to normal

Sequence: the alarm activates, the client disconnects, the level returns to normal, the alarm is not yet acknowledged, the client comes back.

**Solution.** The client must recover the retained states from the server, not rebuild them only from the current measurements. If the policy keeps an unacknowledged branch, the refresh must show it even if ActiveState is false. Events lost during the outage and the current state of the condition are two distinct problems.

**Acceptance criteria.** An acknowledge with an invalid EventId is rejected; the branches do not overwrite each other; the refresh reconciles the list; the UI distinguishes an active alarm from an alarm that has returned to normal and is waiting to be acknowledged.

Reference: [Alarms & Conditions, Part 9](https://reference.opcfoundation.org/Core/Part9/).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
