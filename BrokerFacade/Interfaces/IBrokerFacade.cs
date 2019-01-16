using BrokerFacade.Abstractions;
using BrokerFacade.Model;

namespace BrokerFacade.Interfaces
{
    public interface IBrokerFacade
    {
        Subscription SubscribeShared(string topic, string subscriptionName, AbstractMessageEventHandler handler);
        Subscription Subscribe(string topic, AbstractMessageEventHandler handler);
        void Publish(string topic, MessageEvent messageEvent);
        void Unsubscribe(Subscription subscription);
        bool IsConnected();
    }
}
