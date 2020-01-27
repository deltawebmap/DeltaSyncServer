using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Entities;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class UInt16Property : BaseProperty
    {
        public ushort value;

        public override void Link(ARKDinoDataObject[] data)
        {
            base.Link(data);
        }

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadUShort();
        }
    }
}
