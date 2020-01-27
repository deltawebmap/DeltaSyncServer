using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Entities;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class ArrayProperty : BaseProperty
    {
        public string arrayType;

        public override void Link(ARKDinoDataObject[] data)
        {
            base.Link(data);
        }

        public override void Read(IOMemoryStream ms)
        {
            //We don't care much about arrays, so we'll skip them.

            //Read the type of the array
            arrayType = ms.ReadUEString();

            //Skip
            ms.position += length;
        }
    }
}
