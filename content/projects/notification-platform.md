---
id: projects-notification-platform
slug: notification-platform
title: "Build: Reliable Notification Platform"
category: projects
categoryTitle: Build Real Systems
difficulty: advanced
estimatedMinutes: 75
version:
  minimum: "Architecture exercise"
prerequisites: [architecture-messaging-patterns, architecture-resilience, devops-observability]
tags: [project, notifications, queues, retries, providers]
relatedTopics: [architecture-kafka, architecture-outbox-inbox, architecture-resilience]
order: 30
status: published
---
# Build: Reliable Notification Platform

## Introduction
Design a service that accepts notification requests and delivers email/SMS/push messages asynchronously while handling provider limits, retries, user preferences, templates, duplicate requests, and delivery status.

```text
Producer/API
    ↓ durable enqueue
Notification queue
    ↓ bounded workers
Provider adapters
    ├── email
    ├── SMS
    └── push
    ↓ callbacks/status
Delivery status + audit
```

## Purpose
The product API should not wait for a slow provider. A queue absorbs bursts, workers scale independently, and provider adapters isolate vendor-specific APIs.

## Simple example
```text
POST /notifications
  → validate recipient/template
  → write notification + outbox
  → return Accepted(notificationId)

Worker
  → render template
  → send provider request
  → record Sent/Failed
```

## Professional company-level example
```text
Queue: notification-send
Retry: 1m → 5m → 30m → 2h
DLQ: notification-send-dlq
Limits: provider-specific concurrency/rate
Keys: notificationId + channel/idempotency
Metrics: queue age, retry rate, provider errors, delivery latency
```

A worker acknowledges only after durable processing. A poison message (invalid address/template) should not retry forever. A provider timeout may require status lookup or a provider idempotency key.

## Reliability/security
Protect personal data, restrict template access, avoid sensitive data in logs, encrypt messages, use tenant-scoped authorization, validate callback signatures, and make replay deliberate. A bounded queue creates backpressure; an unbounded queue hides an outage until memory/storage is exhausted.

## Comparison
| Choice | Use |
|---|---|
| Synchronous send | Tiny low-risk interaction |
| Queue/worker | Slow/bursty/retryable delivery |
| Pub/sub | Multiple independent notification consumers |
| DLQ | Poison/permanently failing messages |
| Provider adapter | Hide vendor contract/failure behavior |

## Interview Questions
- **[L1]** Why should notifications usually be asynchronous?
- **[L1]** What is a dead-letter queue?
- **[L2]** When should a message be retried?
- **[L2]** How do you prevent duplicate notifications?
- **[L3]** How would you handle provider rate limits and outages?
- **[L3]** How would you design operator replay safely?

## Interview Answers
1. Providers are slow/unreliable and delivery does not usually need to finish before the API acknowledges durable acceptance.
2. It isolates messages that repeatedly/permanently fail so normal traffic continues and operators can inspect/replay deliberately.
3. Retry bounded transient failures with backoff/jitter; do not retry invalid data or unsafe non-idempotent effects blindly.
4. Store notification/channel idempotency keys, record provider IDs/status, and make worker side effects/retries idempotent.
5. Use per-provider concurrency/rate limits, circuit breakers, backoff, status reconciliation, fallback provider policy where appropriate, and queue-age alerts.
6. Inspect/redact, classify/fix cause, select messages, enforce authorization, use idempotency, replay with rate limits, and reconcile final delivery results.

## Expert Perspective
A notification service is a reliability exercise disguised as a send-email feature. Its quality is measured by durable acceptance, bounded backlog, duplicate safety, provider isolation, observability, and recovery—not just a successful API response.
