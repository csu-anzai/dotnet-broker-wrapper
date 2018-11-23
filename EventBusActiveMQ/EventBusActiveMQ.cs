using System;
using Asseco.EventBus.Abstractions;
using Asseco.EventBus.Events;
using Asseco.EventBus.Exception;
using Asseco.EventBus;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace EventBusActiveMQImpl
{
    public delegate void MessageReceivedDelegate(IntegrationEvent message, string topic);

    public class EventBusActiveMQ : IEventBus
    {
        private IConnection connection;
        private ISession session;
        private Uri uri;
        private InMemoryEventBusSubscriptionsManager memory = new InMemoryEventBusSubscriptionsManager();
        public event MessageReceivedDelegate OnMessageReceived;

        public EventBusActiveMQ(Uri uri, String username, String password)
        {
            var url = uri.AddParameter("transport.startupMaxReconnectAttempts", "5");
            this.uri = url;
            Console.WriteLine(uri.ToString());
            IConnectionFactory factory = new ConnectionFactory(uri);
            try
            {
                this.connection = factory.CreateConnection(username, password);
                this.connection.Start();
                this.session = this.connection.CreateSession();

            }
            catch (NMSException ex)
            {
                throw new NMSException("Could not connect to message broker ", ex);
            }
        }

        public void Publish(IntegrationEvent integrationEvent)
        {
            try
            {
                if (integrationEvent.GetType() == typeof(MessageEvent))
                {
                    MessageEvent msgEvent = (MessageEvent)integrationEvent;
                    ActiveMQTopic topic = new ActiveMQTopic(msgEvent.getEventType());
                    using (IMessageProducer producer = this.session.CreateProducer(topic))
                    {
                        producer.DeliveryMode = MsgDeliveryMode.Persistent;
                        ITextMessage message = session.CreateTextMessage(msgEvent.getText());
                        foreach (String key in msgEvent.getProperties().Keys)
                        {
                            message.Properties[key] = msgEvent.getObjectProperty(key);
                        }
                        message.Properties["destination"] = msgEvent.getEventType();
                        try
                        {
                            producer.Send(message);
                        }
                        catch (NMSException ex)
                        {
                            throw new NMSException("Could not send a message ", ex);
                        }
                    }
                }
            }
            catch (NMSException ex)
            {
                throw new NMSException("Could not publish this event", ex);
            }
        }

        public void Subscribe<T>(string eventName, IIntegrationEventHandler<T> integrationEventHandler)
            where T : IntegrationEvent
        {
            try
            {
                this.OnMessageReceived += (async (msg, topic) =>
                {
                    if (topic.Equals(eventName))
                    {
                        await integrationEventHandler.Handle((T)msg);
                    }
                });
                this.DoInternalSubscription(eventName);
                this.memory.AddSubscription<T>(eventName, integrationEventHandler);
            }
            catch (EventBusException ex)
            {
                throw new EventBusException("Could not subscribe ", ex);
            }
        }

        public void SubscribeDynamic(string eventName, IIntegrationEventHandler<dynamic> handler)
        {
            try
            {
                this.OnMessageReceived += (async (msg, topic) =>
                {
                    if (topic.Equals(eventName))
                    {
                        await handler.Handle(msg);
                    }
                });
                this.DoInternalSubscription(eventName);
                this.memory.AddDynamicSubscription(eventName, handler);
            }
            catch (NMSException ex)
            {
                throw new NMSException("Could not subscribe dynamically ", ex);
            }

        }

        public void Unsubscribe<T>(string eventName, IIntegrationEventHandler<T> integrationEventHandler) where T : IntegrationEvent
        {
            this.memory.RemoveSubscription<T>(integrationEventHandler);
        }

        public void UnsubscribeDynamic(string eventName, IIntegrationEventHandler<dynamic> handler)
        {
            this.memory.RemoveDynamicSubscription(eventName, handler);
        }

        private void DoInternalSubscription(String eventKey)
        {
            //var containsKey = memory.HasSubscriptionsForEvent(eventKey);
            //if (!containsKey) {
            try
            {
                ActiveMQTopic topic = new ActiveMQTopic(eventKey);
                // this.session.CreateDurableConsumer(topic, "subscription-name", null, false);
                IMessageConsumer consumer = this.session.CreateConsumer(topic);
                consumer.Listener += new MessageListener(ReceiveMessage);
            }
            catch (NMSException ex)
            {
                throw new NMSException("Could not subscribe", ex);
            }
            //}
        }

        public void ReceiveMessage(IMessage msg)
        {
            if (this.OnMessageReceived != null)
            {
                String message = null;
                if (msg.GetType() == typeof(ActiveMQTextMessage))
                {
                    message = (msg as ActiveMQTextMessage).Text;
                }
                else if (msg.GetType() == typeof(ActiveMQBytesMessage))
                {
                    ActiveMQBytesMessage bytesMessage = (ActiveMQBytesMessage)msg;
                    message = Encoding.UTF8.GetString(bytesMessage.Content);
                }
                else
                {
                    throw new NMSException("Message could not be parsed ");
                }

                MessageEvent messageEvent = new MessageEvent(msg.Properties["destination"].ToString(), message, msg.NMSMessageId, msg.NMSTimestamp);
                foreach (String key in msg.Properties.Keys)
                {
                    messageEvent.setObjectProperty(key, msg.Properties[key]);
                }
                this.OnMessageReceived(messageEvent, messageEvent.getEventType());
            }
        }

    }
}
