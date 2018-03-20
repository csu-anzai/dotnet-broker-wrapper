using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Asseco.EventBus.Abstractions;
using Asseco.EventBus.Events;

namespace EventBusActiveMQTest
{
    public class MessageEventHandler : IIntegrationEventHandler<MessageEvent>
    {
        Task IIntegrationEventHandler<MessageEvent>.Handle(MessageEvent eventData)
        {
            Debug.WriteLine("Message received: " + eventData.getText());
            foreach (String key in eventData.getProperties().Keys)
            {
                Debug.WriteLine(key + " : " + eventData.getProperties()[key]);
            }
            return Task.CompletedTask;
        }
    }

}
