using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Entities;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class UInt64Property : BaseProperty
    {
        public ulong value;

        public override void Link(ARKDinoDataObject[] data)
        {
            base.Link(data);
        }

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadULong();
        }
    }
}
