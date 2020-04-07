using DeltaSyncServer.Entities.DinoPayload;
using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Services.v2
{
    public class DinosRequestV2 : InjestServerV2SyncService<DinoData, DbDino>
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

        public override DbDino CreateNewEntry(DinoData d, ulong revision_id, byte revision_index)
        {
            DbDino dino = new DbDino
            {
                baby_age = d.baby_age,
                base_level = d.base_level,
                base_levelups_applied = ConvertToStatsInt(d.points_wild),
                classname = Program.TrimArkClassname(d.classname),
                color_indexes = d.colors,
                current_stats = ConvertToStatsFloat(d.current_stats),
                max_stats = ConvertToStatsFloat(d.max_stats),
                dino_id = GetDinoId(d),
                experience = d.experience,
                imprint_quality = d.imprint_quality,
                is_baby = d.baby,
                is_female = d.is_female,
                is_tamed = true,
                level = d.base_level + d.extra_level,
                location = d.location,
                next_imprint_time = d.next_cuddle,
                revision_id = revision_id,
                revision_type = revision_index,
                server_id = server._id,
                status = d.status,
                tamed_levelups_applied = ConvertToStatsInt(d.points_tamed),
                tamed_name = d.name,
                tamer_name = d.tamer,
                taming_effectiveness = 0,
                tribe_id = d.tribe_id,
                experience_points = d.experience,
                is_alive = d.current_stats["0"] > 0,
                is_cryo = false,
                last_sync_time = DateTime.UtcNow,
                last_update_time = DateTime.UtcNow,
                prefs = new LibDeltaSystem.Db.System.Entities.SavedDinoTribePrefs()
            };
            return dino;
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

        public override int GetTribeIdFromItem(DbDino item)
        {
            return item.tribe_id;
        }

        public override object GetRPCVersionOfItem(DbDino item)
        {
            return LibDeltaSystem.Entities.CommonNet.NetDino.ConvertDbDino(item);
        }
    }
}
