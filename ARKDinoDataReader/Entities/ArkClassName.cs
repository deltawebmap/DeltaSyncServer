using ARKDinoDataReader.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARKDinoDataReader.Entities
{
    public class ArkClassName
    {
        public string classname;
        public int index;

        public static ArkClassName Create(string name, int index = 0)
        {
            return new ArkClassName
            {
                classname = name,
                index = index
            };
        }

        public static ArkClassName ReadFromFile(IOMemoryStream ms)
        {
            ArkClassName cn = new ArkClassName();
            cn.classname = ms.ReadUEString();
            cn.index = ms.ReadInt();
            return cn;
        }

        public static ArkClassName ReadFromFileInline(IOMemoryStream ms)
        {
            ArkClassName cn = new ArkClassName();
            //Read classname from the file.
            cn.classname = ms.ReadUEString();
            return cn;
        }

        public bool CompareNameTo(string n)
        {
            return n == classname;
        }

        public bool IsNone()
        {
            return classname == "None";
        }
    }
}
