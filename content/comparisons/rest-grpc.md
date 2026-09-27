---
id: comparisons-rest-grpc
slug: rest-grpc
title: REST vs gRPC vs Events
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 35
version:
  minimum: "API communication concepts"
prerequisites: [architecture-api-gateway-communication, architecture-event-driven]
tags: [rest, grpc, events, api, comparison]
relatedTopics: [dotnet-aspnet-core-backend, architecture-resilience]
order: 50
status: published
---
# REST vs gRPC vs Events

## Decision table

| Choice | Best fit | Trade-off |
|---|---|---|
| REST/JSON | Public/client APIs, interoperability | Payload/contract overhead |
| gRPC/protobuf | Typed internal low-latency RPC | Tooling/proxy/client complexity |
| Events | Async reactions/decoupling | Eventual consistency/replay |

## Example

```text
User asks for current balance → REST/gRPC response
Service needs inventory decision → bounded synchronous call
Order paid → event for billing/notifications/analytics
```

Do not replace every request/response with an event. The caller needs a response when an immediate decision is required; an event is appropriate when consumers can react later.

## Interview Questions
- **[L1]** What is REST good at?
- **[L1]** What is gRPC good at?
- **[L2]** When should an event be used instead of an RPC?
- **[L2]** What failure behavior does each style introduce?
- **[L3]** How would you choose communication for an order workflow?
- **[L3]** Why can a gateway not remove distributed-system complexity?

## Interview Answers
1. Broadly interoperable resource/HTTP client APIs.
2. Typed efficient internal RPC with generated contracts/streaming.
3. When reactions can be delayed, multiple consumers need a fact, or producer/consumer availability should be decoupled.
4. REST/gRPC add timeout/retry/network failure; events add duplicate/late delivery, replay, schema, and eventual-consistency concerns.
5. Synchronous calls for immediate validation/decisions; events for independent reactions; saga/outbox for multi-service workflow.
6. The gateway still has downstream latency, partial failure, retries, authorization, fan-out, and consistency decisions.
