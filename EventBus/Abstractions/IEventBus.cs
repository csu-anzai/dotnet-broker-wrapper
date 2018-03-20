using Asseco.EventBus.Events;
using System;

namespace Asseco.EventBus.Abstractions
{
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);
        void Subscribe<T>(string topic, IIntegrationEventHandler<T> integrationEventHandler) where T : IntegrationEvent;
        void Unsubscribe<T>(string topic, IIntegrationEventHandler<T> integrationEventHandler) where T : IntegrationEvent;
        void SubscribeDynamic(string topic, IIntegrationEventHandler<dynamic> handler);
        void UnsubscribeDynamic(string topic, IIntegrationEventHandler<dynamic> handler);
    }
}
