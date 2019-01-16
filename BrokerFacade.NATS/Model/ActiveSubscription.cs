using BrokerFacade.Model;
using NATS.Client;
using STAN.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.NATS.Model
{
    public class ActiveSubscription : BrokerFacade.Model.Subscription
    {
        public IStanSubscription Channel { get; set; }
    }
}
