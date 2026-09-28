---
id: projects-payment-platform
slug: payment-platform
title: "Build: Reliable Payment Platform"
category: projects
categoryTitle: Build Real Systems
difficulty: architect
estimatedMinutes: 90
version:
  minimum: "Architecture exercise"
prerequisites: [architecture-distributed-systems, architecture-resilience, sql-transactions-isolation, architecture-event-driven]
tags: [project, payments, idempotency, reconciliation, outbox]
relatedTopics: [architecture-saga-process-manager, architecture-resilience, dotnet-aspnet-core-backend]
order: 20
status: published
---
# Build: Reliable Payment Platform

## Introduction
Design a payment capability that accepts payment commands, calls external providers, records an auditable state, handles timeout/duplicate outcomes, and reconciles provider truth.

## Purpose and requirements

- Never intentionally double-charge a customer.
- Accept duplicate client requests safely.
- Handle provider timeout where outcome is unknown.
- Keep immutable audit/history.
- Retry only safe/transient operations.
- Reconcile provider and local records.
- Protect tokens/secrets and authorize merchant actions.

## State model

```text
Created
  ↓ authorize
PendingProvider
  ├── Authorized → Captured → Settled
  ├── Declined
  ├── TimedOut/Unknown → Reconcile
  └── Failed
```

Do not model timeout as “failed” automatically. The provider may have completed the charge after your request timed out.

## API/data example
```text
POST /payments
Idempotency-Key: merchant-123-request-abc

202 { paymentId, status: "PendingProvider" }
GET /payments/{id}
```

Store idempotency key, merchant/account scope, request fingerprint, provider request ID, state history, amount/currency, and timestamps. A unique constraint should prevent the same key from creating two payment operations.

## Production workflow

```text
API request
  ↓ validate/authenticate
Idempotency lookup
  ├── existing completed → return saved result
  ├── existing pending   → return current state
  └── new                → create pending operation
                         provider call
                              ├── approved/declined
                              └── timeout/unknown
                                reconciliation job
```

Use an outbox to publish `PaymentCaptured`/`PaymentFailed` after local state commits. Use a saga when order/inventory/payment cross service boundaries.

## Failure and security
Use TLS, provider signature verification, secret managers, least privilege, redacted logs, fraud/rate controls, and strict amount/currency validation. A retry must carry the same provider idempotency key. Reconciliation must be observable and operator-replayable.

## Comparison
| Approach | Appropriate |
|---|---|
| Synchronous capture | User needs immediate result and provider SLA is reliable |
| Async payment state | Provider is slow/uncertain or workflow spans services |
| Retry | Known transient/idempotent operation |
| Reconciliation | Timeout/unknown provider outcome |
| Compensation | Downstream business correction, not magical rollback |

## Interview Questions
- **[L1]** What is an idempotency key?
- **[L1]** Why can a payment timeout not be treated as failure?
- **[L2]** How do you prevent duplicate payment requests?
- **[L2]** What is reconciliation?
- **[L3]** How would you design payment state across multiple services?
- **[L3]** What should happen when a provider says success but your database transaction fails?

## Interview Answers
1. A client/request identifier that lets the service return the same business result instead of applying a duplicate effect.
2. The provider may have completed the operation while the response was lost or delayed.
3. Enforce a unique scoped key, persist request/result state, reuse provider idempotency keys, and handle concurrent duplicate requests.
4. It compares local payment/order records with authoritative provider records and repairs/flags differences.
5. Use local payment state + outbox/events, a saga/process manager, explicit eventual states, compensation, and reconciliation.
6. Retry local persistence safely from durable provider evidence, query provider status using the idempotency/request ID, and never issue a second charge blindly.

## Expert Perspective
Payment architecture is primarily about uncertain outcomes and auditability. The happy-path provider call is the easy part; production quality is proven by duplicate requests, timeout recovery, reconciliation, security, and operator controls.
