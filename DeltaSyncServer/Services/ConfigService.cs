using DeltaSyncServer.Entities;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LibDeltaSystem.Db.System.Entities;
using System.Linq;

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

            //Get version
            int clientVersion = int.Parse(e.Request.Query["client_version"]);

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

            //Set the user ID if needed
            if(server.owner_uid == null)
            {
                DbUser owner = await Program.conn.GetUserByServerSetupToken(request.user_token);
                if(owner != null)
                {
                    server.owner_uid = owner.id;
                    await server.UpdateAsync();
                }
            }

            //Generate a state token
            string stateToken = SecureStringTool.GenerateSecureString(24);
            while (!await SecureStringTool.CheckStringUniquenessAsync<DbSyncSavedState>(stateToken, Program.conn.system_sync_states))
                stateToken = SecureStringTool.GenerateSecureString(24);

            //Create a state
            DbSyncSavedState state = new DbSyncSavedState
            {
                mod_version = clientVersion,
                server_id = server.id,
                system_version = 0,
                time = DateTime.UtcNow,
                token = stateToken,
                mod_enviornment = e.Request.Query["client_env"]
            };
            await Program.conn.system_sync_states.InsertOneAsync(state);

            //Create the requested INI settings
            List<ResponsePayload_ConfigRequest> iniSettings = CreateIniRequestData();

            //Create a response
            ResponsePayload response = new ResponsePayload
            {
                token = server.token,
                delta_config = new ModRemoteConfig(),
                state = stateToken,
                revision_id_dinos = server.revision_id_dinos,
                revision_id_structures = server.revision_id_structures,
                ini_settings = iniSettings,
                update_speed_multiplier = server.update_speed_multiplier
            };

            //Write response
            await Program.WriteStringToStream(e.Response.Body, Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }

        private static List<ResponsePayload_ConfigRequest> CreateIniRequestData()
        {
            List<ResponsePayload_ConfigRequest> iniSettings = new List<ResponsePayload_ConfigRequest>();
            Type type = typeof(DbServerGameSettings);
            foreach(var prop in type.GetProperties())
            {
                //Get the attribute
                ServerSettingsMetadata metadata = prop.GetCustomAttribute<ServerSettingsMetadata>();
                if (metadata == null)
                    continue;

                //Determine type
                string iniType;
                if (prop.PropertyType == typeof(int))
                    iniType = "INT";
                else if (prop.PropertyType == typeof(float))
                    iniType = "FLOAT";
                else if (prop.PropertyType == typeof(string))
                    iniType = "STR";
                else if (prop.PropertyType == typeof(bool))
                    iniType = "BOOL";
                else
                    continue;

                //Add request data
                iniSettings.Add(new ResponsePayload_ConfigRequest
                {
                    name = metadata.name,
                    section = metadata.section,
                    type = iniType
                });
            }
            return iniSettings;
        }

        class RequestPayload
        {
            public string token;
            public string map;
            public string name;
            public string user_token;
        }

        class ResponsePayload
        {
            public string token;
            public string state;
            public int revision_id_dinos;
            public int revision_id_structures;
            public ModRemoteConfig delta_config;
            public List<ResponsePayload_ConfigRequest> ini_settings;
            public float update_speed_multiplier;
        }

        class ResponsePayload_ConfigRequest
        {
            public string section;
            public string name;
            public string type;
        }
    }
}

