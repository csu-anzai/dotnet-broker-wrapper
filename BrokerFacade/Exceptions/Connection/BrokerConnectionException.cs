using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Exceptions.Connection
{
    public class BrokerConnectionException : BrokerException
    {
        public BrokerConnectionException(string message) : base(message)
        {
        }
    }
}
