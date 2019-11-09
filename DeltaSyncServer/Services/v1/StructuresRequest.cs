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
            List<WriteModel<DbStructure>> actions = new List<WriteModel<DbStructure>>();
            foreach (var s in payload.structures)
            {
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
                    revision_id = payload.revision_id
                };

                //Create filter for updating this dino
                var filterBuilder = Builders<DbStructure>.Filter;
                var filter = filterBuilder.Eq("structure_id", s.id) & filterBuilder.Eq("server_id", server.id);

                //Now, add (or insert) this into the database
                var a = new ReplaceOneModel<DbStructure>(filter, structure);
                a.IsUpsert = true;
                actions.Add(a);
            }

            //Apply actions
            if (actions.Count > 0)
            {
                await Program.conn.content_structures.BulkWriteAsync(actions);
                actions.Clear();
            }

            //Write finished
            e.Response.StatusCode = 200;
            await Program.WriteStringToStream(e.Response.Body, "OK");
        }
    }
}
