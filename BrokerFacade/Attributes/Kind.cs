using System;
namespace BrokerFacade.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Kind : Attribute
    {
        public string MessageKind { get; set; }

        public Kind(string messageKind)
        {
            MessageKind = messageKind;
        }
    }
}
