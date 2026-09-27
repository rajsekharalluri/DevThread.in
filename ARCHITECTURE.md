# Architecture

The application is intentionally a modular monolith.

```text
Markdown + YAML -> Content Repository -> ASP.NET Core API -> Angular -> Browser Store
```

The Angular UI depends on HTTP contracts rather than the filesystem. The backend's `IContentRepository` and `ISearchService` abstractions allow the content source and search engine to evolve later.

## Backend

- `Core`: models and interfaces
- `Infrastructure`: filesystem repository, Markdown parsing, YAML metadata, search, validation
- `Web`: controllers, HTTP configuration, error boundary

## Frontend

Angular standalone components are organized around the shell, learning feature, search feature, and core services. The `LearningStore` uses Signals and a versioned localStorage key. It does not clear statuses automatically; records stay until browser storage is cleared or a future explicit user action removes them.

## Persistence evolution

Phase 1 uses localStorage. Future authenticated persistence can implement equivalent progress/bookmark interfaces backed by a database without changing topic rendering components.
