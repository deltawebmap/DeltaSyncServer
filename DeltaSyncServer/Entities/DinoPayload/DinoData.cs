using DeltaSyncServer.Entities.InventoriesPayload;
using LibDeltaSystem.Db.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.DinoPayload
{
    public class DinoData
    {
        public int tribe_id;
        public int id_1; //actually a uint
        public int id_2; //actually a uint
        public bool is_female;
        public int[] colors;
        public string name;
        public string tamer;
        public string classname;
        public Dictionary<string, float> max_stats;
        public Dictionary<string, float> current_stats;
        public Dictionary<string, int> points_wild;
        public Dictionary<string, int> points_tamed;
        public int extra_level;
        public int base_level;
        public float experience;
        public bool baby;
        public float baby_age;
        public double next_cuddle;
        public float imprint_quality;
        public DbLocation location;
        public string status;
        public InventoriesData inventory;
    }
}
