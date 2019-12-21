using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class BoolProperty : BaseProperty
    {
        public bool value;

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadByte() == 1;
        }
    }
}
