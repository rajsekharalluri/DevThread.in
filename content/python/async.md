---
id: python-async
slug: async
title: Python Asyncio and Asynchronous I/O
category: python
categoryTitle: Python
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Python 3.12+"
prerequisites: [python-fundamentals, python-decorators-generators]
tags: [python, asyncio, async, await, concurrency, cancellation]
relatedTopics: [python-fastapi, csharp-concurrency-tasks]
order: 50
status: published
---
# Python Asyncio and Asynchronous I/O

## Introduction
`asyncio` is Python's cooperative concurrency model. An event loop runs many coroutines on a thread; when one coroutine awaits I/O, it yields control so another ready coroutine can run.

```text
Event loop thread
   ├── request A → await HTTP → suspended
   ├── request B → await DB   → suspended
   ├── request C → process    → running
   └── resume completed I/O
```

## Purpose
Asyncio improves concurrency for I/O-bound work such as HTTP calls, database operations, sockets, and file/network waiting. It does not make CPU-bound Python bytecode parallel.

## Simple example
```python
import asyncio
import httpx

async def fetch_order(client: httpx.AsyncClient, order_id: str) -> dict:
    response = await client.get(f"/orders/{order_id}")
    response.raise_for_status()
    return response.json()

async def fetch_orders(ids: list[str]) -> list[dict]:
    async with httpx.AsyncClient(base_url="https://api.example") as client:
        return await asyncio.gather(
            *(fetch_order(client, order_id) for order_id in ids)
        )
```

`gather` starts the coroutines concurrently. It does not mean ten OS threads are created.

## Professional company-level example
Bound concurrency and timeout each operation:

```python
async def fetch_batch(ids: list[str], limit: int = 10) -> list[dict]:
    semaphore = asyncio.Semaphore(limit)

    async with httpx.AsyncClient(base_url="https://api.example") as client:
        async def one(order_id: str) -> dict:
            async with semaphore:
                try:
                    return await asyncio.wait_for(
                        fetch_order(client, order_id), timeout=5
                    )
                except asyncio.TimeoutError:
                    return {"id": order_id, "error": "timeout"}

        return await asyncio.gather(*(one(order_id) for order_id in ids))
```

The semaphore protects the downstream API; the timeout prevents a hung call from holding a task forever.

## Blocking mistake

```python
# Bad: blocks the entire event loop.
async def bad():
    response = requests.get("https://api.example/orders")
    return response.json()
```

Use an async client inside async code:

```python
async def good(client: httpx.AsyncClient):
    response = await client.get("/orders")
    return response.json()
```

A single blocking call can stall every coroutine sharing that event loop, not just the caller that made the call.

## CPU-bound work

```text
I/O-bound → asyncio tasks can overlap waiting
CPU-bound → asyncio does not create CPU parallelism
```

For CPU-heavy work, use multiprocessing, a native/vectorized library, a worker queue, or a separate service. Do not put a long CPU loop directly inside an async endpoint.

## Comparison
| Tool | Best fit |
|---|---|
| `asyncio` | I/O concurrency in one process |
| `threading` | I/O with blocking libraries |
| `multiprocessing` | CPU parallelism |
| `asyncio.gather` | Run independent coroutines together |
| Semaphore | Bound concurrent work |
| Queue | Producer/consumer coordination |

## Interview Questions
- **[L1]** What does `await` do in Python?
- **[L1]** Does asyncio create a new thread for every coroutine?
- **[L2]** Why does a blocking function inside async code stall unrelated requests?
- **[L2]** Why must concurrency be bounded when using `asyncio.gather`?
- **[L3]** How would you design timeout and cancellation for a production async batch?
- **[L3]** When would you choose asyncio, threads, or multiprocessing?

## Interview Answers
1. It suspends the current coroutine until the awaitable completes and returns control to the event loop.
2. No. Coroutines usually share the event-loop thread and yield cooperatively at `await` points.
3. The event loop has one execution thread; blocking code prevents it from scheduling any other coroutine until the block finishes.
4. An unbounded batch can exhaust connections, trigger rate limits, overload downstream systems, and consume memory for pending tasks.
5. Use per-operation timeouts, propagate cancellation, bound concurrency, classify partial failures, and make side effects idempotent.
6. Use asyncio for async-compatible I/O, threads for blocking I/O libraries, and multiprocessing/native compute for CPU-bound parallelism.

## Expert perspective
Asyncio provides concurrency, not magic speed. Senior Python engineers audit every call inside an async path for blocking behavior and design the concurrency limit from downstream capacity rather than from the number of input items.
