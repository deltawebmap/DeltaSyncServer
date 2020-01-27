using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.ItemsPayload
{
    public class ItemData
    {
        public int iid1; //Inventory ID 1
        public int iid2; //Inventory ID 2
        public byte it; //Inventory type
        public bool imp; //Inventory ID multipart
        public int tribe;
        public int count;
        public int id1;
        public int id2;
        public float durability;
        public string classname;
        public string cryo; //May or may not be set
    }
}
