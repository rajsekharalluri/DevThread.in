---
id: comparisons-events-rest
slug: events-rest
title: Event-Driven vs Request/Response Architecture
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 35
version:
  minimum: "Distributed systems concepts"
prerequisites: [architecture-event-driven, architecture-api-gateway-communication]
tags: [events, rest, synchronous, asynchronous, comparison]
relatedTopics: [architecture-outbox-inbox, architecture-resilience]
order: 110
status: published
---
# Event-Driven vs Request/Response Architecture

## Decision table

| Choice | Best fit | Main cost |
|---|---|---|
| Request/response | Immediate answer/decision | Timing/availability coupling |
| Command queue | Async work with one owner | Delay/retry/duplicate handling |
| Event stream | Many reactions/replay | Eventual consistency/schema ops |

## Example

```text
GET current balance → request/response
POST generate report → command queue, return job ID
OrderPaid → event stream for billing/notifications/analytics
```

Do not make the client wait for independent downstream side effects simply because a synchronous endpoint is familiar.

## Interview Questions
- **[L1]** When is request/response the right choice?
- **[L1]** What is an event-driven reaction?
- **[L2]** What consistency trade-off does async introduce?
- **[L2]** Why are duplicate messages expected?
- **[L3]** How do you decide whether an API operation should return 200 or 202?
- **[L3]** How do you combine synchronous decisions with asynchronous side effects?

## Interview Answers
1. When the caller needs an immediate result/validation and latency/failure budget is bounded.
2. A consumer reacts later to a fact without the producer waiting for every reaction.
3. The caller may see stale/pending state and needs status/retry/reconciliation semantics.
4. Broker/relay/consumer crashes can occur after effect but before acknowledgement; at-least-once delivery is common.
5. Return 200/201 when the requested state change completed; return 202 when durable work was accepted but completion is asynchronous and observable.
6. Commit the critical local decision synchronously, persist an outbox/event, and let independent notifications/analytics/search react asynchronously.
