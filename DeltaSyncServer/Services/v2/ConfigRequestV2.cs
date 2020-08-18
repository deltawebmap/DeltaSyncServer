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
        public const int MIN_ALLOWED_VERSION = 12;

        public ConfigRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task<bool> OnPreRequest()
        {
            return true;
        }

        public override async Task OnRequest()
        {
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
                await Program.WriteStringToStream(e.Response.Body, "D001"+Newtonsoft.Json.JsonConvert.SerializeObject(r));
                return;
            }

            //Create the requested INI settings
            List<ResponsePayload_ConfigRequest> iniSettings = CreateIniRequestData();

            //Create a response
            ResponsePayload response = new ResponsePayload
            {
                delta_config = Program.clientConfig,
                ini_settings = iniSettings,
                start_allowed = true,
                start_msg = "Connected! Welcome to the Delta Web Map beta!",
                debug_timings = false,
                debug_remote_log = false
            };

            //Write response
            await Program.WriteStringToStream(e.Response.Body, "D001" + Newtonsoft.Json.JsonConvert.SerializeObject(response));
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

        class ResponsePayload
        {
            public ModRemoteConfig delta_config;
            public List<ResponsePayload_ConfigRequest> ini_settings;
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
