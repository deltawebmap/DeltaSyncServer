using DeltaSyncServer.Entities.ProfilesPayload;
using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class PlayerProfilesRequestV2 : InjestServerAuthDeltaService
    {
        public PlayerProfilesRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            //Decode sent data
            ProfilesRequestData request = await DecodePOSTBody<ProfilesRequestData>();

            //Run updates on tribe
            List<WriteModel<DbTribe>> tribeUpdates = new List<WriteModel<DbTribe>>();
            foreach (var tribe in request.tribes)
            {
                //Get filter
                var filterBuilder = Builders<DbTribe>.Filter;
                var filter = filterBuilder.Eq("tribe_id", tribe.tribe_id) & filterBuilder.Eq("server_id", server._id);

                //Get update
                var updateBuilder = Builders<DbTribe>.Update;
                var update = updateBuilder.SetOnInsert("server_id", server._id)
                    .Set("tribe_id", tribe.tribe_id)
                    .Set("tribe_name", tribe.name)
                    .Set("tribe_owner", 0)
                    .Set("last_seen", DateTime.UtcNow);

                //Run
                var u = new UpdateOneModel<DbTribe>(filter, update);
                u.IsUpsert = true;
                tribeUpdates.Add(u);
            }

            //Run updates on players
            List<WriteModel<DbPlayerProfile>> playerUpdates = new List<WriteModel<DbPlayerProfile>>();
            foreach (var tribe in request.player_profiles)
            {
                string steamName;
                string steamIcon;
                try
                {
                    //Get Steam info
                    var steam = await conn.GetSteamProfileById(tribe.steam_id);

                    steamName = steam.name;
                    steamIcon = steam.icon_url;
                } catch
                {
                    steamName = "STEAM_ERROR";
                    steamIcon = null;
                }

                //Get filter
                var filterBuilder = Builders<DbPlayerProfile>.Filter;
                var filter = filterBuilder.Eq("steam_id", tribe.steam_id) & filterBuilder.Eq("server_id", server._id);

                //Get update
                var updateBuilder = Builders<DbPlayerProfile>.Update;
                var update = updateBuilder.SetOnInsert("server_id", server._id)
                    .Set("tribe_id", tribe.tribe_id)
                    .Set("name", steamName)
                    .Set("ig_name", tribe.ark_name)
                    .Set("ark_id", tribe.ark_id)
                    .Set("steam_id", tribe.steam_id)
                    .Set("last_seen", DateTime.UtcNow)
                    .Set("icon", steamIcon);

                //Run
                var u = new UpdateOneModel<DbPlayerProfile>(filter, update);
                u.IsUpsert = true;
                playerUpdates.Add(u);
            }

            //Apply these
            var tribeResponse = await conn.content_tribes.BulkWriteAsync(tribeUpdates);
            var playerResponse = await conn.content_player_profiles.BulkWriteAsync(playerUpdates);

            //Notify new users
            foreach(var p in playerResponse.Upserts)
            {
                //Fetch a user account
                var deltaAccount = await conn.GetUserBySteamIdAsync(request.player_profiles[p.Index].steam_id);
                if(deltaAccount != null)
                {
                    LibDeltaSystem.Tools.RPCMessageTool.SendUserServerJoined(conn, deltaAccount, server);
                    LibDeltaSystem.Tools.RPCMessageTool.SystemNotifyUserGroupReset(conn, deltaAccount);
                }
            }

            //Todo: Handle new admins

            //Respond
            await WriteInjestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
