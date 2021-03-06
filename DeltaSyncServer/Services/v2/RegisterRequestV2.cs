﻿using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.Tools;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class RegisterRequestV2 : DeltaWebService
    {
        public RegisterRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
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

            //Read the request data
            var request = await ReadPOSTContentChecked<RequestPayload>();
            if (request == null)
                return;

            //Attempt to authenticate. It's OK of this fails.
            DbServer server = await Program.conn.AuthenticateServerTokenAsync(request.token);
            ObjectId stateId = ObjectId.GenerateNewId();
            if (server == null)
            {
                //Create
                server = await CreateNewServer(request, clientVersion, stateId);
            } else
            {
                //Update info
                await server.GetUpdateBuilder(conn)
                    .UpdateLastSyncState(stateId)
                    .UpdateLastSyncConnectedTime(DateTime.UtcNow)
                    .UpdateLastSyncPingedTime(DateTime.UtcNow)
                    .UpdateLastSyncClientVersion(clientVersion)
                    .Apply();
            }

            //Generate a state token
            string stateToken = SecureStringTool.GenerateSecureString(56);
            while (!await SecureStringTool.CheckStringUniquenessAsync<DbSyncSavedState>(stateToken, Program.conn.system_sync_states))
                stateToken = SecureStringTool.GenerateSecureString(56);

            //Create a state
            DbSyncSavedState state = new DbSyncSavedState
            {
                mod_version = clientVersion,
                server_id = server._id,
                system_version = Program.VERSION_MAJOR,
                time = DateTime.UtcNow,
                token = stateToken,
                mod_enviornment = e.Request.Query["client_env"],
                _id = stateId
            };
            await Program.conn.system_sync_states.InsertOneAsync(state);

            //Create a response
            ResponsePayload response = new ResponsePayload
            {
                refresh_token = server.token,
                session_token = state.token,
                is_claimed = false,
                server_id = server.id,
                content_server_host = server.game_content_server_hostname
            };

            //Write response
            await Program.WriteStringToStream(e.Response.Body, "D002" + Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }

        class ResponsePayload
        {
            public string refresh_token; //Only set if we don't have a token on the client
            public string session_token; //The state token to use
            public bool is_claimed; //Has this been claimed
            public string server_id; //The ID of this server
            public string content_server_host; //Host of this server's content server
        }

        private async Task<DbServer> CreateNewServer(RequestPayload requestInfo, int version, ObjectId stateId)
        {
            //Generate a token to use
            string token = SecureStringTool.GenerateSecureString(46);
            while (!await SecureStringTool.CheckStringUniquenessAsync<DbServer>(token, Program.conn.system_servers))
                token = SecureStringTool.GenerateSecureString(46);
            token = "B." + token;

            //Create a server
            var server = new DbServer
            {
                display_name = requestInfo.name,
                _id = MongoDB.Bson.ObjectId.GenerateNewId(),
                image_url = Program.conn.hosts.master + "/default_server_icon.png",
                token = token,
                has_custom_image = false,
                latest_server_map = requestInfo.map,
                mods = new string[0],
                last_secure_mode_toggled = DateTime.UtcNow,
                flags = 0b00000110,
                last_client_version = version,
                last_connected_time = DateTime.UtcNow,
                last_pinged_time = DateTime.UtcNow,
                last_sync_state = stateId
            };

            //Insert
            await Program.conn.system_servers.InsertOneAsync(server);

            return server;
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        class RequestPayload
        {
            public string token;
            public string map;
            public string name;
            public string user_token;
        }
    }
}
