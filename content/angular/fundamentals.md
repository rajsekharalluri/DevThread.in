---
id: angular-fundamentals
slug: fundamentals
title: Angular Fundamentals
category: angular
categoryTitle: Angular
difficulty: beginner
estimatedMinutes: 50
version:
  minimum: "Angular 17+ standalone"
prerequisites: []
tags: [angular, components, templates, typescript, signals]
relatedTopics: [angular-dependency-injection, angular-routing, angular-change-detection-signals]
order: 10
status: published
---
# Angular Fundamentals

## Introduction
Angular is a component-based TypeScript framework for building structured web applications. A component combines state/behavior in a TypeScript class with an HTML template and optional styles.

```text
Angular application
      │
      ├── root component
      ├── feature components
      ├── services/DI
      ├── router
      ├── HTTP/RxJS
      └── signals/change detection
```

Modern Angular favors standalone components instead of requiring every component to be declared in an `NgModule`.

## Purpose
Angular gives a large team consistent solutions for templates, routing, HTTP, forms, dependency injection, testing, and reactivity. It helps prevent each feature from inventing a different UI architecture.

## Real-World Simple Example
```typescript
@Component({
  selector: 'app-order-summary',
  standalone: true,
  template: `
    <h2>Order #{{ orderId() }}</h2>
    <p>Total: {{ total() | currency }}</p>
    <button (click)="markPaid()" [disabled]="isPaid()">Mark Paid</button>
  `
})
export class OrderSummaryComponent {
  readonly orderId = signal('1001');
  readonly total = signal(129.99);
  readonly isPaid = signal(false);

  markPaid(): void { this.isPaid.set(true); }
}
```

Data flows into the template through interpolation/property binding and comes back through event binding:

```text
Component signal/state → template
button event           → component method
```

## Professional company-level example
```typescript
@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [OrderSummaryComponent],
  template: `
    @if (loading()) { <p>Loading...</p> }
    @for (order of orders(); track order.id) {
      <app-order-summary [orderId]="order.id" [total]="order.total" />
    } @empty {
      <p>No orders found.</p>
    }
  `
})
export class OrderListComponent {
  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
}
```

A production component should focus on presentation and user interaction. API access, caching, business rules, and shared state belong in services/application state rather than being duplicated in templates.

## How Angular renders

```text
State/input changes
Angular schedules change detection
Template bindings are evaluated
DOM instructions update only necessary nodes
Browser paints UI
```

The Angular compiler turns templates into JavaScript instructions during the build. This avoids shipping a template compiler to the browser and catches many template errors before deployment.

Use immutable updates for arrays/objects:

```typescript
// Good: signal receives a new reference
this.orders.update(current => [...current, newOrder]);

// Bad: signal may not observe the same reference being mutated
this.orders().push(newOrder);
```

## Important Angular concepts

- Components should have a small public API.
- Inputs pass data down; outputs/events communicate actions up.
- Services hold shared behavior/state and are supplied by DI.
- `@for` should use a stable `track` expression.
- Signals represent reactive state; `computed` represents pure derived state.
- `effect` is for side effects, not normal value derivation.
- Client-side guards improve UX but do not enforce backend authorization.

## Comparison
| Approach | Best use | Risk |
|---|---|---|
| Local component signal | State used by one component | Not shared automatically |
| Injectable service | Shared feature behavior/state | Scope must be chosen correctly |
| RxJS Observable | Async streams/cancellation | Operator complexity |
| NgRx store | Large cross-feature state/workflows | Boilerplate/learning cost |
| `@Input` | Parent-to-child data | Keep public API focused |
| Output/event | Child-to-parent action | Not a backend event bus |

## Interview Questions
- **[L1]** What is an Angular component?
- **[L1]** What is the difference between interpolation, property binding, and event binding?
- **[L2]** What is a standalone component?
- **[L2]** Why should state updates be immutable when using signals?
- **[L3]** How would you structure a large Angular application so components do not become business-logic containers?
- **[L3]** Why is Angular client-side authorization not a security boundary?
- **[L1]** What is property binding?
- **[L1]** What is content projection used for?
- **[L2]** What is the difference between a component and a service?
- **[L2]** Why should `@for` use a stable `track` expression?
- **[L2]** What is the purpose of `computed` and `effect`?
- **[L3]** How should state ownership be divided between components, services, and NgRx?
- **[L3]** How do you design an accessible Angular component?
- **[L3]** How do you diagnose a component that renders slowly?
- **[L3]** How do standalone components affect large application architecture?

## Interview Answers
1. A component is a TypeScript class plus template metadata that owns a portion of the UI and its behavior.
2. Interpolation renders text, property binding sets DOM/component properties, and event binding sends user events into component methods.
3. It declares its own imports directly and can be routed/lazy-loaded without needing an `NgModule` declaration.
4. Signals detect writes/reference changes. Replacing an array/object makes the update explicit and avoids stale derived/template state caused by in-place mutation.
5. Keep UI components focused on display/input, place API/business workflows in services or feature state, use route-level boundaries, and keep domain models separate from transport models.
6. Browser code is visible and modifiable. Guards can hide UI/navigation, but every API must independently authenticate and authorize the request.
7. Property binding assigns a component expression to a DOM/component property; it is different from interpolating text into a string attribute.
8. Content projection lets a reusable component render caller-provided markup in a controlled slot, useful for cards/dialogs/layout components.
9. Components own UI state/rendering; services own reusable behavior/data access/shared state. Mixing all business logic into components makes testing and reuse harder.
10. A stable `track` identity lets Angular reuse DOM nodes efficiently and avoid recreating list elements unnecessarily.
11. `computed` derives memoized state from signals; `effect` runs side effects when dependencies change and should not replace normal state derivation.
12. Keep local UI state in components, feature/shared workflow in services, and use NgRx only for complex cross-feature state with clear ownership.
13. Use semantic HTML, labels/focus/keyboard behavior, accessible names, sufficient contrast, error announcements, and test with keyboard/screen-reader-oriented checks.
14. Profile main-thread/component checks, template functions, list tracking, DOM size, network/bundle work, and memory before optimizing.
15. Standalone components make imports/dependencies/lazy boundaries explicit and reduce module-level coupling; teams still need feature ownership and shared-component discipline.

## Expert perspective
Angular expertise is not memorizing template syntax. It is controlling state ownership, rendering work, async lifetimes, public component contracts, accessibility, and backend boundaries so a growing application remains understandable.
