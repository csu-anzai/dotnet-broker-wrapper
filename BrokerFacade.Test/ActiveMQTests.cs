using BrokerFacade.ActiveMQ;
using BrokerFacade.Attributes;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;
using BrokerFacade.NATS;
using BrokerFacade.RabbitMQ;
using BrokerFacade.Test;
using Docker.DotNet;
using Docker.DotNet.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{

    public class MessageListener : IMessageEventHandler
    {
        private ActiveMQTests _context;
        public CloudEvent _messageEvent;
        public int MessageCount = 0;
        public MessageListener(ActiveMQTests context)
        {
            _context = context;
        }
        public void OnMessage(CloudEvent messageEvent)
        {
            MessageCount++;
            _messageEvent = messageEvent;
        }
    }

    [CloudEventDefinition("offer.application.visited.v2", "application/json")]
    public class SampleEvent : CloudEvent
    {
        public string ApplicationNumber { get; set; }
        public string UUID { get; set; }
    }
    public class ActiveMQTests
    {
        private DockerClient client;
        public List<string> containersIds = new List<string>();

        [SetUp]
        public void Setup()
        {
            client = new DockerClientConfiguration(TestUtil.LocalDockerUri()).CreateClient();
        }
        [TearDown]
        public void GlobalTeardown()
        {
            foreach (string id in containersIds)
            {
                try
                {
                    TestUtil.RemoveContainer(client, id);
                }
                catch { Console.WriteLine("Container with Id " + id + " already deleted"); }
            }
        }


        [Test]
        [TestCase(ContainerType.NATS)]
        [TestCase(ContainerType.RABBITMQ)]
        [TestCase(ContainerType.ARTEMIS)]
        public void TestIfMessageIsReceivedAsQueue(ContainerType type)
        {
            var container = StartContainer(type);
            var listener1 = new MessageListener(this);
            var listener2 = new MessageListener(this);
            IBrokerFacade facade = null;
            IBrokerFacade facade2 = null;
            IBrokerFacade facade3 = null;
            facade = GetBrokerConnection(type, container.TargetPort);
            facade.Connect();
            facade.Subscribe("test_topic", "sub1", true, listener1);
            facade2 = GetBrokerConnection(type, container.TargetPort, "clientb");
            facade2.Connect();
            facade2.Subscribe("test_topic", "sub1", true, listener2);
            facade3 = GetBrokerConnection(type, container.TargetPort, "clientc");
            facade3.Connect();
            Thread.Sleep(2500);
            for (int i = 0; i < 10; i++)
            {
                facade3.Publish("test_topic", new SampleEvent { ApplicationNumber = "0000010", UUID = Guid.NewGuid().ToString() });
            }
            int timeOut = 5000;
            for (int i = 0; i < timeOut; i += 100)
            {
                if (listener1.MessageCount != 0 && listener2.MessageCount != 0)
                {
                    Assert.AreNotEqual(listener1._messageEvent, listener2._messageEvent);
                    Assert.GreaterOrEqual(listener1.MessageCount, 1);
                    Assert.GreaterOrEqual(listener2.MessageCount, 1);
                    facade.Disconnect();
                    TestUtil.RemoveContainer(client, container.Id);
                    return;
                }
                Thread.Sleep(100);
            }
            try
            {
                facade.Disconnect();
            }
            catch { }
            throw new Exception("Event not received");
        }

        [Test]
        [TestCase(ContainerType.NATS)]
        [TestCase(ContainerType.ARTEMIS)]
        [TestCase(ContainerType.RABBITMQ)]
        public void TestIfMessageIsReceived(ContainerType type)
        {
            var container = StartContainer(type);
            IBrokerFacade facade = GetBrokerConnection(type, container.TargetPort);
            facade.Connect();
            var listener = new MessageListener(this);
            facade.Subscribe("test_topic", "sub1", listener);
            facade.Publish("test_topic", new SampleEvent { ApplicationNumber = "0000010", UUID = Guid.NewGuid().ToString() });
            int timeOut = 5000;
            for (int i = 0; i < timeOut; i += 100)
            {
                if (listener._messageEvent != null)
                {
                    Assert.NotNull(listener._messageEvent);
                    Assert.True(listener._messageEvent is SampleEvent);
                    if (listener._messageEvent is SampleEvent e)
                    {
                        Assert.AreEqual(e.ApplicationNumber, "0000010");
                        Assert.NotNull(e.CloudEventDefinition);
                        Assert.NotNull(e.Id);
                        Assert.NotNull(e.CloudEventDefinition.Type, "offer.application.visited.v2");
                        Assert.NotNull(e.CloudEventDefinition.ContentType, "application/json");
                        Assert.AreEqual(e.CloudEventDefinition.SpecVersion, "0.2");
                        Assert.AreEqual(e.ApplicationNumber, "0000010");
                        TestUtil.RemoveContainer(client, container.Id);
                    }
                    facade.Disconnect();
                    return;
                }
                Thread.Sleep(100);
            }
            try
            {
                facade.Disconnect();
            }
            catch { }
            throw new Exception("Event not received");
        }

        public IBrokerFacade GetBrokerConnection(ContainerType type, string targetPort, string customClient = null)
        {
            if (customClient == null)
            {
                customClient = "clienta";
            }
            switch (type)
            {
                case ContainerType.ARTEMIS:
                    return new BrokerFacadeActiveMQ("localhost", targetPort, "admin", "admin", customClient);
                case ContainerType.NATS:
                    return new BrokerFacadeNATS("localhost", targetPort, "ruser", "T0pS3cr3t", customClient);
                case ContainerType.RABBITMQ:
                    return new BrokerFacadeRabbitMQ("localhost", targetPort, "admin", "admin", customClient);
                default:
                    return null;
            }
        }

        public RunnedContainerData StartContainer(ContainerType type)
        {
            string Id;
            string Port;
            List<string> envs = new List<string>();
            Dictionary<string, string> ports = new Dictionary<string, string>();
            switch (type)
            {
                case ContainerType.ARTEMIS:
                    envs.Add("ARTEMIS_PASSWORD=admin");
                    envs.Add("ARTEMIS_USERNAME=admin");
                    Port = "45411";
                    //  ports.Add("8161/tcp", "8161/tcp");
                    ports.Add("5672/tcp", Port + "/tcp");
                    Id = TestUtil.StartContainer(client, "vromero/activemq-artemis:2.6.3-alpine", envs, ports);
                    break;
                case ContainerType.NATS:
                    Port = "45412";
                    ports.Add("4222/tcp", Port + "/tcp");
                    List<string> cmds = new List<string> { "-store=file", "-dir=datastore", "--hb_interval=1s", "--hb_timeout=1s", "--hb_fail_count=2" };
                    Id = TestUtil.StartContainer(client, "nats-streaming", envs, ports, cmds);
                    break;
                case ContainerType.RABBITMQ:
                    envs.Add("RABBITMQ_DEFAULT_PASS=admin");
                    envs.Add("RABBITMQ_DEFAULT_USER=admin");
                    Port = "45413";
                    ports.Add("5672/tcp", Port + "/tcp");
                    Id = TestUtil.StartContainer(client, "rabbitmq", envs, ports);
                    break;
                default:
                    throw new Exception("Could not create container with provided type");
            }
            containersIds.Add(Id);
            return new RunnedContainerData() { Id = Id, TargetPort = Port };
        }


    }
}