using BrokerFacade.Model;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.RabbitMQ.Model
{
    public class ActiveSubscription : Subscription
    {
        public IModel Channel { get; set; }
    }
}
