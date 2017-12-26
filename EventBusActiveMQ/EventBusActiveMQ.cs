using System;
using Asseco.EventBus.Abstractions;
using Asseco.EventBus.Events;
using Asseco.EventBus.Exception;
using Asseco.EventBus;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System.Text;

namespace Asseco.EventBusActiveMQ
{
    public delegate void MessageReceivedDelegate(ITextMessage message);

    public class EventBusActiveMQ : IEventBus
    {
        private IConnection connection;
        private ISession session;
        private Uri uri;
        private InMemoryEventBusSubscriptionsManager memory = new InMemoryEventBusSubscriptionsManager();
        public event MessageReceivedDelegate OnMessageReceived;

        public EventBusActiveMQ(Uri uri, String username, String password)
        {
            this.uri = uri;
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

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            try
            {
                var eventName = this.memory.GetEventKey<T>();
                this.DoInternalSubscription(eventName);
                this.memory.AddSubscription<T, TH>();
            }
            catch (EventBusException ex)
            {
                throw new EventBusException("Could not subscribe ", ex);
            }
        }

        public void SubscribeDynamic(string eventName) {
            this.SubscribeDynamic<IDynamicIntegrationEventHandler>(eventName);
        }

        public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            try
            {
                this.DoInternalSubscription(eventName);
                this.memory.AddDynamicSubscription<TH>(eventName);
            }
            catch (NMSException ex)
            {
                throw new NMSException("Could not subscribe dynamically ", ex);
            }

        }

        public void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            this.memory.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            this.memory.RemoveDynamicSubscription<TH>(eventName);
        }

        private void DoInternalSubscription(String eventKey)
        {
            try
            {
                ActiveMQTopic topic = new ActiveMQTopic(eventKey);
                IMessageConsumer consumer = this.session.CreateDurableConsumer(topic, "consumer-name", "2 > 1", false);
                consumer.Listener += new MessageListener(ReceiveMessage);
            }
            catch (NMSException ex)
            {
                throw new NMSException("Could not subscribe", ex);
            }
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

                ActiveMQTextMessage activeMqMessage = new ActiveMQTextMessage()
                {
                    Text = message,
                    NMSTimestamp = msg.NMSTimestamp,
                    NMSMessageId = msg.NMSMessageId
                };

                foreach (String key in msg.Properties.Keys) {
                    activeMqMessage.SetObjectProperty(key, msg.Properties[key]);
                }

                this.OnMessageReceived(activeMqMessage);
            }
        }
    }
}
