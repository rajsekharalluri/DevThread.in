---
id: angular-routing
slug: routing
title: Angular Routing, Guards, Resolvers, and Lazy Loading
category: angular
categoryTitle: Angular
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Angular 17+ standalone"
prerequisites: [angular-fundamentals, angular-dependency-injection]
tags: [angular, router, lazy-loading, guards, resolvers]
relatedTopics: [angular-dependency-injection, angular-change-detection-signals]
order: 40
status: published
---
# Angular Routing, Guards, Resolvers, and Lazy Loading

## Introduction
The Angular Router maps URLs to components and feature areas without a full browser reload. It also provides route parameters, query parameters, guards, resolvers, lazy loading, and navigation lifecycle events.

```text
URL
 ↓ match route
canMatch/canActivate
 ↓ optional resolver
load component/data
 ↓ render view
```

## Purpose
Routing makes application state bookmarkable and gives large applications a way to split features into independently loaded bundles. Guards improve user experience; backend authorization remains the real security boundary.

## Real-World Simple Example
```typescript
export const routes: Routes = [
  { path: '', loadComponent: () => import('./home.component').then(m => m.HomeComponent) },
  {
    path: 'orders/:orderId',
    loadComponent: () => import('./order-detail.component').then(m => m.OrderDetailComponent),
    canActivate: [authGuard]
  }
];

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isLoggedIn() || inject(Router).createUrlTree(['/login']);
};
```

## Professional company-level example
Use lazy route boundaries for large feature areas:

```typescript
{
  path: 'admin',
  loadChildren: () => import('./admin/admin.routes').then(m => m.adminRoutes),
  canMatch: [adminRoleGuard]
}
```

`canMatch` can prevent the route from matching at all; `canActivate` runs after a route has matched. Use a resolver only when navigation should wait for required data; otherwise load the page and show a clear loading state.

A route guard may hide a feature from a normal user, but a malicious user can still call the API directly. Every API operation must authenticate and authorize independently.

## Lazy-loading flow

```text
Initial bundle
  ├── shell
  ├── header
  └── home

User opens /admin
Browser requests admin chunk
Router matches/guards
Admin component loads
```

A direct import of a feature component can accidentally pull it into the initial bundle. Verify chunk output instead of trusting route names.

## Comparison
| Feature | Use |
|---|---|
| `canMatch` | Decide whether route config should match |
| `canActivate` | Allow/block activation after match |
| Resolver | Load required data before activation |
| Component loading state | Render immediately while data loads |
| `loadComponent` | Lazy-load one standalone component |
| `loadChildren` | Lazy-load a feature route tree |

## Interview Questions
- **[L1]** What does the Angular Router do?
- **[L1]** What is a route guard?
- **[L2]** What is the difference between `canMatch` and `canActivate`?
- **[L2]** How does `loadComponent` create lazy loading?
- **[L3]** Why are route guards not a security boundary?
- **[L3]** How would you decide between a resolver and loading data after navigation?

## Interview Answers
1. It maps browser URLs to components/features and manages SPA navigation, parameters, query strings, and history.
2. A guard allows, blocks, or redirects navigation based on application state such as authentication or role.
3. `canMatch` participates while selecting a route; `canActivate` runs after the route matches and decides whether activation may proceed.
4. It uses a dynamic import that the Angular build splits into a separate JavaScript chunk fetched when the route is visited.
5. Browser code can be modified and APIs can be called directly. Guards improve UX; backend authorization protects data/actions.
6. Use a resolver when the page cannot render meaningfully without the data and blocking navigation is acceptable. Otherwise navigate quickly and show loading/error states while data loads.

## Expert perspective
Routing is both navigation design and bundle architecture. Senior engineers define route boundaries around features, verify lazy chunks, avoid guard-based false security, and choose resolver versus in-page loading based on user experience and failure behavior.
