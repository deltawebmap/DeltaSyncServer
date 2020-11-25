using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.Tools;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class RealtimePlayersService : InjestServerAuthDeltaService
    {
        public RealtimePlayersService(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            //Read
            var data = await ReadPOSTContentChecked<RequestData>();
            if (data == null)
                return;

            //TODO: Send via RPC

            //Only run if we have new content
            if (data.players.Length > 0)
            {
                //Update player profiles
                List<WriteModel<DbPlayerProfile>> profileWrites = new List<WriteModel<DbPlayerProfile>>();
                foreach (var p in data.players)
                {
                    var filterBuilder = Builders<DbPlayerProfile>.Filter;
                    var updateBuilder = Builders<DbPlayerProfile>.Update;
                    var filter = filterBuilder.Eq("server_id", server._id) & filterBuilder.Eq("steam_id", p.sid);
                    var update = updateBuilder.Set("last_seen", DateTime.UtcNow).Set("x", p.loc.x).Set("y", p.loc.y).Set("z", p.loc.z).Set("yaw", p.loc.yaw);
                    profileWrites.Add(new UpdateOneModel<DbPlayerProfile>(filter, update));
                }
                await conn.content_player_profiles.BulkWriteAsync(profileWrites);
            }

            //Write response
            await WriteIngestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        class RequestData
        {
            public RequestDataPlayer[] players;
        }

        class RequestDataPlayer
        {
            public DbLocation loc;
            public int tribe;
            public string sid; //Steam ID
        }
    }
}
