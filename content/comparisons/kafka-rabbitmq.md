---
id: comparisons-kafka-rabbitmq
slug: kafka-rabbitmq
title: Kafka vs RabbitMQ vs Queue Messaging
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 25
version:
  minimum: "Messaging concepts, .NET 8+"
prerequisites: [architecture-kafka, architecture-messaging-patterns]
tags: [kafka, rabbitmq, messaging, queues, comparison]
relatedTopics: [architecture-event-driven, architecture-outbox-inbox]
order: 40
status: published
---
# Kafka vs RabbitMQ vs Queue Messaging

## Introduction

"Which message broker should we use?" is really three questions: **Is this a stream of facts or a pile of work? How many independent consumers are there? Do we need to replay history?**

- **Apache Kafka** is a distributed, partitioned, append-only **log**. Messages are retained for a configured period regardless of whether they were read. Consumers track their own position (offset) and can re-read history.
- **RabbitMQ** is a **message broker** with exchanges and queues. It routes messages flexibly (direct, topic, fanout, headers) and a message is typically removed from a queue once a consumer acknowledges it. RabbitMQ also offers Streams for log-style use cases.
- **Managed queues** (Amazon SQS, Azure Service Bus queues, Google Cloud Tasks/Pub/Sub) provide durable work delivery with minimal operations.

## Quick Decision Table

| Concern | Kafka | RabbitMQ | Managed Queue (SQS / Service Bus) |
|---|---|---|---|
| Core model | Partitioned, retained log | Exchanges route to queues | Durable work queue |
| After consumption | Message stays (retention-based) | Removed on ack (classic/quorum queues) | Deleted on ack/delete |
| Multiple independent consumers | Native (consumer groups) | Via fanout/topic exchanges to separate queues | Via topics/subscriptions (SNS, Service Bus topics) |
| Replay | Native (reset offsets) | Streams support replay; queues do not | Limited |
| Ordering | Per partition | Per queue (single active consumer for strict order) | FIFO queues / sessions |
| Routing flexibility | Topic + key only | Very rich | Basic (filters on subscriptions) |
| Throughput | Very high (millions msg/s clusters) | High | High, provider-limited |
| Per-message features | Minimal | Priorities, TTL, dead-lettering, delayed messages (plugin) | Visibility timeout, DLQ, scheduled messages |
| Operations | Heavy (partitions, brokers, rebalancing) unless managed (MSK, Confluent, Event Hubs) | Moderate | Minimal |
| Best fit | Event streaming, CDC, analytics, event sourcing | Task routing, workflows, RPC-style messaging | Background jobs in one cloud |

## Kafka

### When to Use Kafka

- Many independent consumers of the same events (billing, analytics, search, ML features)
- Replay: rebuild read models, backfill a new consumer, reprocess after a bug fix
- High-throughput streams: clickstreams, telemetry, change data capture (Debezium)
- Stream processing (Kafka Streams, Flink) and event sourcing

### When Not to Use Kafka

- A few background jobs where a managed queue suffices
- Per-message routing rules, priorities, or delayed delivery as core requirements
- Small teams without capacity to operate it (consider a managed service or a simpler queue)

### Producer and Consumer (Confluent.Kafka)

```csharp
// Producer: key by entity so all events for one order land in the same partition (per-order ordering)
var producerConfig = new ProducerConfig { BootstrapServers = "kafka:9092", Acks = Acks.All, EnableIdempotence = true };
using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
await producer.ProduceAsync("orders.events", new Message<string, string>
{
    Key = orderId.ToString(),
    Value = JsonSerializer.Serialize(new OrderPaid(orderId, total, DateTimeOffset.UtcNow))
});

// Consumer: each group gets every event; instances within a group share partitions
var consumerConfig = new ConsumerConfig
{
    BootstrapServers = "kafka:9092",
    GroupId = "billing-service",
    EnableAutoCommit = false,               // commit only after successful processing
    AutoOffsetReset = AutoOffsetReset.Earliest
};
using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
consumer.Subscribe("orders.events");
while (!ct.IsCancellationRequested)
{
    var result = consumer.Consume(ct);
    await billing.HandleAsync(JsonSerializer.Deserialize<OrderPaid>(result.Message.Value)!, ct); // idempotent handler
    consumer.Commit(result);
}
```

**Why each part exists:** `Acks.All` + idempotent producer prevents data loss and duplicate writes from producer retries; keying by order ID preserves per-order ordering; manual commits after processing give at-least-once semantics, which is why the handler must be idempotent.

## RabbitMQ

### When to Use RabbitMQ

- Work distribution with acknowledgements, retries, and dead-lettering
- Complex routing: route `order.created.eu` to EU workers, `order.*.priority` to a priority queue
- Per-message TTL, priorities, and request/reply patterns
- Moderate throughput with low latency

### When Not to Use RabbitMQ

- Long-term retention and replay of large event histories (unless using Streams deliberately)
- Very high-throughput analytics streams with many consumer groups

### Publish and Consume (RabbitMQ.Client 7.x)

```csharp
var factory = new ConnectionFactory { HostName = "rabbitmq" };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync("orders", ExchangeType.Topic, durable: true);
await channel.QueueDeclareAsync("email-receipts", durable: true, exclusive: false, autoDelete: false,
    arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum", ["x-dead-letter-exchange"] = "orders.dlx" });
await channel.QueueBindAsync("email-receipts", "orders", routingKey: "order.paid");

var body = JsonSerializer.SerializeToUtf8Bytes(new OrderPaid(orderId, total, DateTimeOffset.UtcNow));
await channel.BasicPublishAsync("orders", "order.paid", mandatory: true,
    basicProperties: new BasicProperties { Persistent = true, MessageId = orderId.ToString() }, body: body);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        await receipts.SendAsync(JsonSerializer.Deserialize<OrderPaid>(ea.Body.Span)!, ct);
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch
    {
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false); // goes to dead-letter exchange
    }
};
await channel.BasicQosAsync(0, prefetchCount: 20, global: false);
await channel.BasicConsumeAsync("email-receipts", autoAck: false, consumer);
```

**Why each part exists:** quorum queues give replicated durability; a dead-letter exchange captures poison messages instead of looping forever; `prefetchCount` bounds in-flight work per consumer; persistent messages survive broker restarts.

## Managed Queues

### When to Use

- Background jobs and decoupling inside one cloud provider
- Teams that want zero broker operations
- Autoscaling workers on queue depth (KEDA, Lambda, Azure Functions)

### Example (Amazon SQS)

```csharp
var sqs = new AmazonSQSClient();
await sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = queueUrl, MessageBody = JsonSerializer.Serialize(job) });

var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
{
    QueueUrl = queueUrl, MaxNumberOfMessages = 10, WaitTimeSeconds = 20, VisibilityTimeout = 120
});
foreach (var msg in response.Messages)
{
    await ProcessAsync(msg.Body, ct);                                  // idempotent
    await sqs.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, ct);     // ack by deleting
}
```

## Same Scenario: E-commerce Messaging

| Need | Best Fit | Reason |
|---|---|---|
| `OrderPaid` consumed by billing, analytics, search, recommendations | Kafka | Many groups, replay for new consumers |
| Send receipt email, retry on failure, dead-letter bad messages | RabbitMQ or managed queue | One owner per message, ack/retry semantics |
| Route orders to region-specific fulfillment workers | RabbitMQ topic exchange | Rich routing |
| Resize uploaded images, scale workers on backlog | Managed queue (SQS/Service Bus) | Simple, autoscaling, no ops |
| Stream database changes to a data lake | Kafka + CDC | Ordered change log, high volume |

## Decision Matrix

| Factor | Kafka | RabbitMQ | Managed Queue |
|---|---|---|---|
| Replay / history | Strong | Weak (Streams: Medium) | Weak |
| Fan-out to many consumers | Strong | Medium | Medium (with topics) |
| Routing flexibility | Weak | Strong | Weak |
| Throughput at scale | Strong | Medium | Medium-Strong |
| Operational simplicity | Weak (unless managed) | Medium | Strong |
| Cloud portability | Strong | Strong | Weak |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Choosing Kafka for a handful of background jobs | Use a managed queue |
| Expecting global ordering in Kafka | Order is per partition; key by entity |
| Too few partitions (cannot scale consumers) or far too many | Size partitions from throughput and consumer parallelism targets |
| Auto-ack in RabbitMQ | Manual ack after successful processing |
| No dead-letter handling | DLQ/DLX with alerts and a replay procedure |
| Non-idempotent consumers | Dedupe by message ID or business key |
| Ignoring consumer lag | Alert on lag and oldest-message age |

## Interview Questions
- **[L1]** What is the main difference between a queue and an event stream?
- **[L1]** What is a Kafka consumer group?
- **[L2]** When is RabbitMQ/queue messaging a better fit than Kafka?
- **[L2]** What ordering does Kafka provide?
- **[L3]** How would you choose a messaging platform for order events?
- **[L3]** What operational metrics matter for either platform?

## Interview Answers
1. A queue distributes units of work: each message is delivered to one consumer and removed once acknowledged, so it represents a task to be done. An event stream is a retained, ordered log of facts: messages stay for the retention period regardless of consumption, each consumer group reads independently and tracks its own position, and consumers can replay history. Queues answer "who does this job"; streams answer "what happened, for anyone who cares".
2. A consumer group is a set of consumer instances that share the work of reading a topic. Kafka assigns each partition to exactly one consumer in the group, so instances process different partitions in parallel, and the group commits offsets to track progress. Different groups, such as billing and analytics, each receive the full stream independently. Parallelism within a group is limited by the number of partitions.
3. RabbitMQ or a managed queue is better when messages represent work for a single owner that needs per-message acknowledgement, retries, dead-lettering, delays, priorities, or TTL; when routing rules are complex; when throughput is moderate and replay is not needed; or when the team wants a simpler operational footprint. Examples include sending emails, generating documents, and dispatching tasks to region-specific workers.
4. Kafka guarantees ordering only within a single partition, not across a topic. Messages with the same key are hashed to the same partition, so keying by entity ID, such as order ID, preserves the order of events for each entity while allowing parallelism across entities. Producer settings like idempotence prevent retries from reordering or duplicating writes, and changing the partition count changes key-to-partition mapping, which can disrupt ordering for existing keys.
5. Start from requirements: number of independent consumers, need for replay and backfill, throughput, ordering scope, latency, retention, routing needs, cloud strategy, and team operating capability. Order events are business facts consumed by many services and useful for rebuilding projections, which favors Kafka (often a managed offering) with order ID as key, schema registry compatibility rules, outbox-based publishing, and idempotent consumers. Individual follow-up tasks such as sending emails can still use a queue fed by a consumer of those events.
6. Consumer lag and the age of the oldest unprocessed message, throughput in and out, publish and consume latency, error, retry, and dead-letter rates, consumer group rebalances or consumer counts, broker health (under-replicated partitions for Kafka, queue depth, memory and disk alarms, unacknowledged messages for RabbitMQ), disk usage and retention, connection counts, and end-to-end processing time traced across producer and consumer.
