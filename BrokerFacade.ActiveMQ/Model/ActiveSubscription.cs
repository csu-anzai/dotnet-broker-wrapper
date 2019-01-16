using Amqp;
using BrokerFacade.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.ActiveMQ.Model
{

    public class ActiveSubscription : Subscription
    {
        public ReceiverLink Link { get; set; }
    }
}
