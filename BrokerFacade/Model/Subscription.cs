using BrokerFacade.Abstractions;
using BrokerFacade.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Model
{
    public class Subscription
    {
        public string Topic { get; set; }
        public string SubscriptionName { get; set; }
        public IMessageEventHandler Handler { get; set; }
        public bool Durable { get; set; } = true;
    }
}
