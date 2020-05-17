using DeltaSyncServer.Entities.InventoriesPayload;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static LibDeltaSystem.Db.Content.DbInventory;

namespace DeltaSyncServer.Services.Templates
{
    public abstract class InjestServerV2SyncInventoryService<I, T> : InjestServerV2SyncService<I, T>
    {
        public InjestServerV2SyncInventoryService(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnPostDbUpdate(I[] data)
        {
            await base.OnPostDbUpdate(data);

            //Process all inventories
            Task[] tasks = new Task[data.Length];
            for (int i = 0; i < data.Length; i++)
                tasks[i] = ProcessNewInventory(data[i]);

            //Wait for all to complete
            await Task.WhenAll(tasks);
        }

        private async Task ProcessNewInventory(I obj)
        {
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
            for(int i = 0; i<items.Length; i+=1)
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
                Dictionary<ushort, string> custom = new Dictionary<ushort, string>();
                if (data.cname.Length > 0)
                    custom.Add(0, data.cname);

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

            //Update or insert
            await conn.content_inventories.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<DbInventory, DbInventory>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            });
        }

        public abstract InventoriesData GetInventoryDataOfObject(I obj);

        public abstract ulong GetArkIdOfObject(I obj);

        public abstract DbInventory_InventoryType GetStructureTypeOfObject(I obj);
    }
}
