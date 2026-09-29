---
id: genai-eng-learning-path
slug: learning-path
title: "GenAI Engineer Learning Path"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: beginner
estimatedMinutes: 20
version:
  minimum: "Framework agnostic"
prerequisites: []
tags: [genai-engineer, learning-path, curriculum, roadmap, career]
relatedTopics: [genai-eng-programming-ai-foundations, genai-eng-rag, genai-eng-ai-agents, genai-eng-ai-projects]
order: 0
status: published
---
# GenAI Engineer Learning Path

## Introduction

A GenAI engineer builds reliable, secure, and cost-effective software on top of large language models and other generative models. The role sits between software engineering and machine learning: you rarely train foundation models from scratch, but you must understand how they work, connect them to data and tools, evaluate their behavior, and operate them in production.

This path contains 16 modules grouped into 6 phases. Each module includes explanations, runnable-style code examples, comparison tables, and 8 leveled interview questions with answers. Work through the phases in order; each phase ends with a portfolio milestone so you build proof of skill as you learn.

---

## Who This Path Is For

| Background | Where to Start |
|---|---|
| Software engineer new to AI | Module 1, then follow the full sequence |
| Python developer who has called LLM APIs | Skim Modules 1-2, start properly at Module 3 |
| Data scientist / ML engineer | Skim Modules 1-2 and 11, focus on Modules 4-10 and 13-15 |
| Backend / platform engineer | Modules 1-6, then 13-15, then agents (8-10) |
| Architect / tech lead | Modules 2, 6, 7, 9, 13, 14, 15, and the system design framework in 16 |

---

## Phase 1: Foundations (Modules 1-4)

**Goal:** understand how LLMs work and build a reliable LLM-powered API.

| Module | You Will Learn |
|---|---|
| **1. Programming & AI Foundations** | Python patterns for AI (async, generators, typing), NumPy/Pandas, ML basics, tokenization, FastAPI |
| **2. GenAI & LLM Fundamentals** | Transformers and attention, training stages (pre-training, SFT, RLHF), sampling, model families, hallucination, KV cache |
| **3. Prompt Engineering & Structured Output** | Prompt anatomy, few-shot, chain-of-thought, JSON Schema outputs, function calling, prompt testing |
| **4. LLM Application Development** | Provider APIs, streaming (SSE), memory, retries and rate limits, provider abstraction, caching, workflows |

**Milestone:** a streaming chat API with conversation memory, structured outputs, retries, and tests.

---

## Phase 2: Knowledge Systems (Modules 5-7)

**Goal:** connect LLMs to private data with measurable retrieval quality.

| Module | You Will Learn |
|---|---|
| **5. Embeddings & Vector Search** | Embedding models, similarity metrics, HNSW/IVF, vector databases, filtering, retrieval metrics |
| **6. RAG** | Parsing, chunking, indexing, grounded generation with citations, permissions, RAG evaluation |
| **7. Advanced RAG** | Hybrid search, reranking, query transformation, parent-child chunks, contextual retrieval, Graph RAG, agentic RAG |

**Milestone:** an enterprise knowledge assistant with citations, an evaluation dataset, and before/after metrics for hybrid search and reranking.

---

## Phase 3: Agents and Tools (Modules 8-10)

**Goal:** build AI systems that take actions safely.

| Module | You Will Learn |
|---|---|
| **8. AI Agents** | Agent loop, ReAct, tool design, planning, memory, guardrails, human-in-the-loop, agent evaluation |
| **9. Agentic AI & Multi-Agent Systems** | Orchestration patterns, state, LangGraph, checkpoints and interrupts, supervisor and handoff designs |
| **10. MCP & AI Tool Ecosystem** | Model Context Protocol architecture, building servers and clients, remote auth, MCP security, A2A |

**Milestone:** a support agent with authorized tools, human approval for refunds, an MCP server, and a security test suite.

---

## Phase 4: Specialization (Modules 11-12)

**Goal:** adapt models and go beyond text.

| Module | You Will Learn |
|---|---|
| **11. Fine-Tuning & Model Adaptation** | When to fine-tune, dataset preparation, LoRA/QLoRA, DPO, distillation, hosted fine-tuning, serving adapters |
| **12. Multimodal GenAI** | Vision-language models, document extraction, CLIP search, speech-to-text, voice agents, image generation, multimodal RAG |

**Milestone:** either a document intelligence pipeline with validation and human review, or a fine-tuned small model that matches a large model on a narrow task.

---

## Phase 5: Production (Modules 13-15)

**Goal:** make AI systems measurable, secure, scalable, and affordable.

| Module | You Will Learn |
|---|---|
| **13. AI Engineering** | Evaluation datasets, LLM-as-judge, CI quality gates, OWASP LLM Top 10, guardrails, red teaming, observability |
| **14. LLMOps & Production AI** | Artifact versioning, deployment strategies, vLLM serving, capacity planning, cost engineering, reliability |
| **15. Cloud-Native AI** | Docker, GPUs on Kubernetes, autoscaling, managed AI services, event-driven pipelines, IaC, CI/CD, platform security |

**Milestone:** add evaluation gates, tracing, cost dashboards, Docker/Kubernetes deployment, and IaC to your earlier projects.

---

## Phase 6: Portfolio and Career (Module 16)

**Goal:** prove your skills and perform well in interviews.

| Module | You Will Learn |
|---|---|
| **16. AI Projects & Portfolio** | Six end-to-end projects, README and demo guidance, STAR answers with metrics, GenAI system design framework |

**Milestone:** 2-3 polished, evaluated, deployed projects with write-ups and demo videos.

---

## Suggested Schedule

| Pace | Duration | Weekly Commitment |
|---|---|---|
| Intensive | 8-10 weeks | 15-20 hours |
| Steady | 16-20 weeks | 6-8 hours |
| Part-time | 6 months | 3-4 hours |

Spend at least as much time building as reading. Every module's code examples are meant to be run, modified, and broken.

---

## Core Skills Map

| Skill Area | Modules |
|---|---|
| Python and APIs | 1, 4 |
| LLM internals | 2, 11 |
| Prompting and structured outputs | 3 |
| Retrieval and search | 5, 6, 7 |
| Agents and orchestration | 8, 9, 10 |
| Multimodal | 12 |
| Evaluation and security | 13 |
| Operations and cost | 14 |
| Cloud and infrastructure | 15 |
| System design and communication | 16 |

---

## How to Use Each Module

1. Read the introduction and summary table first to see the big picture
2. Work through each part and run the code examples
3. Answer the interview questions yourself **before** reading the answers
4. Build the phase milestone and record metrics
5. Revisit L3 questions after completing later phases - your answers will improve

---

## Interview Questions

- **[L1]** What does a GenAI engineer do, and how is the role different from a machine learning engineer?
- **[L1]** What are the core skill areas a GenAI engineer needs?
- **[L2]** Why is RAG usually learned before agents and fine-tuning?
- **[L2]** What makes a GenAI portfolio project convincing to hiring managers?
- **[L3]** How would you plan a GenAI upskilling program for a team of 20 backend engineers?
- **[L3]** Which production concerns most often separate a GenAI prototype from a production system?

## Interview Answers

1. A GenAI engineer builds applications and platforms on top of foundation models: designing prompts and structured outputs, connecting models to data through RAG, building agents and tool integrations, evaluating quality, securing systems against prompt injection and data leakage, and operating them with acceptable cost and latency. A machine learning engineer traditionally focuses more on training, tuning, and deploying custom models from data, including feature pipelines and model training infrastructure. The roles overlap in evaluation, fine-tuning, and serving, but GenAI engineers spend most of their time on application architecture around pre-trained models.
2. Strong software engineering in Python and APIs; understanding of how LLMs work (tokens, attention, sampling, limitations); prompt engineering and structured outputs; embeddings, vector search, and RAG; agents, tool calling, and protocols like MCP; model adaptation through fine-tuning; multimodal inputs; evaluation methodology; AI security and guardrails; observability, LLMOps, and cost engineering; and cloud-native deployment. Communication and system design skills are needed to make and explain trade-offs.
3. RAG is the most widely deployed GenAI pattern and teaches the fundamentals that agents and fine-tuning depend on: embeddings, retrieval quality, grounding, context management, and evaluation. Agents frequently use retrieval as a tool, so weak retrieval makes agents fail. Fine-tuning is more expensive and is justified only after prompting and RAG have been tried; understanding RAG first helps engineers recognize that most knowledge problems should not be solved with fine-tuning.
4. It solves a clear problem for a defined user, has a clean, tested codebase, includes an evaluation dataset with reported metrics and before/after comparisons of design choices, handles security (prompt injection, permissions, PII), includes observability and cost tracking, runs with one command and is deployed or demonstrated in a short video, and documents architecture, decisions, trade-offs, and limitations. Concrete numbers and honest discussion of failure modes are the most persuasive elements.
5. Assess current skills and pick one or two real business use cases the team will build during the program. Run a phased curriculum: foundations and LLM APIs (weeks 1-2), RAG with evaluation (weeks 3-5), agents and tool integration with security (weeks 6-8), and production practices including evaluation gates, observability, and cost (weeks 9-10). Pair learning with hands-on labs on the chosen use cases, internal code reviews, shared evaluation and prompt tooling, and demos at the end of each phase. Establish standards (approved providers, data policies, security checklist) and measure outcomes by projects shipped, evaluation metrics achieved, and team confidence.
6. The most common gaps are: no systematic evaluation (only manual spot checks), weak retrieval quality and missing permission filtering, no defenses against prompt injection or unsafe tool use, missing error handling, retries, timeouts, and fallbacks, no observability of prompts, retrieved context, and tool calls, uncontrolled token cost and latency, unversioned prompts and floating model versions, and no feedback loop from production failures back into evaluation datasets. Addressing these turns a demo into a dependable product.
