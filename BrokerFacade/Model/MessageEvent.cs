using BrokerFacade.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrokerFacade.Model
{
    public abstract class MessageEvent
    {
        [Header]
        public string XRequestId { get; set; }

        [Header]
        public string XCorrelationId { get; set; }

        [Header]
        public string Topic { get; set; }

        [Header]
        public string Kind
        {
            get
            {
                try
                {
                    return ((Kind)this.GetType().GetCustomAttributes(typeof(Kind), false).FirstOrDefault()).MessageKind;
                }
                catch (SystemException)
                {
                    return null;
                }
            }
        }
    }
}
