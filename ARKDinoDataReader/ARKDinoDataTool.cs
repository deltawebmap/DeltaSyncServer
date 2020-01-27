using ARKDinoDataReader.ARKProperties;
using ARKDinoDataReader.Entities;
using ARKDinoDataReader.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace ARKDinoDataReader
{
    public static class ARKDinoDataTool
    {
        public static ARKDinoDataObject[] ReadData(byte[] content)
        {
            //Open stream
            IOMemoryStream stream = new IOMemoryStream(new System.IO.MemoryStream(content), true);

            //Read number of values
            int objectCount = stream.ReadInt();

            //Read object headers
            ARKDinoDataObject[] objects = new ARKDinoDataObject[objectCount];
            for (int i = 0; i < objectCount; i++)
                objects[i] = ARKDinoDataObject.ReadFromFile(stream);

            //Now, read payloads for all of these objects
            foreach(ARKDinoDataObject o in objects)
            {
                stream.position = o.payloadStart;
                o.properties = new List<BaseProperty>();
                BaseProperty prop = BaseProperty.ReadProperty(stream);
                while (prop != null)
                {
                    o.properties.Add(prop);
                    prop = BaseProperty.ReadProperty(stream);
                }
            }

            //Now, link all of the properties
            foreach(ARKDinoDataObject o in objects)
            {
                foreach (BaseProperty prop in o.properties)
                    prop.Link(objects);
            }

            return objects;
        }
    }
}
