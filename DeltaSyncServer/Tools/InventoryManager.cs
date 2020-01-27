using DeltaSyncServer.Entities.DinoPayload;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Tools
{
    public static class InventoryManager
    {
        public static void QueueInventoryItem(List<WriteModel<DbItem>> itemActions, DinoItem i, string parent_id, DbInventoryParentType parent_type, string server_id, int tribe_id, ulong revision_id, byte revision_index, string custom_data_name = null, string custom_data_value = null)
        {
            //Convert item
            DbItem item = ConvertToItem(i, parent_id, parent_type, server_id, tribe_id, revision_id, revision_index);

            //Add custom datas
            item.custom_data_name = custom_data_name;
            item.custom_data_value = custom_data_value;

            //Add to queue
            AddItemToQueue(itemActions, item);
        }

        private static DbItem ConvertToItem(DinoItem i, string parent_id, DbInventoryParentType parent_type, string server_id, int tribe_id, ulong revision_id, byte revision_index)
        {
            //Convert item
            DbItem item = new DbItem
            {
                classname = Program.TrimArkClassname(i.classname),
                crafter_name = "",
                crafter_tribe = "",
                is_blueprint = i.blueprint,
                is_engram = false,
                item_id = Program.GetMultipartID(i.id1, i.id2),
                last_durability_decrease_time = -1,
                parent_id = parent_id,
                parent_type = parent_type,
                saved_durability = i.durability,
                server_id = server_id,
                stack_size = i.count,
                tribe_id = tribe_id,
                revision_id = revision_id,
                revision_type = revision_index
            };
            return item;
        }

        public static void AddItemToQueue(List<WriteModel<DbItem>> itemActions, DbItem item)
        {
            //Create filter for updating this dino
            var filterBuilder = Builders<DbItem>.Filter;
            var filter = filterBuilder.Eq("item_id", item.item_id) & filterBuilder.Eq("server_id", item.server_id);

            //Now, add (or insert) this into the database
            var a = new ReplaceOneModel<DbItem>(filter, item);
            a.IsUpsert = true;
            itemActions.Add(a);
        }

        public static async Task UpdateInventoryItems(List<WriteModel<DbItem>> itemActions, DeltaConnection conn)
        {
            if (itemActions.Count > 0)
            {
                var response = await conn.content_items.BulkWriteAsync(itemActions);
                itemActions.Clear();
            }
        }
    }
}
