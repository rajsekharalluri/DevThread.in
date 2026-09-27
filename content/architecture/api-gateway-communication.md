---
id: architecture-api-gateway-communication
slug: api-gateway-communication
title: API Gateway and Service-to-Service Communication
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 45
version:
  minimum: "Concept-level"
prerequisites: [architecture-microservices, architecture-distributed-systems]
tags: [architecture, api-gateway, grpc, rest, service-communication]
relatedTopics: [architecture-resilience, architecture-event-driven]
order: 110
status: published
---
# API Gateway and Service-to-Service Communication

## Overview
An API gateway is an edge component that handles client-facing concerns such as routing, authentication, rate limiting, aggregation, and protocol translation. Service-to-service communication may use REST, gRPC, messaging, or events depending on interaction semantics.

## Why This Exists
Clients should not need to discover every internal service or implement internal retry/authentication/routing rules. A gateway can provide a stable client contract while services communicate through explicit internal contracts.

## Fundamentals
REST is broadly interoperable and resource-oriented. gRPC offers strongly typed, efficient internal RPC with streaming support. Messaging/events decouple timing and availability. A gateway should not become a business-logic monolith or a single unscalable bottleneck.

## Syntax and API
```text
Client -> Gateway
          ├── authenticate token
          ├── rate-limit client
          ├── route /orders -> Orders service
          └── aggregate /dashboard from Orders + Inventory
```

## How It Works
The gateway terminates external protocols and forwards/aggregates requests. Internal calls still need timeouts, authorization, tracing, retries, and versioning; the gateway does not remove distributed-system concerns.

## Internal Implementation
Gateway aggregation creates fan-out latency and partial-response decisions. gRPC contracts use protobuf schemas and generated clients; REST contracts use HTTP semantics and JSON. Events avoid waiting but require eventual consistency and consumer operations.

## Real-World Example
A mobile client requests one dashboard endpoint while the gateway aggregates profile, order summary, and notification status, returning a deliberate partial/degraded response if one non-critical service fails.

## Production Example
```text
External: HTTPS + JSON + OAuth/JWT
Internal synchronous: gRPC with deadlines and mTLS
Internal asynchronous: Kafka events for OrderPaid
Gateway policies: authentication, rate limit, request size, correlation ID
```

## Common Mistakes
- Putting domain decisions in the gateway.
- Gateway calling ten services synchronously for every request.
- Retrying unsafe calls at both gateway and service, multiplying traffic.
- No internal authentication because services are “inside the VPC.”
- Sharing internal DTOs as public contracts.

## Performance
Measure gateway overhead, fan-out latency, connection pools, payload size, and tail latency. Cache safe responses and avoid synchronous aggregation when the user can tolerate eventual data.

## Security
Validate tokens and scopes at the edge, enforce service-level authorization again, use mTLS/service identity internally, limit request sizes, and avoid logging credentials or sensitive bodies.

## Testing
Contract-test gateway/service interfaces, test partial failures and timeouts, verify rate limits, and test backward-compatible rollout of service contracts.

## When to Use
Use a gateway when many clients need a stable edge contract or shared edge policies. Use gRPC for typed internal low-latency calls and events for decoupled asynchronous reactions.

## When Not to Use
Do not create a gateway just to hide a poorly designed service topology, and do not force gRPC/events where a direct simple HTTP call is sufficient.

## Trade-offs
A gateway centralizes useful policy but can become a bottleneck or team dependency. Communication choice determines coupling, latency, consistency, and failure behavior.

## Related Topics
See [Resilience Patterns](/architecture/resilience) and [Event-Driven Architecture](/architecture/event-driven).

## Practical Exercise
Design an API gateway for a mobile dashboard with three backend services. Define authentication, rate limiting, timeouts, partial failure behavior, and which data should be asynchronous instead.

## Interview Questions
- **[L1]** What responsibilities belong in an API gateway?
- **[L1]** Compare REST, gRPC, and events at a high level.
- **[L2]** Why is synchronous fan-out through a gateway dangerous?
- **[L2]** Why does service-to-service communication still need authorization inside a private network?
- **[L3]** How would you choose communication styles for an order workflow?

## Interview Answers
1. **[L1]** Edge routing, authentication, rate limits, request policies, protocol translation, and sometimes carefully designed aggregation. Business ownership should remain in services.
2. **[L1]** REST is broadly interoperable; gRPC is strongly typed and efficient for internal RPC; events decouple timing and availability but introduce eventual consistency.
3. **[L2]** Fan-out adds latency and failure multiplication; one slow or unavailable service can delay the whole request and consume gateway resources.
4. **[L2]** Internal networks can be breached or misconfigured. Authorization must be based on service identity and operation permissions, not network location alone.
5. **[L3]** Use synchronous calls for immediate decisions with bounded latency, events for independent reactions, and a saga/outbox for cross-service workflows that cannot use one transaction.

## Senior Developer Perspective
Communication style is an architectural decision. Senior engineers choose it from user latency, consistency, failure, contract, and ownership requirements — not from the fact that a protocol is fashionable or familiar.
