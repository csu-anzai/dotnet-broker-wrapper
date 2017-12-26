using System;
using System.Collections.Generic;


namespace Asseco.EventBus.Events
{
    public class MessageEvent : IntegrationEvent, Abstractions.IIntegrationEventHandler 
    {
        private String text;
        private String eventType;
        private Dictionary<String, Object> properties = new Dictionary<String, Object>();
        public Boolean objectValue;

        public MessageEvent(String eventType, String text)
        {
            this.text = text;
            this.eventType = eventType;
        }

        public MessageEvent(String eventType, String text, String id, DateTime creationDate)
            : base()
        {
            this.text = text;
            this.eventType = eventType;
        }

        public String getText()
        {
            return text;
        }

        public void setText(String text)
        {
            this.text = text;
        }

        public String getEventType()
        {
            return eventType;
        }

        public void setEventType(String eventType)
        {
            this.eventType = eventType;
        }

        public String toString()
        {
            return eventType;
        }

        public Dictionary<String, Object> getProperties()
        {
            return properties;
        }

        public Boolean getBooleanProperty(String name)
        {
            return (Boolean)this.properties[name];
        }

        public byte getByteProperty(String name)
        {
            return (Byte)this.properties[name];
        }

        public double getDoubleProperty(String name)
        {
            return (Double)this.properties[name];
        }

        public float getFloatProperty(String name)
        {
            return (float)this.properties[name];
        }

        public int getIntProperty(String name)
        {
            return (int)this.properties[name];
        }

        public String getStringProperty(String name)
        {
            return (String)this.properties[name];
        }

        public short getShortProperty(String name)
        {
            return (short)this.properties[name];
        }

        public long getLongProperty(String name)
        {
            return (long)this.properties[name];
        }

        public Object getObjectProperty(String name)
        {
            return (Object)this.properties[name];
        }

        public void setBooleanProperty(String name, Boolean value)
        {
            this.properties.Add(name, value);
        }

        public void setByteProperty(String name, Byte value)
        {
            this.properties.Add(name, value);
        }

        public void setDoubleProperty(String name, Double value)
        {
            this.properties.Add(name, value);
        }

        public void setFloatProperty(String name, float value)
        {
            this.properties.Add(name, value);
        }

        public void setIntProperty(String name, int value)
        {
            this.properties.Add(name, value);
        }

        public void setLongProperty(String name, long value)
        {
            this.properties.Add(name, value);
        }

        public void setObjectProperty(String name, Object value)
        {
            this.properties.Add(name, value);
        }

        public void setShortProperty(String name, short value)
        {
            this.properties.Add(name, value);
        }

        public void setStringProperty(String name, String value)
        {
            this.properties.Add(name, value);
        }
    }
}
