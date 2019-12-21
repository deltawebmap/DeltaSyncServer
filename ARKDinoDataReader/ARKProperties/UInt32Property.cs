using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class UInt32Property : BaseProperty
    {
        public uint value;

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadUInt();
        }
    }
}
