---
id: architecture-bounded-contexts-acl
slug: bounded-contexts-acl
title: Bounded Contexts and Anti-Corruption Layers
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "DDD concepts"
prerequisites: [architecture-design-patterns-ddd, architecture-microservices]
tags: [ddd, bounded-contexts, anti-corruption-layer, integration]
relatedTopics: [architecture-domain-integration-events, architecture-microservices]
order: 160
status: published
---
# Bounded Contexts and Anti-Corruption Layers

## Introduction
A bounded context is a boundary where a domain model and its language have one consistent meaning. An Anti-Corruption Layer (ACL) translates an external model into the local model so another system's concepts do not leak into your domain.

```text
External context
  Customer: AccountStatus, CreditLimit
          ↓ ACL/translator
Local context
  Buyer: Eligibility, SpendingPolicy
```

## Purpose
Different teams and business areas often use the same word differently. Bounded contexts allow each model to stay coherent; an ACL protects the local model while integrating with legacy systems, vendors, or another team's language.

## Simple example
A payment provider returns:

```json
{ "state": "requires_action", "amount_received": 0 }
```

The order domain should not spread provider terminology everywhere:

```csharp
public enum PaymentState { PendingCustomerAction, Authorized, Declined }

public static PaymentState TranslateProviderState(string state) => state switch
{
    "requires_action" => PaymentState.PendingCustomerAction,
    "succeeded" => PaymentState.Authorized,
    "failed" => PaymentState.Declined,
    _ => throw new UnsupportedProviderStateException(state)
};
```

## Professional company-level example
```text
Catalog context        Payments context       Fulfillment context
Product/Price          Authorization/Capture  Reservation/Shipment
      │                        │                      │
      └── versioned integration events/contracts ───┘
```

An ACL maps external IDs, statuses, money/time semantics, error codes, and retries. It should be observable and tested with contract fixtures.

## Common failure scenarios
- Sharing one domain class across contexts.
- Passing vendor DTOs into core business logic.
- Assuming identical words mean identical rules.
- Hiding translation failures or silently mapping unknown states.
- Building an ACL so large that it becomes another undocumented domain.

## Comparison
| Approach | Use |
|---|---|
| Shared model | Very strong ownership/alignment; rare |
| Direct integration | Simple stable external contract |
| ACL/translator | Protect local model from external semantics |
| Published language | Stable explicit integration contract |
| Shared database | Fast integration but high coupling |

## Interview Questions
- **[L1]** What is a bounded context?
- **[L1]** What is an Anti-Corruption Layer?
- **[L2]** Why should a vendor DTO not enter the domain model directly?
- **[L2]** How should unknown external states be handled?
- **[L3]** How do bounded contexts help organizations evolve independently?
- **[L3]** When is an ACL unnecessary complexity?

## Interview Answers
1. A boundary where a model and its language have consistent meaning and ownership.
2. A translation boundary that protects a local model from an external model's concepts/constraints.
3. External schemas change, contain provider language, and often have different invariants; leaking them couples core business rules to integration details.
4. Fail visibly, alert, preserve the original payload safely, and add an explicit mapping after understanding the new state; do not silently treat it as success.
5. Each team/context can evolve its model and release cadence while integrating through stable contracts instead of sharing internal classes/tables.
6. If the external model is stable, small, and already expresses the same meaning, direct mapping may be clearer; add a layer when semantic/ownership translation is real.

## Expert perspective
Bounded contexts are about meaning and ownership, not merely folders or services. An ACL is valuable when it prevents an external model from corrupting local language; it is wasteful when it only renames identical fields without protecting a real boundary.
