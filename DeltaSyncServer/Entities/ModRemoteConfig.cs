using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities
{
    public class ModRemoteConfig
    {
        public int modules_dinos_chunksize = 20;
        public float modules_dinos_tickrate = 1f;
        public float modules_dinos_cyclebreak = 10;

        public int modules_inventories_chunksize = 40;
        public float modules_inventories_tickrate = 1f;
        public float modules_inventories_cyclebreak = 10;

        public float modules_livedinos_tickrate = 1.25f;
        public float modules_livedinos_maxchunk = 40;
        public float modules_livedinos_maxchecked = 400;
        public float modules_livedinos_refreshtime = 30;
        public float[] modules_livedinos_tolerance =
        {
            0,
            500,
            500,
            8,
            8,
            8,
            8
        };

        public float modules_realtimeplayers_refreshtime = 5;
    }
}
