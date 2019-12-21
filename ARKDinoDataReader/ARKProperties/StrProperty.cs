using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class StrProperty : BaseProperty
    {
        public string value;

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadUEString();
        }
    }
}
