---
id: dotnet-testing
slug: testing
title: ".NET Testing: Unit, Integration, and Contract Tests"
category: dotnet
categoryTitle: .NET Backend
difficulty: intermediate
estimatedMinutes: 60
version:
  minimum: ".NET 8+, xUnit 2.x, Testcontainers 3.x"
prerequisites: [dotnet-aspnet-core-backend, dotnet-di-configuration-options]
tags: [dotnet, testing, xunit, integration-testing, testcontainers, mocking, webapplicationfactory]
relatedTopics: [dotnet-data-access, efcore-change-tracking, devops-cicd-fundamentals]
order: 90
status: published
---
# .NET Testing: Unit, Integration, and Contract Tests

## Introduction

Tests exist to let a team change code quickly **without fear**. A good test suite catches regressions before users do, documents intended behavior, and runs fast enough that developers run it constantly. A bad suite is slow, flaky, and so tied to implementation details that every refactor breaks dozens of tests.

This page covers the testing layers for ASP.NET Core services, xUnit fundamentals, test doubles, integration testing with `WebApplicationFactory` and **Testcontainers** (real databases in Docker), testing EF Core correctly, contract testing between services, and how to keep a suite fast and reliable in CI.

## The Testing Layers

| Layer | What It Verifies | Speed | Typical Tools |
|---|---|---|---|
| **Unit** | Pure logic: domain rules, calculations, mapping, validation | Milliseconds | xUnit, NSubstitute/Moq, FluentAssertions/Shouldly |
| **Integration (component)** | Your service with real infrastructure: HTTP pipeline, DI, EF Core against a real DB, serialization | Seconds | `WebApplicationFactory`, Testcontainers |
| **Contract** | Two services agree on API/message shapes | Seconds | Pact, schema compatibility checks |
| **End-to-end** | Whole system through the UI or public API | Minutes | Playwright, deployed environments |

A healthy .NET backend suite usually has **many unit tests for domain logic, a solid layer of integration tests for each endpoint and repository, a few contract tests at service boundaries, and a small number of end-to-end smoke tests.** For API services, integration tests often give the best confidence per test because they exercise real wiring.

## Unit Testing with xUnit

### Anatomy of a Test

```csharp
public sealed class OrderTests
{
    [Fact]
    public void Adding_a_line_increases_the_total()
    {
        // Arrange
        var order = Order.Place(customerId: Guid.NewGuid());

        // Act
        order.AddLine(productId: Guid.NewGuid(), quantity: 2, unitPrice: 25.00m);

        // Assert
        Assert.Equal(50.00m, order.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_positive(int quantity)
    {
        var order = Order.Place(Guid.NewGuid());
        var ex = Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), quantity, 10m));
        Assert.Equal("Quantity must be positive", ex.Message);
    }

    [Fact]
    public void Paid_order_cannot_be_modified()
    {
        var order = Order.Place(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 10m);
        order.MarkPaid(paymentId: "pay_123");

        Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), 1, 5m));
    }
}
```

**Conventions that keep tests readable:**
- Name tests after **behavior**, not methods (`Paid_order_cannot_be_modified`, not `AddLineTest3`)
- One behavior per test; Arrange/Act/Assert structure
- `[Theory]` + `[InlineData]` for the same rule with different inputs
- Test through the **public API** of the class, not private methods

### xUnit Lifecycle and Fixtures

| Mechanism | Scope | Use For |
|---|---|---|
| Constructor / `IDisposable` | Per test | Fresh state for every test (xUnit creates a new class instance per test) |
| `IClassFixture<T>` | Shared across tests in one class | Expensive setup like a `WebApplicationFactory` |
| `ICollectionFixture<T>` + `[Collection]` | Shared across classes | One database container for many test classes |
| `IAsyncLifetime` | Async setup/teardown | Starting containers, seeding data |

### Controlling Time and Randomness

Code that calls `DateTime.UtcNow` directly is hard to test. Use `TimeProvider` (.NET 8+):

```csharp
public sealed class SubscriptionService(TimeProvider clock)
{
    public bool IsExpired(Subscription s) => s.EndsAt <= clock.GetUtcNow();
}

// Test with Microsoft.Extensions.TimeProvider.Testing
[Fact]
public void Subscription_expires_at_end_date()
{
    var clock = new FakeTimeProvider(new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero));
    var service = new SubscriptionService(clock);
    var sub = new Subscription(EndsAt: new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero));

    Assert.False(service.IsExpired(sub));
    clock.Advance(TimeSpan.FromDays(1));
    Assert.True(service.IsExpired(sub));
}
```

## Test Doubles: Mocks, Stubs, and Fakes

| Double | Purpose | Example |
|---|---|---|
| **Stub** | Returns canned data | Exchange-rate provider always returns 1.1 |
| **Mock** | Verifies an interaction happened | Assert the email sender was called once |
| **Fake** | Working lightweight implementation | In-memory repository, `FakeTimeProvider` |
| **Spy** | Records calls for later assertions | Capturing published events |

### NSubstitute Example

```csharp
[Fact]
public async Task Placing_an_order_sends_a_confirmation()
{
    var email = Substitute.For<IEmailSender>();
    var payments = Substitute.For<IPaymentGateway>();
    payments.AuthorizeAsync(Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(PaymentResult.Approved("pay_1"));

    var handler = new PlaceOrderHandler(payments, email, new InMemoryOrderRepository());
    await handler.Handle(new PlaceOrder(CustomerId: Guid.NewGuid(), Total: 99m), CancellationToken.None);

    await email.Received(1).SendAsync(Arg.Is<Email>(e => e.Subject.Contains("confirmed")), Arg.Any<CancellationToken>());
}
```

### When Not to Mock

- **Do not mock what you do not own** (EF Core `DbContext`, `HttpClient` internals, SDK clients). Wrap them behind your own interface, or test against the real thing in integration tests.
- **Do not mock value objects or domain entities** - use the real ones.
- **Too many mocks** in one test usually means the class has too many responsibilities or the test is checking implementation instead of behavior.

## Integration Testing ASP.NET Core with WebApplicationFactory

`WebApplicationFactory<TEntryPoint>` boots your real application in memory (real middleware, routing, DI, serialization, auth) and gives you an `HttpClient` to call it.

```csharp
// In Program.cs, make the entry point visible to tests:
public partial class Program { }
```

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Replace external dependencies you do not control
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        });
    }
}

public sealed class OrdersApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Post_order_returns_201_with_location()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new { customerId = Guid.NewGuid(), lines = new[] { new { sku = "SKU-1", quantity = 2 } } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Invalid_order_returns_problem_details()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/orders", new { customerId = Guid.Empty, lines = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Lines", problem!.Errors.Keys);
    }
}
```

### Testing Authenticated Endpoints

Register a test authentication handler so tests can act as specific users and roles without a real identity provider:

```csharp
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId!), new Claim("tenant_id", Request.Headers["X-Test-Tenant"].ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
    }
}

// In ConfigureTestServices:
services.AddAuthentication(defaultScheme: "Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
```

Now you can write the most valuable security tests: **tenant A cannot read tenant B's orders**, and **unauthenticated calls get 401**.

## Real Databases with Testcontainers

The EF Core InMemory provider does not behave like a relational database (no SQL translation, no constraints, no transactions, different null semantics). Tests that pass against it can fail in production. **Testcontainers** starts a real database in Docker for the test run.

```csharp
// dotnet add package Testcontainers.PostgreSql
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(); // real migrations
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrdersDbContext>>();
            services.AddDbContext<OrdersDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));
        });

    public new async Task DisposeAsync() => await _db.DisposeAsync();
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresApiFactory> { }

[Collection("postgres")]
public sealed class OrderPersistenceTests(PostgresApiFactory factory)
{
    [Fact]
    public async Task Concurrent_updates_are_detected()
    {
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var order = Order.Place(Guid.NewGuid());
        db1.Orders.Add(order);
        await db1.SaveChangesAsync();

        var copy1 = await db1.Orders.SingleAsync(o => o.Id == order.Id);
        var copy2 = await db2.Orders.SingleAsync(o => o.Id == order.Id);
        copy1.AddLine(Guid.NewGuid(), 1, 10m);
        copy2.AddLine(Guid.NewGuid(), 1, 20m);

        await db1.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
    }
}
```

**Why each part exists:**
- **Real Postgres** verifies SQL translation, constraints, and concurrency tokens
- **`MigrateAsync`** tests your actual migrations, catching broken migrations before deployment
- **Collection fixture** starts one container for many test classes (fast)
- **Separate scopes** simulate two concurrent requests with their own `DbContext`

### Keeping Database Tests Isolated

| Technique | How | Trade-off |
|---|---|---|
| Unique data per test | Each test creates its own tenant/customer IDs | Simple; data accumulates (fine for ephemeral containers) |
| Transaction rollback | Begin transaction in setup, roll back in teardown | Fast; cannot test code that commits its own transactions |
| Respawn | Deletes data from all tables between tests | Clean state; small cost per test |
| Container per class | New container per test class | Strong isolation; slower |

## Testing Message Consumers and Background Services

```csharp
[Fact]
public async Task OrderPaid_consumer_is_idempotent()
{
    var store = new InMemoryReceiptStore();
    var email = Substitute.For<IEmailSender>();
    var handler = new SendReceiptHandler(store, email);
    var evt = new OrderPaid(EventId: Guid.NewGuid(), OrderId: Guid.NewGuid(), Total: 50m);

    await handler.Handle(evt, CancellationToken.None);
    await handler.Handle(evt, CancellationToken.None);   // duplicate delivery

    await email.Received(1).SendAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
}
```

Test the handler logic directly; test broker wiring with a containerized broker (Testcontainers has RabbitMQ, Kafka, Azure Service Bus emulator modules) in a smaller number of integration tests.

## Contract Testing Between Services

When service A calls service B, integration tests of A usually fake B - and the fake can drift from reality. **Consumer-driven contract tests** (Pact) make the consumer record its expectations, and the provider verifies them in its own CI.

```text
1. Consumer test (Orders service) defines: GET /customers/42 returns 200 with { id, name, tier }
2. Pact generates a contract file and publishes it to a Pact Broker
3. Provider (Customers service) CI replays the contract against its real API
4. If the provider removes "tier" or changes its type, the provider build fails before deployment
```

For events, the equivalent is **schema compatibility checks** in a schema registry (backward/forward compatibility for Avro/Protobuf/JSON Schema).

## Test Suite Health

### Flaky Test Sources and Fixes

| Cause | Fix |
|---|---|
| `DateTime.Now` / timezones | `TimeProvider` and fixed clocks |
| Shared mutable state between tests | Fresh data per test, avoid static state |
| `Task.Delay` waiting for async work | Await completion signals, poll with timeout |
| Test order dependence | Each test sets up its own data |
| Real external services | Fakes, WireMock.Net, or containers |
| Parallel tests sharing a database | Unique data per test or collection-level isolation |

### Running Tests in CI

```yaml
- name: Test
  run: dotnet test --configuration Release --logger "trx" --collect:"XPlat Code Coverage"
```

GitHub-hosted Ubuntu runners include Docker, so Testcontainers works without extra setup. Track coverage as a signal for untested areas, not as a target to game.

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Testing private methods via reflection | Test behavior through public APIs |
| Asserting on exact log messages or internal calls | Assert on outcomes and observable effects |
| EF Core InMemory provider for repository tests | Testcontainers with the real database engine |
| One giant end-to-end suite | Mostly unit + integration; few E2E smoke tests |
| Mocks returning mocks returning mocks | Simplify design or use a real/fake collaborator |
| No security tests | Test 401/403 and cross-tenant access explicitly |

## Interview Questions
- **[L1]** What is the difference between a unit test and an integration test?
- **[L1]** What is the difference between a mock, a stub, and a fake?
- **[L2]** Why is the EF Core InMemory provider a poor choice for testing data access?
- **[L2]** How does WebApplicationFactory work, and what does it let you test?
- **[L2]** What causes flaky tests, and how do you fix them?
- **[L3]** How would you design the test strategy for a set of microservices that communicate over HTTP and messages?
- **[L3]** How do you test that a multi-tenant API never leaks data between tenants?
- **[L3]** Your test suite takes 25 minutes and developers stopped running it locally. How do you fix it?

## Interview Answers
1. A unit test verifies a small piece of logic, such as a domain rule or calculation, in isolation from infrastructure, running in milliseconds with collaborators replaced by test doubles where needed. An integration test verifies that components work together with real infrastructure, such as the ASP.NET Core pipeline, DI configuration, serialization, and EF Core against a real database, so it catches wiring, configuration, SQL translation, and mapping problems that unit tests cannot, at the cost of more setup and slower execution.
2. A stub supplies canned responses so the code under test can run, such as a price provider that always returns 10. A mock is set up to verify that a specific interaction happened, such as asserting the email sender was called once with a certain subject. A fake is a working lightweight implementation, such as an in-memory repository or `FakeTimeProvider`, that behaves realistically without real infrastructure. Stubs and fakes support state-based assertions; mocks support interaction-based assertions.
3. The InMemory provider is not a relational database: it does not translate LINQ to SQL, so queries that fail or behave differently in real SQL can pass; it does not enforce foreign keys, unique constraints, or check constraints; it has no real transactions or concurrency behavior; and its null and string comparison semantics differ. Tests therefore give false confidence. Use the real database engine via Testcontainers, or SQLite in-memory only for simple cases, while being aware that it also differs from production engines.
4. `WebApplicationFactory<TEntryPoint>` starts the application's real host in memory using `TestServer`, running `Program.cs` with the actual middleware pipeline, routing, DI registrations, configuration, authentication, and serialization, and provides an `HttpClient` that sends requests directly to it without networking. Tests can override configuration and replace services with `ConfigureTestServices`. It lets you test endpoints end to end within the service: status codes, validation responses, authorization, headers, and persistence when combined with a real database container.
5. Flaky tests are usually caused by dependence on real time or time zones, shared mutable state or data between tests, test order dependence, arbitrary sleeps waiting for asynchronous work, real external services and networks, parallel tests colliding on the same database rows, and race conditions in the code itself. Fixes include injecting `TimeProvider`, giving each test its own data, replacing sleeps with awaiting completion or polling with a timeout, faking external services, isolating database state, and quarantining and fixing flaky tests quickly rather than retrying them silently.
6. Within each service: fast unit tests for domain logic, integration tests using `WebApplicationFactory` and Testcontainers for its endpoints, database, and message handlers, with other services faked. Between services: consumer-driven contract tests (Pact) for HTTP APIs and schema-registry compatibility checks for events, verified in each provider's CI so breaking changes fail before deployment. Consumers are tested for idempotency, retries, and poison messages. Above that, a small set of end-to-end smoke tests runs against a deployed environment for critical flows, plus synthetic monitoring in production. Each service's pipeline runs its own suite independently to preserve deployment autonomy.
7. Write integration tests that authenticate as users from two different tenants, create data as tenant A, and assert that every read, list, search, update, and delete endpoint called as tenant B returns 404 or 403 and never includes tenant A's data, including through filters, paging, exports, and IDs guessed directly. Cover background jobs, caches (tenant-scoped keys), and search indexes as well. Enforce tenant filtering centrally, for example with EF Core global query filters, and add tests that fail if any entity lacks a tenant filter. Include these tests in CI as a blocking gate.
8. First measure: find the slowest tests and categories. Common fixes are sharing one database container per test collection instead of per test, resetting data with Respawn or unique data instead of recreating schemas, running tests in parallel with isolated data, replacing sleeps with proper synchronization, moving logic-heavy tests from integration level to unit level, trimming redundant end-to-end tests, and caching dependencies in CI. Split the suite so a fast subset (unit plus key integration tests) runs locally in under a few minutes, while the full suite runs in CI in parallel shards, and treat test speed as a maintained quality metric.
