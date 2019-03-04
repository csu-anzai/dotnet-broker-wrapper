using BrokerFacade.Abstractions;
using BrokerFacade.Attributes;
using BrokerFacade.Context;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;
using System;
using System.Threading;

namespace BrokerFacade.NATS.Test
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
            var clientId = "";
            var topic = "sample";
            if (args.Length > 1)
            {
                topic = args[1];
            }
            if (args.Length >2)
            {
                clientId = args[2];
            }
            var eventBus = new BrokerFacadeNATS("localhost", "4222", "ruser", "T0pS3cr3t", clientId);

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
                Thread.Sleep(1000);
                Console.WriteLine(e.ApplicationNumber + " " + e.UUID);
                Console.WriteLine("Holder UUID " + (MessageEventHolder.MessageEvent.Value as SampleEvent).UUID);
            }
        }
    }

    [Kind("application-visited")]
    public class SampleEvent : MessageEvent
    {
        public string ApplicationNumber { get; set; }
        [Header]
        public string UUID { get; set; }
    }
}
