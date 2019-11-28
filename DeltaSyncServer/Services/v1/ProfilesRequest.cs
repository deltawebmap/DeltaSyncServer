using DeltaSyncServer.Entities.ProfilesPayload;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.RPC.Payloads;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v1
{
    public static class ProfilesRequest
    {
        /// <summary>
        /// To /v1/profiles
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Read structures data
            ProfilesRequestData s = Program.DecodeStreamAsJson<ProfilesRequestData>(e.Request.Body);

            //Add all player profiles
            List<WriteModel<DbPlayerProfile>> playerActions = new List<WriteModel<DbPlayerProfile>>();
            List<WriteModel<DbTribe>> tribeActions = new List<WriteModel<DbTribe>>();
            List<RPCPayloadOnlinePlayers_Player> rpcPlayers = new List<RPCPayloadOnlinePlayers_Player>();
            Dictionary<int, RPCPayloadOnlinePlayers_Tribe> rpcTribes = new Dictionary<int, RPCPayloadOnlinePlayers_Tribe>();
            foreach (var p in s.player_profiles)
            {
                //Fetch Steam info
                var steam = await Program.conn.GetSteamProfileById(p.steam_id);
                if (steam == null)
                    continue;

                //Convert
                DbPlayerProfile profile = new DbPlayerProfile
                {
                    ark_id = ulong.Parse(p.ark_id),
                    ig_name = p.ark_name,
                    last_login = p.last_login,
                    name = steam.name,
                    server_id = server.id,
                    steam_id = p.steam_id,
                    tribe_id = p.tribe_id,
                    icon = steam.icon_url
                };

                //Create filter for updating this dino
                var filterBuilder = Builders<DbPlayerProfile>.Filter;
                var filter = filterBuilder.Eq("steam_id", p.steam_id) & filterBuilder.Eq("server_id", server.id);

                //Now, add (or insert) this into the database
                var a = new ReplaceOneModel<DbPlayerProfile>(filter, profile);
                a.IsUpsert = true;
                playerActions.Add(a);

                //Add to RPC messages
                rpcPlayers.Add(new RPCPayloadOnlinePlayers_Player
                {
                    icon = steam.icon_url,
                    name = steam.name,
                    tribe_id = p.tribe_id
                });
            }

            //Add all tribe profiles
            foreach(var t in s.tribes)
            {
                //Convert
                DbTribe tribe = new DbTribe
                {
                    server_id = server.id,
                    tribe_id = t.tribe_id,
                    tribe_name = t.name,
                    tribe_owner = 0
                };

                //Create filter for updating this dino
                var filterBuilder = Builders<DbTribe>.Filter;
                var filter = filterBuilder.Eq("tribe_id", t.tribe_id) & filterBuilder.Eq("server_id", server.id);

                //Now, add (or insert) this into the database
                var a = new ReplaceOneModel<DbTribe>(filter, tribe);
                a.IsUpsert = true;
                tribeActions.Add(a);

                //Add to RPC messages
                rpcTribes.Add(t.tribe_id, new RPCPayloadOnlinePlayers_Tribe
                {
                    name = t.name
                });
            }

            //Apply actions
            if (tribeActions.Count > 0)
            {
                await Program.conn.content_tribes.BulkWriteAsync(tribeActions);
                tribeActions.Clear();
            }
            if (playerActions.Count > 0)
            {
                await Program.conn.content_player_profiles.BulkWriteAsync(playerActions);
                playerActions.Clear();
            }

            //Send RPC message
            /*Program.conn.GetRPC().SendRPCMessageToServer(LibDeltaSystem.RPC.RPCOpcode.PlayerListChanged, new RPCPayloadOnlinePlayers
            {
                players = rpcPlayers,
                tribes = rpcTribes
            }, server);*/

            //Write finished
            e.Response.StatusCode = 200;
            await Program.WriteStringToStream(e.Response.Body, "OK");
        }
    }
}
