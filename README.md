# Developer Engineering Academy

Developer Engineering Academy is a content-driven learning platform for developers preparing for senior engineering, lead engineering, and architecture roles.

## Phases

### Phase 1: Core Platform

- Angular 20 standalone frontend
- ASP.NET Core 10 REST API
- Filesystem-backed Markdown content with YAML front matter
- Topic rendering, table of contents, search, and previous/next navigation
- Bootstrap 5 and Tabler visual foundation
- Light/dark theme
- Browser-persistent learning statuses: In Progress, Completed, Revisit, Revise, and Skipped
- Browser-persistent bookmarks and last visited topic
- Content validation endpoint

### Phase 2: Analytics & Learning Structure

- Google Analytics 4 integration (Measurement ID: G-6MES54BV3P)
- Google AdSense preparation (Publisher ID: ca-pub-6739348061871300)
- AdSense-readiness pages: About, Contact, Privacy, Terms, Cookies
- Complete learning tracks for C#/.NET, SQL, Angular, DevOps, Architecture, Python, Generative AI, Leadership, EF Core, and Build Real Systems
- API explanation audit for ASP.NET Core and EF Core
- Version context badges on topic pages

### Phase 3: Interview & Decision Support

- Interview Preparation feature with L1/L2/L3 difficulty filtering
- Hidden answers with reveal functionality
- Self-assessment: Confident / Review later ratings
- Comparison library for technology decisions:
  - Task vs Thread vs Async
  - EF Core vs Dapper
  - Monolith vs Microservices
  - Kafka vs RabbitMQ
  - REST vs gRPC vs Events
  - SQL vs NoSQL vs Graph
  - RAG vs Fine-Tuning
  - Containers vs Virtual Machines
  - Liveness vs Readiness vs Startup Probes
  - Cache vs Database vs Read Model
  - Event-Driven vs Request/Response Architecture
- Prerequisite navigation on topic pages

### Phase 4: Content Quality & Branding

- Content standard: every syntax/query example includes complete example, line-by-line explanation, execution flow, expected result, production variation, and common mistake
- Logo integration (LOGO.png)
- Favicon integration (Favicon.png)
- My Space menu: Progress, Bookmarks, My Notes, Interview Preparation
- Contact email: devthread.support@gmail.com
- Interview question expansion to 15+ questions per topic (5 L1, 5 L2, 5 L3)

## Run Locally

### Backend API

The API serves content from the `content/` directory at the repository root.

```powershell
cd C:\AI\SoftwareRAJA
dotnet run --project src/DeveloperEngineeringAcademy.Web
```

The API will start on `http://localhost:5000`.

Available endpoints:
- `GET /api/topics` - List all topics
- `GET /api/topics/{category}/{slug}` - Get topic content
- `GET /api/categories` - List categories
- `GET /api/search?q=query` - Search topics
- `GET /api/content/validate` - Validate content structure
- `GET /api/interview/questions` - Get interview questions

### Frontend

In a new terminal:

```powershell
cd C:\AI\SoftwareRAJA\client\developer-academy
npm install
npm start
```

The Angular dev server will start on `http://localhost:4200`.

Open `http://localhost:4200` in your browser.

## Verify

```powershell
# Build backend
dotnet build src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj

# Run backend tests
dotnet test src/DeveloperEngineeringAcademy.Tests/DeveloperEngineeringAcademy.Tests.csproj

# Build frontend
cd client/developer-academy
npm run build
```

## Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for AWS S3 + EC2 deployment instructions.

## Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture and design decisions
- [CONTENT_GUIDE.md](CONTENT_GUIDE.md) - Content standards and guidelines
- [DEVELOPMENT.md](DEVELOPMENT.md) - Development workflow and API contracts
- [DEPLOYMENT.md](DEPLOYMENT.md) - Deployment procedures and infrastructure
