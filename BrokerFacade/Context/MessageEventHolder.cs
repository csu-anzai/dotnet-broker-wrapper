using BrokerFacade.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BrokerFacade.Context
{
    public class MessageEventHolder
    {
        public static AsyncLocal<CloudEvent> MessageEvent = new AsyncLocal<CloudEvent>();
    }
}
