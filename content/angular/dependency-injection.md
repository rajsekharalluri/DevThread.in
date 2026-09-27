---
id: angular-dependency-injection
slug: dependency-injection
title: Angular Services & Dependency Injection
category: angular
categoryTitle: Angular
difficulty: intermediate
estimatedMinutes: 35
version:
  minimum: "Angular 17+ (standalone)"
prerequisites: [angular-fundamentals]
tags: [angular, dependency-injection, services, providers]
relatedTopics: [angular-fundamentals, angular-rxjs-http]
order: 30
status: published
---
# Angular Services & Dependency Injection

## Overview
A service is a plain injectable class that holds shared state or logic. Dependency injection (DI) is how Angular supplies a component or another service with the instances it needs, without that consumer constructing them itself.

## Why This Exists
If every component directly constructed its own `HttpClient`, logger, or state store, testing would require real network calls and swapping implementations would mean editing every component. DI lets a component simply declare "I need an `OrderService`" and the framework supplies the right instance — a fake one in tests, a real one in production.

## Fundamentals
A class becomes injectable with `@Injectable()`. `providedIn: 'root'` registers it as a singleton available application-wide. Angular resolves dependencies through an injector hierarchy — a component can request a service via its constructor or the `inject()` function, and Angular walks up the injector tree to find (or create) an instance.

## Syntax and API
```typescript
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  getOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`/api/orders/${id}`);
  }
}

@Component({ selector: 'app-order-page', standalone: true, template: `...` })
export class OrderPageComponent {
  private readonly orderService = inject(OrderService); // functional inject(), no constructor needed
}
```

## How It Works
When a component or service first needs `OrderService`, Angular's injector checks whether an instance already exists in the relevant scope; if not, it creates one (calling its constructor, resolving *its* dependencies recursively) and caches it for reuse within that scope.

## Internal Implementation
Angular maintains a hierarchical tree of injectors — a root injector (application-wide singletons), and additional injectors created per-route or per-component (`providers: [...]` on a component scopes a service to that component and its children). A dependency is resolved by walking up this tree from where it was requested until a matching provider is found; this is what allows the same token to resolve to different instances in different parts of the app.

## Real-World Example
An `OrderService` wraps `HttpClient` calls to the orders API; every component that needs order data injects `OrderService` rather than talking to `HttpClient` directly, centralizing the API shape and any caching/retry logic in one place.

## Production Example
```typescript
export const AUTH_TOKEN_PROVIDER = new InjectionToken<() => string | null>('AUTH_TOKEN_PROVIDER');

@Injectable({ providedIn: 'root' })
export class AuthInterceptorService {
  private readonly getToken = inject(AUTH_TOKEN_PROVIDER);

  intercept(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    const token = this.getToken();
    return next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
  }
}

// In app.config.ts:
providers: [
  { provide: AUTH_TOKEN_PROVIDER, useValue: () => sessionStorage.getItem('token') },
]
```
An `InjectionToken` lets you inject something that isn't a class (a function, a primitive, a configuration value) with full type safety — useful for things like environment configuration or a pluggable token provider.

## Common Mistakes
Bad — constructing a dependency manually instead of injecting it, which makes the component untestable without real infrastructure:
```typescript
export class OrderPageComponent {
  private readonly orderService = new OrderService(new HttpClient(...)); // bypasses DI entirely
}
```
Better — inject it and let Angular (and tests) control what's supplied:
```typescript
export class OrderPageComponent {
  private readonly orderService = inject(OrderService);
}
```
Other common mistakes: registering a service with `providedIn: 'root'` when it actually needs per-component state (leading to shared state bugs across unrelated component instances); forgetting that a service provided in a component's `providers` array creates a *new* instance for every instance of that component, not a shared singleton.

## Performance
Root-level singleton services avoid the cost of recreating shared state repeatedly, but scoping a stateful service too broadly (when it should be scoped to one feature/route) can leak memory or stale state across unrelated parts of the app — scope services as narrowly as their actual state lifetime requires.

## Security
Never inject or hardcode secrets/API keys directly into a service shipped to the browser — anything in client-side Angular code is visible to the end user; genuine secrets belong server-side.

## Testing
Override providers in `TestBed.configureTestingModule({ providers: [{ provide: OrderService, useValue: fakeOrderService }] })` to substitute a test double, letting you test a component's behavior without a real HTTP call.

## When to Use
Use a service and DI for anything shared across multiple components — API access, application state, caching, logging, configuration.

## When Not to Use
Don't create a service for pure, stateless utility functions with no dependencies of their own — a plain exported function is simpler and doesn't need DI machinery.

## Trade-offs
DI improves testability and decoupling but adds a layer of indirection that can make it harder to trace, at a glance, which concrete implementation actually runs for a given injection token.

## Related Topics
See [Angular Fundamentals](/angular/fundamentals) and [RxJS & HTTP Client](/angular/rxjs-http).

## Practical Exercise
Build an `OrderService` with an injectable `InjectionToken` for a configurable API base URL, and write a test that overrides both the service and the token in `TestBed`.

## Interview Questions
- **[L1]** What does `@Injectable({ providedIn: 'root' })` do?
- **[L1]** Why is dependency injection useful for testing?
- **[L2]** What is an `InjectionToken`, and when would you use one instead of a class?
- **[L2]** How does Angular's injector hierarchy affect whether a service is shared or per-component?
- **[L3]** How would you design service scoping in a large app with multiple lazy-loaded feature areas?

## Interview Answers
1. **[L1] What does `@Injectable({ providedIn: 'root' })` do?**
   It marks a class as injectable by Angular's DI system and registers it with the application's root injector, meaning a single shared instance is created the first time it's needed and reused (as a singleton) for every subsequent injection anywhere in the app, unless a more specific injector overrides it.
2. **[L1] Why is dependency injection useful for testing?**
   Because a component or service declares *what* it needs (an interface or class token) rather than constructing a concrete instance itself, a test can supply a fake or mock implementation for that same token — letting you test a component's logic in isolation, without needing a real HTTP backend, database, or other external dependency to be available during the test run.
3. **[L2] What is an `InjectionToken`, and when would you use one instead of a class?**
   An `InjectionToken` is a unique, typed identifier you can use as a DI token when the thing being injected isn't a class — a plain value, a function, a configuration object, or even a primitive like a string or number. You'd use one for things like environment configuration, feature flags, or a pluggable strategy function that doesn't naturally fit as its own injectable class.
4. **[L2] How does Angular's injector hierarchy affect whether a service is shared or per-component?**
   Angular resolves an injection request by walking up from where it was requested (a component, then its parent, up to the root injector) until it finds a matching provider. A service registered with `providedIn: 'root'` is found and shared at the top level, giving one instance for the whole app. A service listed in a specific component's own `providers` array is registered at *that* component's injector level, so every instance of that component gets its own separate instance of the service, scoped to that component and its children.
5. **[L3] How would you design service scoping in a large app with multiple lazy-loaded feature areas?**
   Keep genuinely global, cross-cutting services (auth state, a shared HTTP interceptor, application-wide configuration) at the root level, but scope feature-specific state services to the lazy-loaded route/feature they belong to (via that route's own providers), so two different feature areas don't accidentally share or leak state through a singleton that should have been feature-scoped. This also improves lazy-loading behavior, since feature-scoped services and their dependencies can be included only in that feature's bundle rather than the main bundle.

## Senior Developer Perspective
Dependency injection is fundamentally about controlling where an implementation decision is made — components should ask for a capability (a token/interface) and never construct dependencies themselves. Senior engineers are deliberate about service scope (root vs. feature vs. component) since getting this wrong is a common source of subtle state-leakage bugs that only appear once an app has multiple instances of a feature on screen at once.
