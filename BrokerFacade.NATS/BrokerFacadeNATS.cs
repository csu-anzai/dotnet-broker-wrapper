using BrokerFacade.Abstractions;
using BrokerFacade.Model;
using BrokerFacade.NATS.Model;
using BrokerFacade.Serialization;
using BrokerFacade.Util;
using NATS.Client;
using Newtonsoft.Json.Linq;
using Serilog;
using STAN.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrokerFacade.NATS
{
    public class BrokerFacadeNATS : AbstractBrokerFacade
    {
        private IStanConnection Connection;
        private readonly ConcurrentList<ActiveSubscription> activeSubscriptions = new ConcurrentList<ActiveSubscription>();
        private readonly ConcurrentList<BrokerFacade.Model.Subscription> subscriptionRequests = new ConcurrentList<BrokerFacade.Model.Subscription>();
        private readonly ConcurrentList<PublishRequest> publishRequests = new ConcurrentList<PublishRequest>();
        private static object publishLock = new object();

        public BrokerFacadeNATS(string hostname, string port, string username, string password, string clientId) : base(hostname, port, username, password, clientId)
        {
            Task.Run(() =>
            {
                Log.Information("Broker connecting");
                Connect();
            });
        }

        private void Reconnect()
        {
            ConnectionEstablished = false;
            foreach (ActiveSubscription request in activeSubscriptions)
            {
                subscriptionRequests.Add(request);
            }
            Log.Information("Broker disconnected");
            activeSubscriptions.Clear();
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
            try
            {
                ConnectionFactory cf = new ConnectionFactory();
                Options opts = ConnectionFactory.GetDefaultOptions();
                opts.NoRandomize = true;
                opts.User = Username;
                opts.Password = Password;
                opts.Timeout = 1000;
                opts.AllowReconnect = false;
                opts.PingInterval = 1000;
                opts.MaxPingsOut = 2;
                opts.DisconnectedEventHandler += (sender, args) =>
                {
                    Reconnect();
                };

                var splittedServers = Hostname.Split(",");
                var ports = Port.Split(",");
                List<string> servers = new List<string>();
                for (var i = 0; i < splittedServers.Length; i++)
                {
                    servers.Add(splittedServers[i] + ":" + ports[i]);
                }
                opts.Servers = servers.ToArray();
                var stanCf = new StanConnectionFactory();
                var options = StanOptions.GetDefaultOptions();
                options.ConnectTimeout = 1000;
                options.NatsConn = cf.CreateConnection(opts);
                Connection = stanCf.CreateConnection("test-cluster", ClientId, options);
                ConnectionEstablished = true;
                Log.Information("Broker connected");
                OnConnect();
            }
            catch
            {
            }
        }

        public void OnConnect()
        {
            // Add pending publications
            lock (publishLock)
            {
                foreach (PublishRequest request in publishRequests.ToList())
                {
                    Publish(request.Topic, request.MessageEvent);
                    publishRequests.Remove(request);
                }
            }
            // Add pending subscriptions
            foreach (BrokerFacade.Model.Subscription request in subscriptionRequests.ToList())
            {
                if (request.SubscriptionName != null)
                {
                    SubscribeShared(request.Topic, request.SubscriptionName, request.Handler);
                }
                else
                {
                    Subscribe(request.Topic, request.Handler);
                }
                subscriptionRequests.Remove(request);
            }
        }


        private BrokerFacade.Model.Subscription SubscribeInternal(string topic, string subscriptionName, AbstractMessageEventHandler handler)
        {
            if (!ConnectionEstablished)
            {
                subscriptionRequests.Add(
                    new BrokerFacade.Model.Subscription { Topic = topic, SubscriptionName = subscriptionName, Handler = handler }
                );
            }
            else
            {
                EventHandler<StanMsgHandlerArgs> eh = (sender, args) =>
                {
                    var body = Encoding.UTF8.GetString(args.Message.Data);
                    var bodyObj = JObject.Parse(body);
                    if (bodyObj.ContainsKey("kind"))
                    {
                        Dictionary<string, object> headerValues = new Dictionary<string, object>();
                        foreach (JProperty property in bodyObj.Properties())
                        {
                            if (!property.Name.Equals("data"))
                            {
                                var valueProperty = MessageEventSerializer.MapJTokenValue(property.Value);
                                if (valueProperty != null)
                                {
                                    headerValues.Add(property.Name, valueProperty);
                                }
                            }
                        }
                        var obj = bodyObj["data"] != null ? bodyObj["data"].ToString() : "{}";
                        MessageEvent eventMsg = MessageEventSerializer.GetEventObject(obj, headerValues);
                        eventMsg.Topic = topic;
                        handler.OnMessageInternal(eventMsg);
                        args.Message.Ack();
                    }
                };
                IStanSubscription subscription = null;

                var opts = StanSubscriptionOptions.GetDefaultOptions();
                opts.DurableName = subscriptionName;
                opts.ManualAcks = true;
                opts.AckWait = 60000;
                opts.MaxInflight = 1;
                new Thread(() =>
                {
                    subscription = Connection.Subscribe(topic, subscriptionName, opts, eh);

                }).Start();
                activeSubscriptions.Add(
                    new ActiveSubscription { Topic = topic, SubscriptionName = subscriptionName, Handler = handler, Channel = subscription }
                );
            }
            return new BrokerFacade.Model.Subscription { Handler = handler, Topic = topic };
        }

        public override void Publish(string topic, MessageEvent messageEvent)
        {
            if (!ConnectionEstablished)
            {
                lock (publishLock)
                {
                    publishRequests.Add(new PublishRequest { Topic = topic, MessageEvent = messageEvent });
                }
                return;
            }
            try
            {
                var message = MessageEventSerializer.GetMessageEventHeaders(messageEvent);
              //  message.Add("data", messageEvent);
                var msg = MessageEventSerializer.SerializeCustomEventBody(message);
                Connection.Publish(topic, Encoding.UTF8.GetBytes(msg));

            }
            catch (Exception e)
            {
                lock (publishLock)
                {
                    publishRequests.Add(new PublishRequest { Topic = topic, MessageEvent = messageEvent });
                }
            }
        }

        public override BrokerFacade.Model.Subscription Subscribe(string topic, AbstractMessageEventHandler handler)
        {
            var subName = SubscriptionHostname.GetUniqueSubscription();
            return SubscribeInternal(topic, subName, handler);
        }

        public override BrokerFacade.Model.Subscription SubscribeShared(string topic, string subscriptionName, AbstractMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, handler);
        }

        public override void Unsubscribe(BrokerFacade.Model.Subscription subscription)
        {
            var active = activeSubscriptions.Where(x => x.Handler.Equals(subscription.Handler) && x.Topic.Equals(subscription.Topic)).FirstOrDefault();
            if (active != null)
            {
                active.Channel.Unsubscribe();
                activeSubscriptions.Remove(active);
            }

            var inactive = subscriptionRequests.Where(x => x.Handler.Equals(subscription.Handler) && x.Topic.Equals(subscription.Topic)).FirstOrDefault();
            if (inactive != null)
            {
                subscriptionRequests.Remove(inactive);
            }

        }
    }
}
