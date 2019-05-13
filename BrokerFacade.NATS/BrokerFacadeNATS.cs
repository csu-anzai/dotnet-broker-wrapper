using BrokerFacade.Abstractions;
using BrokerFacade.Interfaces;
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
        public override event ConnectionState Connected ;
        public override event ConnectionState ConnectionLost;
        public override event ConnectionState ReconnectionStarted;

        public BrokerFacadeNATS(string hostname, string port, string username, string password, string clientId) : base(hostname, port, username, password, clientId)
        {
        }

        public override void Reconnect()
        {
            ConnectionEstablished = false;
            foreach (ActiveSubscription request in activeSubscriptions)
            {
                subscriptionRequests.Add(request);
            }
            Log.Information("Broker disconnected");
            activeSubscriptions.Clear();
            base.Reconnect();
        }

        protected void DisconnectEventHandler(object sender, ConnEventArgs connEventArgs)
        {

            ConnectionLost?.Invoke();
            ReconnectionStarted?.Invoke();
            Disconnect();
            Reconnect();
        }
        protected override void ConnectInternal()
        {
            try
            {
                ConnectionFactory cf = new ConnectionFactory();
                Options opts = ConnectionFactory.GetDefaultOptions();
                opts.NoRandomize = true;
                opts.User = Username;
                opts.Password = Password;
                opts.Timeout = ConnectionTimeout;
                opts.AllowReconnect = false;
                opts.PingInterval = 1000;
                opts.MaxPingsOut = 2;
                opts.DisconnectedEventHandler += DisconnectEventHandler;
                opts.AsyncErrorEventHandler += (sender, args) =>
                {
                    Log.Information("Error async");
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
                options.ConnectTimeout = ConnectionTimeout;
                options.NatsConn = cf.CreateConnection(opts);
                Connection = stanCf.CreateConnection("test-cluster", ClientId, options);
                ConnectionEstablished = true;
                Log.Information("Broker connected");
                Connected?.Invoke();
                OnConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void OnConnect()
        {
            // Add pending subscriptions
            foreach (BrokerFacade.Model.Subscription request in subscriptionRequests.ToList())
            {
                Subscribe(request.Topic, request.SubscriptionName, request.Durable, request.Handler);
                subscriptionRequests.Remove(request);
            }
        }


        private BrokerFacade.Model.Subscription SubscribeInternal(string topic, string subscriptionName, IMessageEventHandler handler, bool durable)
        {
            void eh(object sender, StanMsgHandlerArgs args)
            {
                var body = Encoding.UTF8.GetString(args.Message.Data);
                var bodyObj = JObject.Parse(body);
                if (bodyObj.ContainsKey("type"))
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
                    CloudEvent eventMsg = MessageEventSerializer.GetEventObject(obj, headerValues);
                    OnMessage(handler, eventMsg);
                    args.Message.Ack();
                }
            }
            IStanSubscription subscription = null;

            var opts = StanSubscriptionOptions.GetDefaultOptions();
            if (durable)
            {
                opts.DurableName = subscriptionName;
            }
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
            return new BrokerFacade.Model.Subscription { Handler = handler, Topic = topic };
        }

        protected override void PublishInternal(string topic, CloudEvent messageEvent)
        {
            var message = MessageEventSerializer.GetMessageEventHeaders(messageEvent);
            message.Add("data", messageEvent);
            //  message.Add("data", messageEvent);
            var msg = MessageEventSerializer.SerializeCustomEventBody(message);
            Connection.Publish(topic, Encoding.UTF8.GetBytes(msg));
        }

        public override BrokerFacade.Model.Subscription Subscribe(string topic, string subscriptionName, bool durable, IMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, handler, durable);
        }

        public override BrokerFacade.Model.Subscription Subscribe(string topic, string subscriptionName, IMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, handler, true);
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

        public override void Disconnect()
        {
            Connection.NATSConnection.Opts.DisconnectedEventHandler -= DisconnectEventHandler;
            try
            {
                Connection.NATSConnection.Close();
                Connection.Close();
            }
            catch {
            }
        }
    }
}
