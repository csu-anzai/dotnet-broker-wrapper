using BrokerFacade.Model;

namespace BrokerFacade.Interfaces
{
    public interface IMessageEventHandler
    {
        void OnMessage(MessageEvent messageEvent);
    }
}
