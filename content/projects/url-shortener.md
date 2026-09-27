---
id: projects-url-shortener
slug: url-shortener
title: "Build: Scalable URL Shortener"
category: projects
categoryTitle: Build Real Systems
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "System design exercise"
prerequisites: [architecture-system-design, sql-indexes-performance, devops-aws]
tags: [project, system-design, caching, scaling, urls]
relatedTopics: [architecture-system-design, sql-indexes-performance, devops-aws]
order: 40
status: published
---
# Build: Scalable URL Shortener

## Introduction
Design a service that creates short codes for long URLs and redirects users quickly. It is a read-heavy system with a small write path and a latency-sensitive redirect path.

```text
POST /links → validate URL → generate unique code → store mapping
GET /abc123 → cache lookup → database fallback → 302 redirect
```

## Purpose
Practice capacity estimation, key generation, caching, hot-key behavior, abuse protection, and analytics without unnecessarily distributing every component.

## Professional design
```text
Client → CDN/load balancer → stateless redirect API
                              ├── Redis/cache
                              ├── SQL/Dynamo-style mapping store
                              └── async click event → analytics
```

The redirect path should not synchronously write analytics. A cache entry needs TTL/invalidation policy; a hot URL needs protection from cache stampede and abuse.

## Important decisions

- Collision-safe code generation and unique database constraint.
- Custom aliases and reserved words.
- URL validation and phishing/malware policy.
- Expiration/deletion semantics.
- Per-user/IP rate limits.
- Analytics privacy/retention.
- Cache consistency after deletion.
- Read/write capacity and hot-key distribution.

## Interview Questions
- **[L1]** What are the main operations?
- **[L1]** Why is the redirect path cacheable?
- **[L2]** How do you generate unique short codes?
- **[L2]** Why should click analytics be asynchronous?
- **[L3]** How would you handle hot URLs and cache stampedes?
- **[L3]** What security/abuse controls does the system need?

## Interview Answers
1. Create a mapping and resolve a code to a long URL, with optional expiration/ownership/analytics.
2. Many users repeatedly access the same mapping; caching avoids database work and reduces redirect latency.
3. Generate random/sequential IDs with collision checks/unique constraints, or encode a durable unique ID; never assume randomness alone guarantees uniqueness.
4. Redirect latency should not depend on analytics storage; queue events and process them independently.
5. Use cache-aside with TTL/jitter, single-flight/request coalescing, hot-key replication, and bounded fallback load.
6. Validate schemes/URLs, block abuse/phishing, rate-limit creation/redirects, protect admin actions, and handle privacy/retention for click data.

## Expert Perspective
A simple URL shortener teaches core system-design skills: access-pattern-first storage, hot-key behavior, cache failure, abuse controls, and the difference between user-critical and analytics work.
