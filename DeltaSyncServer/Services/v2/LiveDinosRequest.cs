using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.RPC.Payloads.Server;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class LiveDinosRequest : InjestServerAuthDeltaService
    {
        public LiveDinosRequest(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            //Decode
            RequestData data = await DecodePOSTBody<RequestData>();

            //Convert this data to an RPC event for tribes
            Dictionary<int, RPCPayload20009LiveDinoUpdate> tribes = new Dictionary<int, RPCPayload20009LiveDinoUpdate>();
            foreach(var d in data.data)
            {
                //Get the area to add this, if any
                RPCPayload20009LiveDinoUpdate rpc;
                if(tribes.ContainsKey(d.tribe))
                {
                    rpc = tribes[d.tribe];
                } else
                {
                    rpc = new RPCPayload20009LiveDinoUpdate
                    {
                        dinos = new List<RPCPayload20009LiveDinoUpdate.LiveDinoUpdateDino>()
                    };
                    tribes.Add(d.tribe, rpc);
                }

                //Convert dino
                Dictionary<int, float> stats = new Dictionary<int, float>();
                foreach (var s in d.stats)
                    stats.Add(int.Parse(s.Key), s.Value);
                ulong id = Program.GetMultipartID((uint)d.id1, (uint)d.id2);

                //Add
                rpc.dinos.Add(new RPCPayload20009LiveDinoUpdate.LiveDinoUpdateDino
                {
                    id = id.ToString(),
                    stats = stats
                });
            }

            //Send all RPC events to their tribes
            foreach (var t in tribes)
            {
                conn.network.SendRPCEventToServerTribeId(LibDeltaSystem.RPC.RPCOpcode.RPCPayload30002UserServerJoined, t.Value, server, t.Key);
            }

            //Finish
            await WriteIngestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        class RequestData
        {
            public RequestDataDinosaur[] data;
        }

        class RequestDataDinosaur
        {
            /// <summary>
            /// Keys are numbers in this table:
            /// 
            /// 1: X
            /// 2: Y
            /// 3: Health
            /// 4: Stamina
            /// 5: Food
            /// 6: Inventory
            /// </summary>
            public Dictionary<string, float> stats;
            public int id1;
            public int id2;
            public int tribe;
        }
    }
}
