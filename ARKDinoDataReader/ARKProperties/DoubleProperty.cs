using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class DoubleProperty : BaseProperty
    {
        public double value;

        public override void Read(IOMemoryStream ms)
        {
            value = ms.ReadDouble();
        }
    }
}
