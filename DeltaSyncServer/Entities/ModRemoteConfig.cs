using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities
{
    public class ModRemoteConfig
    {
        public float profile_sync_policy = 30;
        public float tick_dino_queue_policy = 1;
        public float refresh_dino_queue_policy = 60*1;
        public string api = "http://sync-nossl-prod.deltamap.net/v1";
        public float tick_structure_queue_policy = 2;
        public float refresh_structure_queue_policy = 60*2;
        public int max_structure_sync_size = 30; //Number that can be sent at once
        public int max_dino_sync_size = 5; //Number that can be sent at once
        public float egg_sync_policy = 45;
        public float refresh_player_actor_queue_policy = 10;
        public float tick_player_actor_queue_policy = 1.5f;

        public float live_refresh_max_items = 512;
        public float live_refresh_tolerance_distance = 550;
        public float live_refresh_tolerance_rotation = 30;
        public float live_refresh_tolerance_stat_health = 10;
        public float live_refresh_tolerance_stat_stamina = 20;
        public float live_refresh_tolerance_stat_weight = 10;
        public float live_refresh_tolerance_stat_food = 5;
        public float live_refresh_interval = 1;
        public float live_update_objects_interval = 60;
    }
}
