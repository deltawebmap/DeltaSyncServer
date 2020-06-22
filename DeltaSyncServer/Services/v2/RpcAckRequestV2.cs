using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Tools;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class RpcAckRequestV2 : InjestServerAuthDeltaService
    {
        public RpcAckRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            //Decode JSON
            RequestData request = await DecodePOSTBody<RequestData>();

            //Parse the requested token
            string[] tokenParts = request.t.Split('@');
            ObjectId rpc_id = ObjectId.Parse(tokenParts[0]);
            ObjectId user_id = ObjectId.Parse(tokenParts[1]);

            //Parse custom data
            Dictionary<string, string> custom_data = new Dictionary<string, string>();
            for(int i = 0; i<request.ck.Length; i+=1)
            {
                if (request.ck[i] == "")
                    continue;
                custom_data.Add(request.ck[i], request.cv[i]);
            }

            //Send RPC message to user
            await RPCMessageTool.SendUserArkRpcAck(conn, user_id, server._id, rpc_id, custom_data);

            //Write response
            await WriteInjestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        class RequestData
        {
            public string t; //Token
            public string[] ck; //Custom data key
            public string[] cv; //Custom data value
        }
    }
}
