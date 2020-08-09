using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.RPC.Payloads.Entities;
using LibDeltaSystem.RPC.Payloads.Server;
using LibDeltaSystem.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Tools.RpcSyncEngine
{
    /// <summary>
    /// Class for hnadling RPC sync events
    /// </summary>
    public abstract class BaseRpcSyncEngine<InputEntityType>
    {
        public readonly RPCSyncType rpc_type;

        private readonly Dictionary<int, List<object>> eventItems;

        public BaseRpcSyncEngine(RPCSyncType rpc_type)
        {
            this.rpc_type = rpc_type;

            //Create dictionary to store each type, by tribe ID
            eventItems = new Dictionary<int, List<object>>();
        }

        public void AddItem(InputEntityType d)
        {
            //Get tribe ID
            int tribeId = GetTribeId(d);

            //Convert
            var converted = GetRPCEntity(d);

            //Create list if one hasn't already
            if (!eventItems.ContainsKey(tribeId))
                eventItems.Add(tribeId, new List<object>());

            //Add
            eventItems[tribeId].Add(converted);
        }

        public void SendAll(DeltaConnection conn, DbServer server)
        {
            //Now, create and send RPCPayloads
            foreach (var e in eventItems)
            {
                //Transmit
                RPCMessageTool.SendDbContentUpdateMessage(conn, rpc_type, e.Value, server._id, e.Key);
            }
        }

        public abstract int GetTribeId(InputEntityType e);
        public abstract object GetRPCEntity(InputEntityType e);
    }
}
