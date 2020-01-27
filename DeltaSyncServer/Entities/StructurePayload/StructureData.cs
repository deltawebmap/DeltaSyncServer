using LibDeltaSystem.Db.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.StructurePayload
{
    public class StructureData
    {
        public string classname;
        public int tribe;
        public DbLocation location;
        public float max_health;
        public float health;
        public int id;

        //Only set if this has an inventory
        public string name;
        public int max_items;
        public int item_count;
    }
}
