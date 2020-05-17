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
using LibDeltaSystem.WebFramework;
using LibDeltaSystem;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;

namespace DeltaSyncServer.Services.v2
{
    public class ConfigRequestV2 : DeltaWebService
    {
        public const int MIN_ALLOWED_VERSION = 11;

        public ConfigRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task<bool> OnPreRequest()
        {
            return true;
        }

        public override async Task OnRequest()
        {
            //Decode POST body
            RequestPayload request = Program.DecodeStreamAsJson<RequestPayload>(e.Request.Body);
            request.debug = false;

            //Check to see if this is a valid ARK server
            if (!e.Request.Query.ContainsKey("client_version"))
            {
                e.Response.StatusCode = 400;
                await Program.WriteStringToStream(e.Response.Body, "Required information was not sent, this is likely not a valid ARK server.\r\n\r\nIt's likely that you're looking into how things here work. More information: https://github.com/deltawebmap/Docs/blob/master/basics.md \r\n\r\n(C) DeltaWebMap 2020, RomanPort 2020");
                return;
            }

            //Get version
            int clientVersion = int.Parse(e.Request.Query["client_version"]);

            //If the version is too old, reject
            if (clientVersion < MIN_ALLOWED_VERSION)
            {
                //Create a response
                ResponsePayload r = new ResponsePayload
                {
                    start_allowed = false,
                    start_msg = "Can't Connect: This version of the mod, " + clientVersion + ", is too old. Please upgrade to a newer version using the Steam Workshop. Need help? https://deltamap.net/support/."
                };

                //Write response
                await Program.WriteStringToStream(e.Response.Body, "DELTAWEBMAP.CONFIGRESPONSE"+Newtonsoft.Json.JsonConvert.SerializeObject(r));
                return;
            }

            //Attempt to authenticate. It's OK of this fails.
            DbServer server = await Program.conn.AuthenticateServerTokenAsync(request.token);
            if (server == null)
            {
                //Generate a token to use
                string token = SecureStringTool.GenerateSecureString(82);
                while (!await SecureStringTool.CheckStringUniquenessAsync<DbServer>(token, Program.conn.system_servers))
                    token = SecureStringTool.GenerateSecureString(82);

                //If this is a debug server, modify name
                if (request.debug)
                    request.name = $"TEST-{DateTime.Now.ToShortDateString()}-{DateTime.Now.ToShortTimeString()} " + request.name;

                //Set the owner to the ID of the test user if we're in debug mode
                ObjectId? ownerId = null;
                if(request.debug)
                {
                    ownerId = (await conn.GetUserBySteamIdAsync(DeltaConnection.SYSTEM_USER_TEST))._id;
                }

                //Create a server
                server = new DbServer
                {
                    display_name = request.name,
                    _id = MongoDB.Bson.ObjectId.GenerateNewId(),
                    image_url = DbServer.StaticGetPlaceholderIcon(Program.conn, request.name),
                    token = token,
                    has_custom_image = false,
                    latest_server_map = request.map,
                    mods = new string[0],
                    lock_flags = 0,
                    owner_uid = ownerId,
                    secure_mode = true,
                    last_secure_mode_toggled = DateTime.UtcNow
                };

                //Insert
                await Program.conn.system_servers.InsertOneAsync(server);

                //If this is a debug server, add a test user and tribe
                if(request.debug)
                {
                    await Program.conn.content_tribes.InsertOneAsync(new LibDeltaSystem.Db.Content.DbTribe
                    {
                        server_id = server._id,
                        tribe_id = int.MaxValue,
                        tribe_name = "TEST TRIBE",
                        tribe_owner = 0
                    });
                    await Program.conn.content_player_profiles.InsertOneAsync(new LibDeltaSystem.Db.Content.DbPlayerProfile
                    {
                        ark_id = 0,
                        icon = "",
                        ig_name = "DEBUG USER",
                        server_id = server._id,
                        steam_id = DeltaConnection.SYSTEM_USER_TEST,
                        tribe_id = int.MaxValue
                    });
                }
            }

            //Set the user ID if needed
            if (server.owner_uid == null)
            {
                DbUser owner = await Program.conn.GetUserByServerSetupToken(request.user_token);
                if (owner != null && owner?._id != server.owner_uid)
                {
                    await server.ClaimServer(conn, owner);
                }
            }

            //Generate a state token
            string stateToken = SecureStringTool.GenerateSecureString(56);
            while (!await SecureStringTool.CheckStringUniquenessAsync<DbSyncSavedState>(stateToken, Program.conn.system_sync_states))
                stateToken = SecureStringTool.GenerateSecureString(56);

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
                server_id = server.id,
                state = stateToken,
                revision_ids = server.revision_ids,
                ini_settings = iniSettings,
                update_speed_multiplier = server.update_speed_multiplier,
                start_allowed = true,
                start_msg = "Connected! Welcome to the Delta Web Map beta!",
                debug_timings = true,
                debug_remote_log = true
            };

            //Write response
            await Program.WriteStringToStream(e.Response.Body, "DELTAWEBMAP.CONFIGRESPONSE" + Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }

        private static List<ResponsePayload_ConfigRequest> CreateIniRequestData()
        {
            List<ResponsePayload_ConfigRequest> iniSettings = new List<ResponsePayload_ConfigRequest>();
            Type type = typeof(DbServerGameSettings);
            foreach (var prop in type.GetProperties())
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
            public bool debug;
        }

        class ResponsePayload
        {
            public string token;
            public string state;
            public string server_id;
            public ModRemoteConfig delta_config;
            public List<ResponsePayload_ConfigRequest> ini_settings;
            public float update_speed_multiplier;
            public ulong[] revision_ids;
            public bool start_allowed;
            public string start_msg;
            public bool debug_timings;
            public bool debug_remote_log;
        }

        class ResponsePayload_ConfigRequest
        {
            public string section;
            public string name;
            public string type;
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
