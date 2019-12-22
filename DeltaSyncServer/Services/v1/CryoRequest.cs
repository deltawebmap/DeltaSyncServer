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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static LibDeltaSystem.RPC.Payloads.RPCPayloadDinosaurUpdateEvent;

namespace DeltaSyncServer.Services.v1
{
    public static class CryoRequest
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Decode
            CryoRequestData payload = Program.DecodeStreamAsJson<CryoRequestData>(e.Request.Body);

            //Get primal data
            var primal = await Program.primal_data.LoadFullPackage(server.mods);

            //Get inventory ID
            string inventoryId = payload.id1.ToString();
            if (payload.mp)
                inventoryId = Program.GetMultipartID(payload.id1, payload.id2).ToString();

            //Create queues
            List<WriteModel<DbDino>> dinoActions = new List<WriteModel<DbDino>>();
            List<WriteModel<DbItem>> itemActions = new List<WriteModel<DbItem>>();
            Dictionary<int, List<RPCPayloadDinosaurUpdateEvent_Dino>> rpcDinos = new Dictionary<int, List<RPCPayloadDinosaurUpdateEvent_Dino>>();

            //Handle all
            for (int i = 0; i<payload.d.Length; i++)
            {
                //Get parts
                DinoItem metadata = payload.md[i];

                //Parse the dino data
                DbDino dino = ParseDinoData(out DinosaurEntry entry, server, inventoryId, payload.rid, payload.rindex, payload.it, Program.GetMultipartID(metadata.id1, metadata.id2), primal, payload.d[i]);
                if (dino == null)
                    return;

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

                //Add item
                InventoryManager.QueueInventoryItem(itemActions, metadata, inventoryId, (DbInventoryParentType)payload.it, server.id, dino.tribe_id, payload.rid, payload.rindex, DbItem.CUSTOM_DATA_KEY__CRYO_DINO_ID, dino.dino_id.ToString());

                //Add this dino to the RPC message queue
                var rpcDino = new RPCPayloadDinosaurUpdateEvent_Dino
                {
                    classname = dino.classname,
                    icon = entry.icon.image_thumb_url,
                    id = dino.dino_id.ToString(),
                    level = dino.level,
                    name = dino.tamed_name,
                    status = dino.status,
                    x = dino.location.x,
                    y = dino.location.y,
                    z = dino.location.z,
                    species = entry.screen_name,
                    is_cryo = true
                };
                if (!rpcDinos.ContainsKey(dino.tribe_id))
                    rpcDinos.Add(dino.tribe_id, new List<RPCPayloadDinosaurUpdateEvent_Dino>());
                rpcDinos[dino.tribe_id].Add(rpcDino);
            }

            //Apply actions
            if (dinoActions.Count > 0)
            {
                await Program.conn.content_dinos.BulkWriteAsync(dinoActions);
                dinoActions.Clear();
            }
            await InventoryManager.UpdateInventoryItems(itemActions, Program.conn);

            //Send RPC messages
            foreach (var rpc in rpcDinos)
            {
                RPCPayloadDinosaurUpdateEvent rpcMessage = new RPCPayloadDinosaurUpdateEvent
                {
                    dinos = rpc.Value
                };
                Program.conn.GetRPC().SendRPCMessageToTribe(LibDeltaSystem.RPC.RPCOpcode.DinosaurUpdateEvent, rpcMessage, server, rpc.Key);
            }
        }

        private static DbDino ParseDinoData(out DinosaurEntry entry, DbServer server, string inventoryId, ulong revisionId, byte revisionIndex, int inventoryType, ulong inventoryItemId, DeltaPrimalDataPackage pack, string request)
        {
            //Decode data as bytes and read
            byte[] data = new byte[request.Length / 2];
            for (int index = 0; index < data.Length; index++)
                data[index] = byte.Parse(request.Substring(index * 2, 2), System.Globalization.NumberStyles.HexNumber);

            //Now, decode and parse
            ARKDinoDataObject[] parts = ARKDinoDataTool.ReadData(data);

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
                ByteProperty colorProp = d.GetPropertyByName<ByteProperty>("ColorSetIndexes", i);
                if (colorProp == null)
                    break;
                byte color = colorProp.byteValue;
                if (color <= 0 || color > ArkStatics.ARK_COLOR_IDS.Length)
                    colors[i] = "#FFFFFF";
                else
                    colors[i] = ArkStatics.ARK_COLOR_IDS[colorProp.byteValue - 1]; //Look this up in the color table to get the nice HTML value.
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
                level = d.GetIntProperty("BaseCharacterLevel", 0, 0) + d.GetIntProperty("ExtraCharacterLevel", 0, 0),
                base_level = d.GetIntProperty("BaseCharacterLevel", 0, 0),
                is_tamed = true,
                taming_effectiveness = 1,
                max_stats = ConvertStats(status, "MaxStatusValues"),
                current_stats = ConvertStats(status, "CurrentStatusValues"),
                tamed_levelups_applied = ConvertStats(status, "NumberOfLevelUpPointsAppliedTamed"),
                base_levelups_applied = ConvertStats(status, "NumberOfLevelUpPointsApplied"),
                status = ConvertStatus(d),
                cryo_inventory_type = inventoryType,
                cryo_inventory_itemid = inventoryItemId
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
                if (d.GetBoolProperty("bPassiveFlee"))
                {
                    status = "PASSIVE_FLEE";
                }
            }
            return status;
        }

        private static DbArkDinosaurStats ConvertStats(ARKDinoDataObject c, string name)
        {
            DbArkDinosaurStats s = new DbArkDinosaurStats();
            var props = c.GetPropertiesByName(name);
            foreach (var p in props)
            {
                int index = p.index;
                float data;
                if (p.GetType() == typeof(ByteProperty))
                    data = ((ByteProperty)p).byteValue;
                else if (p.GetType() == typeof(FloatProperty))
                    data = ((FloatProperty)p).value;
                else
                    throw new Exception("Unexpected type for converting stats!");

                switch (index)
                {
                    case 0:
                        s.health = data;
                        break;
                    case 1:
                        s.stamina = data;
                        break;
                    case 2:
                        s.unknown1 = data;
                        break;
                    case 3:
                        s.oxygen = data;
                        break;
                    case 4:
                        s.food = data;
                        break;
                    case 5:
                        s.water = data;
                        break;
                    case 6:
                        s.unknown2 = data;
                        break;
                    case 7:
                        s.inventoryWeight = data;
                        break;
                    case 8:
                        s.meleeDamageMult = data;
                        break;
                    case 9:
                        s.movementSpeedMult = data;
                        break;
                    case 10:
                        s.unknown2 = data;
                        break;
                    case 11:
                        s.unknown4 = data;
                        break;
                    default:
                        //We shouldn't be here...
                        throw new Exception($"Unknown index ID while reading Dinosaur stats {index}!");
                }
            }

            return s;
        }
    }
}
