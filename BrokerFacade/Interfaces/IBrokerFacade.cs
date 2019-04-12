using BrokerFacade.Abstractions;
using BrokerFacade.Model;

namespace BrokerFacade.Interfaces
{
    public interface IBrokerFacade
    {
        Subscription Subscribe(string topicMatchExpression, string subscriptionName, IMessageEventHandler handler);
        Subscription Subscribe(string topicMatchExpression, string subscriptionName, bool durable, IMessageEventHandler handler);
        void Publish(string topic, CloudEvent messageEvent);
        void Unsubscribe(Subscription subscription);
        bool IsConnected();
        void Connect();
        void Disconnect();
    }
}
