using DeltaSyncServer.Entities.LivePayload;
using LibDeltaSystem.Db;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.RPC.Payloads;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static LibDeltaSystem.RPC.Payloads.RPCPayloadLiveUpdate;

namespace DeltaSyncServer.Services.v1
{
    public static class LiveRequest
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Decode
            LiveRequestData request = Program.DecodeStreamAsJson<LiveRequestData>(e.Request.Body);

            //Create an RPC buffer
            Dictionary<int, List<RPCLiveUpdateData>> tribeRpcEvents = new Dictionary<int, List<RPCLiveUpdateData>>();

            //Loop through and apply
            foreach(var r in request.updates)
            {
                //Get the ID
                ulong id = uint.Parse(r.id1);
                if(uint.TryParse(r.id2, out uint id2))
                    id = Program.GetMultipartID((uint)id, id2);

                //Make sure an array exists in the RPC data for this tribe
                if (!tribeRpcEvents.ContainsKey(r.tribeId))
                    tribeRpcEvents.Add(r.tribeId, new List<RPCLiveUpdateData>());

                //Add RPC event
                tribeRpcEvents[r.tribeId].Add(new RPCLiveUpdateData
                {
                    id = id.ToString(),
                    type = r.type,
                    x = r.x,
                    y = r.y,
                    z = r.z,
                    yaw = r.yaw,
                    health = r.health,
                    stamina = r.stamina,
                    food = r.food,
                    weight = r.weight
                });

                //Send updates to the database
                switch(r.type)
                {
                    case 0: await UpdatePlayerDb(server, r); break;
                }
            }

            //Send RPC events
            foreach(var t in tribeRpcEvents)
            {
                Program.conn.GetRPC().SendRPCMessageToTribe(LibDeltaSystem.RPC.RPCOpcode.LiveUpdate, new RPCPayloadLiveUpdate
                {
                    updates = t.Value
                }, server, t.Key);
            }
        }

        private static async Task UpdatePlayerDb(DbServer server, LiveUpdate update)
        {
            var updateBuilder = Builders<DbPlayerCharacter>.Update;
            var filterBuilder = Builders<DbPlayerCharacter>.Filter;

            //Get the ID
            uint id = uint.Parse(update.id1);

            //Create updates
            List<UpdateDefinition<DbPlayerCharacter>> updates = new List<UpdateDefinition<DbPlayerCharacter>>();
            updates.Add(updateBuilder.Set("tribe_id", update.tribeId));
            if (update.x != null && update.y != null && update.z != null)
                updates.Add(updateBuilder.Set("pos", new DbVector3
                {
                    x = update.x.Value,
                    y = update.y.Value,
                    z = update.z.Value
                }));

            //Send update
            var result = await Program.conn.content_player_characters.UpdateOneAsync(filterBuilder.Eq("server_id", server.id) & filterBuilder.Eq("ark_id", id), updateBuilder.Combine(updates));
            
            //Check if this entry even exists
            if(result.MatchedCount == 0 && update.x != null && update.y != null && update.z != null)
            {
                //We'll create a new object with this basic data
                DbPlayerCharacter c = new DbPlayerCharacter
                {
                    server_id = server.id,
                    tribe_id = update.tribeId,
                    ark_id = id,
                    pos = new DbVector3
                    {
                        x = update.x.Value,
                        y = update.y.Value,
                        z = update.z.Value
                    }
                };

                //Insert this
                await Program.conn.content_player_characters.InsertOneAsync(c);
            }
        }
    }
}
