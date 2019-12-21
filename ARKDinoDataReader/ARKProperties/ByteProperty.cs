using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class ByteProperty : BaseProperty
    {
        public bool isNormalByte;

        //If isNormalByte == true
        public byte byteValue;

        //If isNormalByte == false
        public string enumName;
        public string enumValue;

        public override void Read(IOMemoryStream ms)
        {
            //Read first enum
            enumName = ms.ReadUEString();

            //If this is None, this is a normal byte
            isNormalByte = enumName == "None";

            //Read accordingly
            if (isNormalByte)
                byteValue = ms.ReadByte();
            else
                enumValue = ms.ReadUEString(); //Untested! Todo
        }
    }
}
