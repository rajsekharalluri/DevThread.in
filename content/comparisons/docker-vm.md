---
id: comparisons-docker-vm
slug: docker-vm
title: Containers vs Virtual Machines
category: comparisons
categoryTitle: Decision Guides
difficulty: intermediate
estimatedMinutes: 30
version:
  minimum: "Container/virtualization concepts"
prerequisites: [devops-docker]
tags: [docker, containers, virtual-machines, comparison]
relatedTopics: [devops-kubernetes, devops-cicd-fundamentals]
order: 80
status: published
---
# Containers vs Virtual Machines

## Decision table

| Concern | Container | VM |
|---|---|---|
| Isolation | OS/process isolation | Guest OS boundary |
| Startup | Fast | Heavier |
| Density | High | Lower |
| Package | App/runtime | Full operating system |
| Best fit | Repeatable app deployment | Stronger OS isolation/legacy OS |

## Example

```text
VM → guest kernel → container/runtime → process
Container → host kernel → isolated process
```

Containers are not automatically a complete security boundary. Use least privilege, non-root users, image scanning, kernel/runtime patches, and workload isolation.

## Interview Questions
- **[L1]** What is a container?
- **[L1]** How is a VM different?
- **[L2]** Why do containers start faster?
- **[L2]** Why should containers run as non-root?
- **[L3]** When is a VM a better choice?
- **[L3]** What production controls make containers safer?

## Interview Answers
1. An isolated process packaged with its files/runtime configuration, sharing the host kernel.
2. A VM virtualizes/runs a complete guest OS; a container shares the host kernel and isolates processes.
3. It does not boot a guest OS; it starts an application process from an image.
4. A container escape/compromise should have the least possible host/container privilege.
5. Stronger OS isolation, incompatible/legacy OS requirements, or workloads needing a full kernel boundary.
6. Minimal maintained images, non-root, scanning/signing, resource limits, secret injection, read-only filesystem where possible, and runtime/network policies.
