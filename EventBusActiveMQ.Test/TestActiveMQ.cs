using Xunit;
using Xunit.Extensions;
using Xunit.Abstractions;
using Asseco.EventBusActiveMQ;
using System;
using Asseco.EventBus.Events;
using System.Threading;
using System.Diagnostics;

namespace EventBusAMQPTest
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
            EventBusActiveMQ bus = new EventBusActiveMQ(uri, "admin", "admin");
            bus.SubscribeDynamic("notifications");
            bus.OnMessageReceived += (msg =>
            {
                Debug.WriteLine("Message received " + msg.Text);
                foreach (String key in msg.Properties.Keys)
                {
                    Debug.WriteLine(key + " : " + msg.Properties[key]);
                }
            });
            MessageEvent messageEvent = new MessageEvent("notifications", "This is a text of the message");
            messageEvent.setStringProperty("name", "John Smith");
            bus.Publish(messageEvent);
            //Thread.Sleep(2000);
        }
    }
}
