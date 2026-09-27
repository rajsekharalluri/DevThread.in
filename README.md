# Developer Engineering Academy

Developer Engineering Academy is a content-driven learning platform for developers preparing for senior engineering, lead engineering, and architecture roles.

## Phase 1

The current vertical slice includes:

- Angular 20 standalone frontend
- ASP.NET Core 10 REST API
- Filesystem-backed Markdown content with YAML front matter
- Topic rendering, table of contents, search, and previous/next navigation
- Bootstrap 5 and Tabler visual foundation
- Light/dark theme
- Browser-persistent learning statuses: In Progress, Completed, Revisit, Revise, and Skipped
- Browser-persistent bookmarks and last visited topic
- Content validation endpoint

## Run locally

```powershell
dotnet run --project src/DeveloperEngineeringAcademy.Web
```

In another terminal:

```powershell
cd client/developer-academy
npm install
npm start
```

Open `http://localhost:4200`.

## Verify

```powershell
dotnet build src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj
dotnet test src/DeveloperEngineeringAcademy.Tests/DeveloperEngineeringAcademy.Tests.csproj
cd client/developer-academy
npm run build
```

See [ARCHITECTURE.md](ARCHITECTURE.md), [CONTENT_GUIDE.md](CONTENT_GUIDE.md), [DEVELOPMENT.md](DEVELOPMENT.md), and [DEPLOYMENT.md](DEPLOYMENT.md) for more detail.
