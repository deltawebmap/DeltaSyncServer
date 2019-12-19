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
        public StructureInventoryData inventory;
    }
}
