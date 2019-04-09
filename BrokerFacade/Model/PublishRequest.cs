using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Model
{
    public class PublishRequest
    {
        public string Topic { get; set; }
        public CloudEvent CloudEvent { get; set; }
    }
}
