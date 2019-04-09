using BrokerFacade.Attributes;
using BrokerFacade.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SerializationUtils.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BrokerFacade.Serialization
{
    public class LowercaseContractResolver : DefaultContractResolver
    {

        // Using an explicit static constructor enables lazy initialization.
        static LowercaseContractResolver() { Instance = new LowercaseContractResolver(); }
        public static LowercaseContractResolver Instance { get; private set; }

        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }
    public class MessageEventSerializer
    {

        private static readonly JsonSerializerSettings settingsNoHeaders = new JsonSerializerSettings
        {
            ContractResolver = HeaderIgnoreSerializer.Instance,
            Converters =
            {
                new Newtonsoft.Json.Converters.StringEnumConverter()
            }
        };

        private static readonly JsonSerializerSettings settingsWithHeaders = new JsonSerializerSettings
        {
            ContractResolver = LowercaseContractResolver.Instance,
            Converters =
            {
                new Newtonsoft.Json.Converters.StringEnumConverter()
            }
        };

        public static Dictionary<string, object> GetMessageEventHeaders(CloudEvent messageEvent)
        {
            Dictionary<string, object> headers = new Dictionary<string, object>();
            var listProps = messageEvent.GetType().GetProperties()
                .Where(x => x.GetCustomAttribute(typeof(Header)) != null).ToList();

            Dictionary<string, object> headersDefinition = new Dictionary<string, object>();
            var listPropsheadersDefinition = messageEvent.CloudEventDefinition.GetType().GetProperties()
                .Where(x => x.GetCustomAttribute(typeof(Header)) != null).ToList();
            foreach (PropertyInfo property in listProps)
            {
                var value = property.GetValue(messageEvent);
                // TODO: Should header be serialized to Kebab case or not ?
                var key = property.Name;
                headers.Add(key.ToLower(), value);
            }
            foreach (PropertyInfo property in listPropsheadersDefinition)
            {
                var value = property.GetValue(messageEvent.CloudEventDefinition);
                // TODO: Should header be serialized to Kebab case or not ?
                var key = property.Name;
                headers.Add(key.ToLower(), value);
            }
            return headers;
        }

        public static string SerializeEventBody(CloudEvent messageEvent)
        {
            return JsonConvert.SerializeObject(messageEvent, typeof(CloudEvent), settingsNoHeaders);
        }

        public static string SerializeCustomEventBody(object obj)
        {
            return JsonConvert.SerializeObject(obj, settingsNoHeaders);
        }
        public static CloudEvent GetEventObject(string messageText, Dictionary<string, object> headers)
        {
            string kind = headers.ContainsKey("type") ? headers["type"].ToString() : null;
            kind = kind ?? JObject.Parse(messageText)["type"]?.ToString();
            if (kind == null)
            {
                return null;
            }
            // Getting Message Events that has Kind defined
            var kindObjects = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t => t.GetTypes())
                    .Where(t => t.IsClass &&
                                t.IsSubclassOf(typeof(CloudEvent)) &&
                                t.GetCustomAttribute<CloudEventDefinition>() != null).ToList();

            // Matching event kind
            Type kindObject = null;
            foreach (Type obj in kindObjects)
            {
                var testKindObject = obj.GetCustomAttribute<CloudEventDefinition>();
                if (testKindObject != null && testKindObject.Type.Equals(kind))
                {
                    kindObject = obj;
                }
            }
            if (kindObject == null)
            {
                return null;
            }

            // Deserialize body
            var messageObject = JsonConvert.DeserializeObject(messageText, kindObject, settingsNoHeaders);

            // Deserialize headers
            AddCustomHeaders(messageObject, headers);
            return (CloudEvent)messageObject;
        }

        private static void AddCustomHeaders(object messageObject, Dictionary<string, object> headers)
        {
            var listProps = messageObject.GetType().GetProperties()
              .Where(x => x.GetCustomAttribute(typeof(Header)) != null).ToList();
            foreach (PropertyInfo property in listProps)
            {
                var lowerName = property.Name.ToLower();
                var valueToken = (headers.ContainsKey(lowerName)) ? headers[lowerName] : null;
                if (valueToken != null)
                {
                    if (property.PropertyType.Name.Equals("DateTime"))
                    {
                        property.SetValue(messageObject, DateTime.Parse(valueToken.ToString()));
                    }
                    else
                    {
                        property.SetValue(messageObject, valueToken);
                    }
                }
            }
        }


        public static object MapJTokenValue(JToken value)
        {
            if (value == null)
            {
                return null;
            }

            switch (value.Type)
            {
                case JTokenType.Date:
                    return value.Value<DateTime>();
                case JTokenType.Guid:
                    return Guid.Parse(value.Value<string>());
                case JTokenType.String:
                    return value.Value<string>();
                case JTokenType.Uri:
                    return value.Value<string>();
                case JTokenType.TimeSpan:
                    return value.Value<TimeSpan>();
                case JTokenType.Null:
                    return null;
                case JTokenType.Undefined:
                    return null;
                case JTokenType.Integer:
                    return value.Value<int>();
                case JTokenType.Float:
                    return value.Value<double>();
                case JTokenType.Boolean:
                    return value.Value<bool>();
                default:
                    return null;
            }
        }
    }
}
