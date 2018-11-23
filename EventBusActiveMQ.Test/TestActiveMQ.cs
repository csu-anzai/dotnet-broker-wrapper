using System;
using Xunit;
using Xunit.Abstractions;
using EventBusActiveMQImpl;
using Asseco.EventBus.Events;
using System.Diagnostics;
using Asseco.EventBus.Abstractions;

namespace EventBusActiveMQTest
{
    public class TestActiveMQ
    {
        private readonly ITestOutputHelper output;

        public TestActiveMQ(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void PubSub()
        {
            Uri uri = new Uri("activemq:tcp://localhost:61616");
            IEventBus bus = new EventBusActiveMQ(uri, "admin", "admin");
            bus.Subscribe("notifications", new MessageEventHandler());
            Debug.WriteLine("Subscribed.");
            MessageEvent messageEvent = new MessageEvent("notifications", "This is a text of the message");
            messageEvent.setStringProperty("name", "John Smith");
            bus.Publish(messageEvent);
            Debug.WriteLine("Message sent...");
            //System.Threading.Thread.Sleep(5000);
        }
    }
}
