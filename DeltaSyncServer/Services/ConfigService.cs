using DeltaSyncServer.Entities;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services
{
    public static class ConfigService
    {
        /// <summary>
        /// To: /config
        /// Used to provide config files and create new tokens/servers
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Decode POST body
            RequestPayload request = Program.DecodeStreamAsJson<RequestPayload>(e.Request.Body);

            //Attempt to authenticate. It's OK of this fails.
            DbServer server = await Program.conn.AuthenticateServerTokenAsync(request.token);
            if(server == null)
            {
                //Generate a token to use
                string token = SecureStringTool.GenerateSecureString(82);
                while (!await SecureStringTool.CheckStringUniquenessAsync<DbServer>(token, Program.conn.system_servers))
                    token = SecureStringTool.GenerateSecureString(82);

                //Create a server
                server = new DbServer
                {
                    display_name = request.name,
                    _id = MongoDB.Bson.ObjectId.GenerateNewId(),
                    image_url = DbServer.StaticGetPlaceholderIcon(request.name),
                    token = token,
                    has_custom_image = false,
                    revision_id_dinos = 0,
                    revision_id_structures = 0,
                    conn = Program.conn,
                    latest_server_map = request.map,
                    mods = new string[0]
                };

                //Insert
                Program.conn.system_servers.InsertOne(server);
            }

            //Generate a state token
            string stateToken = SecureStringTool.GenerateSecureString(24);
            while (!await SecureStringTool.CheckStringUniquenessAsync<DbSyncSavedState>(stateToken, Program.conn.system_sync_states))
                stateToken = SecureStringTool.GenerateSecureString(24);

            //Create a state
            DbSyncSavedState state = new DbSyncSavedState
            {
                mod_version = 0,
                server_id = server.id,
                system_version = 0,
                time = DateTime.UtcNow,
                token = stateToken
            };
            await Program.conn.system_sync_states.InsertOneAsync(state);

            //Create a fake response for now
            ResponsePayload response = new ResponsePayload
            {
                token = server.token,
                delta_config = new ModRemoteConfig(),
                state = stateToken,
                claimer_name = null,
                has_claim_token = false,
                is_claimed = false,
                revision_id_dinos = server.revision_id_dinos,
                revision_id_structures = server.revision_id_structures
            };

            //Write response
            await Program.WriteStringToStream(e.Response.Body, Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }

        class RequestPayload
        {
            public string token;
            public string map;
            public string name;
        }

        class ResponsePayload
        {
            public string token;
            public string state;
            public int revision_id_dinos;
            public int revision_id_structures;
            public bool is_claimed;
            public bool has_claim_token;
            public string claimer_name;
            public ModRemoteConfig delta_config;
        }
    }
}
