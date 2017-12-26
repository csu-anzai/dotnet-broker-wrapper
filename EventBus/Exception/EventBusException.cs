namespace Asseco.EventBus.Exception
{
    public class EventBusException : System.Exception
    {
        public EventBusException()
        {
        }

        public EventBusException(string message)
            :base(message)                
        {
        }

        public EventBusException(string message, System.Exception cause)
            : base(message, cause)
        {
        }
    }
}
