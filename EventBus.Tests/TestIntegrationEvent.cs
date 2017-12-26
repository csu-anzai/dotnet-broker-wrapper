using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;
using System;
using System.Collections.Generic;
using System.Text;
using EventAMQP;

namespace EventBus.Tests
{
    public class TestIntegrationEvent : IntegrationEvent
    { 
        EventBusAMQP amqp = new EventBusAMQP();
    }
}
