using DeltaSyncServer.Entities.StructurePayload;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v1
{
    public static class StructuresRequest
    {
        /// <summary>
        /// To /v1/structures
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Read structures data
            StructureRequestData payload = Program.DecodeStreamAsJson<StructureRequestData>(e.Request.Body);

            //Convert all structures
            List<WriteModel<DbStructure>> structureActions = new List<WriteModel<DbStructure>>();
            List<WriteModel<DbItem>> itemActions = new List<WriteModel<DbItem>>();
            foreach (var s in payload.structures)
            {
                //Ignore if no tribe ID is set
                if (s.tribe == 0)
                    continue;
                
                //Convert
                DbStructure structure = new DbStructure
                {
                    classname = Program.TrimArkClassname(s.classname),
                    current_health = s.health,
                    current_item_count = 0,
                    has_inventory = false,
                    location = s.location,
                    max_health = s.max_health,
                    max_item_count = 0,
                    server_id = server.id,
                    structure_id = s.id,
                    tribe_id = s.tribe,
                    revision_id = payload.revision_id,
                    custom_name = null
                };

                //Set inventory
                if(s.inventory != null)
                {
                    //Set flags
                    structure.has_inventory = true;
                    structure.max_item_count = s.inventory.max;

                    //Set name, if any
                    if (s.inventory.name.Length > 0)
                        structure.custom_name = s.inventory.name;

                    //Add items
                    foreach (var i in s.inventory.items)
                    {
                        Tools.InventoryManager.QueueInventoryItem(itemActions, i, structure.structure_id.ToString(), DbInventoryParentType.Structure, server.id, structure.tribe_id, structure.revision_id);
                    }
                }

                //Create filter for updating this dino
                var filterBuilder = Builders<DbStructure>.Filter;
                var filter = filterBuilder.Eq("structure_id", s.id) & filterBuilder.Eq("server_id", server.id);

                //Now, update (or insert) this into the database
                var a = new ReplaceOneModel<DbStructure>(filter, structure);
                a.IsUpsert = true;
                structureActions.Add(a);
            }

            //Apply actions
            if (structureActions.Count > 0)
            {
                await Program.conn.content_structures.BulkWriteAsync(structureActions);
                structureActions.Clear();
            }
            await Tools.InventoryManager.UpdateInventoryItems(itemActions, Program.conn);

            //Write finished
            e.Response.StatusCode = 200;
            await Program.WriteStringToStream(e.Response.Body, "OK");
        }
    }
}
