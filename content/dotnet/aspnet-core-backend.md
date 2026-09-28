---
id: dotnet-aspnet-core-backend
slug: aspnet-core-backend
title: ASP.NET Core Backend Engineering
category: dotnet
categoryTitle: .NET Backend
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: ".NET 8+ / ASP.NET Core"
prerequisites: [csharp-fundamentals, csharp-async-await]
tags: [dotnet, aspnet-core, web-api, rest, security, scalability]
relatedTopics: [dotnet-data-access, architecture-clean, architecture-resilience]
order: 10
status: published
---
# ASP.NET Core Backend Engineering

## Overview
A production ASP.NET Core backend combines HTTP/API design, middleware, dependency injection, authentication/authorization, validation, logging, background work, error handling, and operational diagnostics into one coherent request pipeline.

## Why This Exists
An API is a public contract and a production workload, not just a controller method. Teams need consistent behavior for versioning, errors, security, cancellation, observability, and dependency failures across every endpoint.

## Fundamentals
The request pipeline is middleware ordered from outer to inner. Routing selects an endpoint; model binding and validation build request data; authorization runs before handler logic; the handler calls application services; middleware converts exceptions into safe responses and adds correlation/diagnostic information.

## Syntax and API
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

Line by line:

- `CreateBuilder` loads configuration, logging, and host defaults.
- `AddControllers` registers controller discovery/model binding/serialization.
- `AddProblemDetails` provides a standard error response shape.
- `AddAuthentication` registers identity validation; it does not authorize actions by itself.
- `AddAuthorization` enables policies/requirements.
- `AddScoped` creates one application service per request scope.
- `UseExceptionHandler` converts unhandled exceptions at the boundary.
- `UseAuthentication` builds `HttpContext.User` from credentials.
- `UseAuthorization` evaluates endpoint policies.
- `MapControllers` exposes controller routes.
- `Run` starts the host and blocks until shutdown.

## Request pipeline walkthrough

```text
HTTP request
Forwarded headers/proxy context
Exception boundary
Routing
Authentication → User identity
Authorization → policy/resource permission
Model binding + validation
Controller/application service
DTO serialization/status code
Response logging/metrics
```

Middleware is executed in registration order on the way in and reverse order on the way out. A middleware that performs authentication, logging, timeout handling, or exception mapping at the wrong position can change security and reliability behavior.

## Common API methods and their behavior

```csharp
[HttpGet("{id:guid}")]
public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken ct)
{
    var result = await orders.GetAsync(id, User.GetTenantId(), ct);
    return result is null ? NotFound() : Ok(result);
}
```

- `[HttpGet]` maps an HTTP GET request; it does not fetch data by itself.
- `"{id:guid}"` requires a route value that can parse as a GUID.
- `ActionResult<T>` allows a successful `T` response or an HTTP result such as 404.
- `CancellationToken` represents client disconnect/shutdown cancellation.
- `NotFound()` creates a 404 response; `Ok()` creates a 200 response with serialized data.
- The service must enforce tenant/resource authorization; the route ID alone is not permission.

For a request DTO:

```csharp
public sealed record CreateOrderRequest(
    Guid ProductId,
    int Quantity);

[HttpPost]
public async Task<ActionResult<OrderResponse>> Create(
    CreateOrderRequest request,
    CancellationToken ct)
{
    if (!ModelState.IsValid)
        return ValidationProblem(ModelState);

    var order = await orders.CreateAsync(User.GetTenantId(), request, ct);
    return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
}
```

Model binding constructs `CreateOrderRequest` from JSON. Validation should reject malformed/invalid input before business work. `CreatedAtAction` returns 201 and a location for the new resource. It does not replace domain validation, authorization, or database constraints.

## HTTP behavior developers must understand

| Method/status | Meaning in an API |
|---|---|
| `GET` | Read; should not create a business side effect |
| `POST` | Create/process command; repeated calls may duplicate unless idempotent |
| `PUT` | Replace a known resource; commonly designed idempotently |
| `PATCH` | Partial update; define merge/validation semantics |
| `DELETE` | Remove/deactivate; decide repeat behavior |
| `200` | Successful response with result |
| `201` | Created; include a resource location when useful |
| `202` | Accepted for asynchronous processing, not completed yet |
| `204` | Success with no response body |
| `400` | Malformed/invalid request |
| `401` | Missing/invalid authentication |
| `403` | Authenticated but not permitted |
| `404` | Resource not found or intentionally undisclosed |
| `409` | Conflict/concurrency/business state conflict |
| `422` | Semantically invalid input where the API uses this distinction |
| `429` | Rate limit exceeded |
| `5xx` | Server/dependency failure; never expose internal details |

A `202 Accepted` endpoint needs a status model or callback/polling contract. Returning `202` without a way to observe completion leaves clients guessing. A `POST` that may be retried after a timeout should accept an idempotency key.

### Problem Details

```csharp
builder.Services.AddProblemDetails();

app.UseExceptionHandler();
```

Problem Details gives clients a stable error shape such as status/title/detail/type/instance. The server should log the full exception with a trace ID while returning a safe response; `detail` must not contain SQL, stack traces, credentials, or provider internals.

### Pagination contract

```text
GET /orders?limit=25&cursor=<opaque>

200 {
  "items": [...],
  "nextCursor": "..."
}
```

Validate maximum page size, use deterministic ordering, scope queries to the authenticated tenant, and prefer cursor/keyset pagination for deep changing feeds. `offset` pagination is simpler but can scan/skip large volumes and shift under concurrent inserts.

### Versioning
Keep a stable contract while the server evolves:

```text
/api/v1/orders → old response contract
/api/v2/orders → changed contract
```

Alternatively use headers/media types, but choose one explicit strategy, document deprecation, and keep old/new versions compatible during client migration.

## How It Works

## Internal Implementation
ASP.NET Core uses asynchronous request handling and a thread pool. `async` endpoints release threads during I/O; blocking `.Result`/`.Wait()` consumes worker threads and can cause starvation. DI lifetimes matter: singleton services must be thread-safe, scoped services belong to one request, and transient services are created per resolution.

## Real-World Example
An order endpoint validates a request, checks the authenticated user's tenant, calls an application service with the request cancellation token, and returns a DTO rather than exposing an EF entity.

## Production Example
```csharp
[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await orders.GetAsync(id, User.GetTenantId(), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
```
Use API versioning or explicit URL/header policies, Problem Details for errors, authorization policies rather than scattered role checks, and structured logging with IDs that let operators trace a request across dependencies.

## Common Mistakes
- Returning internal exceptions or stack traces to clients.
- Using JWT authentication without validating issuer, audience, signature, expiry, and key rotation.
- Putting business logic inside controllers.
- Missing cancellation and timeout propagation.
- Exposing an unbounded list endpoint.

## Performance
Use async I/O, pagination, response compression where appropriate, caching with explicit invalidation, connection-pool awareness, and measured serialization/query projections. Monitor p95/p99 latency, allocations, thread-pool starvation, and downstream dependency time.

## Security
Use HTTPS in production, strict authentication/authorization, input validation, rate limiting, secure headers, non-sensitive logs, parameterized data access, and consistent error responses. Treat client-side authorization as non-authoritative.

## Testing
Unit-test application policies, integration-test middleware/API contracts, test authorization boundaries, validate OpenAPI examples, and run load tests against realistic dependencies.

## When to Use
Use ASP.NET Core for production APIs, services, background workers, and cloud-native .NET applications.

## When Not to Use
Do not create a large API framework for a static site or one-off script; choose a solution proportional to the workload.

## Trade-offs
ASP.NET Core provides a high-performance, integrated platform, but its flexibility means middleware order, DI lifetime, error handling, and security configuration require deliberate engineering.

## Related Topics
See [EF Core, Dapper & Data Access](/dotnet/data-access), [Clean Architecture](/architecture/clean-architecture), and [Resilience Patterns](/architecture/resilience).

## Practical Exercise
Build a versioned order API with JWT authentication, policy authorization, Problem Details errors, request cancellation, structured logs, pagination, and integration tests for unauthenticated and cross-tenant requests.

## Interview Questions
- **[L1]** What is middleware and why does its order matter?
- **[L1]** Explain Singleton, Scoped, and Transient DI lifetimes.
- **[L2]** How does async improve ASP.NET Core scalability?
- **[L2]** What should JWT validation verify?
- **[L3]** How would you design a production API error, security, and observability strategy?
- **[L1]** What does model binding do?
- **[L1]** What is the difference between `Ok`, `Created`, `Accepted`, and `NoContent`?
- **[L2]** What does `ProblemDetails` provide?
- **[L2]** Why should list endpoints be paginated?
- **[L2]** What is the difference between authentication and authorization middleware?
- **[L2]** Why should controllers remain thin?
- **[L3]** How would you design idempotency for a POST endpoint?
- **[L3]** How should an API handle a dependency timeout?
- **[L3]** How do you design backward-compatible API versioning?
- **[L3]** How would you test an ASP.NET Core API beyond unit tests?

## Interview Answers
1. **[L1]** Middleware wraps request processing and can inspect or modify requests/responses. Order determines whether authentication, exception handling, routing, and authorization run before or after other components.
2. **[L1]** Singleton is one app-wide instance, Scoped is typically one instance per request, and Transient creates a new instance each resolution. A singleton must not capture scoped state.
3. **[L2]** Async releases worker threads while database/HTTP I/O waits, allowing the server to process other requests. It does not make CPU work faster and blocking calls can still starve the pool.
4. **[L2]** Verify signature/issuer, audience, expiry/not-before, algorithm policy, key rotation, and required claims before trusting the token.
5. **[L3]** Define a stable Problem Details contract, centralized exception mapping, policy-based authorization, correlation IDs, structured logs/metrics/traces, timeouts/rate limits, and tests for security and failure paths.
6. **[L1]** Model binding reads route/query/header/body values and converts them into action parameters or request DTOs; validation then checks the resulting model.
7. **[L1]** `Ok` is a completed 200 response, `Created` is 201 for a new resource/location, `Accepted` is 202 for durable asynchronous acceptance, and `NoContent` is 204 with no body.
8. **[L2]** Problem Details provides a standardized machine-readable error shape; it does not mean internal exception details should be returned to clients.
9. **[L2]** Pagination bounds database/network/memory work and gives clients a predictable contract; enforce maximum page size and stable ordering.
10. **[L2]** Authentication establishes `HttpContext.User`; authorization evaluates endpoint policies/requirements for that identity.
11. **[L2]** Controllers should translate HTTP and delegate business rules to application/domain services, making policies testable and transport concerns separate.
12. **[L3]** Accept a scoped idempotency key, persist request/result state with a unique constraint, return the original result for duplicates, and expire records deliberately.
13. **[L3]** Use a timeout budget, bounded retry only when safe, circuit/fallback policy where valid, cancellation propagation, safe error response, and trace/metric evidence.
14. **[L3]** Use explicit URL/header/media-type strategy, additive changes first, contract tests, deprecation windows, and compatible deployment with old clients.
15. **[L3]** Use unit tests for policies, integration tests for middleware/API/database contracts, authorization tests, contract/OpenAPI tests, failure tests, and realistic load tests.

## Senior Developer Perspective
Senior backend engineering is mostly boundary discipline: stable contracts, safe failure behavior, explicit authorization, measurable performance, and operational visibility. A controller that works locally is only the beginning of a production API.
