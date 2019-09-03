﻿using BrokerFacade.Abstractions;
using BrokerFacade.Attributes;
using BrokerFacade.Context;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;
using BrokerFacade.Util;
using Serilog;
using System;
using System.Threading;

namespace BrokerFacade.ActiveMQ.Test
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
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console().CreateLogger();
            var topic = "sample";
            if (args.Length > 1)
            {
                topic = args[1];
            }
            var eventBus = new BrokerFacadeActiveMQ("localhost", "5672", "admin", "admin", topic + "-client")
            {
                SendRetries = 15,
                RetryTimeout = 1500
            };
            eventBus.Connect();
            if (args.Length > 0 && args[0] == "send")
            {
                while (true)
                {
                    Console.WriteLine("Message sent to topic" + topic);
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
                Console.WriteLine("Sub name " + subName + " and topic: " + topic);
                eventBus.Subscribe(topic, subName, new SampleEventHandler());
            }
        }
    }

    public class SampleEventHandler : IMessageEventHandler
    {
        public void OnMessage(CloudEvent messageEvent)
        {
            if (messageEvent is SampleEvent e)
            {
                Console.WriteLine(e.ApplicationNumber + " " + e.UUID);
                Console.WriteLine("Holder UUID " + (MessageEventHolder.MessageEvent.Value as SampleEvent).UUID);
            }
        }
    }
    
    [CloudEventDefinition("offer.application.visited.v2", "application/json")]
    public class SampleEvent : CloudEvent
    {
        public string ApplicationNumber { get; set; }
        public string UUID { get; set; }
    }
}