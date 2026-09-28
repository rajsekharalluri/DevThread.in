---
id: genai-langchain-agents
slug: langchain-agents
title: LangChain and LLM Agent Architectures
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "LangChain current concepts"
prerequisites: [genai-rag-fundamentals, genai-advanced-rag]
tags: [genai, langchain, agents, tools, orchestration]
relatedTopics: [genai-mcp, genai-rag-fundamentals]
order: 70
status: published
---
# LangChain and LLM Agent Architectures

## Introduction
A chain is a fixed sequence of model/retrieval/tool steps. An agent is a model-driven loop that chooses tools and next actions based on the current goal and tool results.

```text
User goal
LLM chooses tool + arguments
Tool executes safely
Tool result returns to LLM
LLM chooses next step or final answer
```

## Purpose
Agents help with open-ended multi-step work where the path cannot be fully predetermined. LangChain provides model, prompt, tool, retriever, memory, and orchestration abstractions, but it does not make an agent reliable automatically.

## Simple example
```python
@tool
def check_order_status(order_id: str) -> str:
    """Return the current status of an order."""
    order = order_service.find(order_id)
    return order.status if order else "not found"

agent = create_agent(
    model="gpt-4o",
    tools=[check_order_status]
)
```

The model sees the tool name/schema/description, requests a call, the framework executes it, and the result is added to the next model context.

## Professional company-level example
Never expose a destructive tool without server-side guardrails:

```python
@tool
def issue_refund(order_id: str, amount: float) -> str:
    """Issue a refund up to the automatic approval limit."""
    if amount > 100:
        return "Escalated for human approval"
    if not order_service.can_refund(order_id, amount):
        return "Refund rejected by business policy"
    return payment_service.refund(order_id, amount)
```

The tool enforces the rule even if the model is manipulated into asking for a larger refund. Add a maximum iteration/tool-call budget, timeouts, audit logs, idempotency keys, and human approval for high-impact actions.

## Chain versus agent

| Choice | Behavior | Use |
|---|---|---|
| Fixed chain | Developer controls every step | Predictable RAG/workflow |
| Agent | Model chooses next tool/action | Open-ended research/support |
| Workflow graph | Explicit branches/state | Complex but governed process |
| Human-in-loop | Human approves action | Financial/destructive decisions |

Prefer a chain or explicit workflow when the process is known. An agent adds latency, cost, nondeterminism, and test complexity for flexibility.

## Failure scenarios
- Ambiguous tools cause incorrect selection.
- The model loops until budget is exhausted.
- Prompt injection persuades a tool call.
- Tool side effect repeats after a timeout.
- Tool result contains sensitive data the model should not see.
- No trace exists for why an action was taken.

## Interview Questions
- **[L1]** What is the difference between a chain and an agent?
- **[L1]** What is a tool in an LLM application?
- **[L2]** Why are agents more expensive and slower than fixed workflows?
- **[L2]** Why must guardrails live inside tool implementations?
- **[L3]** How would you safely expose a refund or deletion tool to an agent?
- **[L3]** When should a team avoid an agent and use a deterministic workflow?

## Interview Answers
1. A chain follows a developer-defined sequence; an agent lets the model choose actions/next steps dynamically.
2. Each planning/tool/result step may require another model call, plus tool/network latency and larger context.
3. Prompt instructions are not hard security constraints. The tool implementation is the final enforcement point.
4. Validate authorization/business limits, require confirmation/human approval, use idempotency, audit every call, bound iterations, and make the tool's blast radius narrow.
5. Use an agent for genuinely open-ended multi-step reasoning; use a deterministic workflow when steps, rules, and outcomes are known because it is cheaper, testable, and predictable.
6. Avoid agents for fixed CRUD, simple RAG, or safety-critical workflows where a state machine/workflow engine can express decisions explicitly.

## Expert perspective
Agents are automated decision-makers with tool permissions. Treat their tools like privileged APIs: least privilege, validation, idempotency, auditability, bounded cost, and human approval are architecture requirements, not prompt-writing preferences.
