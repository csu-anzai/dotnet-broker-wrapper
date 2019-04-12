using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerFacade.Exceptions
{
    public class BrokerException : SystemException
    {
        public BrokerException(string message) : base(message)
        {
        }
    }
}
