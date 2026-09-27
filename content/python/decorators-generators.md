---
id: python-decorators-generators
slug: decorators-generators
title: Python Decorators, Generators, and Context Managers
category: python
categoryTitle: Python
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Python 3.12+"
prerequisites: [python-fundamentals, python-collections]
tags: [python, decorators, generators, yield, context-managers]
relatedTopics: [python-async, python-type-hints-testing]
order: 40
status: published
---
# Python Decorators, Generators, and Context Managers

## Introduction
These features solve different forms of repeated behavior:

```text
Decorator       → wrap behavior around a function
Generator       → produce a sequence lazily
a context manager→ guarantee setup/cleanup
```

## Purpose
They reduce duplication in logging/retry/timing, stream large data without loading it all, and guarantee resource cleanup even when an exception occurs.

## Decorator example
```python
from functools import wraps

def retry(attempts: int):
    def decorator(function):
        @wraps(function)
        def wrapper(*args, **kwargs):
            for attempt in range(attempts):
                try:
                    return function(*args, **kwargs)
                except TransientError:
                    if attempt == attempts - 1:
                        raise
        return wrapper
    return decorator
```

`@retry(3)` is equivalent to assigning the function returned by `retry(3)(function)`. `wraps` preserves the original name/docstring for debugging and tooling.

## Generator example
```python
def read_lines(path: str):
    with open(path) as file:
        for line in file:
            yield line.strip()

for line in read_lines("events.log"):
    process(line)
```

The file is processed one line at a time. A list would load the entire file into memory.

## Context manager example
```python
from contextlib import contextmanager
from time import perf_counter

@contextmanager
def measure(name: str):
    start = perf_counter()
    try:
        yield
    finally:
        print(name, perf_counter() - start)

with measure("order-processing"):
    process_order(order)
```

The `finally` block runs on success and exception, making this suitable for cleanup/timing/transactions.

## Professional company-level decisions
Use decorators for consistent cross-cutting behavior, but do not hide security/business logic behind many stacked decorators. Use generators for large streams, but remember they are one-pass and cannot be randomly indexed. Use context managers for files, locks, connections, transactions, and temporary state.

A decorator that logs all arguments can leak tokens/passwords. A generator that leaves a resource open without a context manager can leak file handles. Abstractions still need security/lifecycle review.

## Comparison
| Feature | Main benefit | Main risk |
|---|---|
| Decorator | Reusable cross-cutting behavior | Hidden execution layers |
| Generator | Low-memory lazy processing | One-pass/stateful behavior |
| Context manager | Guaranteed cleanup | Incorrect ownership/nesting |
| List | Reusable/random access | Full memory cost |

## Interview Questions
- **[L1]** What does a decorator do?
- **[L1]** What is the difference between a list and generator?
- **[L2]** Why is `functools.wraps` important?
- **[L2]** How does a context manager guarantee cleanup?
- **[L3]** How would you design a bounded caching decorator?
- **[L3]** When is a generator worse than an eagerly-built list?

## Interview Answers
1. It takes a function/class and returns a wrapped version with additional behavior without changing the original body.
2. A list eagerly stores every value; a generator yields values lazily and usually only once.
3. It preserves name/docstring/module metadata so debugging, introspection, documentation, and other decorators remain correct.
4. `with` invokes setup and guarantees exit/cleanup code even when the block raises.
5. Key by normalized hashable arguments, define TTL or maximum size/LRU eviction, avoid caching sensitive results, and make concurrency behavior explicit.
6. When you need random access/repeated iteration, the data is small, or generator overhead/complexity exceeds memory savings.

## Expert perspective
These features are powerful because they make lifecycle and cross-cutting behavior composable, but hidden control flow has a readability cost. Use them where the abstraction makes resource ownership or repeated behavior clearer, not merely shorter.
