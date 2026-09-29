---
id: angular-directives-pipes
slug: directives-pipes
title: Angular Directives, Pipes & Component Communication
category: angular
categoryTitle: Angular
difficulty: beginner
estimatedMinutes: 35
version:
  minimum: "Angular 17+ (standalone)"
prerequisites: [angular-fundamentals]
tags: [angular, directives, pipes, input, output]
relatedTopics: [angular-fundamentals, angular-dependency-injection]
order: 20
status: published
---
# Angular Directives, Pipes & Component Communication

## Overview
Directives attach behavior to DOM elements, pipes transform displayed values, and `@Input`/`@Output` (or their signal-based equivalents) let parent and child components communicate.

## Why This Exists
UI needs recurring behaviors — conditionally showing an element, repeating a template for a list, formatting a date — without duplicating that logic in every component. Directives and pipes extract these concerns into reusable, declarative building blocks.

## Fundamentals
- **Structural control flow** (`@if`, `@for`, `@switch`) adds or removes elements from the DOM.
- **Attribute directives** (`[ngClass]`, a custom `appHighlight`) change an element's appearance or behavior without adding/removing it.
- **Pipes** (`{{ value | currency }}`) transform a bound value for display only, without changing the underlying data.
- **`@Input`/`@Output`** (or `input()`/`output()` signal functions) define a component's public API for receiving data and emitting events.

## Syntax and API
```typescript
@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="badge" [class.badge-success]="status() === 'Paid'">{{ status() }}</span>`
})
export class StatusBadgeComponent {
  status = input.required<string>();       // signal-based input
  statusChanged = output<string>();          // signal-based output

  acknowledge(): void {
    this.statusChanged.emit(this.status());
  }
}
```
```html
<app-status-badge [status]="order.status" (statusChanged)="onStatusAck($event)" />
```

## How It Works
`@if`/`@for`/`@switch` compile to instructions that add, remove, or reorder DOM nodes based on the bound expression. `input()` creates a signal Angular updates automatically whenever the parent's bound value changes; `output()` creates an emitter the child calls to notify the parent, and the parent's `(event)` binding runs its handler.

## Internal Implementation
A custom attribute directive is a class decorated with `@Directive`, injected with `ElementRef` (a reference to the host DOM element) to imperatively manipulate it — this is the escape hatch for behavior that can't be expressed purely declaratively in a template. Pipes are pure by default, meaning Angular only re-runs them when their input reference changes, which is both a performance optimization and a common source of confusion when a pipe seems to not "update" after an in-place mutation.

## Real-World Example
A `StatusBadgeComponent` receives an order's status via `input()` and displays it with an appropriate color, while emitting an event when the user acknowledges it — the parent list component doesn't need to know how the badge renders, only how to supply and react to it.

## Production Example
```typescript
@Component({
  selector: 'app-highlight-overdue',
  standalone: true,
  template: `
    @for (order of orders(); track order.id) {
      <div [class.overdue]="order.dueDate < today">{{ order.orderNumber }} — {{ order.dueDate | date:'mediumDate' }}</div>
    }
  `
})
export class HighlightOverdueComponent {
  orders = input.required<Order[]>();
  readonly today = new Date();
}
```
Combining a built-in `date` pipe with a conditional class binding avoids writing a custom directive for a purely presentational concern.

## Common Mistakes
Bad — a custom "impure" pipe recalculating an expensive transformation on every change-detection cycle:
```typescript
@Pipe({ name: 'expensiveFormat', pure: false }) // re-runs constantly, even for unrelated changes
export class ExpensiveFormatPipe implements PipeTransform { /* ... */ }
```
Better — keep pipes pure (the default) so Angular only re-runs them when the input reference actually changes, and compute anything else with a memoized `computed()` signal in the component instead:
```typescript
@Pipe({ name: 'expensiveFormat' }) // pure: true by default
export class ExpensiveFormatPipe implements PipeTransform { /* ... */ }
```
Other common mistakes: using `[ngClass]`/`[ngStyle]` for a single conditional class when `[class.foo]`/`[style.foo]` is simpler and more efficient; forgetting that structural directives (`@if`) fully destroy and recreate their content (losing component state) rather than just hiding it with CSS.

## Performance
Pure pipes and `@for`'s required `track` expression both exist specifically to limit unnecessary DOM work — Angular can skip re-rendering a row whose tracked identity hasn't changed, even if the surrounding list re-rendered.

## Security
Never build a custom directive that writes raw, unsanitized HTML into the DOM via `ElementRef.nativeElement.innerHTML` — use Angular's sanitizer-aware bindings (`[innerHTML]` still gets sanitized, but bypassing it manually does not).

## Testing
Test directives against a real host element using `TestBed.createComponent` with a small test host component, and test pipes as plain functions (`transform(value)`) since most are pure, stateless transformations.

## When to Use
Use a custom directive when behavior needs to imperatively manipulate the DOM or attach event listeners across many different elements/components; use a pipe for any pure, display-only value transformation.

## When Not to Use
Don't reach for `[ngClass]`'s object/array syntax for a single boolean class toggle — `[class.name]="condition"` is simpler and clearer; don't write a custom pipe for logic that's really business logic (put that in a service and expose a plain signal instead).

## Trade-offs
Directives and pipes keep templates declarative and behavior reusable, but too many custom ones can make a template's actual behavior harder to trace without checking multiple files.

## Related Topics
See [Angular Fundamentals](/angular/fundamentals) and [Dependency Injection & Services](/angular/dependency-injection).

## Practical Exercise
Build a `StatusBadgeComponent` using signal-based `input()`/`output()`, and a custom pure pipe that formats a `Money` value with its currency symbol.

## Interview Questions
- **[L1]** What is the difference between a structural directive and an attribute directive?
- **[L1]** What does `@Input` (or `input()`) do, and how does data flow with it?
- **[L2]** Why does Angular only re-run a pure pipe when its input reference changes?
- **[L2]** What's the difference between `[class.foo]="condition"` and using `*ngIf`/`@if` to conditionally show an element?
- **[L3]** How would you design a component's public API (inputs/outputs) to stay backward compatible as requirements evolve?
- **[L3]** When would you implement behavior as a custom directive, a pipe, or a component, and how do you keep that logic testable?

## Interview Answers
1. **[L1] What is the difference between a structural directive and an attribute directive?**
   A structural directive (`@if`, `@for`, `@switch`, or legacy `*ngIf`/`*ngFor`) changes the DOM's structure — adding, removing, or repeating elements. An attribute directive changes the appearance or behavior of an *existing* element without adding or removing it from the DOM — like toggling a CSS class or attaching a custom event listener.
2. **[L1] What does `@Input` (or `input()`) do, and how does data flow with it?**
   It declares a property on a component that a parent template can bind to using `[propertyName]="value"` syntax, allowing data to flow one direction: from the parent component down into the child. The signal-based `input()` function additionally exposes that value as a readable signal inside the component, so the child can react to changes the same way it would to any other signal.
3. **[L2] Why does Angular only re-run a pure pipe when its input reference changes?**
   Pure pipes are a deliberate performance optimization: Angular assumes a pure pipe's output depends only on its declared inputs, so if the input's reference hasn't changed (even if something nested inside an object or array was mutated), there's no need to recompute the transformation. This avoids potentially expensive re-computation on every change-detection cycle, at the cost of requiring immutable updates (a new array/object reference) for the pipe to notice a real change.
4. **[L2] What's the difference between `[class.foo]="condition"` and using `*ngIf`/`@if` to conditionally show an element?**
   `[class.foo]="condition"` keeps the element in the DOM at all times and just toggles a CSS class, meaning any component state or child element state (like a form input's typed value) is preserved when the condition changes. `*ngIf`/`@if` actually adds or removes the element (and everything inside it, including any component instances) from the DOM entirely — condition becoming false destroys that content, and it's recreated fresh when it becomes true again, losing any in-progress state.
5. **[L3] How would you design a component's public API (inputs/outputs) to stay backward compatible as requirements evolve?**
   Prefer a small number of focused inputs/outputs with clear, intention-revealing names over a single generic "options" object input that silently grows more properties over time — small, explicit inputs make breaking changes obvious at the call site. When you must add new optional behavior, add a new optional input with a sensible default rather than changing the meaning of an existing one, and treat a component's inputs/outputs the same way you'd treat a public API contract in a library — changes should be deliberate, documented, and ideally accompanied by a deprecation path rather than a silent behavior change.
6. **[L3] When would you implement behavior as a custom directive, a pipe, or a component, and how do you keep that logic testable?**
   Use a **pipe** for pure, synchronous value transformation in templates (formatting currency, truncating text) — it has no DOM access and should have no side effects. Use an **attribute directive** to add reusable behavior to an existing element without owning its markup (auto-focus, click-outside detection, permission-based disabling, tooltips). Use a **component** when the behavior owns its own template and visual structure. To keep them testable, put non-trivial logic in plain TypeScript functions or injectable services that the pipe or directive delegates to, so the logic can be unit tested without rendering; then add a small host-component test with `TestBed` to verify the directive's DOM interaction or the pipe's template usage. Keep pipes pure unless there is a measured reason not to, since impure pipes run on every change-detection cycle.

## Senior Developer Perspective
Directives and pipes are about keeping templates declarative — the moment a template needs a comment explaining "why" a binding behaves a certain way, that's often a sign the logic belongs in a well-named directive, pipe, or computed signal instead. Senior engineers treat a component's inputs and outputs as a real API contract with its own compatibility expectations, not an implementation detail that can change freely.
