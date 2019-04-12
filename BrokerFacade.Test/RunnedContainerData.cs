using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Test
{
    public class RunnedContainerData
    {
        public string TargetPort { get; set; }
        public string Id { get; set; }
    }

    public enum ContainerType
    {
        ARTEMIS,
        NATS,
        RABBITMQ
    }
}
