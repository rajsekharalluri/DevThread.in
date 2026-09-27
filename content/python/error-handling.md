---
id: python-error-handling
slug: error-handling
title: Python Error Handling and Exceptions
category: python
categoryTitle: Python
difficulty: intermediate
estimatedMinutes: 40
version:
  minimum: "Python 3.12+"
prerequisites: [python-fundamentals]
tags: [python, exceptions, error-handling, validation]
relatedTopics: [python-fastapi, python-type-hints-testing]
order: 60
status: published
---
# Python Error Handling and Exceptions

## Introduction
Python uses exceptions to represent failures that normal control flow cannot complete. `try`, `except`, `else`, and `finally` let code handle known failures, separate success logic, and guarantee cleanup.

```text
try operation
   ├── success → else
   ├── known failure → except
   └── always → finally cleanup
```

## Purpose
Explicit exception design prevents silent data loss, gives callers actionable failure categories, and keeps internal details from leaking through APIs/logs.

## Simple example
```python
class InsufficientStockError(Exception):
    def __init__(self, sku: str, requested: int, available: int):
        self.sku = sku
        self.requested = requested
        self.available = available
        super().__init__(f"Insufficient stock for {sku}")

try:
    reserve_stock("SKU-1", 5)
except InsufficientStockError as error:
    logger.warning("Stock reservation rejected for %s", error.sku)
```

The exception carries structured data; callers do not need to parse a message to understand what happened.

## Professional company-level example
Translate low-level failures at application boundaries while preserving the original cause:

```python
try:
    result = await payment_gateway.charge(order_id, amount)
except GatewayTimeoutError as error:
    raise PaymentProcessingError(order_id, "Gateway timeout") from error
```

At an API boundary, log the full internal exception securely and return a safe status/message. Do not expose stack traces, SQL details, file paths, or credentials.

## Important rules

```python
# Catch what you understand.
try:
    value = parse_order(payload)
except ValueError as error:
    return invalid_request(str(error))

# Do not silently swallow everything.
except Exception:
    raise
```

`finally` executes even when an exception propagates, making it suitable for cleanup. Prefer context managers for files, locks, connections, and transactions:

```python
with open(path) as file:
    content = file.read()
```

## Comparison
| Choice | Use |
|---|---|
| Return `None`/result | Expected common outcome |
| Raise custom exception | Failure caller should handle by category |
| `finally` | Guaranteed cleanup |
| `raise ... from error` | Preserve low-level cause while translating |
| Broad catch | Only at deliberate boundary with safe logging |

## Interview Questions
- **[L1]** What does `finally` guarantee?
- **[L1]** Why use custom exception types?
- **[L2]** What does `raise NewError(...) from original` do?
- **[L2]** Why is a bare `except:` dangerous?
- **[L3]** How should exceptions be handled at a public API boundary?
- **[L3]** How do you distinguish retryable from permanent failures?

## Interview Answers
1. It runs whether the protected code succeeds, raises, or propagates an exception, so it is useful for guaranteed cleanup.
2. It lets callers catch a precise failure category and exposes structured attributes instead of forcing message parsing.
3. It explicitly chains the original exception as context, preserving the original traceback while presenting a higher-level domain error.
4. It catches everything including system-level interrupts and can silently hide programming bugs. Catch only errors you understand.
5. Map internal exceptions to safe status codes/messages, log full context securely with correlation ID, and never return internal stack/schema/secret details.
6. Define exception categories/contracts: transient dependency timeout can retry with limits; validation/auth/business-rule failures should not be retried blindly; make side effects idempotent.

## Expert perspective
Exception handling is an ownership decision: catch an error only where the code has enough context to recover or translate it correctly. Everything else should propagate to a deliberate boundary rather than being silently swallowed.
