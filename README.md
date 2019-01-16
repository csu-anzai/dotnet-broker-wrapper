# Broker Facade in .Net

This repository contains Broker Facade interface. Idea is to provide simple interface over different implementation of message brokers.

## Supported features are:
- Durability
- Shared Subscriptions (Queue Groups or Virtual Topics )
- Regular Topic Subscriptions
- Connection Failover (Auto reconnect)

## Implemented brokers:
- ActiveMQ Artemis
https://activemq.apache.org/artemis/
- RabbitMQ
https://www.rabbitmq.com/
- NATS Streaming
https://nats.io/

## Usage

### Creating events

Events are created by extending MessageEvent and defining Kind with Attribute.
Setting Header attribute makes attribute go into Message header.

```csharp
   [Kind("application-visited")]
   public class SampleEvent : MessageEvent
   {
       public string ApplicationNumber { get; set; }
       public string UUID { get; set; }
   }
```


### Connection

Connection parameters may be different for different brokers. Here are example of connection to different brokers.

#### ActiveMQ Artemis
```csharp
IBrokerFacade brokerFacade = new BrokerFacadeActiveMQ("localhost", "5672", "admin", "admin", "client-a");
```

#### RabbitMQ
```csharp
IBrokerFacade brokerFacade = new BrokerFacadeRabbitMQ("localhost", "5672", "admin", "admin", "client-a");
```

#### NATS

NATS connection will also contain cluster name in future. Currently default is "test-cluster" as this is default for NATS.
```csharp
IBrokerFacade brokerFacade = new BrokerFacadeNATS("localhost", "5672", "admin", "admin", "client-a");
```

### Publishing messages

To publish event on broker just specify topic and MessageEvent you want to send.

```csharp
eventBus.Publish(topic, new SampleEvent { ApplicationNumber = "000000007", UUID = Guid.NewGuid().ToString() });
```

### Consuming messages

There is two ways of consuming messages. In order to have shared subscription use SubscribeShared and for normal Subscription use Subscribe method.

### Regular subscription

Subscription:
```csharp
eventBus.Subscribe(topic, new SampleEventHandler());
```

### Shared subscription

```csharp
eventBus.SubscribeShared(topic, subscriptionName, new SampleEventHandler());
```

### Handling messages:
```csharp
public class SampleEventHandler : AbstractMessageEventHandler
{
	public override void OnMessage(MessageEvent messageEvent)
	{
		if (messageEvent is SampleEvent e)
		{
			Console.WriteLine(e.ApplicationNumber + " " + e.UUID);
		}
	}
}
```

## Left to do:

### Implemented Storage for unsent broker messages

We should have an option to Save messages until they are delivered to Broker server in order to ensure that message will be delivered. After it is delivered to server broker needs to asure with Durability that message is delivered to consumer.

### Test redelivery of messages on different brokers

- Redelivery of messages has different behavior dependent on Broker behind. For example NATS is redelivering message after the Timeout for Acknoledgment passes. So for NATS this is handled by the server. 

- RabbitMQ on the other hand is not redelivering message if Consumer lives after that message is not procesed, so in order to redeliver message to consumer the consumer must be recreated if it has no ability to handle message. For

- ActiveMQ Artemis we should reseach how this is handled. 

### Test with NATS Streaming containing fix for issue #725

While there is no release with this fix included we should start server with next options:
--hb_interval=1s --hb_timeout=1s --hb_fail_count=2

https://github.com/nats-io/nats-streaming-server/issues/725

### Implement CloudEvent specification - Create a disscusion on this topic

CloudEvent specification defines standard how same events should be represented in different protocols like AMQP, MQTT, HTTP or NATS. We should follow this specification in order to ensure message convertability between protocols.

https://github.com/cloudevents/spec

This specification requires headers such as:

Required:

**type** - com.example.object.delete.v2

This should contain namespace (com.example). Resource, Method description and Version of events

**specversion** - Version of cloud events

**id** - Id of the event

**source** - Source URI reference

**time** - Optional 

**schemaurl** - Optional

**contenttype** - Optional

Currently we define event type with property named Kind. If we want to follow CloudEvents we need to change Bankapi.net definition according to this.

Also there should be a difference between Meta-data and data in event. And this should be also specified on bankapi.net

One way is to just specify which properties are meta-data and other options is to use Json Event Format as base for bankapi.net definition of events.
