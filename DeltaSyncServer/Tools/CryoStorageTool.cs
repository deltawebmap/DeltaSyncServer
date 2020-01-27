using ARKDinoDataReader;
using ARKDinoDataReader.ARKProperties;
using ARKDinoDataReader.Entities;
using DeltaSyncServer.Entities;
using DeltaSyncServer.Entities.CryoPayload;
using DeltaSyncServer.Entities.DinoPayload;
using DeltaSyncServer.Tools;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.Entities.ArkEntries.Dinosaur;
using LibDeltaSystem.RPC.Payloads;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static LibDeltaSystem.RPC.Payloads.RPCPayloadDinosaurUpdateEvent;

namespace DeltaSyncServer.Tools
{
    public static class CryoStorageTool
    {
        public static DbDino QueueDino(List<WriteModel<DbDino>> dinoActions, out DinosaurEntry entry, DbServer server, string inventoryId, ulong revisionId, byte revisionIndex, int inventoryType, ulong inventoryItemId, DeltaPrimalDataPackage pack, string request)
        {
            //Parse the dino data
            DbDino dino = ParseDinoData(out entry, server, inventoryId, revisionId, revisionIndex, inventoryType, inventoryItemId, pack, request);
            if (dino == null)
                return null;

            //Enqueue adding this dino
            {
                //Create filter for updating this dino
                var filterBuilder = Builders<DbDino>.Filter;
                var filter = filterBuilder.Eq("dino_id", dino.dino_id) & filterBuilder.Eq("server_id", server.id);

                //Now, add (or insert) this into the database
                var a = new ReplaceOneModel<DbDino>(filter, dino);
                a.IsUpsert = true;
                dinoActions.Add(a);
            }

            return dino;
        }

        private static DbDino ParseDinoData(out DinosaurEntry entry, DbServer server, string inventoryId, ulong revisionId, byte revisionIndex, int inventoryType, ulong inventoryItemId, DeltaPrimalDataPackage pack, string request)
        {
            //Decode data as bytes and read
            byte[] data = new byte[request.Length / 2];
            for (int index = 0; index < data.Length; index++)
                data[index] = byte.Parse(request.Substring(index * 2, 2), System.Globalization.NumberStyles.HexNumber);

            //Now, decode and parse
            ARKDinoDataObject[] parts = ARKDinoDataTool.ReadData(data);
            Console.WriteLine(JsonConvert.SerializeObject(parts));

            //Get dino part
            ARKDinoDataObject d = parts[0];

            //Get this dino entry
            entry = pack.GetDinoEntry(d.name);
            if (entry == null)
                return null;

            //Convert colors
            List<string> colors = new List<string>();
            for (int i = 0; true; i++)
            {
                ByteProperty colorProp = d.GetPropertyByName<ByteProperty>("ColorSetIndices", i);
                if (colorProp == null)
                    break;
                byte color = colorProp.byteValue;
                if (color <= 0 || color > ArkStatics.ARK_COLOR_IDS.Length)
                    colors.Add("#FFFFFF");
                else
                    colors.Add(ArkStatics.ARK_COLOR_IDS[colorProp.byteValue - 1]); //Look this up in the color table to get the nice HTML value.
            }

            //Get status component
            ARKDinoDataObject status = d.GetPropertyByName<ObjectProperty>("MyCharacterStatusComponent").localLinkObject;

            //Read as dino, adding required fields first
            DbDino dino = new DbDino
            {
                revision_id = revisionId,
                revision_type = revisionIndex,
                is_cryo = true,
                cryo_inventory_id = inventoryId,
                dino_id = Program.GetMultipartID(d.GetPropertyByName<UInt32Property>("DinoID1").value, d.GetPropertyByName<UInt32Property>("DinoID2").value),
                tribe_id = d.GetPropertyByName<IntProperty>("TargetingTeam").value,
                tamed_name = d.GetStringProperty("TamedName", 0, entry.screen_name),
                classname = Program.TrimArkClassname(d.name),
                tamer_name = d.GetStringProperty("TamerString", 0, ""),
                baby_age = d.GetFloatProperty("BabyAge", 0, 1),
                is_baby = d.GetBoolProperty("bIsBaby", 0, false),
                next_imprint_time = d.GetDoubleProperty("BabyNextCuddleTime", 0, 0),
                imprint_quality = d.GetFloatProperty("DinoImprintingQuality", 0, 0),
                colors = colors.ToArray(),
                experience = status.GetFloatProperty("ExperiencePoints", 0, 0),
                server_id = server.id,
                location = new DbLocation(0, 0, 0),
                is_female = d.GetBoolProperty("bIsFemale", 0, false),
                level = status.GetIntProperty("BaseCharacterLevel", 0, 0) + status.GetUInt16Property("ExtraCharacterLevel", 0, 0),
                base_level = status.GetIntProperty("BaseCharacterLevel", 0, 0),
                is_tamed = true,
                taming_effectiveness = 1,
                max_stats = ConvertStatsFloat(status, "MaxStatusValues"),
                current_stats = ConvertStatsFloat(status, "CurrentStatusValues"),
                tamed_levelups_applied = ConvertStatsInt(status, "NumberOfLevelUpPointsAppliedTamed"),
                base_levelups_applied = ConvertStatsInt(status, "NumberOfLevelUpPointsApplied"),
                status = ConvertStatus(d),
                cryo_inventory_type = inventoryType,
                cryo_inventory_itemid = inventoryItemId,
                experience_points = status.GetFloatProperty("ExperiencePoints", 0, 0)
            };

            return dino;
        }

        private static string ConvertStatus(ARKDinoDataObject d)
        {
            string status = "YOUR_TARGET";
            if (d.HasProperty("TamedAggressionLevel"))
            {
                int agroLevel = d.GetIntProperty("TamedAggressionLevel");
                switch (agroLevel)
                {
                    case 0:
                        status = "PASSIVE";
                        break;
                    case 1:
                        status = "NEUTRAL";
                        break;
                    case 2:
                        status = "AGGRESSIVE";
                        break;
                }
                if (d.GetBoolProperty("bPassiveFlee", 0, false))
                {
                    status = "PASSIVE_FLEE";
                }
            }
            return status;
        }

        private static int[] ConvertStatsInt(ARKDinoDataObject c, string name)
        {
            int[] s = new int[15];
            var props = c.GetPropertiesByName(name);
            foreach (var p in props)
            {
                int index = p.index;
                int data;
                if (p.GetType() == typeof(ByteProperty))
                    data = ((ByteProperty)p).byteValue;
                else if (p.GetType() == typeof(IntProperty))
                    data = ((IntProperty)p).value;
                else
                    throw new Exception("Unexpected type for converting stats!");
                if (index > s.Length)
                    throw new Exception("Unexpected index for converting stats!");
                s[index] = data;
            }
            return s;
        }

        private static float[] ConvertStatsFloat(ARKDinoDataObject c, string name)
        {
            float[] s = new float[15];
            var props = c.GetPropertiesByName(name);
            foreach (var p in props)
            {
                int index = p.index;
                float data;
                if (p.GetType() == typeof(FloatProperty))
                    data = ((FloatProperty)p).value;
                else
                    throw new Exception("Unexpected type for converting stats!");
                if (index > s.Length)
                    throw new Exception("Unexpected index for converting stats!");
                s[index] = data;
            }
            return s;
        }
    }
}
