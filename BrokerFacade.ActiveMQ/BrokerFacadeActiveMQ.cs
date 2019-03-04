using Amqp;
using Amqp.Framing;
using Amqp.Types;
using BrokerFacade.Abstractions;
using BrokerFacade.ActiveMQ.Model;
using BrokerFacade.Model;
using BrokerFacade.Serialization;
using BrokerFacade.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrokerFacade.ActiveMQ
{

    public class BrokerFacadeActiveMQ : AbstractBrokerFacade
    {
        private Connection Connection;
        private readonly int linkCredit = 300;
        private Session sendSession;
        private readonly string connectionPath;
        private readonly ConcurrentList<Subscription> subscriptionRequests = new ConcurrentList<Subscription>();
        private readonly ConcurrentList<ActiveSubscription> activeSubscriptions = new ConcurrentList<ActiveSubscription>();
        private readonly Dictionary<string, SenderLink> senderLinks = new Dictionary<string, SenderLink>();

        private static object publishLock = new object();

        public BrokerFacadeActiveMQ(
            string hostname,
            string port,
            string username,
            string password,
            string clientId)
        : base(hostname, port, username, password, clientId)
        {
            connectionPath = "amqp://" + username + ":" + password + "@" + hostname + ":" + port;
            Connect();
        }
        private void Connect()
        {
            while (!ConnectionEstablished)
            {
                ConnectAgain();
                Thread.Sleep(500);
            }
        }

        private void ConnectAgain()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.AMQP.ContainerId = ClientId;
            Connection = factory.CreateAsync(new Address(connectionPath)).Result;
            Connection.Closed += Connection_Closed;
            ConnectionEstablished = true;
            Log.Information("Broker connected");
            OnConnect();
        }

        private void Connection_Closed(IAmqpObject sender, Error error)
        {
            foreach (ActiveSubscription request in activeSubscriptions)
            {
                subscriptionRequests.Add(request);
            }
            Log.Information("Broker disconnected");
            activeSubscriptions.Clear();
            senderLinks.Clear();
            sendSession = null;
            ConnectionEstablished = false;
            Log.Information("Broker reconnecting");
            Connect();
        }


        public void OnConnect()
        {
            // Add pending subscriptions
            foreach (Subscription request in subscriptionRequests.ToList())
            {
                Subscribe(request.Topic, request.SubscriptionName, request.Handler);
                subscriptionRequests.Remove(request);
            }
        }


        public override void Publish(string topic, MessageEvent messageEvent)
        {
            try
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
            catch (AmqpException e)
            {
                Console.WriteLine(e);
            }
        }

        private Subscription SubscribeInternal(string topic, string subscriptionName, AbstractMessageEventHandler handler, bool durable)
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
                MessageEvent eventMsg = MessageEventSerializer.GetEventObject(message.Body.ToString(), GetDictonaryFromMap(message.ApplicationProperties?.Map));
                eventMsg.Topic = topic;
                handler.OnMessageInternal(eventMsg);
                receiver.Accept(message);
            });
            activeSubscriptions.Add(
                new ActiveSubscription { Topic = topic, SubscriptionName = topic + "_" + subscriptionName, Handler = handler, Link = receiverLink }
            );
            return new Subscription { Handler = handler, Topic = topic };
        }

        public override Subscription Subscribe(string topic, string subscriptionName, AbstractMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, handler, true);
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

        public override Subscription Subscribe(string topic, string subscriptionName, bool durable, AbstractMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, handler, durable);
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
    }
}
