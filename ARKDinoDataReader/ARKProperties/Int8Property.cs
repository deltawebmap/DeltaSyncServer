using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class Int8Property : BaseProperty
    {
        public byte value;

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadByte();
        }
    }
}
