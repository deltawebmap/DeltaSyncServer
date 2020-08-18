using DeltaSyncServer.Entities.InventoriesPayload;
using DeltaSyncServer.Tools.RpcSyncEngine;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Entities.CommonNet;
using LibDeltaSystem.Tools;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static LibDeltaSystem.Db.Content.DbInventory;
using static LibDeltaSystem.Entities.CommonNet.NetInventory;

namespace DeltaSyncServer.Services.Templates
{
    public abstract class InjestServerV2SyncInventoryService<I, T> : InjestServerV2SyncService<I, T>
    {
        public InjestServerV2SyncInventoryService(DeltaConnection conn, HttpContext e, BaseRpcSyncEngine<I> rpcEngine) : base(conn, e, rpcEngine)
        {
            inventoryRpcEngine = new RpcSyncEngineInventories();
            inventoryWrites = new List<WriteModel<DbInventory>>();
        }

        private RpcSyncEngineInventories inventoryRpcEngine;
        private List<WriteModel<DbInventory>> inventoryWrites;

        public override async Task OnProcessingBegin()
        {
            await base.OnProcessingBegin();
        }

        public override void HandleItem(I obj)
        {
            base.HandleItem(obj);

            //Get inventory data
            InventoriesData inventory = GetInventoryDataOfObject(obj);
            if (inventory == null)
                return;

            //Get the type and object ID
            ulong id = GetArkIdOfObject(obj);
            DbInventory_InventoryType type = GetStructureTypeOfObject(obj);
            int tribe = GetTribeIdFromItem(obj);

            //Convert objects
            DbInventory_InventoryItem[] items = new DbInventory_InventoryItem[inventory.items.Length];
            for (int i = 0; i < items.Length; i += 1)
            {
                var data = inventory.items[i];

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
                Dictionary<string, string> custom = new Dictionary<string, string>();
                if (data.cname.Length > 0)
                    custom.Add("CNAME", data.cname);

                //Add
                items[i] = new DbInventory_InventoryItem
                {
                    item_id = Program.GetMultipartID(data.i1, data.i2),
                    classname = data.c,
                    durability = data.d,
                    stack_size = data.q,
                    custom_data = custom,
                    flags = flag
                };
            }

            //Make filter
            var filterBuilder = Builders<DbInventory>.Filter;
            var filter = filterBuilder.Eq("holder_id", id) & filterBuilder.Eq("holder_type", type) & filterBuilder.Eq("server_id", server._id);

            //Make update
            var updateBuilder = Builders<DbInventory>.Update;
            var update = updateBuilder.Set("items", items)
                .Set("last_update_time", DateTime.UtcNow)
                .SetOnInsert("created_time", DateTime.UtcNow)
                .SetOnInsert("holder_type", type)
                .SetOnInsert("holder_id", id)
                .SetOnInsert("server_id", server._id)
                .SetOnInsert("tribe_id", tribe);

            //Make command
            var command = new UpdateOneModel<DbInventory>(filter, update);
            command.IsUpsert = true;

            //Add
            inventoryWrites.Add(command);

            //Convert RPC items
            NetInventory_Item[] itemsConverted = new NetInventory_Item[items.Length];
            for (var i = 0; i < items.Length; i += 1)
            {
                itemsConverted[i] = NetInventory_Item.ConvertItem(items[i]);
            }

            //Add RPC command
            inventoryRpcEngine.AddItem(new NetInventory
            {
                holder_id = id.ToString(),
                holder_type = type,
                items = itemsConverted,
                tribe_id = tribe
            });
        }

        public override async Task OnProcessingEnd()
        {
            await base.OnProcessingEnd();

            //Write
            if (inventoryWrites.Count > 0)
                await conn.content_inventories.BulkWriteAsync(inventoryWrites);

            //Send RPC
            inventoryRpcEngine.SendAll(conn, server);
        }

        public abstract InventoriesData GetInventoryDataOfObject(I obj);

        public abstract ulong GetArkIdOfObject(I obj);

        public abstract DbInventory_InventoryType GetStructureTypeOfObject(I obj);

        public abstract int GetTribeIdFromItem(I obj);
    }
}
