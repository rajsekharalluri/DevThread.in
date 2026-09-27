---
id: architecture-saga-process-manager
slug: saga-process-manager
title: Saga and Process Manager Patterns
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-event-driven, architecture-distributed-systems]
tags: [saga, process-manager, distributed-transactions, compensation]
relatedTopics: [architecture-outbox-inbox, architecture-microservices]
order: 150
status: published
---
# Saga and Process Manager Patterns

## Introduction
A saga coordinates a business workflow across local transactions in multiple services. A process manager stores workflow state and decides what command/event should happen next.

```text
OrderPlaced
    ↓
ReserveInventory
    ↓ success                 ↓ failure
AuthorizePayment              CancelOrder
    ↓ success                 ↓
ConfirmOrder                  PublishOrderFailed
```

Each step commits locally. There is no single ACID transaction across all services.

## Purpose
Use a saga when a business process spans service-owned databases and needs explicit progress, retries, compensation, timeout, and failure handling.

## Simple example
```text
Step 1: Order service → Pending
Step 2: Inventory → Reserved
Step 3: Payment → Captured
Step 4: Order → Confirmed
```

If payment fails after inventory was reserved, a compensating command releases inventory. Compensation is not always a perfect undo; it is a new business action that restores an acceptable state.

## Professional company-level example
A process manager stores:

```text
SagaId
OrderId
CurrentState
CompletedSteps
Retry counts
Timeout/deadline
Correlation IDs
Last error
```

It consumes events and issues commands idempotently. Every transition is observable and recoverable after process restart.

## Choreography versus orchestration

| Style | Behavior | Risk |
|---|---|---|
| Choreography | Services react directly to events | Flow becomes hard to see |
| Orchestration | Process manager coordinates steps | Central workflow component |

Use orchestration when the workflow has many steps/timeouts/compensations and needs an explicit owner.

## Interview Questions
- **[L1]** What is a saga?
- **[L1]** What is a compensating action?
- **[L2]** Compare choreography and orchestration.
- **[L2]** Why is a compensation not always a true rollback?
- **[L3]** What state should a process manager persist?
- **[L3]** How do you make a saga safe across retries and service restarts?

## Interview Answers
1. A saga is a sequence of local transactions coordinating an eventual-consistent distributed workflow.
2. It is a new action that counteracts a prior business effect, such as releasing reserved stock after payment failure.
3. Choreography has decentralized event reactions; orchestration has an explicit coordinator that tracks/commands workflow progress.
4. External actions may be irreversible or have different business effects; a refund/release is not identical to erasing the original action.
5. Saga/workflow ID, current state, completed steps, retry/deadline, correlation IDs, errors, and idempotency markers.
6. Persist each transition, use idempotency keys, bounded retries/backoff, timeouts, durable events/outbox, reconciliation, and recovery after restart.

## Expert perspective
A saga is business workflow engineering under partial failure. Senior engineers define states, deadlines, compensation semantics, idempotency, operator recovery, and user-visible status before choosing a saga framework.
