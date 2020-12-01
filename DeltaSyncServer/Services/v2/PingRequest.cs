using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class PingRequest : InjestServerAuthDeltaService
    {
        public PingRequest(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            //Read
            var data = await ReadPOSTContentChecked<RequestData>();
            if (data == null)
                return;

            //Convert
            DbServerPing ping = new DbServerPing
            {
                game_delta = data.game_delta,
                max_tick_seconds = data.max_tick_time,
                min_tick_seconds = data.min_tick_time,
                player_count = data.player_count,
                server_id = server._id,
                session_id = session._id,
                ticks_per_second = (float)(data.ticks / data.ping_delta),
                avg_tick_seconds = (float)(data.ping_delta / data.ticks),
                ping_delta = data.ping_delta,
                time = DateTime.UtcNow
            };

            //Insert
            await conn.system_server_pings.InsertOneAsync(ping);

            //Update server info
            await server.GetUpdateBuilder(conn)
                .UpdateLastSyncPingedTime(DateTime.UtcNow)
                .Apply();

            //Write response
            await WriteIngestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        class RequestData
        {
            public int ticks; //Number of ticks since last ping
            public double ping_delta; //Time since last ping
            public float max_tick_time; //Max number of seconds a single tick took
            public float min_tick_time; //Min number of seconds a single tick took
            public int player_count; //Number of players logged in
            public double game_delta; //Game time
        }
    }
}
