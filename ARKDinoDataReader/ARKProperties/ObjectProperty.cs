using System;
using System.Collections.Generic;
using System.Text;
using ARKDinoDataReader.Entities;
using ARKDinoDataReader.Tools;

namespace ARKDinoDataReader.ARKProperties
{
    public class ObjectProperty : BaseProperty
    {
        public bool isLocalLink; //If this is true, we map to an index of another object. If this is false, this links to a game path

        //Only set if isLocalLink == true
        public int localLinkIndex;
        public ARKDinoDataObject localLinkObject;

        //Only set if isLocalLink == false
        public string gameLinkIndex;

        public override void Read(IOMemoryStream ms)
        {
            if (length != 8)
                throw new Exception("Unexpected ObjectProperty length: " + length);

            //Check type
            int linkType = ms.ReadInt();
            if (linkType != 0 && linkType != 1)
                throw new Exception("Unexepcted link type: " + linkType);

            //Read
            isLocalLink = linkType == 0;
            if (isLocalLink)
                localLinkIndex = ms.ReadInt();
            else
                gameLinkIndex = ms.ReadUEString(); //Not tested
        }

        public override void Link(ARKDinoDataObject[] data)
        {
            if (isLocalLink)
                localLinkObject = data[localLinkIndex];
        }
    }
}
