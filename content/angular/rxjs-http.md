---
id: angular-rxjs-http
slug: rxjs-http
title: RxJS and Angular HTTP Client
category: angular
categoryTitle: Angular
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Angular 17+"
prerequisites: [angular-fundamentals, angular-dependency-injection]
tags: [angular, rxjs, http, observables, cancellation]
relatedTopics: [angular-change-detection-signals, dotnet-aspnet-core-backend]
order: 60
status: published
---
# RxJS and Angular HTTP Client

## Introduction
RxJS represents values/events over time as Observables. Angular `HttpClient` returns cold Observables: the request normally starts when something subscribes, uses the `async` pipe, or bridges it with `toSignal`.

```text
user input
    ↓ debounceTime
stable search term
    ↓ switchMap
HTTP request
    ↓ response/error
UI state
```

## Purpose
RxJS is useful when timing, cancellation, combination, retry, or multiple emissions matter. It gives Angular a consistent way to compose search boxes, HTTP calls, WebSockets, timers, polling, and event streams.

## Real-World Simple Example
```typescript
searchOrders(term$: Observable<string>): Observable<Order[]> {
  return term$.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(term => term.trim()
      ? this.http.get<Order[]>('/api/orders', { params: { q: term } })
      : of([]))
  );
}
```

`debounceTime` avoids a request per keystroke. `distinctUntilChanged` skips identical terms. `switchMap` cancels the previous request when a newer term arrives.

## Operator decision guide

```text
New value makes old work irrelevant?
    → switchMap
Every value must finish, order not important?
    → mergeMap with bounded concurrency
Every value must finish in order?
    → concatMap
Ignore new values while current work runs?
    → exhaustMap
Need all independent results?
    → forkJoin / combineLatest, depending on completion semantics
```

## Professional company-level example
```typescript
readonly results = toSignal(
  toObservable(this.searchTerm).pipe(
    debounceTime(300),
    distinctUntilChanged(),
    filter(term => term.length >= 2),
    switchMap(term => this.api.search(term).pipe(
      retry({ count: 2, delay: 500 }),
      catchError(() => of([]))
    )),
    takeUntilDestroyed()
  ),
  { initialValue: [] }
);
```

The component does not manually subscribe and forget to unsubscribe. The observable lifetime is tied to the component. Retry is bounded; failure becomes an intentional empty/error state rather than silently killing the stream.

## HTTP and error behavior
Angular HTTP Observables generally emit one response and complete. If an error reaches a long-lived stream without `catchError` at the right boundary, that stream terminates and later search values may no longer work. Put error handling where you know how to recover:

```typescript
this.http.get<Order[]>('/api/orders').pipe(
  catchError(error => {
    this.logger.error('Order search failed', error);
    return throwError(() => new UserVisibleApiError());
  })
);
```

Use interceptors for cross-cutting concerns such as auth headers, correlation IDs, response error mapping, and metrics — not for business decisions specific to one screen.

## Common mistakes

```typescript
// Bad: every subscription starts a cold HTTP request
this.api.getOrders().subscribe(...);
this.api.getOrders().subscribe(...); // second network call
```

Use a service cache/share strategy when multiple consumers genuinely share one response:

```typescript
orders$ = this.http.get<Order[]>('/api/orders').pipe(
  shareReplay({ bufferSize: 1, refCount: true })
);
```

Do not use `mergeMap` for type-ahead search if stale results can arrive after current results. Do not use `retry` for validation/authorization errors or non-idempotent writes without an idempotency strategy.

## Comparison
| Operator | Behavior |
|---|---|
| `switchMap` | Cancel previous inner work |
| `mergeMap` | Run inner work concurrently |
| `concatMap` | Queue inner work in order |
| `exhaustMap` | Ignore new input while busy |
| `forkJoin` | Emit once after all complete |
| `combineLatest` | Emit when each source has emitted, then on changes |

## Interview Questions
- **[L1]** What is an Observable and when does an Angular HTTP call start?
- **[L1]** What does `debounceTime` do?
- **[L2]** Why is `switchMap` useful for type-ahead search?
- **[L2]** Compare `switchMap`, `mergeMap`, `concatMap`, and `exhaustMap`.
- **[L3]** How would you design cancellation/retry for dependent API calls?
- **[L3]** How do you prevent subscriptions and shared HTTP state from leaking across components?

## Interview Answers
1. An Observable represents a stream; Angular's cold HTTP request normally starts when subscribed or consumed through `async`/`toSignal`.
2. It waits for a quiet period and emits the latest value, reducing rapid repeated work.
3. A new search makes the previous request irrelevant, so `switchMap` unsubscribes/cancels the prior inner request and prevents stale results from winning.
4. `switchMap` replaces, `mergeMap` overlaps, `concatMap` queues, and `exhaustMap` ignores new values while current work is active.
5. Use the operator matching cancellation/order semantics, bounded retry with backoff only for transient failures, propagated cancellation, timeout budgets, and an explicit error state.
6. Prefer `async` pipe/`toSignal`/`takeUntilDestroyed`, use `shareReplay` only with a clear cache lifetime, and test teardown/retry/error behavior.

## Expert perspective
RxJS operator selection is behavior design, not syntax preference. The correct choice depends on whether old work is still valuable, whether order matters, how much concurrency downstream can handle, and how cancellation should affect the user experience.
