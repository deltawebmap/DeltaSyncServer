using DeltaSyncServer.Entities.InventoriesPayload;
using DeltaSyncServer.Entities.StructurePayload;
using LibDeltaSystem.Entities.CommonNet;
using LibDeltaSystem.RPC.Payloads.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Tools.RpcSyncEngine
{
    public class RpcSyncEngineStructures : BaseRpcSyncEngine<StructureData>
    {
        public RpcSyncEngineStructures() : base(RPCSyncType.Structure)
        {
        }

        public override object GetRPCEntity(StructureData item)
        {
            return new NetStructure
            {
                classname = item.classname,
                has_inventory = item.inv != null,
                location = item.location,
                structure_id = item.id,
                tribe_id = item.tribe
            };
        }

        public override int GetTribeId(StructureData e)
        {
            return e.tribe;
        }
    }
}
