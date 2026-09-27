---
id: architecture-outbox-inbox
slug: outbox-inbox
title: Outbox and Inbox Patterns
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 45
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-event-driven, sql-transactions-isolation]
tags: [outbox, inbox, idempotency, messaging]
relatedTopics: [architecture-domain-integration-events, architecture-messaging-patterns]
order: 140
status: published
---
# Outbox and Inbox Patterns

## Introduction
The Outbox pattern reliably publishes an event after a local database transaction. The Inbox/idempotency pattern safely handles duplicate delivery at the consumer.

```text
Producer transaction
  ├── business state
  └── outbox message
         ↓ committed together
Relay publishes message
         ↓ duplicate possible
Consumer inbox/processed IDs
  ├── skip duplicate
  └── apply local effect + record ID atomically
```

## Purpose
Without an outbox, a service can commit business state and crash before publishing, or publish then roll back state. Without an inbox/idempotency record, redelivery can duplicate side effects.

## Simple example
```sql
BEGIN TRANSACTION;
UPDATE Orders SET Status = 'Paid' WHERE Id = @orderId;
INSERT INTO OutboxMessages(EventId, Type, Payload, CreatedAt)
VALUES (@eventId, 'OrderPaid', @payload, SYSUTCDATETIME());
COMMIT;
```

A relay later reads unpublished rows, publishes them, and marks them published. Publishing can still happen twice; consumers must tolerate it.

## Professional company-level example
```sql
BEGIN TRANSACTION;
INSERT INTO InboxMessages(Consumer, EventId, ReceivedAt)
VALUES (@consumer, @eventId, SYSUTCDATETIME());

-- unique key prevents a duplicate event from entering twice
UPDATE CustomerSummary SET LifetimeValue = LifetimeValue + @amount
WHERE CustomerId = @customerId;
COMMIT;
```

The processed-event record and local side effect belong in one local transaction. If the consumer crashes before commit, it can safely retry; if after commit, the duplicate is recognized.

## Operational requirements

- Outbox polling/relay retry and lock behavior.
- Message age/lag and failed publish metrics.
- Payload/schema versioning.
- Dead-letter and replay tooling.
- Retention/cleanup after consumers no longer need messages.
- Idempotency for external side effects that cannot share the local transaction.

## Interview Questions
- **[L1]** What problem does the Outbox pattern solve?
- **[L1]** What problem does the Inbox pattern solve?
- **[L2]** Why can an outbox still publish duplicates?
- **[L2]** How should a consumer record processing safely?
- **[L3]** How do you handle an external payment effect that cannot share the local database transaction?
- **[L3]** What operational metrics and recovery actions are required for an outbox?

## Interview Answers
1. It atomically records business state and the intent to publish, preventing a committed change with a lost event.
2. It records processed event IDs so redelivery does not apply the same local side effect twice.
3. The relay can crash after publishing but before marking the outbox row complete, so another relay attempt may publish again. At-least-once is normal.
4. Insert a unique consumer/event ID and apply the local effect in the same transaction; duplicate key means the effect already succeeded.
5. Persist pending state/idempotency key, call the provider safely, reconcile uncertain outcomes, and publish the definitive result afterward.
6. Monitor oldest outbox age, publish failures/retries, queue lag, dead letters, and cleanup; provide safe replay and reconciliation procedures.

## Expert perspective
Outbox/inbox patterns do not create magic exactly-once delivery. They turn unreliable cross-boundary timing into explicit at-least-once processing with durable local state and idempotent effects.
