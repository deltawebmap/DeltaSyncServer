using DeltaSyncServer.Entities.InventoriesPayload;
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
using static LibDeltaSystem.Db.Content.DbInventory;

namespace DeltaSyncServer.Services.v2
{
    public class InventoriesRequestV2 : InjestServerV2PutService<InventoriesRequestPayload, InventoriesData, DbInventory>
    {
        public InventoriesRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public ulong GetInventoryId(InventoriesData data)
        {
            //Check if this even needs to be combined
            DbInventory_InventoryType type = (DbInventory_InventoryType)data.type;
            if (type == DbInventory_InventoryType.Structure)
                return data.id1;

            //Combine
            return Program.GetMultipartID(data.id1, data.id2);
        }

        public DbInventory_InventoryItem[] GetInventoryItems(InventoriesData_Item[] items)
        {
            DbInventory_InventoryItem[] output = new DbInventory_InventoryItem[items.Length];
            for(int i = 0; i<items.Length; i+=1)
            {
                InventoriesData_Item data = items[i];

                //Get flags
                ushort flag = 0;
                if (data.tek)
                    flag |= 1 << 0;
                if (data.blp)
                    flag |= 1 << 1;
                if (data.cryo != null)
                    flag |= 1 << 2;
                if (data.cname.Length > 0)
                    flag |= 1 << 3;

                //Get custom data
                Dictionary<ushort, string> custom = new Dictionary<ushort, string>();
                if (data.cname.Length > 0)
                    custom.Add(0, data.cname);

                //Set output
                output[i] = new DbInventory_InventoryItem
                {
                    classname = data.c,
                    durability = data.d,
                    item_id = Program.GetMultipartID(data.i1, data.i2),
                    stack_size = data.q,
                    custom_data = custom,
                    flags = flag
                };
            }
            return output;
        }

        public override FilterDefinition<DbInventory> CreateFilterDefinition(InventoriesData data)
        {
            var filterBuilder = Builders<DbInventory>.Filter;
            var filter = filterBuilder.Eq("holder_id", GetInventoryId(data)) & filterBuilder.Eq("server_id", server._id);
            return filter;
        }

        public override DbInventory CreateNewEntry(InventoriesData data, InventoriesRequestPayload context)
        {
            //Return object
            return new DbInventory
            {
                holder_id = GetInventoryId(data),
                holder_type = (DbInventory_InventoryType)data.type,
                server_id = server._id,
                tribe_id = data.tribe,
                items = GetInventoryItems(data.items),
                created_time = DateTime.UtcNow,
                lsat_update_time = DateTime.UtcNow
            };
        }

        public override UpdateDefinition<DbInventory> CreateUpdateDefinition(InventoriesData data, InventoriesRequestPayload context)
        {
            var builder = Builders<DbInventory>.Update;
            return builder.Set("tribe_id", data.tribe)
                .Set("lsat_update_time", DateTime.UtcNow)
                .Set("items", GetInventoryItems(data.items));
        }

        public override IMongoCollection<DbInventory> GetMongoCollection()
        {
            return conn.content_inventories;
        }

        public override InventoriesData[] GetRequestData(InventoriesRequestPayload request)
        {
            return request.inv;
        }

        public override RPCSyncType GetRPCContentUpdateType()
        {
            return RPCSyncType.Inventory;
        }

        public override object GetRPCVersionOfItem(DbInventory item)
        {
            return NetInventory.ConvertInventory(item);
        }

        public override int GetTribeIdFromItem(DbInventory item)
        {
            return item.tribe_id;
        }
    }
}
