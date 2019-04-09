using BrokerFacade.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace BrokerFacade.Model
{
    public abstract class CloudEvent
    {
        [Header]
        public string Id { get; set; }
        [Header]
        public string Source { get; set; }
        [Header]
        public DateTime Time { get; set; }
        [Header]
        public string XRequestId { get; set; }
        [Header]
        public string XCorrelationId { get; set; }

        public CloudEvent()
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
        }

        [JsonIgnore]
        public CloudEventDefinition CloudEventDefinition
        {
            get
            {
                return (CloudEventDefinition)this.GetType().GetCustomAttributes(typeof(CloudEventDefinition), false).FirstOrDefault();
            }
        }
        [JsonExtensionData]
        public Dictionary<string, object> Extenstions { get; set; } = new Dictionary<string, object>();

    }
}
