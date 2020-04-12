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

            //Add tribes
            await CreateOrUpdateItems<ProfilesTribeData, DbTribe>(request.tribes, conn.content_tribes, (ProfilesTribeData tribe) =>
            {
                var builder = Builders<DbTribe>.Filter;
                var filter = builder.Eq("tribe_id", tribe.tribe_id) & builder.Eq("server_id", server._id);
                return filter;
            }, (ProfilesTribeData tribe) =>
            {
                var builder = Builders<DbTribe>.Update;
                return builder.Set("tribe_name", tribe.name);
            }, (ProfilesTribeData tribe) =>
            {
                return new DbTribe
                {
                    server_id = server._id,
                    tribe_id = tribe.tribe_id,
                    tribe_name = tribe.name,
                    tribe_owner = 0
                };
            });

            //Add players
            await CreateOrUpdateItems<ProfilesProfileData, DbPlayerProfile>(request.player_profiles, conn.content_player_profiles, (ProfilesProfileData tribe) =>
            {
                var builder = Builders<DbPlayerProfile>.Filter;
                var filter = builder.Eq("steam_id", tribe.steam_id) & builder.Eq("server_id", server._id);
                return filter;
            }, (ProfilesProfileData tribe) =>
            {
                var builder = Builders<DbPlayerProfile>.Update;
                return builder.Set("ark_id", tribe.ark_id)
                .Set("ig_name", tribe.ark_name)
                .Set("steam_id", tribe.steam_id)
                .Set("tribe_id", tribe.tribe_id)
                .Set("name", tribe.ark_name);
            }, (ProfilesProfileData tribe) =>
            {
                return new DbPlayerProfile
                {
                    ark_id = ulong.Parse(tribe.ark_id),
                    icon = null,
                    ig_name = tribe.ark_name,
                    last_login = 0,
                    server_id = server._id,
                    steam_id = tribe.steam_id,
                    tribe_id = tribe.tribe_id,
                    name = tribe.ark_name,
                    x = null,
                    y = null,
                    z = null,
                    yaw = null,
                    health = null,
                    stamina = null,
                    food = null,
                    weight = null
                };
            });

            //Respond
            await WriteInjestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
