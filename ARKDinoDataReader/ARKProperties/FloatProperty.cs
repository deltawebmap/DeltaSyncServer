using ARKDinoDataReader.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARKDinoDataReader.ARKProperties
{
    public class FloatProperty : BaseProperty
    {
        public float value;

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadFloat();
        }
    }
}
