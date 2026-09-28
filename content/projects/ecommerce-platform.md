---
id: projects-ecommerce-platform
slug: ecommerce-platform
title: "Build: Production E-commerce Platform"
category: projects
categoryTitle: Build Real Systems
difficulty: architect
estimatedMinutes: 90
version:
  minimum: "Architecture exercise"
prerequisites: [architecture-system-design, architecture-event-driven, sql-database-design, devops-kubernetes]
tags: [project, ecommerce, architecture, microservices, kafka, sql]
relatedTopics: [architecture-microservices, architecture-kafka, sql-database-design, dotnet-aspnet-core-backend]
order: 10
status: published
---
# Build: Production E-commerce Platform

## Introduction
Design and incrementally build an e-commerce platform where customers browse products, create orders, pay, and receive status updates. The goal is to practice boundaries and operational decisions, not to create a diagram containing every technology.

## Purpose and requirements
Functional:

- Browse/search products.
- Maintain inventory.
- Create and submit orders.
- Authorize/capture payments.
- Notify customers.
- Track order status/history.

Non-functional:

- Fast product reads with cache.
- No double payment or inventory reservation.
- Auditable order/payment transitions.
- Graceful dependency failure.
- Independent scaling for catalog and checkout where justified.

## Architecture evolution

```text
Start: modular monolith
  ├── Catalog
  ├── Orders
  ├── Inventory
  └── Payments

Extract only when evidence supports it:
Angular → API Gateway → services/modules
                         ├── Catalog + cache
                         ├── Orders + SQL
                         ├── Inventory + reservation
                         └── Payments + provider adapter
                                      ↓ events/outbox
                                  Kafka/broker
```

## API/data example
```text
GET  /products?query=&page=
POST /orders
POST /orders/{id}/submit
POST /orders/{id}/payment-intent
GET  /orders/{id}
```

Orders own order state and historical charged prices. Payments own provider interaction. Inventory owns availability/reservation. Do not share tables as a shortcut once ownership is separated.

## Production workflow

```text
Submit order
  ↓ local OrderPending transaction + outbox
Reserve inventory
  ├── rejected → OrderRejected
  └── reserved
Authorize payment
  ├── failed → release inventory + OrderPaymentFailed
  └── captured → OrderConfirmed
Notifications/search/analytics react asynchronously
```

## Reliability/security decisions
Use idempotency keys for order/payment commands, optimistic concurrency for stock, outbox for events, bounded retries for providers, dead-letter handling, tenant/user authorization, token validation, parameterized SQL, and trace IDs across the workflow.

## Scaling and cost
Catalog reads are usually more cacheable and read-heavy than checkout writes. Scale them differently only after measuring. Kafka is useful when multiple consumers/replay justify it; a simple queue or transactional outbox may be enough for an early system.

## Practical build sequence

1. Modular .NET API with SQL schema.
2. Angular catalog/order UI.
3. Inventory reservation transaction.
4. Payment adapter with idempotency.
5. Outbox and background relay.
6. Events/consumer retries and DLQ.
7. Cache catalog reads.
8. Observability/load/failure tests.
9. Extract services only with evidence.

## Interview Questions
- **[L1]** What are the core bounded capabilities?
- **[L1]** Why should payment be idempotent?
- **[L2]** How does inventory reservation avoid overselling?
- **[L2]** Why use an outbox?
- **[L3]** When would you extract Catalog or Payments into a service?
- **[L3]** How would you handle payment timeout after the provider may have charged?

## Interview Answers
1. Catalog, Orders, Inventory, Payments, and Notifications have distinct rules/ownership and may evolve differently.
2. Retries/client refreshes can repeat a command; an idempotency key ensures one business effect.
3. Use atomic conditional update/reservation with concurrency control and commit before publishing the result.
4. It atomically records local state and event intent, preventing lost events after a successful transaction.
5. Extract when ownership, compliance, scaling, or deployment/failure isolation creates a measurable benefit larger than distributed complexity.
6. Store pending/idempotency state, query/reconcile provider status, and only finalize based on an authoritative result; never blindly charge again.

## Expert Perspective
The best first implementation is usually a modular monolith with strong boundaries. The project becomes “production architecture” when it handles retries, duplicate commands, consistency, observability, and recovery—not when it contains the most services.
