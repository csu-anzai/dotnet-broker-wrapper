using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Exceptions.Connection
{
    public class BrokerBadCredentialsException : BrokerConnectionException
    {
        public BrokerBadCredentialsException(string message) : base(message)
        {
        }
    }
}
