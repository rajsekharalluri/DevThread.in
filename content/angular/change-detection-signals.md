---
id: angular-change-detection-signals
slug: change-detection-signals
title: Angular Change Detection, Signals, and Performance
category: angular
categoryTitle: Angular
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Angular 17+"
prerequisites: [angular-fundamentals, angular-rxjs-http]
tags: [angular, signals, change-detection, onpush, zoneless, performance]
relatedTopics: [angular-ngrx, angular-fundamentals]
order: 70
status: published
---
# Angular Change Detection, Signals, and Performance

## Introduction
Change detection is Angular's process for deciding when bindings need to be evaluated and the DOM updated. Signals provide dependency-aware reactivity: Angular can know which component/template depends on which value instead of broadly checking everything after unrelated asynchronous work.

```text
signal.set/update
      ↓
consumers marked stale
      ↓
Angular schedules update
      ↓
affected template bindings refresh
```

## Purpose
Large component trees become slow when expensive work runs during every change-detection cycle. `OnPush`, immutable state, signals, and memoized `computed` values reduce unnecessary checks while making state flow easier to reason about.

## Real-World Simple Example
```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p>{{ itemCount() }} items</p>
    <p>{{ total() | currency }}</p>
  `
})
export class CartSummaryComponent {
  readonly items = input.required<CartItem[]>();
  readonly itemCount = computed(() => this.items().length);
  readonly total = computed(() =>
    this.items().reduce((sum, item) => sum + item.price * item.quantity, 0)
  );
}
```

`computed` is for pure derived values. It recalculates when its dependencies change and caches the result between changes.

## Professional company-level example
```typescript
@Component({ /* ... */ })
export class OrderDashboardComponent {
  private readonly api = inject(OrderApi);
  readonly filter = signal<'all' | 'pending'>('all');
  readonly orders = toSignal(this.api.getOrders(), { initialValue: [] as Order[] });

  readonly visibleOrders = computed(() => {
    const orders = this.orders();
    return this.filter() === 'all'
      ? orders
      : orders.filter(order => order.status === 'Pending');
  });

  // Use effects for side effects only, such as analytics or third-party widgets.
  constructor() {
    effect(() => this.analytics.track('visible_orders', this.visibleOrders().length));
  }
}
```

Use `OnPush`, stable `track` expressions in loops, pure transformations, virtual scrolling for huge lists, and profiling tools before changing architecture. Do not put expensive function calls directly in templates:

```html
<!-- Avoid: can execute on every check -->
<p>{{ calculateTotal(order) }}</p>
```

Prefer a computed value or precomputed view model.

## Zone-based versus zoneless mental model

```text
Zone-based:
async event → Angular notices “something may have changed” → broader checks

Signal/zoneless-oriented:
explicit signal change → known consumers marked → targeted update
```

A single asynchronous operation can still trigger expensive work if state is mutated broadly or a component uses default checking with expensive template expressions.

## Comparison
| Tool | Use |
|---|---|
| Default checking | Simple apps/legacy code |
| `OnPush` | Skip checks until known inputs/events/reactive values change |
| `signal` | Writable reactive state |
| `computed` | Memoized pure derivation |
| `effect` | Logging/analytics/imperative side effect |
| `async` pipe/`toSignal` | Bind observable lifetime to UI |

## Interview Questions
- **[L1]** What does `OnPush` change?
- **[L1]** What is the difference between `signal`, `computed`, and `effect`?
- **[L2]** Why is mutating an array in place dangerous for reactive UI state?
- **[L2]** Why should `effect` not normally derive application state?
- **[L3]** How would you diagnose unnecessary change detection in a large Angular app?
- **[L3]** How would you migrate a Zone.js-heavy application toward signal-based change detection safely?

## Interview Answers
1. `OnPush` lets Angular skip a component unless relevant input references/events/observable or signal dependencies indicate a possible update.
2. `signal` is writable state, `computed` is read-only memoized derivation, and `effect` runs side effects when the signals it reads change.
3. In-place mutation can keep the same array/object reference, so consumers may not be notified. Replace the reference with immutable updates.
4. Effects are for external side effects. Using them to set derived signals creates indirect update chains, weakens memoization, and can create feedback loops.
5. Profile with Angular DevTools, find components checked and expensive template expressions, add OnPush/signals, move calculations into computed values, stabilize list tracking, and re-measure.
6. Start with leaf/feature components, convert shared state to immutable signals, add tests around inputs/effects, remove template work, measure before/after, and migrate boundaries incrementally rather than changing the entire app at once.

## Expert perspective
Performance is mostly about update scope and work per update. Senior Angular engineers make state ownership and reactive dependencies explicit, then use profiling to prove which components and computations are actually expensive.
