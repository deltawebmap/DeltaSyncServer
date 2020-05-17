using DeltaSyncServer.Entities.DinoPayload;
using DeltaSyncServer.Entities.InventoriesPayload;
using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Entities.CommonNet;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Services.v2
{
    public class DinosRequestV2 : InjestServerV2SyncInventoryService<DinoData, DbDino>
    {
        public DinosRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        private ulong GetDinoId(DinoData data)
        {
            return Program.GetMultipartID((uint)data.id_1, (uint)data.id_2);
        }

        public override FilterDefinition<DbDino> CreateFilterDefinition(DinoData data)
        {
            var filterBuilder = Builders<DbDino>.Filter;
            var filter = filterBuilder.Eq("dino_id", GetDinoId(data)) & filterBuilder.Eq("server_id", server._id);
            return filter;
        }

        public override UpdateDefinition<DbDino> CreateUpdateDefinition(DinoData d, ulong revision_id, byte revision_index)
        {
            var builder = Builders<DbDino>.Update;
            return builder.Set("baby_age", d.baby_age)
                .Set("base_level", d.base_level)
                .Set("base_levelups_applied", ConvertToStatsInt(d.points_wild))
                .Set("classname", Program.TrimArkClassname(d.classname))
                .Set("color_indexes", d.colors)
                .Set("current_stats", ConvertToStatsFloat(d.current_stats))
                .Set("max_stats", ConvertToStatsFloat(d.max_stats))
                .SetOnInsert("dino_id", GetDinoId(d))
                .Set("experience", d.experience)
                .Set("imprint_quality", d.imprint_quality)
                .Set("is_baby", d.baby)
                .Set("is_female", d.is_female)
                .Set("is_tamed", true)
                .Set("level", d.base_level + d.extra_level)
                .Set("location", d.location)
                .Set("next_imprint_time", d.next_cuddle)
                .Set("revision_id", revision_id)
                .Set("revision_type", revision_index)
                .SetOnInsert("server_id", server._id)
                .Set("status", d.status)
                .Set("tamed_levelups_applied", ConvertToStatsInt(d.points_tamed))
                .Set("tamed_name", d.name)
                .Set("tamer_name", d.tamer)
                .Set("taming_effectiveness", 0)
                .Set("tribe_id", d.tribe_id)
                .Set("experience_points", d.experience)
                .Set("is_alive", d.current_stats["0"] > 0)
                .Set("last_sync_time", DateTime.UtcNow)
                .Set("last_update_time", DateTime.UtcNow)
                .SetOnInsert("prefs", new LibDeltaSystem.Db.System.Entities.SavedDinoTribePrefs())
                .Set("is_cryo", false);
        }

        public override IMongoCollection<DbDino> GetMongoCollection()
        {
            return conn.content_dinos;
        }

        private static DbArkDinosaurStats ConvertToDinoStats(Dictionary<string, float> data)
        {
            return new DbArkDinosaurStats
            {
                health = data["Health"],
                stamina = data["Stamina"],
                unknown1 = data["Torpidity"],
                oxygen = data["Oxygen"],
                food = data["Food"],
                water = data["Water"],
                unknown2 = data["Temperature"],
                inventoryWeight = data["Weight"],
                meleeDamageMult = data["MeleeDamageMultiplier"],
                movementSpeedMult = data["SpeedMultiplier"],
                unknown3 = data["TemperatureFortitude"],
                unknown4 = data["CraftingSpeedMultiplier"]
            };
        }

        private static int[] ConvertToStatsInt(Dictionary<string, int> data)
        {
            int[] values = new int[15];
            foreach (var v in data)
            {
                int key = int.Parse(v.Key);
                if (key < values.Length)
                    values[key] = v.Value;
            }
            return values;
        }

        private static float[] ConvertToStatsFloat(Dictionary<string, float> data)
        {
            float[] values = new float[15];
            foreach (var v in data)
            {
                int key = int.Parse(v.Key);
                if (key < values.Length)
                    values[key] = v.Value;
            }
            return values;
        }

        public override LibDeltaSystem.RPC.Payloads.Entities.RPCSyncType GetRPCContentUpdateType()
        {
            return LibDeltaSystem.RPC.Payloads.Entities.RPCSyncType.Dino;
        }

        public override int GetTribeIdFromItem(DinoData item)
        {
            return item.tribe_id;
        }

        public override object GetRPCVersionOfItem(DinoData dino)
        {
            return new NetDino
            {
                tribe_id = dino.tribe_id,
                dino_id = GetDinoId(dino).ToString(),
                is_female = dino.is_female,
                colors = dino.colors,
                colors_hex = new string[6],
                tamed_name = dino.name,
                tamer_name = dino.tamer,
                classname = Program.TrimArkClassname(dino.classname),
                current_stats = ConvertToStatsFloat(dino.current_stats),
                max_stats = ConvertToStatsFloat(dino.max_stats),
                base_levelups_applied = ConvertToStatsInt(dino.points_wild),
                tamed_levelups_applied = ConvertToStatsInt(dino.points_tamed),
                base_level = dino.base_level,
                level = dino.base_level + dino.extra_level,
                experience = dino.experience,
                is_baby = dino.baby,
                baby_age = dino.baby_age,
                next_imprint_time = dino.next_cuddle,
                imprint_quality = dino.imprint_quality,
                location = dino.location,
                status = dino.status,
                taming_effectiveness = 0,
                is_cryo = false,
                experience_points = dino.experience
            };
        }

        public override InventoriesData GetInventoryDataOfObject(DinoData obj)
        {
            return obj.inventory;
        }

        public override ulong GetArkIdOfObject(DinoData obj)
        {
            return Program.GetMultipartID((uint)obj.id_1, (uint)obj.id_2);
        }

        public override DbInventory.DbInventory_InventoryType GetStructureTypeOfObject(DinoData obj)
        {
            return DbInventory.DbInventory_InventoryType.Dino;
        }
    }
}
