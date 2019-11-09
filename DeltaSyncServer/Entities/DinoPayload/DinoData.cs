using LibDeltaSystem.Db.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.DinoPayload
{
    public class DinoData
    {
        public int tribe_id;
        public string id_1;
        public string id_2;
        public bool is_female;
        public byte[] colors;
        public string name;
        public string tamer;
        public string classname;
        public Dictionary<string, float> max_stats;
        public Dictionary<string, float> current_stats;
        public Dictionary<string, float> points_wild; //actually an int
        public Dictionary<string, float> points_tamed; //actually an int
        public int extra_level;
        public int base_level;
        public float experience;
        public bool baby;
        public float baby_age;
        public double next_cuddle;
        public float imprint_quality;
        public DbLocation location;
        public string status;
        public DinoItem[] items;
    }
}
