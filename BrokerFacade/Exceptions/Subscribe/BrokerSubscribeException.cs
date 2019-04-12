using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Exceptions.Subscribe
{
    public class BrokerSubscribeException : BrokerException
    {
        public BrokerSubscribeException(string message) : base(message)
        {
        }
    }
}
