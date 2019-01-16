using BrokerFacade.Abstractions;
using BrokerFacade.Model;
using BrokerFacade.RabbitMQ.Model;
using BrokerFacade.Serialization;
using BrokerFacade.Util;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrokerFacade.RabbitMQ
{
    public class BrokerFacadeRabbitMQ : AbstractBrokerFacade
    {
        private IConnection connection;
        private static object publishLock = new object();
        private readonly Dictionary<string, IModel> senderLinks = new Dictionary<string, IModel>();
        private readonly ConcurrentList<Subscription> subscriptionRequests = new ConcurrentList<Subscription>();
        private readonly ConcurrentList<PublishRequest> publishRequests = new ConcurrentList<PublishRequest>();
        private readonly ConcurrentList<ActiveSubscription> activeSubscriptions = new ConcurrentList<ActiveSubscription>();


        public BrokerFacadeRabbitMQ(string hostname, string port, string username, string password, string clientId) : base(hostname, port, username, password, clientId)
        {
            Task.Run(() =>
            {
                Log.Information("Broker connecting");
                Connect();
            });
        }

        private void Connect()
        {
            while (!ConnectionEstablished)
            {
                ConnectAgain();
                Thread.Sleep(500);
            }
        }

        public void ConnectAgain()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = Hostname,
                    UserName = Username,
                    Port = int.Parse(Port),
                    Password = Password,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(1)
                };
                connection = factory.CreateConnection();
                connection.RecoverySucceeded += Connection_RecoverySucceeded;
                connection.ConnectionRecoveryError += Connection_ConnectionRecoveryError;
                connection.ConnectionShutdown += Connection_ConnectionShutdown;
                ConnectionEstablished = true;
                Log.Information("Broker connected");
                OnConnect();
            }
            catch
            {
                Thread.Sleep(300);
                Connect();
            }
        }

        private void Connection_ConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs e)
        {
            Log.Information("Broker disconnected");
            ConnectionEstablished = false;
            Log.Information("Broker reconnecting");
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Log.Information("Broker disconnected");
            ConnectionEstablished = false;
            Log.Information("Broker reconnecting");
        }

        private void Connection_RecoverySucceeded(object sender, EventArgs e)
        {
            ConnectionEstablished = true;
            OnConnect();
        }

        public void OnConnect()
        {
            lock (publishLock)
            {
                foreach (PublishRequest request in publishRequests.ToList())
                {
                    Publish(request.Topic, request.MessageEvent);
                    publishRequests.Remove(request);
                }
            }
            // Add pending subscriptions
            foreach (Subscription request in subscriptionRequests.ToList())
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
            IModel channel = null;
            if (senderLinks.ContainsKey(topic))
            {
                channel = senderLinks[topic];
            }
            else
            {
                channel = connection.CreateModel();
                channel.ExchangeDeclare(exchange: topic,
                    durable: true,
                    type: "topic");
                senderLinks.Add(topic, channel);
            }
            try
            {
                string body = MessageEventSerializer.SerializeEventBody(messageEvent);
                var headers = MessageEventSerializer.GetMessageEventHeaders(messageEvent);
                BasicProperties properties = new BasicProperties();
                
                properties.Headers = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> entry in headers)
                {
                    properties.Headers.Add(entry.Key, entry.Value);
                }
                var bodyBytes = Encoding.UTF8.GetBytes(body);

                channel.BasicPublish(exchange: topic,
                                     routingKey: topic + ".*",
                                     basicProperties: properties,
                                     body: bodyBytes);
            }
            catch
            {
                lock (publishLock)
                {
                    publishRequests.Add(new PublishRequest { Topic = topic, MessageEvent = messageEvent });
                }
            }
        }

        private Subscription SubscribeInternal(string topic, string subscriptionName, AbstractMessageEventHandler handler)
        {
            if (!ConnectionEstablished)
            {
                subscriptionRequests.Add(
                    new Subscription { Topic = topic, SubscriptionName = subscriptionName, Handler = handler }
                );
            }
            else
            {
                var channel = connection.CreateModel();
                channel.ExchangeDeclare(exchange: topic, type: "topic", durable: true);
                var queueName = topic + "." + subscriptionName;
                channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

                channel.QueueBind(queue: queueName,
                                  exchange: topic,
                                  routingKey: topic + ".*");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    var headers = ea.BasicProperties.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    MessageEvent eventMsg = MessageEventSerializer.GetEventObject(message, headers);
                    eventMsg.Topic = topic;
                    handler.OnMessageInternal(eventMsg);
                    channel.BasicAck(ea.DeliveryTag, false);
                };
                channel.BasicConsume(queue: queueName,
                                     autoAck: false,
                                     consumer: consumer);
                activeSubscriptions.Add(
                   new ActiveSubscription { Topic = topic, SubscriptionName = subscriptionName, Handler = handler, Channel = channel }
               );
            }
            return new Subscription { Handler = handler, Topic = topic };
        }

        public override Subscription Subscribe(string topic, AbstractMessageEventHandler handler)
        {
            var subName = SubscriptionHostname.GetUniqueSubscription();
            return SubscribeInternal(topic, subName, handler);
        }

        public override Subscription SubscribeShared(string topic, string subscriptionName, AbstractMessageEventHandler handler)
        {
            return SubscribeInternal(topic, subscriptionName, handler);
        }

        public override void Unsubscribe(Subscription subscription)
        {
            var active = activeSubscriptions.Where(x => x.Handler.Equals(subscription.Handler) && x.Topic.Equals(subscription.Topic)).FirstOrDefault();
            if (active != null)
            {
                active.Channel.Close();
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
