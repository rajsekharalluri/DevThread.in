---
id: dotnet-learning-path
slug: learning-path
title: .NET Backend Learning Path
category: dotnet
categoryTitle: .NET Backend
difficulty: beginner
estimatedMinutes: 15
version:
  minimum: ".NET 8+"
prerequisites: [csharp-learning-path]
tags: [dotnet, learning-path, aspnet-core, backend, curriculum]
relatedTopics: [dotnet-aspnet-core-backend, efcore-learning-path, dotnet-testing]
order: 1
status: published
---
# .NET Backend Learning Path

## Introduction

This path takes you from C# knowledge to building, testing, and operating production ASP.NET Core services. It assumes you have completed the C# learning path (types, classes, interfaces, generics, collections, LINQ, async/await). Each stage lists the topics to study and a practical outcome that proves you understood them.

## Stage 1: Build an API

| Topic | What You Learn |
|---|---|
| ASP.NET Core Backend Engineering | Hosting model, middleware pipeline, routing, minimal APIs vs controllers, validation, problem details |
| DI, Configuration & Options | Service lifetimes, configuration providers, strongly typed options, environment-specific settings |

**Outcome:** a CRUD API with validation, consistent error responses, and configuration per environment.

## Stage 2: Persist Data

| Topic | What You Learn |
|---|---|
| EF Core, Dapper & Data Access | Choosing EF Core vs Dapper, repositories, projections |
| EF Core Change Tracking & Concurrency | Tracking, relationships, optimistic concurrency |
| EF Core Loading, N+1, DbContext Lifetime | Loading strategies, pooling, transactions, bulk operations |
| EF Core Query Performance | Query shape, indexes, compiled queries, diagnostics |
| EF Core Migrations & Deployment | Migration workflow, zero-downtime schema changes |

**Outcome:** the API persists to PostgreSQL or SQL Server with migrations, no N+1 queries, and concurrency protection.

## Stage 3: Test It

| Topic | What You Learn |
|---|---|
| .NET Testing | xUnit, test doubles, `WebApplicationFactory`, Testcontainers, contract tests |

**Outcome:** unit tests for domain rules and integration tests for every endpoint against a real database in CI.

## Stage 4: Secure and Scale It

| Topic | What You Learn |
|---|---|
| Caching, Auth & Serialization | Authentication and authorization, caching layers, System.Text.Json |
| Runtime & Background Services | Hosted services, workers, graceful shutdown |
| Logging & Diagnostics | Structured logging, health checks, OpenTelemetry traces and metrics |

**Outcome:** JWT/OIDC-protected endpoints, a background worker, health probes, and traces in a local observability stack.

## Stage 5: Understand the Runtime

| Topic | What You Learn |
|---|---|
| CLR, JIT & Assemblies | How code is compiled, loaded, and executed |
| Memory, GC & Performance | Allocations, garbage collection, benchmarking and profiling |

**Outcome:** a BenchmarkDotNet comparison and a profiling session that removes a measured hot-path allocation.

## Where to Go Next

- **Architecture** track: modular monoliths, microservices, messaging, resilience
- **DevOps & Cloud** track: Docker, Kubernetes, CI/CD, observability
- **Decision Guides:** EF Core vs Dapper, Task vs Thread vs async, REST vs gRPC

## Interview Questions
- **[L1]** What are the main building blocks of an ASP.NET Core application?
- **[L1]** What are the three DI service lifetimes and when is each used?
- **[L2]** Why should integration tests run against a real database?
- **[L2]** What production concerns go beyond writing endpoint code?
- **[L3]** How would you structure a new .NET backend service so it stays maintainable as it grows?
- **[L3]** How would you onboard a developer from another stack onto a .NET backend team?

## Interview Answers
1. The host (configuration, logging, DI container, server), the middleware pipeline that processes each request in order (exception handling, HTTPS, authentication, authorization, routing, endpoints), endpoints defined with minimal APIs or controllers, model binding and validation, dependency injection for services, and configuration through providers and options. Background services, health checks, and OpenTelemetry integration are added on the same host.
2. Transient services are created each time they are requested, suitable for lightweight stateless services. Scoped services are created once per scope, which is one HTTP request in ASP.NET Core, suitable for units of work such as `DbContext`. Singletons are created once for the application lifetime, suitable for thread-safe shared services such as caches, `HttpClient` factories, or configuration. A singleton must not depend on scoped services, or it will capture them beyond their intended lifetime.
3. Real databases enforce constraints, transactions, and concurrency behavior, and EF Core translates LINQ to real SQL whose behavior differs from in-memory providers. Testing against the real engine, for example with Testcontainers, catches translation errors, migration problems, constraint violations, and concurrency issues before production, which fake providers hide.
4. Security (authentication, authorization, input validation, secrets management), observability (structured logs, metrics, traces, health checks), resilience (timeouts, retries, circuit breakers), performance (query efficiency, caching, allocations), data safety (migrations, concurrency, backups), background processing, graceful shutdown, configuration per environment, automated testing, and CI/CD with safe deployments.
5. Organize by business capability or feature rather than technical layer, with clear module boundaries and contracts; keep domain logic in domain types rather than controllers; depend on abstractions at infrastructure boundaries; use consistent cross-cutting patterns for validation, errors, logging, and authorization; keep persistence behind application interfaces; enforce boundaries with architecture tests; and maintain a fast, reliable test suite with integration tests for each endpoint. Add structure incrementally as needs appear rather than predicting every abstraction upfront.
6. Map concepts from their stack to .NET (packages to NuGet, middleware, DI, async/await, LINQ), then have them follow a guided path through the team's service template: run it locally with Docker Compose, read the architecture decision records, make a small end-to-end change with tests, and deploy it through the pipeline. Pair them with a buddy, provide the C# and .NET learning paths for self-study, review their first PRs thoroughly with explanations, and gradually increase ownership.
