---
id: python-oop
slug: oop
title: Python Object-Oriented Programming
category: python
categoryTitle: Python
difficulty: intermediate
estimatedMinutes: 50
version:
  minimum: "Python 3.12+"
prerequisites: [python-fundamentals]
tags: [python, oop, classes, protocols, dataclasses]
relatedTopics: [python-fundamentals, python-type-hints-testing]
order: 20
status: published
---
# Python Object-Oriented Programming

## Introduction
Python supports classes, inheritance, composition, abstract base classes, protocols, and special methods. Its common style favors duck typing and composition instead of deep inheritance trees.

```text
Object
  ├── state (__dict__/slots)
  ├── behavior (methods)
  ├── protocol methods (__iter__, __eq__, __repr__)
  └── type/MRO
```

## Purpose
Use objects when state and behavior belong together, invariants need protection, or a capability must be substituted/tested. Use plain functions/data when a class would only add ceremony.

## Simple example
```python
from dataclasses import dataclass

@dataclass(frozen=True)
class Money:
    amount: float
    currency: str

    def __post_init__(self) -> None:
        if self.amount < 0:
            raise ValueError("amount cannot be negative")

    def add(self, other: "Money") -> "Money":
        if self.currency != other.currency:
            raise ValueError("currencies must match")
        return Money(self.amount + other.amount, self.currency)
```

A frozen dataclass is useful for a value object: values define meaning and callers cannot mutate it after construction.

## Professional company-level example
Use a protocol/ABC at a boundary and composition for provider variation:

```python
from typing import Protocol

class PaymentAuthorizer(Protocol):
    async def authorize(self, amount: Money) -> str: ...

class CheckoutService:
    def __init__(self, authorizer: PaymentAuthorizer) -> None:
        self._authorizer = authorizer

    async def checkout(self, order: Order) -> str:
        return await self._authorizer.authorize(order.total)
```

A Stripe adapter, test fake, and another provider can satisfy the protocol without inheriting from a large hierarchy. The checkout policy depends on a capability, not the provider SDK.

## Python object behavior
Special methods integrate classes with the language:

```python
class Batch:
    def __init__(self, items): self._items = list(items)
    def __len__(self): return len(self._items)
    def __iter__(self): return iter(self._items)
    def __repr__(self): return f"Batch({self._items!r})"
```

`__eq__` changes equality semantics; if value equality is implemented, consider `__hash__` consistently. A leading underscore is a convention, not real access control. `__slots__` can reduce per-instance memory but removes flexible attributes and has inheritance trade-offs.

## Comparison
| Approach | Best fit |
|---|---|
| Plain function | Stateless transformation |
| Dataclass | Data/value object with little ceremony |
| Class | Stateful behavior/invariants |
| Protocol | Structural capability boundary |
| ABC | Explicit enforced inheritance contract |
| Composition | Reusable interchangeable behavior |
| Deep inheritance | Rare; only with a stable true subtype relationship |

## Interview Questions
- **[L1]** What is duck typing?
- **[L1]** Why use a dataclass?
- **[L2]** How does composition differ from inheritance?
- **[L2]** What are special/dunder methods used for?
- **[L3]** When would you use a Protocol versus an abstract base class?
- **[L3]** How would you keep a Python domain model clear without creating a Java-style class hierarchy?

## Interview Answers
1. Code depends on an object's available behavior rather than its declared inheritance/type; missing behavior fails at runtime.
2. A dataclass generates common data-object behavior such as initialization/repr/equality and makes intent concise; frozen dataclasses suit immutable values.
3. Composition assembles behavior from objects and is usually more flexible; inheritance creates a tighter subtype/base-class contract and should represent a real substitutable relationship.
4. Methods such as `__eq__`, `__iter__`, `__len__`, `__repr__`, and context methods integrate custom objects with Python syntax/built-ins.
5. Use a Protocol for structural typing and lightweight consumer-owned capabilities; use ABC when you want explicit inheritance, shared implementation, or enforced abstract methods.
6. Use value objects/dataclasses for data, focused classes for stateful invariants, protocols at volatile boundaries, and plain functions when behavior has no meaningful state.

## Expert perspective
Python OOP works best when it respects Python's strengths: small composable objects, protocols, dataclasses, and explicit behavior. Senior engineers avoid copying rigid class-hierarchy habits from other languages when a function or composition communicates the design more clearly.
