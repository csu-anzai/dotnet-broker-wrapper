using Amqp;
using Amqp.Framing;
using Amqp.Types;
using BrokerFacade.Abstractions;
using BrokerFacade.ActiveMQ.Model;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;
using BrokerFacade.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrokerFacade.ActiveMQ
{

    public class BrokerFacadeActiveMQ : AbstractBrokerFacade
    {
        private Connection Connection;
        private readonly int linkCredit = 200;
        private Session sendSession;
        private readonly string connectionPath;
        private readonly ConcurrentList<Subscription> subscriptionRequests = new ConcurrentList<Subscription>();
        private readonly ConcurrentList<ActiveSubscription> activeSubscriptions = new ConcurrentList<ActiveSubscription>();
        private readonly Dictionary<string, SenderLink> senderLinks = new Dictionary<string, SenderLink>();

        public override event ConnectionState Connected;
        public override event ConnectionState ConnectionLost;
        public override event ConnectionState ReconnectionStarted;

        public BrokerFacadeActiveMQ(
            string hostname,
            string port,
            string username,
            string password,
            string clientId)
        : base(hostname, port, username, password, clientId)
        {
            connectionPath = "amqp://" + username + ":" + password + "@" + hostname + ":" + port;
        }

        protected override void ConnectInternal()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.AMQP.ContainerId = ClientId;
            factory.TCP.SendTimeout = SendTimeout;
            factory.TCP.ReceiveTimeout = ReceiveTimeout;
            Connection = factory.CreateAsync(new Address(connectionPath)).Result;
            Connection.Closed += Connection_Closed;
            ConnectionEstablished = true;
            Connected?.Invoke();
            Log.Information("Broker connected");
            OnConnect();

        }

        private void Connection_Closed(IAmqpObject sender, Error error)
        {
            Console.WriteLine(error);
            ConnectionLost?.Invoke();
            foreach (ActiveSubscription request in activeSubscriptions)
            {
                try
                {
                    this.Connection.Close();
                }
                catch { }
                try
                {
                    foreach (KeyValuePair<string, SenderLink> link in senderLinks)
                    {
                        link.Value.Close();
                    }
                }
                catch { }
                try
                {
                    request.Link.Close();
                }
                catch { }
                subscriptionRequests.Add(request);
            }
            Log.Information("Broker disconnected");
            activeSubscriptions.Clear();
            senderLinks.Clear();
            sendSession = null;
            ConnectionEstablished = false;
            Log.Information("Broker reconnecting");
            ReconnectionStarted?.Invoke();
            Reconnect();
        }


        public void OnConnect()
        {
            // Add pending subscriptions
            foreach (Subscription request in subscriptionRequests.ToList())
            {
                Subscribe(request.Topic, request.SubscriptionName, request.Durable, request.Handler);
                subscriptionRequests.Remove(request);
            }
        }

        protected override void PublishInternal(string topic, CloudEvent messageEvent)
        {
            if (sendSession == null)
            {
                sendSession = new Session(Connection);
            }
            Target target = new Target
            {
                Address = topic,
                Capabilities = new Symbol[] { new Symbol("topic") }
            };
            SenderLink sender = null;
            if (senderLinks.ContainsKey(topic))
            {
                sender = senderLinks[topic];
            }
            else
            {
                sender = new SenderLink(sendSession, "sender-link-" + topic, target, null);
                senderLinks.Add(topic, sender);
            }
            Message message = new Message(MessageEventSerializer.SerializeEventBody(messageEvent));
            var headers = MessageEventSerializer.GetMessageEventHeaders(messageEvent);
            if (message.ApplicationProperties == null)
            {
                message.ApplicationProperties = new ApplicationProperties();
            }
            foreach (KeyValuePair<string, object> entry in headers)
            {
                message.ApplicationProperties[entry.Key] = entry.Value;
            }

            sender.Send(message);
        }

        private Subscription SubscribeInternal(string topic, string subscriptionName, bool durable, IMessageEventHandler handler)
        {
            Session session = new Session(Connection);
            Symbol[] capabilities = null;
            if (subscriptionName != null)
            {
                capabilities = new Symbol[] { new Symbol("topic"), new Symbol("global"), new Symbol("shared"), new Symbol("SHARED-SUBS") };
            }
            else
            {
                capabilities = new Symbol[] { new Symbol("topic") };
            }
            Source target = new Source
            {
                Address = topic,
                Durable = (durable) ? 2u : 0u,
                Capabilities = capabilities
            };
            ReceiverLink receiverLink = new ReceiverLink(session, subscriptionName, target, null);
            receiverLink.Start(linkCredit, (receiver, message) =>
            {
                CloudEvent eventMsg = MessageEventSerializer.GetEventObject(message.Body.ToString(), GetDictonaryFromMap(message.ApplicationProperties?.Map));
                OnMessage(handler, eventMsg);
                receiver.Accept(message);
            });
            activeSubscriptions.Add(
                new ActiveSubscription { Topic = topic, SubscriptionName = subscriptionName, Handler = handler, Link = receiverLink }
            );
            return new Subscription { Handler = handler, Topic = topic };
        }

        public override Subscription Subscribe(string topic, string subscriptionName, IMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, true, handler);
        }

        public override Subscription Subscribe(string topic, string subscriptionName, bool durable, IMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, durable, handler);
        }

        private Dictionary<string, object> GetDictonaryFromMap(Map map)
        {
            Dictionary<string, object> headers = new Dictionary<string, object>();
            if (map == null)
            {
                return headers;
            }
            foreach (KeyValuePair<object, object> entry in map)
            {
                headers.Add(entry.Key.ToString(), entry.Value);
            }
            return headers;
        }

        public override void Unsubscribe(Subscription subscription)
        {
            var active = activeSubscriptions.Where(x => x.Handler.Equals(subscription.Handler) && x.Topic.Equals(subscription.Topic)).FirstOrDefault();
            if (active != null)
            {
                active.Link.Close();
                activeSubscriptions.Remove(active);
            }
        }

        public override void Disconnect()
        {
            Connection.Closed -= Connection_Closed;
            Connection.Close();
        }
    }
}
