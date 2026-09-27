---
id: python-collections
slug: collections
title: Python Collections, Comprehensions, and Data Access
category: python
categoryTitle: Python
difficulty: beginner
estimatedMinutes: 40
version:
  minimum: "Python 3.12+"
prerequisites: [python-fundamentals]
tags: [python, collections, list, dict, set, comprehensions]
relatedTopics: [python-decorators-generators, python-type-hints-testing]
order: 30
status: published
---
# Python Collections, Comprehensions, and Data Access

## Introduction
Python's main built-in collections are `list`, `tuple`, `dict`, and `set`. Their characteristics determine lookup, ordering, uniqueness, mutation, and memory behavior.

```text
list  → ordered/indexed/mutable, duplicates allowed
tuple → ordered/immutable, fixed value
dict  → key → value lookup
set   → unique values/membership lookup
```

## Purpose
Choosing a collection from the access pattern prevents accidental O(n) scans, duplicate data, and unnecessary intermediate allocations.

## Simple examples
```python
orders = [
    {"id": "1", "status": "Paid", "total": 100},
    {"id": "2", "status": "Pending", "total": 50},
]

paid = [order for order in orders if order["status"] == "Paid"]
by_id = {order["id"]: order for order in orders}
statuses = {order["status"] for order in orders}
total = sum(order["total"] for order in orders if order["status"] == "Paid")
```

The last expression is a generator consumed by `sum`; it does not need to create an intermediate list.

## Professional company-level example
Use `defaultdict` for grouping and explicit types for stable contracts:

```python
from collections import defaultdict

def group_by_customer(orders: list[Order]) -> dict[str, list[Order]]:
    grouped: defaultdict[str, list[Order]] = defaultdict(list)
    for order in orders:
        grouped[order.customer_id].append(order)
    return dict(grouped)
```

For large data, stream batches from the database/file instead of loading every row into a list. For repeated membership, use a set/dict rather than scanning a list.

## Complexity mental model

```text
list[index]       → average O(1)
value in list     → O(n)
dict[key] lookup  → average O(1)
value in set      → average O(1)
insert middle list→ O(n)
```

Hash-based lookup depends on good hash behavior and uses more memory than a compact list.

## Comparison
| Collection | Ordering | Mutable | Membership | Use |
|---|---:|---:|---:|---|
| `list` | Yes | Yes | O(n) | Ordered sequence |
| `tuple` | Yes | No | O(n) | Fixed value/record |
| `dict` | Insertion | Yes | Key O(1) avg | Lookup/index |
| `set` | No semantic order | Yes | O(1) avg | Unique membership |
| Generator | Lazy sequence | N/A | One pass | Streaming |

## Interview Questions
- **[L1]** When would you use a list, tuple, dictionary, or set?
- **[L1]** What is a comprehension?
- **[L2]** Why is set membership usually faster than list membership?
- **[L2]** What is the difference between a list comprehension and generator expression?
- **[L3]** How would you process a dataset too large to fit in memory?
- **[L3]** How do you prevent a collection choice from becoming a production bottleneck?

## Interview Answers
1. Use list for ordered mutable sequences, tuple for immutable fixed values, dict for key lookup, and set for unique membership checks.
2. It creates a collection by transforming/filtering another iterable in a concise expression.
3. Sets/dicts use hash tables for average constant-time membership/key lookup; lists scan linearly.
4. A list comprehension builds all results eagerly; a generator yields lazily and can process large/unbounded input with lower peak memory.
5. Stream/generate rows, process bounded batches, aggregate incrementally, and avoid materializing the entire dataset.
6. Identify access operations, estimate size, choose complexity-appropriate structures, profile memory/time, and test with production-like data distribution.

## Expert perspective
Collection choice is a design decision. A list used for repeated membership in a hot path, or an eager list built from millions of records, can create more real cost than a complex algorithm elsewhere.
