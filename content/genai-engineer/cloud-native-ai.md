---
id: genai-eng-cloud-native-ai
slug: cloud-native-ai
title: "Module 15: Cloud-Native AI"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: advanced
estimatedMinutes: 110
version:
  minimum: "Docker 24+, Kubernetes 1.29+, Terraform 1.6+, any major cloud"
prerequisites: [genai-eng-llmops-production]
tags: [genai-engineer, cloud-native, docker, kubernetes, gpu, autoscaling, terraform, aws, azure, gcp]
relatedTopics: [genai-eng-ai-projects]
order: 15
status: published
---
# Module 15: Cloud-Native AI

## Introduction

Enterprise AI platforms must serve many teams, scale with unpredictable demand, stay secure and compliant, and run reliably across regions. **Cloud-native** practices - containers, Kubernetes, infrastructure as code, managed services, and automated delivery - are how organizations achieve this for AI workloads.

This module covers containerizing AI services, running GPU workloads on Kubernetes, autoscaling inference, choosing between managed AI services on AWS, Azure, and GCP, event-driven AI pipelines, infrastructure as code, CI/CD for AI applications, networking and security for AI platforms, and a complete reference architecture for an enterprise GenAI platform.

---

## Part 1: The Cloud-Native AI Stack

```text
Experience layer:     Web/mobile apps, chat UIs, IDE plugins, internal tools
API layer:            AI gateway, application APIs (FastAPI/.NET), auth, rate limits
Orchestration layer:  RAG pipelines, agents, workflows (LangGraph, Temporal), queues
Model layer:          Provider APIs (Azure OpenAI, Bedrock, Vertex) + self-hosted (vLLM on K8s)
Data layer:           Vector DB, relational DB, object storage, cache, feature/metadata stores
Platform layer:       Kubernetes (CPU + GPU node pools), serverless, networking, IAM, secrets
Operations layer:     IaC, CI/CD, observability (OTel), cost management, policy as code
```

### Workload Types and Where They Run

| Workload | Characteristics | Typical Platform |
|---|---|---|
| API / orchestration services | Stateless, CPU, spiky | Kubernetes Deployments, serverless containers (Cloud Run, Container Apps, Fargate) |
| LLM inference (self-hosted) | GPU, stateful KV cache, long-lived | Kubernetes GPU node pools, managed endpoints (SageMaker, Vertex, Azure ML) |
| Embedding / reranking | GPU or CPU, batchable | K8s with GPU/CPU, managed endpoints |
| Document ingestion | Bursty, batch, long-running | Queue + workers (K8s Jobs, KEDA), serverless functions |
| Fine-tuning | Heavy GPU, hours-days | K8s Jobs, Ray, managed training (SageMaker, Vertex, Azure ML) |
| Vector search | Stateful, memory-heavy | Managed vector DB, StatefulSets, pgvector on managed Postgres |

---

## Part 2: Containerizing AI Services

### Dockerfile for a Python RAG API

```dockerfile
# syntax=docker/dockerfile:1.7
FROM python:3.12-slim AS builder
ENV PIP_NO_CACHE_DIR=1 PIP_DISABLE_PIP_VERSION_CHECK=1
WORKDIR /app
COPY requirements.txt .
RUN pip install --prefix=/install -r requirements.txt

FROM python:3.12-slim
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
RUN useradd --create-home --uid 10001 appuser
WORKDIR /app
COPY --from=builder /install /usr/local
COPY src/ ./src/
USER appuser
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8080/health')"
CMD ["uvicorn", "src.app:app", "--host", "0.0.0.0", "--port", "8080", "--workers", "2"]
```

**Best practices applied:**
- **Multi-stage build** keeps build tools out of the runtime image
- **Slim base image** reduces size and attack surface
- **Non-root user**
- **No secrets baked in** - API keys come from environment/secret stores at runtime
- **Health check** for orchestrators

### GPU Inference Container

```dockerfile
FROM vllm/vllm-openai:latest
# Model weights are NOT baked in: mount from a volume / download at startup from a private registry or object storage
ENV HF_HOME=/models
EXPOSE 8000
ENTRYPOINT ["python3", "-m", "vllm.entrypoints.openai.api_server"]
CMD ["--model", "Qwen/Qwen2.5-7B-Instruct", "--max-model-len", "16384", "--enable-prefix-caching", "--port", "8000"]
```

### Handling Large Model Weights

Model weights (5-150+ GB) should not live in container images:

| Approach | Notes |
|---|---|
| **Persistent volume cache** | Download once to a shared volume (ReadOnlyMany) |
| **Object storage + init container** | Pull weights from S3/GCS/Blob at pod start; slow cold starts |
| **Node-local cache / pre-pulled images** | Fastest startup; manage via DaemonSets |
| **Streaming loaders** | Stream weights directly from object storage into GPU memory |

**Cold start** (pulling image + loading weights) for large models can take minutes - a critical factor for autoscaling design.

### Local Development with Docker Compose

```yaml
# docker-compose.yml
services:
  api:
    build: .
    ports: ["8080:8080"]
    environment:
      DATABASE_URL: postgresql://app:app@postgres:5432/ai
      LLM_BASE_URL: http://llm:11434/v1
      REDIS_URL: redis://redis:6379
    depends_on: [postgres, redis, llm]
  postgres:
    image: pgvector/pgvector:pg16
    environment: { POSTGRES_USER: app, POSTGRES_PASSWORD: app, POSTGRES_DB: ai }
    volumes: ["pgdata:/var/lib/postgresql/data"]
  redis:
    image: redis:7-alpine
  llm:
    image: ollama/ollama:latest
    volumes: ["ollama:/root/.ollama"]
volumes:
  pgdata:
  ollama:
```

Developers get the full stack (API + pgvector + cache + local LLM) with one command, using OpenAI-compatible endpoints so production code paths remain identical.

---

## Part 3: Kubernetes for AI Workloads

### GPU Scheduling Basics

- GPU nodes run the **NVIDIA device plugin** (usually via the **NVIDIA GPU Operator**), which advertises `nvidia.com/gpu` as a schedulable resource
- Pods request GPUs in `resources.limits`
- GPU node pools are **tainted** so only GPU workloads land on expensive nodes
- **MIG** (Multi-Instance GPU) or time-slicing lets small models share a GPU

### vLLM Deployment on Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: llm-qwen-7b
  namespace: ai-inference
spec:
  replicas: 2
  selector:
    matchLabels: { app: llm-qwen-7b }
  template:
    metadata:
      labels: { app: llm-qwen-7b }
    spec:
      nodeSelector:
        gpu-type: l4
      tolerations:
        - key: nvidia.com/gpu
          operator: Exists
          effect: NoSchedule
      containers:
        - name: vllm
          image: vllm/vllm-openai:v0.6.3
          args: ["--model", "Qwen/Qwen2.5-7B-Instruct", "--max-model-len", "8192",
                 "--gpu-memory-utilization", "0.90", "--enable-prefix-caching", "--port", "8000"]
          ports: [{ containerPort: 8000 }]
          env:
            - name: HF_TOKEN
              valueFrom: { secretKeyRef: { name: hf-token, key: token } }
          resources:
            limits: { nvidia.com/gpu: 1, memory: 32Gi, cpu: "8" }
            requests: { nvidia.com/gpu: 1, memory: 24Gi, cpu: "4" }
          volumeMounts:
            - { name: model-cache, mountPath: /root/.cache/huggingface }
            - { name: shm, mountPath: /dev/shm }
          startupProbe:
            httpGet: { path: /health, port: 8000 }
            failureThreshold: 60        # allow up to 10 minutes for model loading
            periodSeconds: 10
          readinessProbe:
            httpGet: { path: /health, port: 8000 }
            periodSeconds: 10
          livenessProbe:
            httpGet: { path: /health, port: 8000 }
            periodSeconds: 30
      volumes:
        - name: model-cache
          persistentVolumeClaim: { claimName: hf-model-cache }
        - name: shm
          emptyDir: { medium: Memory, sizeLimit: 8Gi }
---
apiVersion: v1
kind: Service
metadata:
  name: llm-qwen-7b
  namespace: ai-inference
spec:
  selector: { app: llm-qwen-7b }
  ports: [{ port: 80, targetPort: 8000 }]
```

**Key details:**
- **startupProbe** with a long threshold prevents Kubernetes from killing pods during slow model loading
- **Shared memory** (`/dev/shm`) is needed for tensor-parallel communication
- **Taints/tolerations + nodeSelector** keep inference on the right GPU type
- **Model cache PVC** avoids re-downloading weights on every restart

### AI-Specific Kubernetes Ecosystem

| Tool | Purpose |
|---|---|
| **NVIDIA GPU Operator** | Drivers, device plugin, monitoring (DCGM) on GPU nodes |
| **KServe** | Standardized model serving (InferenceService CRD), autoscaling, canaries |
| **KubeRay** | Ray clusters on K8s for distributed training, batch inference, Ray Serve |
| **Kueue / Volcano** | Batch job queueing and fair-share GPU scheduling across teams |
| **KEDA** | Event-driven autoscaling (queue length, Prometheus metrics) |
| **Karpenter / Cluster Autoscaler** | Provision GPU nodes on demand |
| **Gateway API inference extensions / llm-d** | Model-aware routing (KV-cache-aware, LoRA-aware load balancing) |

---

## Part 4: Autoscaling AI Inference

### Why CPU-Based Autoscaling Fails for LLMs

GPU inference pods often show low CPU while the GPU is saturated. Scaling on CPU either never triggers or triggers too late. Scale on **inference-aware signals**:

| Signal | Meaning |
|---|---|
| **Pending/waiting requests** (`vllm:num_requests_waiting`) | Queue building up - scale out |
| **KV cache utilization** (`vllm:gpu_cache_usage_perc`) | Memory pressure on running replicas |
| **TTFT p95** | User-visible latency degrading |
| **Queue depth** (for async workers) | Backlog in ingestion or batch jobs |

### KEDA Scaling on vLLM Metrics

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: llm-qwen-7b-scaler
  namespace: ai-inference
spec:
  scaleTargetRef:
    name: llm-qwen-7b
  minReplicaCount: 1
  maxReplicaCount: 8
  cooldownPeriod: 600            # scale down slowly - GPU pods are expensive to restart
  triggers:
    - type: prometheus
      metadata:
        serverAddress: http://prometheus.monitoring:9090
        query: sum(vllm:num_requests_waiting{namespace="ai-inference",app="llm-qwen-7b"})
        threshold: "10"
```

### Scaling Ingestion Workers on Queue Depth

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: ingestion-workers
spec:
  scaleTargetRef: { name: ingestion-worker }
  minReplicaCount: 0              # scale to zero when idle
  maxReplicaCount: 50
  triggers:
    - type: aws-sqs-queue
      metadata:
        queueURL: https://sqs.us-east-1.amazonaws.com/123456789012/doc-ingestion
        queueLength: "20"         # target messages per replica
        awsRegion: us-east-1
      authenticationRef: { name: keda-aws-auth }
```

### Cold Start Strategies

| Strategy | Trade-off |
|---|---|
| Keep minimum warm replicas | Cost of idle GPUs vs latency |
| Pre-provisioned warm node pool | Faster node availability |
| Smaller/quantized models | Faster load, lower quality |
| Fast weight loading (local NVMe cache, streaming loaders) | Engineering effort |
| Fallback to API provider during scale-up | Requires multi-provider gateway |
| Predictive/scheduled scaling | Based on daily traffic patterns |

---

## Part 5: Managed AI Services on Major Clouds

### Service Map

| Capability | AWS | Azure | Google Cloud |
|---|---|---|---|
| **Foundation model APIs** | Amazon Bedrock (Claude, Llama, Mistral, Titan, Nova) | Azure OpenAI / Azure AI Foundry (GPT, plus catalog models) | Vertex AI (Gemini, Claude, Llama via Model Garden) |
| **Custom model training / hosting** | SageMaker | Azure Machine Learning | Vertex AI Training / Endpoints |
| **Managed RAG / knowledge** | Bedrock Knowledge Bases | Azure AI Search + "On Your Data" | Vertex AI Search, RAG Engine |
| **Agents** | Bedrock Agents / AgentCore | Azure AI Agent Service | Vertex AI Agent Builder |
| **Vector search** | OpenSearch Serverless, Aurora/RDS pgvector | Azure AI Search, Cosmos DB vector, PostgreSQL pgvector | Vertex Vector Search, AlloyDB/Cloud SQL pgvector |
| **Guardrails / safety** | Bedrock Guardrails | Azure AI Content Safety | Vertex safety filters, Model Armor |
| **Managed Kubernetes (GPU)** | EKS | AKS | GKE |
| **Serverless containers** | ECS Fargate / App Runner | Container Apps (incl. serverless GPU) | Cloud Run (incl. GPU) |
| **Document AI** | Textract | Document Intelligence | Document AI |
| **Speech** | Transcribe / Polly | AI Speech | Speech-to-Text / Text-to-Speech |

### Managed vs Self-Managed Decision

| Choose Managed Services When | Choose Self-Managed (K8s + open models) When |
|---|---|
| Speed to market matters most | Need specific open models or custom fine-tunes at scale |
| Team lacks GPU/ML infra expertise | Strict data sovereignty or air-gapped requirements |
| Workloads are variable or moderate | High sustained volume makes GPUs cheaper than tokens |
| Enterprise compliance features (private networking, data residency) are provided | Need custom inference optimizations or multi-cloud portability |

### Example: Calling Bedrock and Azure OpenAI

```python
# AWS Bedrock (Converse API - unified across Bedrock models)
import boto3

bedrock = boto3.client("bedrock-runtime", region_name="us-east-1")
resp = bedrock.converse(
    modelId="anthropic.claude-3-5-sonnet-20240620-v1:0",
    messages=[{"role": "user", "content": [{"text": "Summarize the benefits of IaC in 2 bullets."}]}],
    inferenceConfig={"maxTokens": 200, "temperature": 0},
)
print(resp["output"]["message"]["content"][0]["text"])
```

```python
# Azure OpenAI with Microsoft Entra ID (no API keys in code)
from openai import AzureOpenAI
from azure.identity import DefaultAzureCredential, get_bearer_token_provider

token_provider = get_bearer_token_provider(DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default")
client = AzureOpenAI(
    azure_endpoint="https://my-company-openai.openai.azure.com",
    azure_ad_token_provider=token_provider,
    api_version="2024-10-21",
)
r = client.chat.completions.create(model="gpt-4o-prod-deployment", messages=[{"role": "user", "content": "Hello"}])
print(r.choices[0].message.content)
```

**Note:** in Azure OpenAI, `model` refers to your **deployment name**. Using managed identity instead of keys is a security best practice on every cloud (IAM roles on AWS, Workload Identity on GCP).

---

## Part 6: Event-Driven AI Pipelines

Many AI workloads are asynchronous: document ingestion, batch summarization, transcription, evaluation runs. Event-driven architectures decouple producers from AI workers.

```text
1. User uploads a document to object storage (S3 / Blob / GCS)
2. Storage event -> message queue (SQS / Service Bus / Pub/Sub / Kafka)
3. Autoscaled workers consume messages: parse, OCR, chunk, embed
4. Results written to vector DB + metadata DB; status updated
5. Completion event -> notification service -> user sees "Document ready"
6. Failures -> retries with backoff -> dead-letter queue -> alert
```

### Worker Pattern with Idempotency

```python
import json
import hashlib
import boto3

sqs = boto3.client("sqs")
s3 = boto3.client("s3")
QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/123456789012/doc-ingestion"

def process_message(body: dict):
    bucket, key = body["bucket"], body["key"]
    obj = s3.get_object(Bucket=bucket, Key=key)
    data = obj["Body"].read()
    content_hash = hashlib.sha256(data).hexdigest()

    if already_indexed(key, content_hash):          # idempotency: skip duplicates / redeliveries
        return
    text = parse_document(data, key)                 # PDF/HTML/Office parsing, OCR if needed
    chunks = chunk_text(text)
    vectors = embed_batch([c["text"] for c in chunks])
    replace_document_chunks(doc_id=key, chunks=chunks, vectors=vectors, content_hash=content_hash)

def run_worker():
    while True:
        resp = sqs.receive_message(QueueUrl=QUEUE_URL, MaxNumberOfMessages=5, WaitTimeSeconds=20, VisibilityTimeout=900)
        for msg in resp.get("Messages", []):
            try:
                process_message(json.loads(msg["Body"]))
                sqs.delete_message(QueueUrl=QUEUE_URL, ReceiptHandle=msg["ReceiptHandle"])
            except Exception as err:
                print(f"Failed {msg['MessageId']}: {err}")  # message becomes visible again; DLQ after max receives
```

**Principles:** at-least-once delivery means handlers must be **idempotent**; visibility timeouts must exceed processing time; dead-letter queues catch poison messages; workers scale on queue depth.

---

## Part 7: Infrastructure as Code

Every AI environment (dev, staging, prod, per region) should be reproducible from code.

### Terraform: EKS GPU Node Group (AWS)

```hcl
module "eks" {
  source          = "terraform-aws-modules/eks/aws"
  version         = "~> 20.0"
  cluster_name    = "ai-platform-prod"
  cluster_version = "1.30"
  vpc_id          = module.vpc.vpc_id
  subnet_ids      = module.vpc.private_subnets

  eks_managed_node_groups = {
    general = {
      instance_types = ["m6i.xlarge"]
      min_size       = 2
      max_size       = 10
      desired_size   = 3
    }
    gpu_l4 = {
      ami_type       = "AL2_x86_64_GPU"
      instance_types = ["g6.2xlarge"]          # NVIDIA L4
      min_size       = 0
      max_size       = 8
      desired_size   = 1
      labels         = { "gpu-type" = "l4" }
      taints = [{
        key    = "nvidia.com/gpu"
        value  = "true"
        effect = "NO_SCHEDULE"
      }]
    }
  }
}
```

### Terraform: Azure OpenAI with Private Networking

```hcl
resource "azurerm_cognitive_account" "openai" {
  name                          = "company-openai-prod"
  location                      = "eastus2"
  resource_group_name           = azurerm_resource_group.ai.name
  kind                          = "OpenAI"
  sku_name                      = "S0"
  public_network_access_enabled = false
  custom_subdomain_name         = "company-openai-prod"
  identity { type = "SystemAssigned" }
}

resource "azurerm_cognitive_deployment" "gpt4o" {
  name                 = "gpt-4o-prod-deployment"
  cognitive_account_id = azurerm_cognitive_account.openai.id
  model {
    format  = "OpenAI"
    name    = "gpt-4o"
    version = "2024-08-06"
  }
  sku {
    name     = "Standard"
    capacity = 100      # thousands of tokens per minute
  }
}

resource "azurerm_private_endpoint" "openai" {
  name                = "pe-openai-prod"
  location            = azurerm_resource_group.ai.location
  resource_group_name = azurerm_resource_group.ai.name
  subnet_id           = azurerm_subnet.private_endpoints.id
  private_service_connection {
    name                           = "openai-connection"
    private_connection_resource_id = azurerm_cognitive_account.openai.id
    subresource_names              = ["account"]
    is_manual_connection           = false
  }
}
```

Pinning the model **version** in IaC makes model upgrades explicit, reviewable changes.

---

## Part 8: CI/CD for AI Applications

### Pipeline Stages

```text
1. Code quality: lint, type check, unit tests (deterministic components)
2. Security: dependency scanning, container image scanning, secret scanning, IaC scanning
3. Build: container images tagged with git SHA, signed, pushed to registry
4. LLM evaluation gate: offline eval suite + security/red-team suite vs baseline (Module 13)
5. Deploy to staging: IaC apply, Helm/Kustomize/GitOps sync, smoke tests
6. Load test (for inference changes): TTFT/TPOT SLOs
7. Production: canary via feature flags or progressive delivery (Argo Rollouts / Flagger), automated rollback on SLO breach
```

### GitHub Actions Example

```yaml
name: ai-service-ci
on:
  pull_request:
  push:
    branches: [main]

jobs:
  test-and-evaluate:
    runs-on: ubuntu-latest
    permissions: { id-token: write, contents: read }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with: { python-version: "3.12" }
      - run: pip install -r requirements.txt -r requirements-dev.txt
      - run: ruff check . && mypy src
      - run: pytest -m "not llm_eval" --maxfail=1
      - name: LLM evaluation gate
        if: contains(github.event.pull_request.labels.*.name, 'prompt-change') || github.ref == 'refs/heads/main'
        env:
          OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY_EVAL }}
        run: pytest -m llm_eval --junitxml=eval-results.xml

  build-and-push:
    needs: test-and-evaluate
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    permissions: { id-token: write, contents: read }
    steps:
      - uses: actions/checkout@v4
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::123456789012:role/ci-ecr-push
          aws-region: us-east-1
      - uses: aws-actions/amazon-ecr-login@v2
      - run: |
          docker build -t $ECR_REPO:${{ github.sha }} .
          docker push $ECR_REPO:${{ github.sha }}
        env:
          ECR_REPO: 123456789012.dkr.ecr.us-east-1.amazonaws.com/rag-api
```

**Notes:** CI authenticates to the cloud with **OIDC federation** (no long-lived cloud keys); a separate, budget-limited API key is used for evaluations; GitOps (Argo CD / Flux) then deploys the new image tag.

---

## Part 9: Security and Networking for AI Platforms

| Control | Implementation |
|---|---|
| **Private connectivity** | Private endpoints / PrivateLink to model APIs, vector DBs, storage; no public exposure |
| **Identity** | Workload identity (IRSA/EKS Pod Identity, Azure Workload Identity, GKE Workload Identity); no static keys |
| **Secrets** | Cloud secret managers + CSI driver / External Secrets Operator; rotate regularly |
| **Network policies** | Restrict pod-to-pod traffic; only the gateway talks to inference services |
| **Egress control** | Allowlist outbound domains (prevents data exfiltration by compromised agents/tools) |
| **Data protection** | Encryption at rest (KMS/CMK) and in transit (TLS/mTLS); data residency per region |
| **Supply chain** | Scan images, sign with Sigstore/cosign, verify model provenance, pin model hashes |
| **Policy as code** | OPA Gatekeeper / Kyverno: no privileged pods, required labels, approved registries only |
| **Tenant isolation** | Namespaces, separate indexes/partitions, per-tenant encryption keys for high-sensitivity tenants |
| **Audit** | Cloud audit logs + AI gateway logs retained per compliance requirements |

---

## Part 10: Reference Architecture - Enterprise GenAI Platform

```text
USERS AND APPS
  Web/mobile apps, Teams/Slack bots, IDE plugins, internal tools
  -> Identity provider (Entra ID / Okta) for SSO and user tokens

EDGE
  CDN + WAF -> API Gateway (auth, rate limiting, request validation)

APPLICATION TIER (Kubernetes, CPU node pools, multi-AZ)
  - Chat/RAG API services (FastAPI / .NET) - stateless, HPA on RPS/latency
  - Agent orchestration (LangGraph) with Postgres checkpointer
  - AI Gateway (LiteLLM or cloud gateway): routing, quotas, caching, fallback, cost attribution, PII redaction

MODEL TIER
  - Managed APIs via private endpoints (Azure OpenAI / Bedrock / Vertex) with pinned versions
  - Self-hosted vLLM on GPU node pools (KServe/Deployments), KEDA autoscaling on queue/KV metrics
  - Embedding + reranker services (GPU/CPU)

DATA TIER
  - Vector DB (managed or pgvector) with tenant partitions
  - Postgres (conversations, metadata, checkpoints), Redis (cache, rate limits, semantic cache)
  - Object storage (documents, model weights, eval datasets) with lifecycle policies

PIPELINES
  - Event-driven ingestion: storage events -> queue -> autoscaled workers (parse, OCR, chunk, embed)
  - Batch jobs: evaluations, re-embedding, fine-tuning (Kueue/Ray on GPU pools)

OPERATIONS
  - IaC (Terraform) + GitOps (Argo CD), CI with evaluation gates
  - Observability: OpenTelemetry -> traces/metrics/logs, LLM tracing (Langfuse/LangSmith), DCGM GPU metrics
  - Cost: per-team/tenant dashboards, budgets, alerts; GPU utilization reports
  - Security: private networking, workload identity, secret manager, policy as code, audit logs
  - Multi-region: active-passive or active-active for critical assistants; regional data residency
```

### Architecture Decisions to Document (ADRs)

- Which model providers and regions, and why (quality, cost, residency)
- Managed vector DB vs pgvector
- When to self-host vs use APIs
- Tenant isolation model
- RTO/RPO targets and multi-region strategy
- Data retention for prompts, responses, and traces

---

## Summary

| Area | Key Practices |
|---|---|
| Containers | Multi-stage, non-root, no secrets, weights outside images |
| Kubernetes | GPU operator, taints/tolerations, startup probes, model caches, KServe/KubeRay/Kueue |
| Autoscaling | Scale on queue/KV-cache/latency metrics with KEDA; manage cold starts |
| Managed services | Bedrock, Azure OpenAI/AI Foundry, Vertex AI - pick by requirements |
| Event-driven | Queues, idempotent workers, DLQs, scale-to-zero |
| IaC and CI/CD | Terraform, GitOps, OIDC, evaluation gates, progressive delivery |
| Security | Private endpoints, workload identity, egress control, policy as code, supply chain |

---

## Interview Questions

- **[L1]** Why are containers and Kubernetes commonly used for AI workloads?
- **[L1]** How does Kubernetes schedule GPU workloads?
- **[L2]** Why is CPU-based autoscaling ineffective for LLM inference, and what metrics should you use instead?
- **[L2]** How do you handle large model weights and cold starts in containerized inference?
- **[L2]** Compare Amazon Bedrock, Azure OpenAI/AI Foundry, and Vertex AI. What factors drive the choice?
- **[L3]** Design a cloud-native document ingestion pipeline for RAG that processes millions of documents reliably.
- **[L3]** Design a secure, multi-tenant enterprise GenAI platform on Kubernetes. Cover networking, identity, isolation, and operations.
- **[L3]** How would you build a CI/CD pipeline for an AI application that includes prompt changes, model upgrades, and infrastructure changes?

## Interview Answers

1. Containers package AI services with their exact dependencies (Python libraries, CUDA versions, inference engines), making them reproducible across laptops, CI, and every environment. Kubernetes adds declarative deployment, self-healing, autoscaling, rolling updates, service discovery, and efficient bin-packing of expensive resources including GPUs across many teams, plus a rich ecosystem for AI (GPU operator, KServe, KubeRay, Kueue, KEDA). Together they provide portability across clouds and on-prem and a consistent operational model for APIs, inference, batch pipelines, and training jobs.
2. GPU nodes run the NVIDIA device plugin, typically installed via the NVIDIA GPU Operator along with drivers and monitoring, which advertises GPUs as the extended resource `nvidia.com/gpu`. Pods request whole GPUs in their resource limits, and the scheduler places them only on nodes with available GPUs. GPU node pools are usually tainted so only pods with matching tolerations run there, with node selectors or affinity choosing specific GPU types. GPUs can be partitioned with MIG or shared via time-slicing for small models, and batch queueing systems like Kueue manage fair-share allocation across teams.
3. LLM inference is bound by GPU compute and memory bandwidth, so CPU utilization stays low even when the GPU is saturated and requests are queuing; scaling on CPU therefore reacts late or never. Better signals are inference-aware: number of waiting/queued requests, KV cache utilization, running sequences per replica, TTFT or end-to-end latency percentiles, and for asynchronous workers, queue depth. Tools like KEDA or custom metrics adapters with Prometheus scale on these, with slow scale-down to avoid thrashing because GPU pods are expensive and slow to start.
4. Keep weights out of container images and store them in object storage or a model registry, then load via a shared persistent volume cache, node-local NVMe caches, init containers, or streaming loaders. Use startup probes with generous thresholds so pods are not killed while loading. Mitigate cold starts by keeping a minimum of warm replicas, maintaining warm GPU node capacity, pre-pulling images, using quantized or smaller models where acceptable, predictive or scheduled scaling based on traffic patterns, and routing overflow to a managed API provider through the gateway while new replicas start.
5. All three provide managed access to foundation models with enterprise security, private networking, and tooling for RAG, agents, guardrails, and fine-tuning. Bedrock offers a broad catalog of third-party models (Anthropic Claude, Meta Llama, Mistral, Amazon Nova) with a unified API and strong AWS integration. Azure OpenAI/AI Foundry provides OpenAI GPT and reasoning models with Microsoft Entra ID integration, Microsoft 365 and enterprise compliance, plus a model catalog. Vertex AI provides Gemini (long context, multimodal) plus Model Garden models and strong data/analytics integration with BigQuery. Selection depends on required models and quality, existing cloud footprint and identity, data residency and compliance, regional availability and quotas, pricing and provisioned throughput, and ecosystem integration.
6. Documents arrive via uploads or connectors into object storage, whose events are published to a durable queue (SQS, Service Bus, Pub/Sub, or Kafka). Autoscaled stateless workers (KEDA on queue depth, scale-to-zero) fetch each document, compute a content hash for idempotency, parse with layout-aware parsers and OCR, chunk, embed in batches through a rate-limited embedding service, delete old chunks, and upsert new chunks with ACL metadata into the vector database, recording status in a metadata database. Visibility timeouts exceed processing time, retries use backoff, poison messages go to a dead-letter queue with alerts, and large files are split into page-level sub-tasks. The pipeline tracks throughput, lag, and failure metrics, applies per-tenant fairness and quotas, and supports reprocessing or re-embedding via replayable jobs. Everything is provisioned with IaC.
7. Networking: private clusters, private endpoints to model APIs, vector databases, and storage, WAF and API gateway at the edge, network policies restricting east-west traffic, and egress allowlists. Identity: SSO via the corporate identity provider, user tokens propagated to services for authorization, workload identity for pods instead of static keys, and secrets from a cloud secret manager. Isolation: namespaces per team or environment, tenant-scoped data partitions or dedicated indexes, mandatory tenant filters enforced server-side, per-tenant quotas at the AI gateway, and optionally per-tenant encryption keys or dedicated clusters for regulated tenants. Operations: Terraform and GitOps, policy as code (Kyverno/OPA), image signing and scanning, OpenTelemetry tracing with LLM observability, cost attribution per tenant, GPU node pools with autoscaling and Kueue for batch fairness, multi-AZ deployments, backups, runbooks, and audit logging.
8. Keep application code, prompts, model configuration (pinned versions), evaluation datasets, and infrastructure in version control. Pull requests trigger lint, type checks, unit tests, dependency/image/secret/IaC scanning, and container builds. Changes touching prompts, model config, or retrieval trigger the LLM evaluation gate (quality, per-slice, and security suites compared against the production baseline, plus cost and latency checks). Infrastructure changes run `terraform plan` with policy checks and require approval. After merge, images are signed and pushed, and GitOps deploys to staging for smoke tests and, for inference changes, load tests against TTFT/TPOT SLOs. Production rollout uses progressive delivery (canary or feature flags) with automated rollback on SLO or quality metric breaches. Model upgrades follow the same path: update the pinned version in config, evaluate, canary, and promote.
