﻿using DeltaSyncServer.Services.Templates;
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

            //Create RPC message
            LibDeltaSystem.RPC.Payloads.Server.RPCPayload20011OnlinePlayersUpdated msg = new LibDeltaSystem.RPC.Payloads.Server.RPCPayload20011OnlinePlayersUpdated
            {
                players = new List<LibDeltaSystem.RPC.Payloads.Server.RPCPayload20011OnlinePlayersUpdated.OnlinePlayer>(),
                player_count = data.players.Length
            };

            //Add public info
            foreach (var p in data.players)
            {
                msg.players.Add(new LibDeltaSystem.RPC.Payloads.Server.RPCPayload20011OnlinePlayersUpdated.OnlinePlayer
                {
                    tribe_id = p.tribe,
                    steam_token = conn.CreateSteamIdTokenString(p.sid)
                });
            }

            //Send to all players
            RPCMessageTool.SendRPCMsgToServer(conn, LibDeltaSystem.RPC.RPCOpcode.RPCServer20011OnlinePlayersUpdated, msg, server._id);

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
