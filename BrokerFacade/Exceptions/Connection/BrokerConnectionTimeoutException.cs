using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Exceptions.Connection
{
    public class BrokerConnectionTimeoutException : BrokerConnectionException
    {
        public BrokerConnectionTimeoutException(string message) : base(message)
        {
        }
    }
}
