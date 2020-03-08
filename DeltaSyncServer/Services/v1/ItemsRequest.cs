using DeltaSyncServer.Entities;
using DeltaSyncServer.Entities.ItemsPayload;
using DeltaSyncServer.Tools;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.Entities.ArkEntries.Dinosaur;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v1
{
    public static class ItemsRequest
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Decode
            RevisionMappedDataPutRequest<ItemData> request = Program.DecodeStreamAsJson<RevisionMappedDataPutRequest<ItemData>>(e.Request.Body);

            //Get primal data
            var pack = await Program.conn.GetPrimalDataPackage(server.mods);

            //Create queues
            List<WriteModel<DbDino>> dinoQueue = new List<WriteModel<DbDino>>();
            List<WriteModel<DbItem>> queue = new List<WriteModel<DbItem>>();

            //Add items
            foreach (var i in request.data)
            {
                //Get parent ID
                string parentId = i.iid1.ToString();
                if (i.imp)
                    parentId = Program.GetMultipartID((uint)i.iid1, (uint)i.iid2).ToString();

                //Get item entry
                ItemEntry ientry = await pack.GetItemEntryByClssnameAsnyc(i.classname);
                if (ientry == null)
                    continue;
                
                //Convert
                DbItem item = new DbItem
                {
                    classname = Program.TrimArkClassname(i.classname),
                    crafter_name = "",
                    crafter_tribe = "",
                    is_blueprint = false,
                    is_engram = false,
                    item_id = Program.GetMultipartID((uint)i.id1, (uint)i.id2),
                    last_durability_decrease_time = -1,
                    parent_id = parentId,
                    parent_type = (DbInventoryParentType)i.it,
                    saved_durability = i.durability,
                    server_id = server._id,
                    stack_size = i.count,
                    tribe_id = i.tribe,
                    revision_id = request.revision_id,
                    revision_type = request.revision_index,
                    entry_display_name = ientry.name
                };

                //Add cryo dino if this is one
                if (i.cryo != null)
                {
                    try
                    {
                        DbDino d = CryoStorageTool.QueueDino(dinoQueue, out DinosaurEntry entry, server, parentId, request.revision_id, request.revision_index, (int)item.parent_type, item.item_id, pack, i.cryo);
                        if (d != null)
                        {
                            item.custom_data_name = "CRYOPOD";
                            item.custom_data_value = d.dino_id.ToString();
                        }
                    } catch (Exception ex)
                    {
                        Console.WriteLine("Failed to read cryo data: " + ex.Message);
                    }
                }

                //Add to queue
                InventoryManager.AddItemToQueue(queue, item);
            }

            //Apply updates
            await InventoryManager.UpdateInventoryItems(queue, Program.conn);
            if (dinoQueue.Count > 0)
            {
                await Program.conn.content_dinos.BulkWriteAsync(dinoQueue);
                dinoQueue.Clear();
            }
        }
    }
}
