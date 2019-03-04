using BrokerFacade.Abstractions;
using BrokerFacade.Model;

namespace BrokerFacade.Interfaces
{
    public interface IBrokerFacade
    {
        Subscription Subscribe(string topicMatchExpression, string subscriptionName, AbstractMessageEventHandler handler);
        Subscription Subscribe(string topicMatchExpression, string subscriptionName, bool durable, AbstractMessageEventHandler handler);
        void Publish(string topic, MessageEvent messageEvent);
        void Unsubscribe(Subscription subscription);
        bool IsConnected();
    }
}
