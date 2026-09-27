---
id: python-fundamentals
slug: fundamentals
title: Python Fundamentals
category: python
categoryTitle: Python
difficulty: beginner
estimatedMinutes: 50
version:
  minimum: "Python 3.12+"
prerequisites: []
tags: [python, fundamentals, types, functions, runtime]
relatedTopics: [python-oop, python-collections, python-type-hints-testing]
order: 10
status: published
---
# Python Fundamentals

## Introduction
Python is a dynamically typed, interpreted language designed for readability and rapid development. It is widely used for automation, APIs, data engineering, AI, testing, and scripting.

```text
Python source
   ↓
CPython bytecode
   ↓
Python virtual machine
   ↓
Runtime objects + reference counting + cyclic GC
```

## Purpose
Python trades some compile-time guarantees and raw execution speed for short feedback loops, expressive built-in types, and a very large ecosystem. That trade is excellent for automation, data/AI workflows, and APIs when the team uses tests and type hints to recover safety.

## Core types and names
Python variables are names bound to objects, not fixed typed memory boxes:

```python
quantity = 3
quantity = "three"  # legal at runtime; static checkers can flag this if annotated

order = {"id": "1001", "total": 129.99}
items = ["book", "keyboard"]
unique_statuses = {"Paid", "Pending"}
```

`list` is ordered/mutable, `tuple` is ordered/immutable, `dict` is a hash map, and `set` stores unique hashable values. The choice changes lookup complexity and mutation behavior.

## Functions and practical example
```python
def calculate_total(
    items: list[dict[str, float]],
    tax_rate: float = 0.08,
) -> float:
    subtotal = sum(
        item["price"] * item["quantity"]
        for item in items
    )
    return round(subtotal * (1 + tax_rate), 2)
```

Default arguments are evaluated once when the function is defined. Never use a mutable list/dict as a default unless shared state is intentional:

```python
# Good

def add_item(item: str, items: list[str] | None = None) -> list[str]:
    result = [] if items is None else items
    result.append(item)
    return result
```

## Professional company-level example
Use dataclasses/value objects for validated domain data, type hints at public boundaries, and explicit exceptions:

```python
from dataclasses import dataclass

@dataclass(frozen=True)
class Money:
    amount: float
    currency: str

    def __post_init__(self) -> None:
        if self.amount < 0:
            raise ValueError("Money cannot be negative")
        if len(self.currency) != 3:
            raise ValueError("Currency must be an ISO-style code")
```

At an API/message boundary, validate untrusted input rather than assuming type hints are runtime enforcement. Python type hints help editors and mypy/Pyright; the interpreter does not enforce them automatically.

## Runtime and performance
CPython uses reference counting plus cyclic garbage collection. The Global Interpreter Lock (GIL) allows only one thread to execute Python bytecode at a time in a process, so:

```text
I/O-bound → asyncio or threads can improve concurrency
CPU-bound → multiprocessing/native libraries usually provide parallelism
```

Profile before optimizing. `cProfile`, sampling profilers, memory tools, and production metrics are more reliable than guessing from source appearance.

## Comparison
| Need | Python approach |
|---|---|
| Small script/automation | Plain functions/modules |
| Data validation | Pydantic/dataclass + explicit checks |
| I/O concurrency | `asyncio`/async libraries |
| CPU parallelism | Multiprocessing/native libraries |
| Static safety | Type hints + mypy/Pyright |
| Web API | FastAPI/ASGI |

## Interview Questions
- **[L1]** Is Python statically or dynamically typed?
- **[L1]** Why is a mutable default argument dangerous?
- **[L2]** What is the GIL and how does it affect CPU-bound threads?
- **[L2]** How do list, set, and dictionary access patterns differ?
- **[L3]** How would you decide whether a performance-critical component should remain Python?
- **[L3]** How do you make a dynamically typed Python codebase safer for a large team?

## Interview Answers
1. Python is dynamically typed at runtime; optional type hints and static tools provide compile-time-like analysis but the interpreter does not enforce them by default.
2. Mutable defaults are created once at function definition and shared across calls, so one call can unexpectedly affect later calls. Use `None` and create a new value inside.
3. The CPython GIL permits one thread to execute Python bytecode at a time, limiting CPU-bound thread parallelism. I/O waits can still overlap; multiprocessing/native code handles CPU parallelism.
4. Lists provide ordered/indexed storage but linear membership; sets provide average constant-time membership; dictionaries provide average constant-time key lookup.
5. Profile first. If CPU-bound Python is truly the bottleneck, try algorithm/data-structure improvements, NumPy/native libraries, multiprocessing, or a targeted Rust/C extension before rewriting an entire system.
6. Add type hints at public boundaries, run mypy/Pyright in CI, use pytest behavior tests, validate external input, enforce formatting/linting, and document runtime/version conventions.

## Expert perspective
Python expertise is knowing where dynamic flexibility helps and where it creates risk. Senior engineers combine type hints, tests, profiling, explicit validation, and correct concurrency models rather than assuming Python's simplicity removes engineering trade-offs.
