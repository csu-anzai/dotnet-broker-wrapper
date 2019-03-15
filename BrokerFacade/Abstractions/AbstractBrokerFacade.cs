using BrokerFacade.Abstractions;
using BrokerFacade.Interfaces;
using BrokerFacade.Model;
using System;
using System.Threading;

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

        public int InitialConnectionAttempts = -1;
        public int ReconnectAttempts = -1;

        public int ConnectionTimeout = 1000;
        public int ReconnectTimeout = 200;

        public int SendTimeout = 500;
        public int ReceiveTimeout = 200;

        public int RetryTimeout = 50;
        public int SendRetries = 1;

        public delegate void ConnectionState();
        public abstract event ConnectionState Connected;
        public abstract event ConnectionState ConnectionLost;
        public event ConnectionState ConnectionFailed;
        public abstract event ConnectionState ReconnectionStarted;

        public AbstractBrokerFacade(
            string hostname,
            string port,
            string username,
            string password,
            string clientId
            )
        {
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;
            ClientId = clientId;
        }

        public abstract Subscription Subscribe(string topic, string subscriptionName, AbstractMessageEventHandler handler);
        public abstract Subscription Subscribe(string topic, string subscriptionName, bool durable, AbstractMessageEventHandler handler);


        public void Connect()
        {
            ConnectWait(InitialConnectionAttempts);
        }

        public virtual void Reconnect()
        {
            ConnectWait(ReconnectAttempts);
        }

        public void ConnectWait(int waitArgument)
        {
            var connectionAttempts = 0;
            if (waitArgument == -1 || waitArgument > 0)
            {
                try
                {
                    while (!ConnectionEstablished)
                    {
                        ConnectInternal();
                        Thread.Sleep(ReconnectTimeout);
                    }
                }
                catch (Exception e)
                {
                    connectionAttempts++;
                    ConnectionFailed();
                    ConnectionEstablished = false;
                    if (waitArgument != -1 && connectionAttempts == waitArgument)
                    {
                        throw e;
                    }
                }
            }
            else
            {
                ConnectInternal();
            }
        }

        public void Publish(string topic, MessageEvent messageEvent)
        {
            if (SendRetries > 0)
            {
                for (int i = 1; i <= SendRetries; i++)
                {
                    try
                    {
                        PublishInternal(topic, messageEvent);
                    }
                    catch (Exception e)
                    {
                        if (i == SendRetries)
                        {
                            throw e;
                        }
                        if (RetryTimeout != 0)
                        {
                            Thread.Sleep(RetryTimeout);
                        }
                    }
                }
            }
            else
            {
                PublishInternal(topic, messageEvent);
            }
        }

        protected abstract void ConnectInternal();
        protected abstract void PublishInternal(string topic, MessageEvent messageEvent);
        public abstract void Unsubscribe(Subscription subscription);

        public bool IsConnected()
        {
            return ConnectionEstablished;
        }
    }
}
