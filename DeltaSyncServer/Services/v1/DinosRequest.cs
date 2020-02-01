﻿using DeltaSyncServer.Entities;
using DeltaSyncServer.Entities.DinoPayload;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
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
    public static class DinosRequest
    {
        /// <summary>
        /// To: /v1/dinos
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Read payload data
            RevisionMappedDataPutRequest<DinoData> s = Program.DecodeStreamAsJson<RevisionMappedDataPutRequest<DinoData>>(e.Request.Body);

            //Get primal data
            var primal = await Program.conn.GetPrimalDataPackage(server.mods);

            //Loop through and add dinosaurs
            List<WriteModel<DbDino>> dinoActions = new List<WriteModel<DbDino>>();
            List<WriteModel<DbItem>> itemActions = new List<WriteModel<DbItem>>();
            Dictionary<int, List<RPCPayloadDinosaurUpdateEvent_Dino>> rpcDinos = new Dictionary<int, List<RPCPayloadDinosaurUpdateEvent_Dino>>(); //Events to send on the RPC, by tribe ID
            foreach (var d in s.data)
            {
                //Get dino entry
                var entry = await primal.GetDinoEntryByClssnameAsnyc(d.classname);
                if (entry == null)
                    continue;

                //Create dino ID
                ulong dinoId = Program.GetMultipartID((uint)d.id_1, (uint)d.id_2);

                //Convert colors to a readable hex code
                string[] colors = new string[d.colors.Length];
                for(int i = 0; i<d.colors.Length; i++)
                {
                    byte color = d.colors[i];
                    if (color <= 0 || color > ArkStatics.ARK_COLOR_IDS.Length)
                        colors[i] = "#FFFFFF";
                    else
                        colors[i] = ArkStatics.ARK_COLOR_IDS[d.colors[i] - 1]; //Look this up in the color table to get the nice HTML value.
                }

                //If the dino name is blank, fetch it from the dino entry
                if (d.name.Length <= 1)
                    d.name = entry.screen_name;

                //Convert this to it's DbDino equivalent
                DbDino dino = new DbDino
                {
                    baby_age = d.baby_age,
                    base_level = d.base_level,
                    base_levelups_applied = ConvertToStatsInt(d.points_wild),
                    classname = Program.TrimArkClassname(d.classname),
                    colors = colors,
                    current_stats = ConvertToStatsFloat(d.current_stats),
                    max_stats = ConvertToStatsFloat(d.max_stats),
                    dino_id = dinoId,
                    experience = d.experience,
                    imprint_quality = d.imprint_quality,
                    is_baby = d.baby,
                    is_female = d.is_female,
                    is_tamed = true,
                    level = d.base_level + d.extra_level,
                    location = d.location,
                    next_imprint_time = d.next_cuddle,
                    revision_id = s.revision_id,
                    revision_type = s.revision_index,
                    server_id = server.id,
                    status = d.status,
                    tamed_levelups_applied = ConvertToStatsInt(d.points_tamed),
                    tamed_name = d.name,
                    tamer_name = d.tamer,
                    taming_effectiveness = 0,
                    tribe_id = d.tribe_id
                };

                {
                    //Create filter for updating this dino
                    var filterBuilder = Builders<DbDino>.Filter;
                    var filter = filterBuilder.Eq("dino_id", dino.dino_id) & filterBuilder.Eq("server_id", server.id);

                    //Now, add (or insert) this into the database
                    var a = new ReplaceOneModel<DbDino>(filter, dino);
                    a.IsUpsert = true;
                    dinoActions.Add(a);
                }

                //Get prefs
                var prefs = await dino.GetPrefs(Program.conn);

                //Add this dino to the RPC message queue
                var rpcDino = new RPCPayloadDinosaurUpdateEvent_Dino
                {
                    dino_id = dino.dino_id.ToString(),
                    dino = dino,
                    species = entry,
                    prefs = prefs
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
            await Tools.InventoryManager.UpdateInventoryItems(itemActions, Program.conn);

            //Send RPC messages
            foreach(var rpc in rpcDinos)
            {
                RPCPayloadDinosaurUpdateEvent rpcMessage = new RPCPayloadDinosaurUpdateEvent
                {
                    dinos = rpc.Value
                };
                Program.conn.GetRPC().SendRPCMessageToTribe(LibDeltaSystem.RPC.RPCOpcode.DinosaurUpdateEvent, rpcMessage, server, rpc.Key);
            }

            //Write finished
            e.Response.StatusCode = 200;
            await Program.WriteStringToStream(e.Response.Body, "OK");
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
            foreach(var v in data)
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
    }
}