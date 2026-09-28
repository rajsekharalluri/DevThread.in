---
id: angular-ngrx
slug: ngrx
title: "NgRx State Management: Store, Actions, Reducers, Effects, and Selectors"
category: angular
categoryTitle: Angular
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "NgRx 17+ / Angular 17+"
prerequisites: [angular-rxjs-http, angular-change-detection-signals]
tags: [angular, ngrx, store, actions, reducers, effects, selectors]
relatedTopics: [angular-dependency-injection, angular-rxjs-http]
order: 80
status: published
---
# NgRx State Management: Store, Actions, Reducers, Effects, and Selectors

## Introduction
NgRx is a Redux-style state-management library for Angular. State is stored centrally, changes are described by actions, reducers update state immutably, effects handle side effects, and selectors read/derive data.

```text
Component
   │ dispatch action
   ▼
Store → reducer → new immutable state
   │
   └── effect → HTTP/side effect → success/failure action
                                   reducer
```

## Purpose
NgRx makes complex shared state traceable. It is useful when many unrelated components depend on the same state, async workflows have multiple transitions, or action history/debugging is valuable.

Do not introduce NgRx for a small component's local state. Angular signals and a feature service are simpler until real cross-feature complexity appears.

## Real-World Simple Example
```typescript
export const ordersActions = createActionGroup({
  source: 'Orders',
  events: {
    'Load': emptyProps(),
    'Load Success': props<{ orders: Order[] }>(),
    'Load Failure': props<{ message: string }>()
  }
});

export const reducer = createReducer(
  initialState,
  on(ordersActions.load, state => ({ ...state, loading: true })),
  on(ordersActions.loadSuccess, (state, { orders }) => ({
    ...state, orders, loading: false
  }))
);
```

## Professional company-level example
```typescript
export const loadOrders = createEffect(
  (actions$ = inject(Actions), api = inject(OrderApi)) => actions$.pipe(
    ofType(ordersActions.load),
    switchMap(() => api.getOrders().pipe(
      map(orders => ordersActions.loadSuccess({ orders })),
      catchError(error => of(ordersActions.loadFailure({ message: error.message })))
    ))
  ),
  { functional: true }
);

export const selectPendingOrders = createSelector(
  selectOrdersState,
  state => state.orders.filter(order => order.status === 'Pending')
);
```

The component only dispatches and selects:

```typescript
readonly pendingOrders = this.store.selectSignal(selectPendingOrders);

load(): void {
  this.store.dispatch(ordersActions.load());
}
```

## Important rules

- Reducers must be pure: no HTTP, timers, logging side effects, or mutation.
- Effects own HTTP, navigation, analytics, and other side effects.
- Selectors derive data; do not duplicate derived state in the store.
- Actions describe events (`Order Submitted`), not vague commands (`Do Thing`) when possible.
- Immutable updates preserve change detection and time-travel debugging.
- Store only state that genuinely needs cross-feature ownership.

## Comparison
| State approach | Best fit | Cost |
|---|---|---|
| Component signal | Local UI state | Not shared |
| Injectable signal service | Feature/shared state | Less formal traceability |
| RxJS service | Async streams | Subscription/operator management |
| NgRx | Complex cross-feature workflows | Boilerplate and indirection |

## Interview Questions
- **[L1]** What are actions, reducers, effects, and selectors?
- **[L1]** Why must reducers be pure?
- **[L2]** Why does HTTP belong in an effect instead of a reducer?
- **[L2]** How do selectors improve performance?
- **[L3]** When should a team choose NgRx instead of a signal-based service?
- **[L3]** How would you prevent a large NgRx store from becoming an unmaintainable global object?

## Interview Answers
1. Actions describe events, reducers produce new state, effects handle side effects and dispatch follow-up actions, and selectors read/derive state.
2. Purity makes state transitions deterministic, replayable, testable, and compatible with action/time-travel debugging.
3. HTTP is a side effect and reducers must be synchronous/pure. Effects centralize async orchestration and success/failure transitions.
4. Memoized selectors recompute only when relevant input references change and prevent components from duplicating derivation logic.
5. Use NgRx for genuinely shared, complex, async state and debugging needs. Use signals/services for local or simple feature state.
6. Split state by bounded feature, keep actions/selectors close to ownership, avoid storing derivations, define effects boundaries, and review state ownership rather than putting every API response in one global slice.

## Expert perspective
NgRx is a complexity-management tool. Senior engineers introduce it only when centralized, observable state transitions repay the boilerplate, and they preserve its value by keeping reducers pure, effects explicit, selectors focused, and feature ownership clear.
