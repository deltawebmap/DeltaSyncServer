using ARKDinoDataReader.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARKDinoDataReader.ARKProperties
{
    public class IntProperty : BaseProperty
    {
        public int value;

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadInt();
        }
    }
}
