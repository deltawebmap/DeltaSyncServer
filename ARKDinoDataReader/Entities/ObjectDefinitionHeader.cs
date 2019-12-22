using ARKDinoDataReader.ARKProperties;
using ARKDinoDataReader.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARKDinoDataReader.Entities
{
    public class ARKDinoDataObject
    {
        public string name;
        public int unknown1;
        public int unknown2;
        public string type; //Seems to be blank for some?
        public string unknown3;
        public string unknown4;
        public string unknown5;
        public string unknown52;
        public int unknown6;
        public int unknown7;
        public int payloadStart;
        public int unknown8;

        public List<BaseProperty> properties;

        public static ARKDinoDataObject ReadFromFile(IOMemoryStream ms)
        {
            //Skip some unknown data, not even sure if this is part of the struct
            ms.position += 4 * 4;

            //Read
            ARKDinoDataObject h = new ARKDinoDataObject();
            h.name = ms.ReadUEString();
            h.unknown1 = ms.ReadInt();
            h.unknown2 = ms.ReadInt();
            h.type = ms.ReadUEString();
            h.unknown3 = ms.ReadUEString();
            h.unknown4 = ms.ReadUEString();
            h.unknown5 = ms.ReadUEString();
            if (h.unknown2 == 5) //Don't know why
                h.unknown52 = ms.ReadUEString();

            //Don't understand any of this section, but seems to work
            h.unknown6 = ms.ReadInt();
            h.unknown7 = ms.ReadInt();
            int test = ms.ReadInt();
            if(test == 1)
                ms.position += 6 * 4;

            //Read more
            h.payloadStart = ms.ReadInt();
            h.unknown8 = ms.ReadInt();

            return h;
        }

        public bool HasProperty(string name, int index = 0)
        {
            return GetBasePropertyByName(name, index) != null;
        }

        public BaseProperty GetBasePropertyByName(string name, int index = 0)
        {
            //Validate
            if (properties == null)
                return null;

            //Search
            foreach (BaseProperty p in properties)
            {
                if (p.index == index && p.name == name)
                    return p;
            }
            return null;
        }

        public List<BaseProperty> GetPropertiesByName(string name)
        {
            //Validate
            if (properties == null)
                return null;

            //Search
            List<BaseProperty> props = new List<BaseProperty>();
            foreach (BaseProperty p in properties)
            {
                if (p.name == name)
                    props.Add(p);
            }
            return props;
        }

        public T GetPropertyByName<T>(string name, int index = 0)
        {
            BaseProperty prop = GetBasePropertyByName(name, index);
            if (prop == null)
                return default(T);
            return (T)Convert.ChangeType(prop, typeof(T));
        }

        public string GetStringProperty(string name, int index = 0, string defaultValue = null)
        {
            StrProperty p = GetPropertyByName<StrProperty>(name, index);
            if (p == null && defaultValue == null)
                throw new Exception($"No values found for {name}:{index}, and no defaults were given!");
            else if (p == null)
                return defaultValue;
            else
                return p.value;
        }

        public int GetIntProperty(string name, int index = 0, int? defaultValue = null)
        {
            IntProperty p = GetPropertyByName<IntProperty>(name, index);
            if (p == null && defaultValue == null)
                throw new Exception($"No values found for {name}:{index}, and no defaults were given!");
            else if (p == null)
                return defaultValue.Value;
            else
                return p.value;
        }

        public float GetFloatProperty(string name, int index = 0, float? defaultValue = null)
        {
            FloatProperty p = GetPropertyByName<FloatProperty>(name, index);
            if (p == null && defaultValue == null)
                throw new Exception($"No values found for {name}:{index}, and no defaults were given!");
            else if (p == null)
                return defaultValue.Value;
            else
                return p.value;
        }

        public bool GetBoolProperty(string name, int index = 0, bool? defaultValue = null)
        {
            BoolProperty p = GetPropertyByName<BoolProperty>(name, index);
            if (p == null && defaultValue == null)
                throw new Exception($"No values found for {name}:{index}, and no defaults were given!");
            else if (p == null)
                return defaultValue.Value;
            else
                return p.value;
        }

        public double GetDoubleProperty(string name, int index = 0, double? defaultValue = null)
        {
            DoubleProperty p = GetPropertyByName<DoubleProperty>(name, index);
            if (p == null && defaultValue == null)
                throw new Exception($"No values found for {name}:{index}, and no defaults were given!");
            else if (p == null)
                return defaultValue.Value;
            else
                return p.value;
        }
    }
}
