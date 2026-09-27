---
id: genai-mcp
slug: mcp
title: Model Context Protocol (MCP)
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "MCP current concepts"
prerequisites: [genai-langchain-agents]
tags: [genai, mcp, tools, protocol, interoperability, security]
relatedTopics: [genai-langchain-agents, architecture-api-gateway-communication]
order: 80
status: published
---
# Model Context Protocol (MCP)

## Introduction
MCP is a standard protocol for connecting AI clients to external tools, resources, and reusable prompts. An MCP server exposes capabilities; an MCP client discovers and invokes them.

```text
AI client/agent
      │ MCP protocol
      ▼
MCP server
   ├── tools: actions/functions
   ├── resources: readable data
   └── prompts: reusable templates
      │
      ▼
Database/API/filesystem/business system
```

## Purpose
Without a shared protocol, every AI application must build a custom integration for every tool/data source. A standard protocol allows multiple clients to reuse one server and multiple servers to work with compatible clients.

## Simple example
```python
@server.list_tools()
async def list_tools():
    return [Tool(
        name="check_order_status",
        description="Look up an order status by ID",
        inputSchema={
            "type": "object",
            "properties": {"order_id": {"type": "string"}},
            "required": ["order_id"]
        }
    )]
```

The client discovers the tool schema and presents it to the model. A tool call is then sent back to the MCP server using the protocol.

## Professional company-level example
A server should enforce authorization and safety independently:

```text
MCP tool call
   ↓ validate schema
   ↓ authenticate client/session
   ↓ authorize operation/resource
   ↓ enforce business limits
   ↓ audit request
   ↓ execute idempotently
   ↓ return safe result
```

For destructive actions, require explicit confirmation or human approval inside the server. Do not rely on the AI client's prompt to enforce a deletion/refund/security rule.

## Transport and operations
MCP servers may run locally through process I/O or remotely through a network transport. Remote servers need normal API controls: TLS, authentication, authorization, rate limits, input validation, timeouts, audit logs, and version compatibility.

Tools should have precise descriptions and schemas. Ambiguous descriptions lead models to choose the wrong tool or send malformed arguments. The server must still reject malformed/unauthorized requests rather than trusting the model.

## Comparison
| Approach | Strength | Cost |
|---|---|---|
| Direct function call | Simple/fast in one app | Not reusable across clients |
| REST API | Familiar external contract | Each AI client writes adapters |
| MCP | Standard AI tool/resource discovery | Protocol/security/operations |
| Plugin-specific integration | Tailored control | M×N maintenance growth |

## Interview Questions
- **[L1]** What problem does MCP solve?
- **[L1]** What can an MCP server expose?
- **[L2]** How does MCP reduce integration work?
- **[L2]** Why must an MCP server validate and authorize every call itself?
- **[L3]** How would you secure an MCP server with destructive tools?
- **[L3]** When is a direct API/function integration better than MCP?

## Interview Answers
1. It standardizes how AI clients discover and use external tools, data resources, and prompts.
2. Tools/actions, readable resources, and reusable prompts are the main capability types.
3. Each client and server implements the protocol once; any compatible client can use any compatible server rather than requiring custom pairwise adapters.
4. The server cannot trust what prompt/reasoning produced the call. A client may be compromised, manipulated, or misconfigured, so the server is its own security boundary.
5. Use authenticated client identity, least-privilege authorization, schema validation, confirmation/human approval, idempotency, rate limits, audit logs, bounded blast radius, and safe error output.
6. Use a direct integration when one application owns both sides and the capability will not be reused; MCP adds protocol and operational overhead in exchange for interoperability.

## Expert perspective
MCP standardizes connectivity, not trust. Senior engineers treat every MCP server as an API exposed to automated decision-makers and apply normal defensive API engineering plus explicit controls for model-driven tool use.
