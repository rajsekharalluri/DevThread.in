---
id: devops-docker
slug: docker
title: Docker and Containerization
category: devops
categoryTitle: DevOps & Cloud
difficulty: intermediate
estimatedMinutes: 50
version:
  minimum: "Docker 24+"
prerequisites: [devops-cicd-fundamentals]
tags: [docker, containers, images, security]
relatedTopics: [devops-kubernetes, devops-cicd-fundamentals]
order: 20
status: published
---
# Docker and Containerization

## Introduction
A Docker image is a versioned, read-only package containing application files and runtime metadata. A container is a running instance of that image with an isolated process/network/filesystem view.

```text
Dockerfile
   ↓ build
Image layers
   ↓ run
Container process + writable layer
Port/volume/network configuration
```

A container shares the host kernel; it is not a complete virtual machine.

## Purpose
Containers make the runtime environment repeatable across developer machines, CI, staging, and production. They are useful for packaging, deployment, isolation, and orchestration.

## Simple example
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /out .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeveloperEngineeringAcademy.Web.dll"]
```

```bash
docker build -t academy-api:1.0 .
docker run --rm -p 8080:8080 academy-api:1.0
```

## Professional company-level example
Use a multi-stage build so the final image contains only the runtime and published output, not the SDK/source/build cache. Pin a maintained base image, scan it, run as non-root, configure a health endpoint, and inject secrets at runtime rather than baking them into a layer.

```text
Source → build image with SDK
          ↓ dotnet publish
        runtime image only
          ↓ registry scan/sign
        deploy to Kubernetes/PaaS
```

Docker layer order matters:

```dockerfile
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /out
```

Dependencies can be cached when only source code changes.

## Production failure scenarios
- Using `:latest` makes deployments non-reproducible.
- Secrets copied into an image remain in image history.
- Running as root increases compromise impact.
- A container works locally but depends on a missing volume/env variable.
- No health check means the orchestrator sends traffic to a broken process.
- Writable container filesystem loses data when the container is replaced.

Use external storage for durable data and environment/secret injection for configuration.

## Comparison
| Container | Virtual machine |
|---|---|
| Shares host kernel | Own guest kernel |
| Starts quickly | Heavier startup |
| Efficient density | Stronger isolation boundary |
| Packages process/runtime | Packages entire OS |
| Needs image discipline | Needs OS/VM patching |

## Interview Questions
- **[L1]** What is the difference between an image and a container?
- **[L1]** How is a container different from a VM?
- **[L2]** Why use multi-stage Docker builds?
- **[L2]** Why should secrets never be copied into an image?
- **[L3]** How would you make a production image secure and reproducible?
- **[L3]** What should remain outside a container's writable filesystem?

## Interview Answers
1. An image is a read-only template; a container is a running instance with a writable layer and runtime configuration.
2. Containers share the host kernel and use OS isolation; VMs virtualize a complete operating system and generally provide stronger isolation at greater cost.
3. They keep SDK/source/build tools out of the final image, reducing size, startup/download time, and attack surface.
4. Deleted files can remain in earlier image layers/history. Secrets must be injected at runtime from a secure provider.
5. Pin image digests/tags, use minimal maintained bases, run non-root, scan/sign images, use multi-stage builds, define health checks, and make builds reproducible.
6. Databases, uploads, and durable business state belong in managed storage/volumes/external services; containers should be replaceable.

## Expert perspective
A container is a supply-chain artifact and an operational contract, not just a packaging trick. Senior engineers review its base image, user permissions, secret handling, health behavior, resource limits, and reproducibility.
