using DeltaSyncServer.Entities.InventoriesPayload;
using DeltaSyncServer.Entities.StructurePayload;
using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Entities.CommonNet;
using LibDeltaSystem.RPC.Payloads.Entities;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Services.v2
{
    public class StructuresRequestV2 : InjestServerV2SyncInventoryService<StructureData, DbStructure>
    {
        public StructuresRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override FilterDefinition<DbStructure> CreateFilterDefinition(StructureData data)
        {
            var builder = Builders<DbStructure>.Filter;
            return builder.Eq("server_id", server._id) & builder.Eq("structure_id", data.id);
        }

        public override UpdateDefinition<DbStructure> CreateUpdateDefinition(StructureData data, ulong revision_id, byte revision_index)
        {
            var builder = Builders<DbStructure>.Update;
            return builder.SetOnInsert("server_id", server._id)
                .Set("tribe_id", data.tribe)
                .Set("location", data.location)
                .SetOnInsert("classname", Program.TrimArkClassname(data.classname))
                .Set("has_inventory", data.inv != null)
                .Set("current_item_count", data.item_count)
                .Set("max_item_count", data.max_items)
                .Set("max_health", data.max_health)
                .Set("current_health", data.health)
                .SetOnInsert("structure_id", data.id)
                .Set("custom_name", data.name);
        }

        public override ulong GetArkIdOfObject(StructureData obj)
        {
            return (ulong)obj.id;
        }

        public override InventoriesData GetInventoryDataOfObject(StructureData obj)
        {
            return obj.inv;
        }

        public override IMongoCollection<DbStructure> GetMongoCollection()
        {
            return conn.content_structures;
        }

        public override RPCSyncType GetRPCContentUpdateType()
        {
            return RPCSyncType.Structure;
        }

        public override object GetRPCVersionOfItem(StructureData item)
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

        public override DbInventory.DbInventory_InventoryType GetStructureTypeOfObject(StructureData obj)
        {
            return DbInventory.DbInventory_InventoryType.Structure;
        }

        public override int GetTribeIdFromItem(StructureData item)
        {
            return item.tribe;
        }
    }
}
