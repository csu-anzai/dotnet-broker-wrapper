using System;
using System.Collections.Generic;

namespace BrokerFacade.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CloudEventDefinition : Attribute
    {
        [Header]
        public string Type { get; set; }
        [Header]
        public string ContentType { get; set; }
        [Header]
        public string SpecVersion { get; } = "0.2";

        public CloudEventDefinition(string type)
        {
            this.Type = type;
            ContentType = "application/json";
        }

        public CloudEventDefinition(string type,  string contentType) : this(type)
        {
            this.ContentType = contentType;
        }
    }
}
