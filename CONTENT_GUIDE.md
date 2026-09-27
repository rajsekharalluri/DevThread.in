# Content Guide

Add a Markdown file below `content/<category>/` with YAML front matter.

```yaml
---
id: csharp-example
slug: example
title: C# Example
category: csharp
categoryTitle: C#
difficulty: intermediate
estimatedMinutes: 30
version:
  minimum: "12 / .NET 8+"
prerequisites: [csharp-fundamentals]
tags: [csharp]
relatedTopics: []
order: 70
status: published
---
```

Use a compact, topic-specific guide rather than a fixed checklist. The required anchors are:

- Introduction / Definition
- Purpose
- Real-World Simple Example
- Professional Company-Level Example
- Complete Example
- Line-by-Line Explanation
- Execution Flow / Expected Result
- Advantages and Disadvantages (when applicable)
- Comparison (when related alternatives exist)
- Interview Questions
- Interview Answers

DevThread is a guided learning/reference platform, not an interactive coding-practice platform. Do not require users to submit code. Every important code or SQL example must be followed by a line-by-line explanation, execution flow, expected result, production variation, and common mistake.

## API and query explanation rule

Never mention an API, operator, method, CLI command, SQL clause, framework object, or configuration property as an unexplained keyword. For every important API, explain:

- What it is and what problem it solves.
- Whether it executes immediately or builds/defer work.
- What it returns, including empty/null/error behavior.
- What happens internally or on the network/database.
- A complete usage example.
- A line-by-line explanation of that example.
- When to use it and when not to use it.
- A comparison with the closest alternatives.
- Production performance, security, cancellation, and failure considerations where relevant.

If an API is only mentioned as a related concept, link it to a topic that explains it fully rather than pretending the mention is coverage. Add other headings only when they genuinely improve understanding.

Topic IDs referenced by prerequisites and related topics must exist. Content is rendered as display-only Markdown; the application never executes code examples.

## Interview question format

Interview questions use three difficulty tags instead of five separate headings:

- `[L1]` — Foundational/practical knowledge any working developer should have.
- `[L2]` — Deeper technical or internals-level understanding.
- `[L3]` — Senior/architect-level judgment, trade-offs, and system-level thinking.

List exactly 6 questions as a flat list with the tag inline, then repeat each question with its answer in the **Interview Answers** section immediately below. Use two L1, two L2, and two L3 questions so the set covers fundamentals, production practice, and senior judgment without becoming repetitive:

```markdown
## Interview Questions
- **[L1]** What problem does X solve?
- **[L2]** How does X behave internally?
- **[L3]** When would you avoid X in a distributed system?

## Interview Answers
1. **[L1] What problem does X solve?**
   Answer explaining the concept clearly with a concrete example.
2. **[L2] How does X behave internally?**
   Answer describing internal/runtime behavior.
3. **[L3] When would you avoid X in a distributed system?**
   Answer describing trade-offs and alternative approaches.
```

Every topic should also include realistic, runnable-looking code examples (not just prose) in `Syntax and API`, `Production Example`, and ideally a bad-vs-good comparison in `Common Mistakes`.

## Leadership / soft-skills template

Non-technical topics (category `leadership`) use a different section set, since "Internal Implementation," "Performance," and "Security" don't apply to people/process topics:

```markdown
## Overview
## Why It Matters
## How To Approach It
## Real-World Example
## Common Pitfalls
## When To Apply
## When Not To Apply
## Trade-offs
## Related Topics
## Practical Exercise
## Interview Questions
## Interview Answers
## Senior Leadership Perspective
```

The interview question format (L1/L2/L3 tags + a matching `Interview Answers` section) is the same as the technical template. `ContentValidator` checks category-specific required sections automatically based on the topic's `category` front-matter value.

## Technology-level curriculum coverage

The topic list is organized as learning tracks. A track is responsible for complete day-to-day development coverage; individual pages should explain one coherent group of decisions rather than become isolated glossary entries.

```text
C# / .NET
  Language fundamentals → OOP/types → collections/LINQ → async/runtime → ASP.NET Core → data access → testing/security/observability

SQL / Databases
  Querying → joins/aggregation → modeling/normalization → transactions/concurrency → indexes/performance → relational/NoSQL/graph choices

Angular
  Components/templates → services/DI → routing/forms → RxJS/HTTP → signals/change detection → state management → production UI concerns

Architecture
  Boundaries → clean architecture → patterns/DDD → API communication → events/Kafka → distributed systems → resilience → system design

DevOps/Cloud
  Git/CI/CD → containers → Kubernetes → AWS/Azure → PCF/platform migration → IaC → observability/production operations

Python
  Language/collections → OOP → generators/decorators → async → errors/testing/type hints → FastAPI

Generative AI
  Transformers → embeddings/vector search → chunking → RAG → hybrid/advanced retrieval → agents/LangChain → MCP

Leadership
  Team leadership → mentoring → reviews/feedback → decisions → communication → conflict → agile delivery → career growth
```

## Current top-level categories

```text
csharp                    — C#
programming-fundamentals  — Programming Fundamentals
sql                        — SQL / Databases
angular                    — Angular
dotnet                     — .NET Backend
databases                  — NoSQL, Graph & Data Access
devops                     — DevOps & Cloud
python                     — Python
genai                       — Generative AI
architecture                 — Architecture & Distributed Systems
leadership                 — Leadership & Soft Skills
```
