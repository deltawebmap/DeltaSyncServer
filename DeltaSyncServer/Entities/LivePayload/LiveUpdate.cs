using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.LivePayload
{
    public class LiveUpdate
    {
        //REQUIRED
        [JsonProperty("tid")]
        public int tribeId;
        public string id1;
        public string id2;
        [JsonProperty("ty")]
        public int type;

        //DATA
        public float? x;
        public float? y;
        public float? z;
        [JsonProperty("r")]
        public float? yaw;
        [JsonProperty("sh")]
        public float? health;
        [JsonProperty("ss")]
        public float? stamina;
        [JsonProperty("sw")]
        public float? weight;
        [JsonProperty("sf")]
        public float? food;
    }
}
