using BrokerFacade.Attributes;
using BrokerFacade.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SerializationUtils.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BrokerFacade.Serialization
{
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
            ContractResolver = KebabCasePropertyNameResolver.Instance,
            Converters =
            {
                new Newtonsoft.Json.Converters.StringEnumConverter()
            }
        };

        public static Dictionary<string, object> GetMessageEventHeaders(MessageEvent messageEvent)
        {
            Dictionary<string, object> headers = new Dictionary<string, object>();
            var listProps = messageEvent.GetType().GetProperties()
                .Where(x => x.GetCustomAttribute(typeof(Header)) != null).ToList();

            foreach (PropertyInfo property in listProps)
            {
                var value = property.GetValue(messageEvent);
                // TODO: Should header be serialized to Kebab case or not ?
                var key = property.Name;
                headers.Add(key, value);
            }
            headers.Add("kind", messageEvent.Kind);
            return headers;
        }

        public static string SerializeEventBody(MessageEvent messageEvent)
        {
            return JsonConvert.SerializeObject(messageEvent, typeof(MessageEvent), settingsNoHeaders);
        }

        public static string SerializeCustomEventBody(object obj)
        {
            return JsonConvert.SerializeObject(obj, settingsNoHeaders);
        }
        public static MessageEvent GetEventObject(string messageText, Dictionary<string, object> headers)
        {
            string kind = headers.ContainsKey("kind") ? headers["kind"].ToString() : null;
            kind = (kind == null) ? JObject.Parse(messageText)["kind"]?.ToString() : kind;
            if (kind == null)
            {
                return null;
            }
            // Getting Message Events that has Kind defined
            var kindObjects = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t => t.GetTypes())
                    .Where(t => t.IsClass &&
                                t.IsSubclassOf(typeof(MessageEvent)) &&
                                t.GetCustomAttribute<Kind>() != null).ToList();

            // Matching event kind
            Type kindObject = null;
            foreach (Type obj in kindObjects)
            {
                var testKindObject = obj.GetCustomAttribute<Kind>();
                if (testKindObject != null && testKindObject.MessageKind.Equals(kind))
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
            return (MessageEvent)messageObject;
        }

        private static void AddCustomHeaders(object messageObject, Dictionary<string, object> headers)
        {
            var listProps = messageObject.GetType().GetProperties()
              .Where(x => x.GetCustomAttribute(typeof(Header)) != null).ToList();
            foreach (PropertyInfo property in listProps)
            {
                var kebabName = CaseUtil.ToKebabCase(property.Name);
                // Support Kebab and Pascal case properties
                var valueToken = (headers.ContainsKey(kebabName)) ? headers[kebabName] : null;
                valueToken = (headers.ContainsKey(property.Name)) ? headers[property.Name] : valueToken;
                if (valueToken != null)
                {
                    // TODO: Type here can be simular so this must be tested
                    if (!property.Name.ToLower().Equals("kind"))
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
