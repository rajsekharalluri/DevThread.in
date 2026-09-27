---
id: comparisons-kafka-rabbitmq
slug: kafka-rabbitmq
title: Kafka vs RabbitMQ vs Queue Messaging
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 35
version:
  minimum: "Messaging concepts"
prerequisites: [architecture-kafka, architecture-messaging-patterns]
tags: [kafka, rabbitmq, messaging, queues, comparison]
relatedTopics: [architecture-event-driven, architecture-outbox-inbox]
order: 40
status: published
---
# Kafka vs RabbitMQ vs Queue Messaging

## Decision table

| Concern | Kafka | RabbitMQ/queue | Simple managed queue |
|---|---|---|---|
| Model | Durable partitioned log | Brokered messages/routing | Work delivery |
| Multiple consumer groups | Strong fit | Pub/sub possible | Usually explicit subscriptions |
| Replay | Native offset replay | Retention/replay patterns vary | Usually limited |
| Ordering | Per partition | Queue/order constraints | Queue/partition scope |
| Best fit | Event streams/high throughput | Routing/workflows | Background work |
| Main cost | Partition/lag operations | Broker/routing operations | Provider limits/features |

## Example

```text
OrderPaid event → Kafka topic → billing group
                              → analytics group
                              → search group

SendEmail command → queue → one worker consumes
```

Choose Kafka when durable replayable streams/multiple independent groups matter. Choose a queue when one worker should process a unit of work with acknowledgement/retry semantics.

## Interview Questions
- **[L1]** What is the main difference between a queue and an event stream?
- **[L1]** What is a Kafka consumer group?
- **[L2]** When is RabbitMQ/queue messaging a better fit than Kafka?
- **[L2]** What ordering does Kafka provide?
- **[L3]** How would you choose a messaging platform for order events?
- **[L3]** What operational metrics matter for either platform?

## Interview Answers
1. A queue distributes work; an event stream retains history for independent consumers/replay.
2. Consumers in a group share partitions/work; different groups independently receive the stream.
3. For task routing, acknowledgements, delayed retry, and one-work-owner semantics without replay/high-throughput stream needs.
4. Order within a partition, not global order; key related events consistently for per-entity order.
5. Use requirements: consumers/replay/throughput/order/latency/operations. Avoid choosing from brand familiarity.
6. Queue/topic depth, oldest age, consumer lag, throughput, retry/DLQ rate, publish/consume latency, broker health, disk, and error rate.
