using Asseco.EventBus.Events;
using System.Threading.Tasks;

namespace Asseco.EventBus.Abstractions
{
    public interface IIntegrationEventHandler<T>
    {
        Task Handle(T @event);
    }

}
