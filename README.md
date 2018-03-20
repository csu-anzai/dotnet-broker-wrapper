# Asseco Message Broker Wrapper Library

## Usage

- From root of this repository download NuGet.config and put it in root of your project

- Use NuGet to set reference to EventBusActiveMQ package. 
Alternatively, make a change directly in your .csproj file and add following package reference:
```
<ItemGroup>
    <PackageReference Include="EventBusActiveMQ" Version="1.0.0" />
<ItemGroup>
```

- Restore NuGet packages (dotnet restore)

- In your class, add following imports:
```
using Asseco.EventBus.Events;
using Asseco.EventBusActiveMQ;
```

- Instance a EventBusActiveMQ (with proper values for host, port, username and password):
```
Uri uri = new Uri("activemq:tcp://localhost:61616");
String username = "enter-your-username";
String password = "enter-your-password";
EventBusActiveMQ bus = new EventBusActiveMQ(uri, username, password);
```
- Use method Publish to send messages and/or SubscribeDynamic method to receive them. See examples bellow.

## Creating ActiveMQ Topic Event Publisher (Producer)
```
   Uri uri = new Uri("failover:tcp://localhost:61616");
   IEventBus bus = new EventBusActiveMQ(uri, "your-username", "your-password");
   
   MessageEvent messageEvent = new MessageEvent("topic-name", "This is a text message!");
   messageEvent.setStringProperty("message-name", "exampleMessage");
   messageEvent.setStringProperty("random-property", "Random value");
   
   bus.Publish(messageEvent);
```

## Creating ActiveMQ Topic Listener (Consumer)
```
   Uri uri = new Uri("failover:tcp://localhost:61616");
   IEventBus bus = new EventBusActiveMQ(uri, "your-username", "your-password");
   
   bus.Subscribe("topic-name", new MessageEventHandler());
```
where MessageEventHandler is implementation of IIntegrationEventHandler, like this:
```
public class MessageEventHandler : IIntegrationEventHandler<MessageEvent>
{
    Task IIntegrationEventHandler<MessageEvent>.Handle(MessageEvent eventData)
    {
        Console.WriteLine("Message received " + eventData.getText());
        foreach (String key in eventData.getProperties().Keys)
        {
            Console.WriteLine(key + " : " + eventData.getProperties()[key]);
        }
        return Task.CompletedTask;
    }
}
```

Both listener and publisher must have an open connection in order to work. Same connection can  be used for receiving and sending events (messages) to message broker.

Failover is an ActiveMQ specific part of connection string for auto reconnect on Socket Exception.

Failover parameters can be found [here](http://activemq.apache.org/failover-transport-reference.html).

