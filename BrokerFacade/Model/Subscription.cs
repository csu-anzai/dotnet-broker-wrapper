using BrokerFacade.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Model
{
    public class Subscription
    {
        public string Topic { get; set; }
        public string SubscriptionName { get; set; }
        public AbstractMessageEventHandler Handler { get; set; }
    }
}
