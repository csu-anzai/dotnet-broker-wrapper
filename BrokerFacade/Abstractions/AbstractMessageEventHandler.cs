using BrokerFacade.Context;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;

namespace BrokerFacade.Abstractions
{
    public abstract class AbstractMessageEventHandler : IMessageEventHandler
    {
        public abstract void OnMessage(MessageEvent messageEvent);

        public void OnMessageInternal(MessageEvent messageEvent)
        {
            OnStart(messageEvent);
            OnMessage(messageEvent);
            OnEnd(messageEvent);
        }

        private void OnStart(MessageEvent messageEvent) {
            MessageEventHolder.MessageEvent.Value = messageEvent;
        }

        private void OnEnd(MessageEvent messageEvent)
        {
            MessageEventHolder.MessageEvent.Value = null;
        }

    }
}
