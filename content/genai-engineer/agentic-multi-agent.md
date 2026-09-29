---
id: genai-eng-agentic-multi-agent
slug: agentic-multi-agent
title: "Module 9: Agentic AI & Multi-Agent Systems"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: advanced
estimatedMinutes: 110
version:
  minimum: "Python 3.11+, LangGraph 0.2+, OpenAI SDK 1.x"
prerequisites: [genai-eng-ai-agents]
tags: [genai-engineer, agentic-ai, multi-agent, orchestration, langgraph, state-machines, supervisor, human-in-the-loop]
relatedTopics: [genai-eng-mcp-tool-ecosystem]
order: 9
status: published
---
# Module 9: Agentic AI & Multi-Agent Systems

## Introduction

A single agent with a handful of tools works well for focused tasks. As tasks grow - researching a market, writing and testing a feature, processing an insurance claim end-to-end - a single agent struggles: its prompt grows huge, it has too many tools to choose from, its context fills with irrelevant history, and a failure halfway through means starting over.

**Agentic systems** solve this with explicit **orchestration**: durable state, graph-based control flow, checkpoints, human approvals, and sometimes **multiple specialized agents** that collaborate. This module covers orchestration patterns, state management, building stateful agent graphs with LangGraph, multi-agent architectures (supervisor, hierarchical, handoffs, debate), durability and human-in-the-loop, and how to decide when multiple agents are actually worth it.

---

## Part 1: From Agents to Agentic Systems

### Why Single Agents Hit Limits

| Problem | Symptom |
|---|---|
| **Tool overload** | With 30+ tools, the model picks the wrong one more often |
| **Prompt bloat** | One giant system prompt covering every responsibility |
| **Context pollution** | Research notes, code, and logs crowd out the current task |
| **No durability** | A crash at step 18 of 20 loses all progress |
| **No oversight points** | Cannot pause for approval mid-task |
| **Hard to test** | One opaque loop instead of testable units |

### Properties of Production Agentic Systems

1. **Explicit state** - a typed object that captures everything the process knows
2. **Defined control flow** - a graph of steps with conditional routing, loops, and parallelism
3. **Persistence** - checkpoints after each step so the process can resume
4. **Human-in-the-loop** - interrupts for approval, editing, or input
5. **Observability** - traces of every node, model call, and tool call
6. **Bounded autonomy** - LLMs make decisions inside guardrails the code enforces

---

## Part 2: Orchestration Patterns

| Pattern | Description | Example |
|---|---|---|
| **Sequential pipeline** | Agents or steps run in order | Research, then write, then edit |
| **Router** | Classify and dispatch to one specialist | Support triage to billing/tech/sales agents |
| **Parallel fan-out / fan-in** | Specialists run concurrently, results merged | Security, performance, and style code reviewers |
| **Supervisor (orchestrator-worker)** | Central agent delegates sub-tasks dynamically and integrates results | Research lead assigns topics to researcher agents |
| **Hierarchical** | Supervisors of supervisors | Program manager over team leads over workers |
| **Handoff / swarm** | Agents transfer control to each other directly | Triage agent hands the conversation to a refunds agent |
| **Evaluator-optimizer loop** | Generator and critic iterate until criteria pass | Writer and reviewer on a document |
| **Debate / consensus** | Multiple agents propose and critique; a judge decides | High-stakes analysis |

### Choosing a Pattern

```text
Is the sequence of steps known in advance?
  Yes -> Sequential pipeline or workflow graph (most reliable)
  No  -> Can the task be split into independent sub-tasks?
           Yes, known count -> Parallel fan-out
           Yes, dynamic     -> Supervisor / orchestrator-worker
           No               -> Single agent with good tools (ReAct)
Does the conversation need a different specialist mid-way?
  Yes -> Handoffs
Is quality verifiable by a critic or test?
  Yes -> Add an evaluator-optimizer loop
```

---

## Part 3: State Management

### State as the Backbone

In agentic systems, the **state** is a structured object passed between steps. Each node reads what it needs and returns updates.

```python
from typing import TypedDict, Annotated, Literal
import operator

class ResearchState(TypedDict):
    question: str
    plan: list[str]
    findings: Annotated[list[dict], operator.add]   # reducer: new findings are appended
    draft: str
    review_feedback: str
    revision_count: int
    status: Literal["planning", "researching", "writing", "reviewing", "done"]
```

**Reducers** define how concurrent updates merge. `operator.add` appends lists, which is essential when parallel workers each return findings.

### State Design Principles

- Keep state **minimal and typed**; store large artifacts (documents, files) externally and keep references
- Separate **user-visible messages** from **internal working data**
- Include **counters and flags** (revision_count, approved) for loop control
- Make state **serializable** so it can be checkpointed

---

## Part 4: Building Stateful Agents with LangGraph

LangGraph models an agentic system as a **graph**: nodes are functions (LLM calls, tools, logic), edges define flow, and conditional edges route based on state. It provides checkpointing, interrupts, streaming, and parallel execution.

### A Research-Write-Review Graph

```python
# pip install langgraph langchain-openai
from typing import TypedDict, Annotated
import operator
from langgraph.graph import StateGraph, START, END
from langgraph.checkpoint.memory import MemorySaver
from langchain_openai import ChatOpenAI
from pydantic import BaseModel

llm = ChatOpenAI(model="gpt-4o-mini", temperature=0)

class State(TypedDict):
    question: str
    plan: list[str]
    findings: Annotated[list[str], operator.add]
    draft: str
    feedback: str
    revisions: int

class Plan(BaseModel):
    topics: list[str]

class Review(BaseModel):
    approved: bool
    feedback: str

def planner(state: State) -> dict:
    plan = llm.with_structured_output(Plan).invoke(
        f"Break this research question into 3 focused sub-topics:\n{state['question']}"
    )
    return {"plan": plan.topics, "revisions": 0}

def researcher(state: State) -> dict:
    notes = []
    for topic in state["plan"]:
        # In production this would call search/RAG tools; here the LLM summarizes its knowledge.
        result = llm.invoke(f"Give 3 key factual points about: {topic}. Be concise.")
        notes.append(f"## {topic}\n{result.content}")
    return {"findings": notes}

def writer(state: State) -> dict:
    prompt = (
        f"Question: {state['question']}\n\nResearch notes:\n" + "\n\n".join(state["findings"]) +
        (f"\n\nReviewer feedback to address:\n{state['feedback']}" if state.get("feedback") else "") +
        "\n\nWrite a clear 200-word answer."
    )
    return {"draft": llm.invoke(prompt).content}

def reviewer(state: State) -> dict:
    review = llm.with_structured_output(Review).invoke(
        f"Question: {state['question']}\n\nDraft:\n{state['draft']}\n\n"
        "Approve only if it directly answers the question, is accurate per the notes, and is under 250 words."
    )
    return {"feedback": "" if review.approved else review.feedback, "revisions": state["revisions"] + 1}

def route_after_review(state: State) -> str:
    if not state["feedback"] or state["revisions"] >= 3:
        return END
    return "writer"

graph = StateGraph(State)
graph.add_node("planner", planner)
graph.add_node("researcher", researcher)
graph.add_node("writer", writer)
graph.add_node("reviewer", reviewer)

graph.add_edge(START, "planner")
graph.add_edge("planner", "researcher")
graph.add_edge("researcher", "writer")
graph.add_edge("writer", "reviewer")
graph.add_conditional_edges("reviewer", route_after_review, ["writer", END])

app = graph.compile(checkpointer=MemorySaver())

config = {"configurable": {"thread_id": "research-001"}}
final_state = app.invoke({"question": "What are the trade-offs of serverless for ML inference?"}, config)
print(final_state["draft"])
print("Revisions:", final_state["revisions"])
```

**What this demonstrates:**
- **Typed state** flowing through nodes
- **Conditional edges** implementing an evaluator-optimizer loop with a hard cap (3 revisions)
- **Structured outputs** for machine-readable decisions (plan, approval)
- **Checkpointing** keyed by `thread_id` - the run can be inspected or resumed

### Parallel Fan-Out with the Send API

Run one researcher per topic concurrently, then merge with the reducer.

```python
from langgraph.types import Send

class TopicState(TypedDict):
    topic: str

def research_one(state: TopicState) -> dict:
    result = llm.invoke(f"Give 3 key factual points about: {state['topic']}")
    return {"findings": [f"## {state['topic']}\n{result.content}"]}

def fan_out(state: State) -> list[Send]:
    return [Send("research_one", {"topic": t}) for t in state["plan"]]

g = StateGraph(State)
g.add_node("planner", planner)
g.add_node("research_one", research_one)
g.add_node("writer", writer)
g.add_edge(START, "planner")
g.add_conditional_edges("planner", fan_out, ["research_one"])
g.add_edge("research_one", "writer")   # writer runs after all parallel branches complete
g.add_edge("writer", END)
parallel_app = g.compile()
```

### Prebuilt ReAct Agent Node

```python
from langgraph.prebuilt import create_react_agent
from langchain_core.tools import tool

@tool
def get_stock_level(sku: str) -> int:
    """Return the current stock level for a product SKU."""
    return {"SKU-1": 12, "SKU-2": 0}.get(sku, -1)

inventory_agent = create_react_agent(llm, tools=[get_stock_level])
result = inventory_agent.invoke({"messages": [("user", "Is SKU-2 in stock?")]})
print(result["messages"][-1].content)
```

Agents built this way can themselves be **nodes** in a larger graph - the foundation for multi-agent systems.

---

## Part 5: Human-in-the-Loop and Durability

### Interrupts for Approval

Pause the graph before a risky step, show the pending action to a human, and resume with their decision.

```python
from langgraph.types import interrupt, Command

class DeployState(TypedDict):
    change_summary: str
    approved: bool
    result: str

def propose(state: DeployState) -> dict:
    return {"change_summary": "Scale payments-service from 4 to 8 replicas in production"}

def human_approval(state: DeployState) -> dict:
    decision = interrupt({"action": "deploy", "summary": state["change_summary"]})  # pauses here
    return {"approved": decision == "approve"}

def execute(state: DeployState) -> dict:
    return {"result": "Scaled to 8 replicas" if state["approved"] else "Cancelled by reviewer"}

dg = StateGraph(DeployState)
dg.add_node("propose", propose)
dg.add_node("human_approval", human_approval)
dg.add_node("execute", execute)
dg.add_edge(START, "propose")
dg.add_edge("propose", "human_approval")
dg.add_edge("human_approval", "execute")
dg.add_edge("execute", END)
deploy_app = dg.compile(checkpointer=MemorySaver())

cfg = {"configurable": {"thread_id": "deploy-42"}}
paused = deploy_app.invoke({"change_summary": "", "approved": False, "result": ""}, cfg)
print(paused["__interrupt__"])  # show this to the approver in a UI / Slack message

# Later - possibly hours later, in a different process when using a database checkpointer:
final = deploy_app.invoke(Command(resume="approve"), cfg)
print(final["result"])
```

### Durable Execution

- Use a **persistent checkpointer** (Postgres, Redis, SQLite) instead of in-memory for production
- Each node's output is saved, so failures resume from the last completed node rather than restarting
- **Time travel**: inspect or replay from any earlier checkpoint for debugging
- For long-running business processes (days), durable workflow engines such as **Temporal** or cloud workflow services can orchestrate agent steps with retries and timers

### Idempotency

Because steps can be retried or resumed, **side-effecting nodes must be idempotent** (use idempotency keys when calling payment, ticketing, or deployment APIs).

---

## Part 6: Multi-Agent Architectures

### Why Multiple Agents?

- **Specialization**: each agent has a focused prompt, a small toolset, and possibly a different model
- **Context isolation**: sub-agents work in clean contexts and return condensed results
- **Parallelism**: independent sub-tasks run concurrently
- **Separation of duties**: a reviewer agent that did not write the code catches different errors
- **Organizational fit**: different teams own different agents

### Supervisor Pattern

A supervisor LLM decides which worker to call next, passes it a task, receives the result, and decides whether to continue or finish.

```python
from typing import Literal

class SupervisorDecision(BaseModel):
    next_agent: Literal["researcher", "coder", "FINISH"]
    task: str

class TeamState(TypedDict):
    goal: str
    history: Annotated[list[str], operator.add]
    next_agent: str
    task: str
    steps: int

WORKERS = {
    "researcher": "You research technical topics and return concise factual notes.",
    "coder": "You write clean, tested Python code for the given task. Return only code and a short explanation.",
}

def supervisor(state: TeamState) -> dict:
    decision = llm.with_structured_output(SupervisorDecision).invoke(
        f"You manage a team: {list(WORKERS)}.\nGoal: {state['goal']}\n"
        f"Work so far:\n" + "\n".join(state["history"][-6:]) +
        "\nChoose the next agent and give it a specific task, or FINISH if the goal is met."
    )
    return {"next_agent": decision.next_agent, "task": decision.task, "steps": state["steps"] + 1}

def make_worker(name: str):
    def worker(state: TeamState) -> dict:
        out = llm.invoke([("system", WORKERS[name]), ("user", state["task"])]).content
        return {"history": [f"[{name}] task: {state['task']}\nresult: {out[:1500]}"]}
    return worker

def route(state: TeamState) -> str:
    if state["next_agent"] == "FINISH" or state["steps"] >= 6:
        return END
    return state["next_agent"]

tg = StateGraph(TeamState)
tg.add_node("supervisor", supervisor)
for name in WORKERS:
    tg.add_node(name, make_worker(name))
    tg.add_edge(name, "supervisor")          # workers always report back
tg.add_edge(START, "supervisor")
tg.add_conditional_edges("supervisor", route, [*WORKERS, END])
team = tg.compile()

out = team.invoke({"goal": "Explain exponential backoff and implement it in Python", "history": [], "next_agent": "", "task": "", "steps": 0})
print("\n\n".join(out["history"]))
```

### Hierarchical Teams

For large problems, supervisors manage sub-supervisors (e.g. a "research team" graph and a "writing team" graph as nodes under a top-level supervisor). Each subgraph encapsulates its own state and agents.

### Handoffs (Swarm-Style)

Instead of a central supervisor, each agent can **transfer control** to another agent via a special tool. The receiving agent takes over the conversation.

```text
Customer: "I want to return my laptop and also upgrade my plan."
Triage agent -> transfer_to_returns_agent()
Returns agent handles the return, then -> transfer_to_sales_agent()
Sales agent handles the upgrade and completes the conversation.
```

Handoffs fit customer-facing conversations where the active specialist changes. The OpenAI Agents SDK and LangGraph both support handoff patterns.

### Debate and Consensus

Several agents independently answer or critique; a judge synthesizes. Improves reasoning on contested questions but multiplies cost.

### Communication Between Agents

| Mechanism | Description |
|---|---|
| **Shared state** | All agents read/write a common state object (LangGraph) |
| **Message passing** | Agents send structured messages to each other |
| **Blackboard** | Shared workspace (files, DB) where agents post results |
| **Agent-to-agent protocols** | Standards like **A2A (Agent2Agent)** for agents from different vendors/services to discover and call each other over HTTP |

Keep inter-agent messages **structured and concise**. Passing full transcripts between agents wastes tokens and spreads confusion.

---

## Part 7: When NOT to Use Multi-Agent Systems

Multi-agent systems are powerful but frequently over-engineered.

| Cost | Detail |
|---|---|
| **Tokens** | Multi-agent research systems can use many times more tokens than a single agent |
| **Latency** | Sequential agent hops add seconds each |
| **Error compounding** | If each agent is 90% reliable, a 5-agent chain is ~59% reliable |
| **Debugging complexity** | Failures emerge from interactions between agents |
| **Coordination failures** | Duplicate work, conflicting outputs, agents waiting on each other |

**Guidelines:**
1. Start with a **single agent** with good tools, or a **deterministic workflow**
2. Add agents only when you can name the concrete problem they solve (tool overload, context isolation, parallelism)
3. Prefer **parallel, independent** sub-tasks over tightly coupled conversations between agents
4. Measure: does the multi-agent version beat the single-agent baseline on your evaluation set enough to justify the cost?

---

## Part 8: Observability and Evaluation for Agentic Systems

### Tracing

Trace each run as a tree: run, nodes, LLM calls, tool calls - with inputs, outputs, tokens, latency, and cost at each level. Tools: LangSmith, Langfuse, Arize Phoenix, OpenTelemetry GenAI semantic conventions.

### Evaluation Levels

| Level | What to Test |
|---|---|
| **Node / unit** | Planner produces valid plans; router picks correct route on labeled examples |
| **Trajectory** | Sequence of agents/tools is reasonable (no redundant loops, correct delegation) |
| **Outcome** | Final result meets task success criteria |
| **Efficiency** | Tokens, cost, steps, latency per task |
| **Safety** | No unapproved actions, no policy violations |

### Production Controls

- Global **budgets** per run (steps, tokens, dollars, time)
- **Recursion limits** on graphs (LangGraph `recursion_limit` in config)
- **Circuit breakers** on tools and model providers
- **Dashboards** for success rate, cost per task, interrupt/approval rates, failure categories

---

## Part 9: Reference Architecture - Claims Processing System

```text
1. Intake node: parse claim email and attachments (multimodal extraction) into structured Claim state
2. Validation node (deterministic code): policy exists, coverage dates valid, required fields present
3. Router: simple claim -> fast path; complex claim -> investigation team
4. Investigation team (supervisor + workers, parallel):
   - Policy agent: retrieves policy clauses via RAG
   - Fraud agent: checks history and anomaly signals via tools
   - Damage assessment agent: analyzes photos via vision model
5. Decision agent: drafts decision with citations to policy clauses and evidence
6. Human-in-the-loop interrupt: adjuster approves, edits, or rejects (required above payout threshold)
7. Action node: issues payment or denial letter via idempotent APIs
8. Audit node: stores full trace, state snapshots, and decision rationale
```

Notice that LLM agents are used where judgment and unstructured data are involved, while validation, thresholds, payments, and audit are **deterministic code**.

---

## Summary

| Concept | Key Takeaway |
|---|---|
| Agentic systems | Explicit state, graph control flow, persistence, human oversight |
| Orchestration patterns | Pipeline, router, fan-out, supervisor, hierarchical, handoff, evaluator loop |
| State | Typed, minimal, serializable, reducers for parallel updates |
| LangGraph | Nodes, edges, conditional routing, Send for fan-out, checkpoints, interrupts |
| Multi-agent | Specialization and context isolation - at a real cost |
| Production | Budgets, recursion limits, idempotency, tracing, trajectory evaluation |

---

## Interview Questions

- **[L1]** What is the difference between a single AI agent and a multi-agent system?
- **[L1]** Name common multi-agent orchestration patterns and give a use case for each.
- **[L2]** Why is explicit state important in agentic systems, and what are reducers used for in graph frameworks like LangGraph?
- **[L2]** How do checkpoints and interrupts enable human-in-the-loop and durable execution?
- **[L2]** Compare the supervisor pattern with the handoff (swarm) pattern.
- **[L3]** When would you advise against a multi-agent architecture? How do you justify the added cost when you do use one?
- **[L3]** Design an agentic system for automated code changes: from a Jira ticket to a reviewed pull request. What agents, state, controls, and evaluation would you use?
- **[L3]** A multi-agent research system produces inconsistent results and high costs. How do you debug and optimize it?

## Interview Answers

1. A single agent is one LLM loop with one system prompt and a set of tools that handles the whole task. A multi-agent system decomposes the task across several specialized agents, each with its own instructions, tools, context, and possibly model, coordinated through an orchestration pattern such as a supervisor, handoffs, or a graph. Multi-agent systems gain specialization, context isolation, and parallelism but add coordination overhead, cost, latency, and debugging complexity.
2. **Sequential pipeline**: research, write, edit a report. **Router**: triage support requests to billing, technical, or sales specialists. **Parallel fan-out**: independent security, performance, and style reviews of a pull request merged into one report. **Supervisor/orchestrator-worker**: a lead agent dynamically assigns research sub-topics to workers and synthesizes results. **Hierarchical**: supervisors managing team subgraphs for very large projects. **Handoff**: a customer conversation transferred from triage to a refunds agent. **Evaluator-optimizer**: a writer and reviewer iterate until quality criteria pass. **Debate**: multiple agents argue positions and a judge decides on a high-stakes analysis.
3. Explicit, typed state is the single source of truth for what the process knows; it makes steps testable, allows routing decisions based on data, supports checkpointing and resumption, and avoids passing ever-growing transcripts between agents. Reducers define how updates to a state key are merged, which matters when multiple nodes, especially parallel branches, write to the same key; for example, `operator.add` appends each worker's findings to a list instead of the last writer overwriting the others.
4. A checkpointer saves the graph state after every node, keyed by a thread ID. If a process crashes or is redeployed, execution resumes from the last completed node instead of restarting, and previous states can be inspected or replayed. An interrupt pauses execution at a defined point and persists the state; the application shows the pending decision to a human, who may approve, edit state, or reject, possibly hours later, and the graph resumes with that input. Together they enable approvals for risky actions, human edits to plans or drafts, and long-running processes. Side-effecting nodes must be idempotent because they may be retried.
5. In the **supervisor** pattern, a central agent receives all results and decides which worker runs next, giving centralized control, easier global reasoning and budget enforcement, and clear accountability, but the supervisor can become a bottleneck and adds an extra LLM call per hop. In the **handoff** pattern, agents transfer control directly to one another through transfer tools, which suits conversational flows where the active specialist changes and reduces hops, but global control is weaker and there is a risk of ping-pong transfers or loops. Choose supervisor for task decomposition and synthesis; choose handoffs for customer-facing routing between specialists.
6. Advise against it when a deterministic workflow or a single agent with well-designed tools meets the requirements, when steps are tightly coupled and require shared context, when latency or cost budgets are tight, or when the team lacks tracing and evaluation maturity. Justify multi-agent designs by naming the concrete problem (tool overload, context window limits, parallelizable sub-tasks, separation of duties) and demonstrating on an evaluation set that the multi-agent version improves task success enough to outweigh increased tokens, latency, and complexity compared with a single-agent baseline, and by monitoring cost per task in production.
7. State: ticket details, repository context, plan, files changed, test results, review comments, iteration counts, and approval status. Nodes and agents: an intake node fetches the ticket and relevant code via search tools; a planner agent proposes a change plan that a human can approve for larger changes; a coder agent edits files in an isolated sandboxed workspace/branch using file and search tools; a deterministic test node runs linters, type checks, and tests; an evaluator loop feeds failures back to the coder with a max iteration cap; a reviewer agent (separate prompt, possibly different model) reviews the diff for correctness, security, and style; a PR node opens the pull request with a summary and links. Controls: sandboxed execution without production secrets, least-privilege repository tokens, no direct pushes to protected branches, required human code review, budgets, and full tracing. Evaluation: a benchmark of historical tickets with known fixes and tests, measuring the rate of passing tests, reviewer acceptance, iterations, cost, and time.
8. Collect traces for many runs of the same inputs and compare trajectories to find where variance arises: planner decomposition, supervisor routing, worker outputs, or synthesis. Common issues are vague delegation instructions causing duplicate or overlapping work, workers returning verbose outputs that bloat context, missing stopping criteria causing extra rounds, and high temperature. Fixes: give the supervisor explicit task decomposition rules and require structured, concise worker outputs with sources; set temperature 0 for control decisions; cap iterations and parallel workers; use cheaper models for workers and a stronger model for planning and synthesis; cache repeated searches; and add per-run budgets. Build an evaluation set to measure consistency and quality, and compare against a simpler single-agent baseline to confirm the multi-agent design is worth keeping.
