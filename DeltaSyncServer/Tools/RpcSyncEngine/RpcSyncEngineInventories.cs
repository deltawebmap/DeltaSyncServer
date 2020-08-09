using DeltaSyncServer.Entities.InventoriesPayload;
using LibDeltaSystem.Entities.CommonNet;
using LibDeltaSystem.RPC.Payloads.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Tools.RpcSyncEngine
{
    public class RpcSyncEngineInventories : BaseRpcSyncEngine<NetInventory>
    {
        public RpcSyncEngineInventories() : base(RPCSyncType.Inventory)
        {
        }

        public override object GetRPCEntity(NetInventory e)
        {
            return e;
        }

        public override int GetTribeId(NetInventory e)
        {
            return e.tribe_id;
        }
    }
}
