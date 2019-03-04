using BrokerFacade.Abstractions;
using BrokerFacade.Attributes;
using BrokerFacade.Context;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;
using System;
using System.Threading;

namespace BrokerFacade.RabbitMQ.Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            new Program(args);
            Console.ReadLine();
        }

        public Program(string[] args)
        {
            var topic = "sample";
            if (args.Length > 1)
            {
                topic = args[1];
            }
            var eventBus = new BrokerFacadeRabbitMQ("localhost", "5672", "admin", "admin", topic + "-client");

            if (args.Length > 0 && args[0] == "send")
            {
                while (true)
                {
                    eventBus.Publish(topic, new SampleEvent { ApplicationNumber = "000000007", UUID = Guid.NewGuid().ToString() });
                    Thread.Sleep(1000);
                }
            }
            else
            {
                var subName = "subscription-1";
                if (args.Length > 0)
                {
                    subName = args[0];
                }

                eventBus.Subscribe(topic, subName, new SampleEventHandler());
            }
        }
    }

    public class SampleEventHandler : AbstractMessageEventHandler
    {
        public override void OnMessage(MessageEvent messageEvent)
        {
            if (messageEvent is SampleEvent e)
            {
                Console.WriteLine(e.ApplicationNumber + " " + e.UUID);
                Console.WriteLine("Holder UUID " + (MessageEventHolder.MessageEvent.Value as SampleEvent).UUID);
            }
        }
    }

    [Kind("application-visited")]
    public class SampleEvent : MessageEvent
    {
        public string ApplicationNumber { get; set; }
        public string UUID { get; set; }
    }
}
