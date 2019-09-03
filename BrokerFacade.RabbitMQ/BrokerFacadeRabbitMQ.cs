using BrokerFacade.Abstractions;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;
using BrokerFacade.RabbitMQ.Model;
using BrokerFacade.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BrokerFacade.RabbitMQ
{
    public class BrokerFacadeRabbitMQ : AbstractBrokerFacade
    {
        private IConnection connection;
        private readonly Dictionary<string, IModel> senderLinks = new Dictionary<string, IModel>();
        private readonly ConcurrentList<ActiveSubscription> activeSubscriptions = new ConcurrentList<ActiveSubscription>();

        public override event ConnectionState Connected;
        public override event ConnectionState ConnectionLost;
        public override event ConnectionState ReconnectionStarted;

        public BrokerFacadeRabbitMQ(string hostname, string port, string username, string password, string clientId) : base(hostname, port, username, password, clientId)
        {
        }

        protected override void ConnectInternal()
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
                Connected?.Invoke();
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
            ConnectionLost?.Invoke();
            ConnectionEstablished = false;
            Log.Information("Broker reconnecting");
            ReconnectionStarted?.Invoke();
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Log.Information("Broker disconnected");
            ConnectionLost?.Invoke();
            ConnectionEstablished = false;
            Log.Information("Broker reconnecting");
            ReconnectionStarted?.Invoke();
        }

        private void Connection_RecoverySucceeded(object sender, EventArgs e)
        {
            ConnectionEstablished = true;
        }

        protected override void PublishInternal(string topic, CloudEvent messageEvent)
        {
            IModel channel;
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
            string body = MessageEventSerializer.SerializeEventBody(messageEvent);
            var headers = MessageEventSerializer.GetMessageEventHeaders(messageEvent);
            BasicProperties properties = new BasicProperties
            {
                Headers = new Dictionary<string, object>()
            };
            foreach (KeyValuePair<string, object> entry in headers)
            {
                if (entry.Value != null)
                {
                    if (entry.Value is DateTime)
                    {
                        var value = entry.Value as DateTime?;
                        properties.Headers.Add(entry.Key, value?.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    }
                    else
                    {
                        properties.Headers.Add(entry.Key, entry.Value);
                    }
                }
            }
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            channel.BasicPublish(exchange: topic,
                                 routingKey: topic,
                                 basicProperties: properties,
                                 body: bodyBytes);

        }

        private Subscription SubscribeInternal(string topic, string subscriptionName, bool durable, IMessageEventHandler handler)
        {
            // Use same topic matching as RabbitMQ and ActiveMQ Artemis
            topic = topic.Replace("#", ">");
            var channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: topic, type: "topic", durable: durable);
            var queueName = topic + "." + subscriptionName;
            channel.QueueDeclare(queue: queueName, durable: durable, exclusive: false, autoDelete: false);

            channel.QueueBind(queue: queueName,
                              exchange: topic,
                              routingKey: topic);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                var headers = ea.BasicProperties.Headers.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value is byte[]) ? Encoding.UTF8.GetString(kvp.Value as byte[]) : kvp.Value);
                CloudEvent eventMsg = MessageEventSerializer.GetEventObject(message, headers);
                OnMessage(handler, eventMsg);
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
            activeSubscriptions.Add(
               new ActiveSubscription { Topic = topic, SubscriptionName = subscriptionName, Handler = handler, Channel = channel }
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

        public override void Unsubscribe(Subscription subscription)
        {
            var active = activeSubscriptions.Where(x => x.Handler.Equals(subscription.Handler) && x.Topic.Equals(subscription.Topic)).FirstOrDefault();
            if (active != null)
            {
                active.Channel.Close();
                activeSubscriptions.Remove(active);
            }
        }

        public override void Disconnect()
        {
            connection.RecoverySucceeded -= Connection_RecoverySucceeded;
            connection.ConnectionRecoveryError -= Connection_ConnectionRecoveryError;
            connection.ConnectionShutdown -= Connection_ConnectionShutdown;
            connection.Close();
        }
    }
}
