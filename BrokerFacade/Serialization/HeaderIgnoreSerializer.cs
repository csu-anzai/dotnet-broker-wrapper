using BrokerFacade.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SerializationUtils.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BrokerFacade.Serialization
{
    public class HeaderIgnoreSerializer : DefaultContractResolver
    {
        static readonly HeaderIgnoreSerializer instance;

        // Using an explicit static constructor enables lazy initialization.
        static HeaderIgnoreSerializer() { instance = new HeaderIgnoreSerializer(); }

        public static HeaderIgnoreSerializer Instance { get { return instance; } }

        public HeaderIgnoreSerializer()
        {
            NamingStrategy = new KebabCaseNamingStrategy
            {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true
            };
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (member.GetCustomAttribute(typeof(Header)) != null)
            {
                property.ShouldSerialize = instance => { return false; };
                property.ShouldDeserialize = instance => { return false; };
            }
            return property;
        }
    }
}
