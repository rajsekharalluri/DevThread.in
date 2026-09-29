---
id: efcore-migrations-deployment
slug: migrations-deployment
title: "EF Core Migrations and Zero-Downtime Schema Deployment"
category: efcore
categoryTitle: Entity Framework Core
difficulty: advanced
estimatedMinutes: 45
version:
  minimum: "EF Core 8+"
prerequisites: [efcore-change-tracking]
tags: [ef-core, migrations, schema, deployment, zero-downtime, database, ci-cd]
relatedTopics: [efcore-loading-dbcontext-lifetime, devops-cicd-fundamentals, sql-database-design]
order: 30
status: published
---
# EF Core Migrations and Zero-Downtime Schema Deployment

## Introduction

EF Core migrations turn changes in your C# model into versioned schema change scripts. Creating a migration is easy; **deploying schema changes safely to a production database that is serving traffic** is the real engineering problem. A careless migration can lock a large table for minutes, break the previous application version during a rolling deploy, or lose data.

This page covers how migrations work, the development workflow, the options for applying migrations in production, the **expand/contract** pattern for zero-downtime changes, data migrations, and CI/CD practices.

## Part 1: How Migrations Work

| Artifact | Purpose |
|---|---|
| Migration class (`20250314_AddOrderNotes.cs`) | `Up()` and `Down()` methods describing schema operations |
| Model snapshot (`OrdersDbContextModelSnapshot.cs`) | EF's picture of the current model, used to compute the next migration's diff |
| `__EFMigrationsHistory` table | Records which migrations have been applied to a database |

### Development Workflow

```bash
dotnet tool install --global dotnet-ef

# 1. Change the model (add a property, entity, index...)
# 2. Create a migration
dotnet ef migrations add AddOrderNotes --project src/Orders.Infrastructure --startup-project src/Orders.Api

# 3. Review the generated code AND the SQL
dotnet ef migrations script --idempotent --output artifacts/migrate.sql

# 4. Apply locally
dotnet ef database update
```

```csharp
public partial class AddOrderNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Notes", table: "Orders", type: "character varying(2000)", maxLength: 2000, nullable: true);

        migrationBuilder.CreateIndex(name: "IX_Orders_CustomerId_CreatedAt", table: "Orders", columns: ["CustomerId", "CreatedAt"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Orders_CustomerId_CreatedAt", table: "Orders");
        migrationBuilder.DropColumn(name: "Notes", table: "Orders");
    }
}
```

**Always review generated migrations.** EF can interpret a property rename as "drop column + add column", which **deletes data**. Fix it by using `RenameColumn` explicitly.

```csharp
// Generated (dangerous):  DropColumn("Comment"); AddColumn("Notes")
// Correct:
migrationBuilder.RenameColumn(name: "Comment", table: "Orders", newName: "Notes");
```

### Team Rules for Migrations

- One migration per logical change; descriptive names
- Never edit a migration that has been applied to a shared environment - add a new one
- Resolve snapshot merge conflicts by regenerating the migration on top of the latest main branch
- Keep migrations in the infrastructure project that owns the `DbContext`

## Part 2: Applying Migrations in Production

| Approach | How | Pros | Cons |
|---|---|---|---|
| `Database.Migrate()` at app startup | App applies pending migrations when it starts | Simple | Multiple instances race; app needs DDL permissions; slow startup; failures crash the app |
| **Idempotent SQL script** in the pipeline | `dotnet ef migrations script --idempotent` reviewed and executed by CD | Reviewable, auditable, DBA-friendly | Extra pipeline step |
| **Migration bundle** | `dotnet ef migrations bundle` produces a self-contained executable run by CD or a Kubernetes Job | No SDK needed at deploy time, single artifact | Still needs orchestration |
| Dedicated migration job/container | Kubernetes Job or init step runs the bundle once before rollout | One runner, controlled ordering | More deployment configuration |

**Recommended for production:** generate an idempotent script or a bundle in CI, review it, and run it **once** as a separate deployment step with a dedicated database identity that has DDL rights. Application instances then run with least-privilege credentials (no DDL).

```bash
# CI: build artifacts
dotnet ef migrations bundle --project src/Orders.Infrastructure --startup-project src/Orders.Api \
  --configuration Release --self-contained -r linux-x64 --output artifacts/efbundle

# CD: run before deploying the new application version
./efbundle --connection "$MIGRATOR_CONNECTION_STRING"
```

```yaml
# Kubernetes Job that runs the bundle before the Deployment is updated
apiVersion: batch/v1
kind: Job
metadata:
  name: orders-migrate-v42
spec:
  backoffLimit: 0
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: migrate
          image: registry.example.com/orders-migrator:v42
          env:
            - name: MIGRATOR_CONNECTION_STRING
              valueFrom: { secretKeyRef: { name: orders-db-migrator, key: connection } }
```

## Part 3: Zero-Downtime Changes with Expand/Contract

During a rolling deployment, **old and new application versions run at the same time against the same database.** Every schema change must be compatible with both versions. The expand/contract (parallel change) pattern splits breaking changes into safe steps.

### Example: Rename `Orders.Comment` to `Orders.Notes`

A direct rename breaks old instances that still query `Comment`.

```text
Release 1 (expand):   Add nullable column Notes. App writes to BOTH Comment and Notes, reads Comment.
Backfill:             Copy Comment into Notes in batches for existing rows.
Release 2 (migrate):  App reads Notes, still writes both (so rollback to Release 1 is safe).
Release 3 (contract): App stops writing Comment. Later migration drops Comment.
```

### Safe vs Risky Schema Operations

| Change | Risk | Safe Approach |
|---|---|---|
| Add nullable column | Safe | Deploy directly |
| Add NOT NULL column | Breaks old inserts; may rewrite table | Add nullable, backfill, then add constraint |
| Rename column/table | Breaks old code | Expand/contract |
| Drop column | Breaks old code still reading it | Stop using it in code first, drop in a later release |
| Change column type | May rewrite/lock table, break code | New column + backfill + switch + drop |
| Add index on large table | Locks writes during build | PostgreSQL `CREATE INDEX CONCURRENTLY`; SQL Server `ONLINE = ON` (edition-dependent) |
| Add foreign key on large table | Validation scan locks | PostgreSQL: add `NOT VALID`, then `VALIDATE CONSTRAINT` separately |

### Custom SQL in Migrations for Online Operations

```csharp
public partial class AddOrdersStatusIndexConcurrently : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // CREATE INDEX CONCURRENTLY cannot run inside a transaction
        migrationBuilder.Sql(
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Orders_Status" ON "Orders" ("Status");""",
            suppressTransaction: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""DROP INDEX CONCURRENTLY IF EXISTS "IX_Orders_Status";""", suppressTransaction: true);
}
```

## Part 4: Data Migrations and Backfills

Schema migrations should be fast. Large data changes should **not** run inside a schema migration transaction that locks a table for minutes.

```csharp
public sealed class BackfillOrderNotesJob(IDbContextFactory<OrdersDbContext> contexts, ILogger<BackfillOrderNotesJob> log)
{
    public async Task RunAsync(CancellationToken ct)
    {
        const int batchSize = 5_000;
        int updated;
        do
        {
            await using var db = await contexts.CreateDbContextAsync(ct);
            updated = await db.Database.ExecuteSqlRawAsync("""
                UPDATE "Orders" SET "Notes" = "Comment"
                WHERE "Id" IN (
                    SELECT "Id" FROM "Orders"
                    WHERE "Notes" IS NULL AND "Comment" IS NOT NULL
                    LIMIT 5000)
                """, ct);
            log.LogInformation("Backfilled {Count} orders", updated);
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);   // give the database breathing room
        } while (updated == batchSize);
    }
}
```

**Backfill principles:** batch, make it resumable and idempotent (the `WHERE Notes IS NULL` condition), throttle, monitor replication lag, and run it outside peak hours if needed.

## Part 5: Migrations in CI/CD

```text
Pull request:
  1. Build and run tests (integration tests apply migrations to a Testcontainers database)
  2. Check for pending model changes: `dotnet ef migrations has-pending-model-changes` (EF Core 8+) fails if the model and snapshot differ
  3. Generate the idempotent SQL script as a build artifact for review
  4. Flag destructive operations (DropColumn, DropTable, AlterColumn) for mandatory review

Deployment:
  5. Back up / ensure point-in-time recovery is enabled
  6. Run the migration bundle or script once with the migrator identity
  7. Roll out the new application version (rolling / blue-green)
  8. Run contract (cleanup) migrations in a LATER release
```

### Rollback Strategy

Down migrations are rarely safe in production after new data has been written. Prefer **roll-forward**: fix with a new migration. Design every release so the **previous application version still works with the new schema** (expand/contract) - then rolling back the application does not require rolling back the database.

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Applying migrations from every app instance at startup | Run once in a dedicated deployment step |
| Accepting generated drop+add for a rename | Use `RenameColumn`, or expand/contract for zero downtime |
| Adding NOT NULL columns to large tables in one step | Nullable, backfill, then constraint |
| Large data updates inside schema migrations | Separate batched backfill jobs |
| Building indexes with blocking DDL on hot tables | Concurrent/online index creation |
| Relying on Down migrations for rollback | Backward-compatible schema + roll-forward |
| App runtime identity has DDL rights | Separate migrator identity; least privilege for the app |

## Interview Questions
- **[L1]** What is an EF Core migration and what is the model snapshot used for?
- **[L1]** How does EF Core know which migrations have already been applied to a database?
- **[L2]** What are the options for applying migrations in production, and which would you choose?
- **[L2]** Why can a generated migration for a renamed property lose data, and how do you prevent it?
- **[L3]** Explain the expand/contract pattern with an example of a breaking schema change.
- **[L3]** How would you add a NOT NULL column and an index to a table with 500 million rows without downtime?
- **[L3]** How do you design database deployments so an application rollback is always safe?

## Interview Answers
1. A migration is a versioned C# class with `Up` and `Down` methods describing schema operations, generated by comparing the current model with the previous state. The model snapshot is a code representation of the model as of the latest migration; EF compares the current `DbContext` model against the snapshot to compute what the next migration must change. Migrations are committed to source control and applied in order.
2. EF Core stores applied migration IDs in the `__EFMigrationsHistory` table in the target database. When applying migrations, it compares the migrations in the assembly with the rows in that table and runs only the missing ones in order, then inserts their IDs. Idempotent scripts check this table before each migration block, so they can be run safely against databases at different versions.
3. Options include calling `Database.Migrate()` at application startup, generating an idempotent SQL script with `dotnet ef migrations script --idempotent` and running it in the pipeline, producing a migration bundle executable with `dotnet ef migrations bundle`, or running the bundle as a dedicated Kubernetes Job before rollout. For production I would generate the script or bundle in CI, review it, and run it once as a controlled deployment step with a dedicated migrator identity, because startup migrations race across instances, slow startups, and require the application to have DDL permissions.
4. EF infers operations from model differences, and a renamed property can look like one property removed and another added, so the generated migration drops the old column, destroying its data, and adds an empty new column. Prevent it by reviewing every migration and its generated SQL, replacing drop-and-add with `RenameColumn`, flagging destructive operations in CI for mandatory review, and using expand/contract when the rename must be zero-downtime.
5. Expand/contract splits a breaking change into backward-compatible steps so old and new application versions can run against the same schema during rolling deployments. For renaming `Comment` to `Notes`: first add the new `Notes` column and deploy code that writes both columns but reads the old one; backfill existing rows in batches; then deploy code that reads `Notes` while still writing both, so rollback remains safe; then deploy code that stops using `Comment`; finally drop `Comment` in a later migration. At each step, the previous release still works.
6. Add the column as nullable, which is a fast metadata change, and deploy code that writes it for new rows. Backfill existing rows in small batches with throttling, monitoring replication lag and locks, in a resumable job. Then enforce non-null safely: on PostgreSQL, add a `CHECK (col IS NOT NULL) NOT VALID` constraint, validate it separately (which does not block writes), and then set `NOT NULL`, which can use the validated constraint. Create the index with `CREATE INDEX CONCURRENTLY` (or online index builds on SQL Server) in a migration with `suppressTransaction: true`. Test timings on a production-sized copy first.
7. Make every schema change backward compatible with the previous application version: only additive changes in the release that introduces them, destructive changes (drops, type changes, constraints the old code violates) deferred to a later release after no running code depends on the old shape, and data backfills decoupled from schema changes. Run migrations before rolling out the new application version, so the new schema supports both versions. Then rolling back the application is just redeploying the previous image, with no database rollback required; fix schema mistakes by rolling forward with new migrations, and keep backups and point-in-time recovery as a last resort.
