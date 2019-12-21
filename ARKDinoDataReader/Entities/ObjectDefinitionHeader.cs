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
    }
}
