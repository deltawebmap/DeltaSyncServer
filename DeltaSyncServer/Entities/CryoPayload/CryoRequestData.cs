using DeltaSyncServer.Entities.DinoPayload;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.CryoPayload
{
    public class CryoRequestData
    {
        public uint id1;
        public uint id2;
        public bool mp; //Is ID multipart?
        public byte rindex; //Revision ID index
        public ulong rid; //Revision ID
        public int it; //Inventory type
        public string[] d; //Cryo data
        public DinoItem[] md; //Items, mapped to indexes in d
    }
}
