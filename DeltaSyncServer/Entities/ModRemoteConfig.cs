using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities
{
    public class ModRemoteConfig
    {
        public int modules_dinos_chunksize = 5;
        public float modules_dinos_tickrate = 1f;
        public float modules_dinos_cyclebreak = 10;

        public int modules_inventories_chunksize = 40;
        public float modules_inventories_tickrate = 1f;
        public float modules_inventories_cyclebreak = 10;
    }
}
