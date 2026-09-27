---
id: architecture-kafka
slug: kafka
title: Apache Kafka Fundamentals
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Kafka 3.x concepts"
prerequisites: [architecture-event-driven]
tags: [kafka, streaming, events, partitions, consumers]
relatedTopics: [architecture-event-driven, architecture-distributed-systems]
order: 40
status: published
---
# Apache Kafka Fundamentals

## Overview
Kafka is a distributed event streaming platform built around durable, partitioned, append-only logs that producers write to and consumers read at their own pace.

## Why This Exists
Traditional queues often remove a message after one consumer processes it. Kafka retains ordered event history for a configured period, allowing multiple consumer groups to independently process the same events, replay history, and scale consumption through partitions.

## Fundamentals
A **topic** is a named stream. A topic is divided into **partitions**, each an ordered log. A record has a key, value, offset, and timestamp. A **consumer group** divides partitions among its consumers; within a group, one partition is assigned to one consumer at a time. Offsets are consumer progress markers, not global delete pointers.

## Syntax and API
```csharp
var producerConfig = new ProducerConfig { BootstrapServers = "kafka:9092" };
using var producer = new ProducerBuilder<string, OrderPaid>(producerConfig).Build();

await producer.ProduceAsync("order-events", new Message<string, OrderPaid>
{
    Key = order.Id.ToString(),
    Value = new OrderPaid(order.Id, order.Total)
});
```
Using the order ID as the key keeps events for the same order in one partition, preserving per-order ordering.

## How It Works
Producers append records to partition leaders. Consumers poll records and commit offsets after processing. Kafka brokers replicate partitions for fault tolerance. Adding partitions increases parallelism, but partition count and key strategy affect ordering and future scaling.

## Internal Implementation
Kafka's log is immutable and sequentially written, with retention based on time or size. Consumer groups coordinate partition ownership through the group protocol. A consumer can re-read records by moving its committed offset backward, enabling replay — but replaying side effects requires idempotent consumers.

## Real-World Example
An order event topic feeds billing, notifications, analytics, and search-index consumers independently. Each group sees every relevant event, while each group scales its own consumers.

## Production Example
```text
Topic: order-events
Partitions: 12
Key: orderId
Groups:
  billing-v1          -> commits after ledger transaction
  notification-v1    -> retries transient provider errors
  analytics-v1       -> can replay from a historical offset
```
A partition increase can improve throughput but does not preserve a global order across the topic; only ordering for records sharing the same partition/key is guaranteed.

## Common Mistakes
- Assuming Kafka provides global ordering across partitions.
- Committing offsets before side effects complete.
- Using random keys when per-entity ordering is required.
- Creating too few partitions for required consumer parallelism.
- Treating Kafka as a database without retention/replay governance.

## Performance
Throughput depends on partition count, batching, compression, broker disks, producer acknowledgements, and consumer processing. Monitor consumer lag, produce latency, under-replicated partitions, and disk usage.

## Security
Use TLS, SASL/IAM or equivalent authentication, topic-level authorization, encryption at rest, and careful retention of sensitive event payloads. Events often contain more data than a public API response and need equivalent protection.

## Testing
Test serialization compatibility, duplicate delivery, rebalance behavior, consumer restart, partition skew, replay, and poison-message handling.

## When to Use
Use Kafka for durable event streams, high-throughput pipelines, replayable history, and multiple independent consumers.

## When Not to Use
Don't use Kafka as a simple request/response queue for a small application when a managed queue or in-process background job is sufficient; Kafka brings operational and conceptual overhead.

## Trade-offs
Kafka offers throughput, retention, replay, and consumer independence, but requires partition/key design, schema governance, operations, and careful side-effect handling.

## Related Topics
See [Event-Driven Architecture](/architecture/event-driven) and [Distributed Systems](/architecture/distributed-systems).

## Practical Exercise
Design an `order-events` topic with a key strategy, partition count, consumer groups, retry topic, dead-letter topic, and replay procedure. Explain what ordering is guaranteed.

## Interview Questions
- **[L1]** What is a Kafka topic partition?
- **[L1]** What is a consumer group?
- **[L2]** What ordering guarantees does Kafka provide?
- **[L2]** When should a consumer commit an offset?
- **[L3]** How would you design Kafka topics for ordered per-order processing and scalable consumers?

## Interview Answers
1. **[L1]** A partition is an ordered, append-only log within a topic. Kafka distributes partitions across brokers for storage and parallel processing.
2. **[L1]** A consumer group is a set of consumers sharing work: each partition is assigned to at most one consumer in that group. Different groups independently receive the full stream.
3. **[L2]** Kafka guarantees order within a partition, not across all partitions. Using the same key sends related records to the same partition, enabling per-key ordering.
4. **[L2]** Commit after the consumer has durably completed the side effect represented by the record. Committing earlier risks losing work; committing later causes duplicates after a crash, so the side effect must be idempotent.
5. **[L3]** Key records by order ID, choose enough partitions for expected consumer parallelism, use separate groups per business capability, and design idempotent consumers with retry/dead-letter handling. Document that ordering is per order, not global.

## Senior Developer Perspective
Kafka is a durable distributed log, not merely a faster queue. Senior engineers start with ordering scope, replay policy, consumer ownership, schema compatibility, and side-effect idempotency before choosing partition counts or tuning throughput.
