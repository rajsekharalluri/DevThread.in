---
id: architecture-messaging-patterns
slug: messaging-patterns
title: Messaging Patterns, Queues, Retries, and Dead Letters
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 45
version:
  minimum: "Concept-level"
prerequisites: [architecture-event-driven]
tags: [architecture, messaging, queues, retry, dead-letter]
relatedTopics: [architecture-kafka, architecture-event-driven]
order: 80
status: published
---
# Messaging Patterns, Queues, Retries, and Dead Letters

## Overview
Messaging systems move work or events between producers and consumers asynchronously. Reliable messaging requires explicit delivery, acknowledgement, retry, ordering, and poison-message policies.

## Why This Exists
Queues absorb bursts, isolate slow dependencies, and let workers process work independently from request lifecycles. Without deliberate failure handling, a queue can hide outages until backlog grows or repeatedly retry a permanently invalid message forever.

## Fundamentals
A work queue usually gives one consumer the job; a pub/sub stream gives multiple consumer groups their own copy. Delivery may be at-most-once, at-least-once, or effectively-once through idempotency. Retry with exponential backoff for transient errors; move poison messages to a dead-letter queue after a bounded attempt count.

## Syntax and API
```csharp
public async Task HandleAsync(Message message, CancellationToken cancellationToken)
{
    if (await processedEvents.ExistsAsync(message.Id, cancellationToken)) return;

    try
    {
        await ProcessAsync(message, cancellationToken);
        await processedEvents.MarkAsync(message.Id, cancellationToken);
        await consumer.AcknowledgeAsync(message, cancellationToken);
    }
    catch (TransientException) when (message.Attempts < 5)
    {
        await consumer.RetryWithBackoffAsync(message, cancellationToken);
    }
    catch (Exception ex)
    {
        await deadLetters.StoreAsync(message, ex.Message, cancellationToken);
        await consumer.AcknowledgeAsync(message, cancellationToken);
    }
}
```

## How It Works
A consumer acknowledges only after durable processing. Transient failures return the message for delayed retry; permanent failures are isolated in a dead-letter queue for inspection and controlled replay. Idempotency prevents duplicate delivery from duplicating side effects.

## Internal Implementation
Backoff should include jitter so many consumers do not retry simultaneously. Visibility timeouts must exceed normal processing time but be renewed for long jobs. Queue depth and oldest-message age are more useful signals than raw message count alone.

## Real-World Example
A notification service queues email work, retries provider timeouts, and dead-letters invalid recipient data. The API responds immediately after durable enqueue rather than waiting for SMTP.

## Production Example
```text
Queue: email-send
Retry schedule: 1m, 5m, 30m, 2h
Dead-letter: email-send-dlq
Metrics: queue depth, oldest age, success rate, retry rate, DLQ rate
Operator action: inspect/fix/replay selected messages
```

## Common Mistakes
- Acknowledging before the side effect commits.
- Infinite immediate retries causing a retry storm.
- No idempotency key or processed-message record.
- No dead-letter visibility or replay tool.
- Putting slow work behind a short visibility timeout.

## Performance
Tune batch size, consumer concurrency, prefetch, visibility timeouts, and backoff. Backlog age is a key user-impact metric; processing more messages per second is not useful if failures continually requeue.

## Security
Encrypt messages, restrict queue/topic permissions, avoid sensitive payloads where possible, and protect dead-letter data because it often contains failed requests and personal information.

## Testing
Test duplicates, out-of-order delivery, transient/permanent failure classification, consumer crashes, timeout/visibility expiry, DLQ replay, and malformed payloads.

## When to Use
Use messaging for asynchronous work, burst absorption, independent consumers, and workflows that can tolerate delayed completion.

## When Not to Use
Do not add a broker for a small synchronous operation with no burst or decoupling need; a direct call may be simpler and more observable.

## Trade-offs
Messaging improves resilience and decoupling but adds eventual consistency, operational infrastructure, debugging complexity, and duplicate-delivery handling.

## Related Topics
See [Kafka](/architecture/kafka) and [Event-Driven Architecture](/architecture/event-driven).

## Practical Exercise
Design a payment-processing queue with retryable versus permanent failures, an idempotency record, DLQ, replay procedure, and metrics.

## Interview Questions
- **[L1]** Why acknowledge a message only after processing?
- **[L1]** What is a dead-letter queue?
- **[L2]** Why are retries with exponential backoff and jitter important?
- **[L2]** How can a consumer safely process duplicate messages?
- **[L3]** How would you design operational recovery for a growing dead-letter queue?

## Interview Answers
1. **[L1]** Acknowledge after durable processing so a consumer crash before completion causes the message to be delivered again instead of silently losing work.
2. **[L1]** A DLQ stores messages that repeatedly fail or are permanently invalid, separating them from normal traffic for investigation and controlled replay.
3. **[L2]** Backoff reduces pressure on a failing dependency; jitter prevents many workers retrying at the same instant and creating a synchronized retry storm.
4. **[L2]** Use an idempotency key/message ID and record successful processing atomically with the side effect, or make the side effect itself naturally idempotent.
5. **[L3]** Alert on DLQ rate and oldest age, classify root causes, provide safe inspection/redaction, fix or quarantine invalid data, replay selected messages, and reconcile outcomes rather than blindly requeueing everything.

## Senior Developer Perspective
A queue is not reliability by itself. Reliability comes from acknowledgement timing, bounded retries, idempotent consumers, dead-letter operations, and metrics that tell operators whether work is completing or merely moving between failure states.
