using BrokerFacade.Abstractions;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;

namespace BrokerFacade.Abstractions
{
    public abstract class AbstractBrokerFacade : IBrokerFacade
    {
        public string Hostname { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; }
        public bool ConnectionEstablished = false;

        public AbstractBrokerFacade(string hostname, string port, string username, string password, string clientId)
        {
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;
            ClientId = clientId;
        }

        public abstract Subscription Subscribe(string topic, string subscriptionName, AbstractMessageEventHandler handler);
        public abstract Subscription Subscribe(string topic, string subscriptionName, bool durable, AbstractMessageEventHandler handler);
        public abstract void Publish(string topic, MessageEvent messageEvent);
        public abstract void Unsubscribe(Subscription subscription);

        public bool IsConnected()
        {
            return ConnectionEstablished;
        }
 }
}
