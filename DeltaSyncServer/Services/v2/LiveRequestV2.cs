using DeltaSyncServer.Entities.LivePayload;
using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.RPC.Payloads.Entities;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class LiveRequestV2 : InjestServerAuthDeltaService
    {
        public LiveRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public static readonly Dictionary<int, RPCSyncType> SYNC_TYPE_MAP = new Dictionary<int, RPCSyncType>
        {
            {0, RPCSyncType.Player},
            {1, RPCSyncType.Dino}
        };

        public override async Task OnRequest()
        {
            //Decode
            LiveRequestData request = await DecodePOSTBody<LiveRequestData>();

            //Create async buffers
            List<Task> tasks = new List<Task>();

            //Loop through and apply
            foreach (var r in request.updates)
            {
                //Get the ID
                ulong id = uint.Parse(r.id1);
                if (uint.TryParse(r.id2, out uint id2))
                    id = Program.GetMultipartID((uint)id, id2);

                //Get type
                if (!SYNC_TYPE_MAP.ContainsKey(r.type))
                    continue;
                RPCSyncType type = SYNC_TYPE_MAP[r.type];

                //Create RPC update message
                var update = new LibDeltaSystem.RPC.Payloads.Server.RPCPayload20002PartialUpdate.RPCPayload20002PartialUpdate_Update
                {
                    x = r.x,
                    y = r.y,
                    z = r.z,
                    yaw = r.yaw,
                    food = r.food,
                    health = r.health,
                    stamina = r.stamina,
                    weight = r.weight
                };

                //Create and send RPC message
                tasks.Add(LibDeltaSystem.Tools.RPCMessageTool.SendDbUpdatePartial(conn, type, server._id, r.tribeId, id.ToString(), update));

                //Run update for this
                Task updateTask;
                switch (type)
                {
                    case RPCSyncType.Dino: updateTask = UpdateTaskDino(r); break;
                    case RPCSyncType.Player: updateTask = UpdateTaskPlayer(r); break;
                    default: continue;
                }
                tasks.Add(updateTask);
            }

            //Wait for all actions
            await Task.WhenAll(tasks);

            //Write response
            await WriteInjestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        private async Task UpdateTaskDino(LiveUpdate update)
        {

        }

        private async Task UpdateTaskPlayer(LiveUpdate update)
        {
            //Create a filter definition for this
            var filterBuilder = Builders<DbPlayerProfile>.Filter;
            var filterAction = filterBuilder.Eq("ark_id", int.Parse(update.id1)) & filterBuilder.Eq("server_id", server._id);

            //Create update
            var updateBuilder = Builders<DbPlayerProfile>.Update;
            List<UpdateDefinition<DbPlayerProfile>> updates = new List<UpdateDefinition<DbPlayerProfile>>();
            if (update.x != null)
                updateBuilder.Set("x", update.x);
            if (update.y != null)
                updateBuilder.Set("y", update.y);
            if (update.z != null)
                updateBuilder.Set("z", update.z);
            if (update.yaw != null)
                updateBuilder.Set("yaw", update.yaw);
            var updateAction = updateBuilder.Combine(updates);

            //Apply to MongoDB
            await conn.content_player_profiles.UpdateOneAsync(filterAction, updateAction);
        }
    }
}
