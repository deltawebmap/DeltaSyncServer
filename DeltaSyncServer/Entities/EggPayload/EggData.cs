using LibDeltaSystem.Db.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.EggPayload
{
    public class EggData
    {
        public float min_temp;
        public float max_temp;
        public float health;
        public float incubation;
        public int tribe_id;
        public bool hot;
        public bool cold;
        public float time_remaining; //In seconds
        public float temp; //Temperature
        public DbLocation pos;
        public string type; //Type of dino
        public string parents;
        public uint id1;
        public uint id2;
    }
}
