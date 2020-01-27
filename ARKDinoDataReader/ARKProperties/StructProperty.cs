using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ARKDinoDataReader.Entities;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class StructProperty : BaseProperty
    {
        public string structType;

        public override void Link(ARKDinoDataObject[] data)
        {
            base.Link(data);
        }

        public override void Read(IOMemoryStream ms)
        {
            //Read type
            structType = ms.ReadUEString();

            //Skip
            ms.position += length;
        }
    }
}
