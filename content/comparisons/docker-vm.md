---
id: comparisons-docker-vm
slug: docker-vm
title: Containers vs Virtual Machines
category: comparisons
categoryTitle: Decision Guides
difficulty: intermediate
estimatedMinutes: 20
version:
  minimum: "Container/virtualization concepts"
prerequisites: [devops-docker]
tags: [docker, containers, virtual-machines, comparison]
relatedTopics: [devops-kubernetes, devops-cicd-fundamentals]
order: 80
status: published
---
# Containers vs Virtual Machines

## Introduction

Both containers and virtual machines (VMs) let you run isolated workloads on shared hardware, but they isolate at different layers.

- A **VM** runs a complete guest operating system with its own kernel on top of a hypervisor (VMware ESXi, Hyper-V, KVM, AWS Nitro).
- A **container** is an isolated process (or group of processes) that **shares the host's kernel**, packaged with its application files, libraries, and runtime configuration as an image.

That single difference - separate kernel versus shared kernel - drives most trade-offs in startup time, density, isolation strength, and operating model. In practice, most cloud workloads run **containers inside VMs**: the VM provides a strong tenant boundary and the containers provide fast, repeatable application packaging.

## Quick Decision Table

| Concern | Container | Virtual Machine |
|---|---|---|
| Isolation boundary | Kernel namespaces + cgroups (shared kernel) | Hypervisor + separate guest kernel |
| Startup time | Milliseconds to seconds | Tens of seconds to minutes |
| Image size | MBs to a few hundred MBs | GBs (full OS) |
| Density per host | High (dozens to hundreds) | Lower (a handful to dozens) |
| OS flexibility | Must match host kernel family (Linux containers on Linux kernel) | Any OS the hypervisor supports |
| Patching | Rebuild and redeploy images | Patch OS in place or rebuild golden images |
| Best fit | Stateless services, microservices, CI jobs, batch | Legacy apps, different OS/kernel needs, strong multi-tenant isolation |
| Main risk | Kernel-level escape, misconfiguration (privileged, root) | Heavier ops, slower scaling, configuration drift |

## How They Differ Under the Hood

```text
VM stack:        Hardware -> Hypervisor -> Guest OS kernel -> Libraries -> App
Container stack: Hardware -> Host OS kernel -> Container runtime -> Libraries -> App
Common cloud:    Hardware -> Hypervisor -> VM (Linux kernel) -> containerd -> Containers
```

Containers use Linux kernel features:
- **Namespaces** isolate what a process can see (PIDs, network, mounts, users, hostname)
- **cgroups** limit what a process can use (CPU, memory, I/O)
- **Capabilities, seccomp, AppArmor/SELinux** restrict what a process can do

## Containers

### When to Use Containers

- Packaging applications with all dependencies for consistent dev, CI, and production
- Microservices and APIs that scale horizontally and must start quickly
- CI/CD build and test jobs
- Batch and event-driven workers that scale to zero

### When Not to Use Containers Alone

- Workloads that need a different kernel or OS (Windows-only legacy apps on Linux hosts)
- Untrusted multi-tenant code where a shared kernel is an unacceptable risk (use VMs or sandboxed runtimes such as gVisor, Kata Containers, or Firecracker microVMs)
- Applications that assume they own the whole machine (kernel modules, custom drivers)

### Example: Production-Minded Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Api/Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
```

```bash
docker run --read-only --cap-drop=ALL --security-opt no-new-privileges \
  --memory=512m --cpus=1 -p 8080:8080 myregistry/api:1.4.2
```

**Why each part exists:** multi-stage builds keep the SDK out of production; chiseled/distroless images remove shells and package managers that attackers use; `USER app` avoids root; read-only filesystems, dropped capabilities, and resource limits reduce blast radius.

## Virtual Machines

### When to Use VMs

- Legacy or vendor applications that require a specific OS, kernel, or full-machine install
- Strong isolation between tenants or security zones
- Stateful systems with specific kernel tuning or licensing tied to machines
- Hosts for Kubernetes nodes and other container platforms

### When Not to Use VMs

- Many small stateless services where density and fast scaling matter
- Short-lived jobs where boot time dominates run time

### Example: Immutable VM Image (Packer) Instead of Hand-Patched Servers

```hcl
source "amazon-ebs" "api" {
  region        = "us-east-1"
  instance_type = "t3.medium"
  source_ami_filter {
    filters     = { name = "ubuntu/images/*ubuntu-noble-24.04-amd64-server-*" }
    owners      = ["099720109477"]
    most_recent = true
  }
  ssh_username = "ubuntu"
  ami_name     = "legacy-api-{{timestamp}}"
}

build {
  sources = ["source.amazon-ebs.api"]
  provisioner "shell" {
    inline = ["sudo apt-get update", "sudo apt-get -y upgrade", "sudo /tmp/install-legacy-app.sh"]
  }
}
```

Baking images and replacing VMs (instead of patching in place) gives VMs some of the repeatability containers provide.

## Same Scenario: Migrating a Company's Workloads

| Workload | Recommendation | Reason |
|---|---|---|
| .NET/Node REST APIs | Containers on Kubernetes or a serverless container platform | Fast scaling, consistent builds |
| Nightly batch jobs | Containers (Kubernetes Jobs, cloud batch) | Start fast, pay per run |
| Legacy Windows app with COM dependencies | Windows VM (or Windows containers if supported) | OS-specific requirements |
| Customer-submitted code execution | MicroVMs / sandboxed runtimes | Shared kernel too risky for untrusted code |
| Database with strict kernel tuning | Managed database service or dedicated VM | Stateful, tuned, licensing |

## Decision Matrix

| Factor | Favors Containers | Favors VMs |
|---|---|---|
| Deployment speed and density | Strong | Weak |
| Different OS / kernel | Weak | Strong |
| Isolation for untrusted tenants | Weak (unless sandboxed) | Strong |
| Immutable, reproducible builds | Strong | Medium (with image baking) |
| Legacy application support | Weak | Strong |
| Team skills | Needs container/K8s skills | Traditional ops skills |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Running containers as root or `--privileged` | Non-root user, drop capabilities, no privileged mode |
| Treating containers as VMs (SSH in, patch in place) | Rebuild images and redeploy |
| Huge images with build tools and shells | Multi-stage builds, minimal base images |
| Storing state in the container filesystem | Volumes, databases, or object storage |
| Assuming containers are a hard security boundary for hostile code | Use VMs or sandboxed runtimes |
| Hand-configured "pet" VMs | Golden images and infrastructure as code |

## Interview Questions
- **[L1]** What is a container?
- **[L1]** How is a VM different?
- **[L2]** Why do containers start faster?
- **[L2]** Why should containers run as non-root?
- **[L3]** When is a VM a better choice?
- **[L3]** What production controls make containers safer?

## Interview Answers
1. A container is an isolated process, or group of processes, running from an image that packages the application with its libraries, runtime, and configuration. It shares the host operating system kernel and relies on kernel namespaces for isolation of what it can see and cgroups for limits on what it can use, which makes it lightweight and portable across environments.
2. A VM virtualizes hardware through a hypervisor and runs a complete guest operating system with its own kernel. That gives a stronger isolation boundary and the ability to run a different OS, but each VM carries the overhead of a full OS in memory, disk, boot time, and patching. Containers share one kernel and isolate processes instead of machines.
3. Starting a container only starts a process inside new namespaces and cgroups on an already-running kernel, after the image layers are available locally. There is no firmware initialization, kernel boot, or OS service startup as with a VM, so containers typically start in milliseconds to seconds rather than tens of seconds or minutes.
4. If an attacker compromises a process running as root inside a container, a container escape or kernel vulnerability can more easily translate into root access on the host, and root inside the container can modify more of the filesystem and use more kernel capabilities. Running as a non-root user with dropped capabilities follows least privilege and significantly limits the blast radius of a compromise.
5. A VM is better when the workload needs a different operating system or kernel than the host, when legacy or vendor software expects a full machine, when running untrusted or multi-tenant code where a shared kernel is unacceptable, when regulatory or security zoning requires hypervisor-level isolation, or for stateful systems with machine-level tuning or licensing. VMs are also the usual host layer for container platforms.
6. Use minimal, regularly rebuilt base images; scan images for vulnerabilities and sign them; run as non-root with dropped Linux capabilities, `no-new-privileges`, and seccomp/AppArmor profiles; avoid privileged mode and host mounts; use read-only root filesystems; set CPU and memory limits; inject secrets at runtime from a secret manager rather than baking them into images; apply network policies; allow only trusted registries through admission policies; and monitor runtime behavior for anomalies.
