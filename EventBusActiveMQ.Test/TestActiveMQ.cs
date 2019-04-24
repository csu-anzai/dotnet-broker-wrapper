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
            IEventBus bus = new EventBusActiveMQ("172.16.89.235:30745", "admin", "admin");
            bus.Subscribe("content", new MessageEventHandler());
            Debug.WriteLine("Subscribed.");
            Debug.WriteLine("Message sent...");
            System.Threading.Thread.Sleep(100000);
        }
    }
}
