---
id: python-type-hints-testing
slug: type-hints-testing
title: Python Type Hints and Testing with pytest
category: python
categoryTitle: Python
difficulty: intermediate
estimatedMinutes: 45
version:
  minimum: "Python 3.12+"
prerequisites: [python-fundamentals, python-error-handling]
tags: [python, type-hints, mypy, pytest, testing]
relatedTopics: [python-fastapi, python-error-handling]
order: 70
status: published
---
# Python Type Hints and Testing with pytest

## Introduction
Python type hints document expected values and enable static tools such as mypy/Pyright. The interpreter does not enforce them automatically. `pytest` tests runtime behavior using plain assertions, fixtures, parametrization, and plugins.

```text
Type hints → static analysis before running
pytest     → behavior verification while running
```

## Purpose
Dynamic typing speeds experimentation but allows type mistakes to reach runtime. Type checking, tests, linting, and CI provide feedback before a change reaches users.

## Simple example
```python
from dataclasses import dataclass

@dataclass(frozen=True)
class Order:
    id: str
    total: float

def total_orders(orders: list[Order]) -> float:
    return sum(order.total for order in orders)
```

Run a checker in CI:

```bash
mypy src/
```

A type hint is not runtime validation; external JSON still needs Pydantic/schema validation or explicit checks.

## Professional company-level example
```python
import pytest

@pytest.fixture
def order_repository():
    repository = InMemoryOrderRepository()
    repository.seed([Order(id="1", total=100)])
    yield repository
    repository.clear()

@pytest.mark.parametrize(
    "order_id, expected",
    [("1", 100), ("missing", None)]
)
def test_find_order(order_repository, order_id, expected):
    result = order_repository.find(order_id)
    assert result is None if expected is None else result.total == expected
```

A maintainable test suite asserts outcomes and business behavior rather than internal private method calls. Use real integration dependencies for provider-specific behavior such as SQL translation; use fakes/mocks for slow/external side effects.

## Testing layers

```text
Pure functions/domain rules → fast unit tests
Application policies        → fakes + unit tests
HTTP/API contract           → integration tests
Database/provider behavior  → real-engine integration tests
External provider           → sandbox/contract tests
```

## Comparison
| Tool | Purpose |
|---|---|
| Type hints | Communicate/check structure |
| mypy/Pyright | Static analysis |
| pytest | Runtime behavior tests |
| Fixture | Reusable setup/teardown |
| Parametrize | Same behavior across cases |
| Integration test | Real boundary/provider behavior |

## Interview Questions
- **[L1]** Are Python type hints enforced by the interpreter?
- **[L1]** How does pytest discover tests?
- **[L2]** What is a pytest fixture?
- **[L2]** Why should mypy run in CI?
- **[L3]** How do you choose between mocks, fakes, and real dependencies?
- **[L3]** What makes a Python test suite trustworthy rather than merely large?

## Interview Answers
1. No. They are annotations for tools/documentation unless a runtime validation library explicitly uses them.
2. It discovers conventional test files/functions, normally files like `test_*.py` and functions beginning with `test_`.
3. A fixture provides reusable setup and optional teardown to tests that request it as a parameter.
4. Local type hints can drift or be skipped. CI enforces consistent checking for every contributor/change.
5. Use real dependencies where behavior is fast/deterministic, fakes for application policies, and mocks/sandboxes for external side effects or expensive boundaries.
6. It tests meaningful behavior and edge cases, is deterministic, catches regressions, uses the right integration level, and remains understandable when it fails.

## Expert perspective
Type checking and testing solve different problems. Senior Python teams use both: static tools catch structural mistakes early, while behavior/integration tests prove what the running system actually does.
