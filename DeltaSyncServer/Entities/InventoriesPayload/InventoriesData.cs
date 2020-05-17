using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.InventoriesPayload
{
    //This is embedded in another request, usually
    public class InventoriesData
    {
        public InventoriesData_Item[] items;
    }

    public class InventoriesData_Item
    {
        public uint i1;
        public uint i2;
        public string c; //Classname
        public float d; //Durability
        public int q; //Stack size

        public string cname; //Custom name
        public string cryo; //Cryo data
        public bool tek; //Is tek
        public bool blp; //Is blueprint
    }
}
