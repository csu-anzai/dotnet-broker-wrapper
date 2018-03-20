using Asseco.EventBus.Abstractions;
using Asseco.EventBus.Events;
using System;
using System.Collections.Generic;
using static Asseco.EventBus.InMemoryEventBusSubscriptionsManager;

namespace Asseco.EventBus
{
    public interface IEventBusSubscriptionsManager
    {
        bool IsEmpty { get; }
        event EventHandler<string> OnEventRemoved;
        void AddDynamicSubscription(string eventName, IIntegrationEventHandler<dynamic> handler);
        void AddSubscription<T>(string eventName, IIntegrationEventHandler<T> integrationEventHandler)
           where T : IntegrationEvent;
        void RemoveSubscription<T>(IIntegrationEventHandler<T> integrationEventHandler)
             where T : IntegrationEvent;
        void RemoveDynamicSubscription(string eventName, IIntegrationEventHandler<dynamic> handler);
        bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent;
        bool HasSubscriptionsForEvent(string eventName);
        Type GetEventTypeByName(string eventName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
        string GetEventKey<T>();
    }
}