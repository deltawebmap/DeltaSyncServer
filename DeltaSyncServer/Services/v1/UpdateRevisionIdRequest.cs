using DeltaSyncServer.Entities.UpdateRevisionIdPayload;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v1
{
    public static class UpdateRevisionIdRequest
    {
        /// <summary>
        /// To: /v1/update_revision_id
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Read payload data
            UpdateRevisionIdRequestData s = Program.DecodeStreamAsJson<UpdateRevisionIdRequestData>(e.Request.Body);

            //Update accordingly
            switch (s.name)
            {
                case "structures":
                    await UpdateStructures(server, s);
                    break;
                case "dinos":
                    await UpdateDinos(server, s);
                    break;
                default:
                    e.Response.StatusCode = 400;
                    await Program.WriteStringToStream(e.Response.Body, "Unexpected Type");
                    return;
            }

            //Write OK
            e.Response.StatusCode = 200;
            await Program.WriteStringToStream(e.Response.Body, "OK");
        }

        private static async Task UpdateStructures(DbServer server, UpdateRevisionIdRequestData payload)
        {
            //Update on server
            var updateBuilder = Builders<DbServer>.Update;
            var update = updateBuilder.Set("revision_id_structures", payload.value);
            await server.ExplicitUpdateAsync(update);

            //Find and remove older revisions
            {
                var filterBuilder = Builders<DbStructure>.Filter;
                var filter = filterBuilder.Lt("revision_id", payload.value) & filterBuilder.Eq("server_id", server.id);
                await Program.conn.content_structures.DeleteManyAsync(filter);
            }
            {
                var filterBuilder = Builders<DbItem>.Filter;
                var filter = filterBuilder.Lt("revision_id", payload.value) & filterBuilder.Eq("server_id", server.id) & filterBuilder.Eq("parent_type", DbInventoryParentType.Structure);
                await Program.conn.content_items.DeleteManyAsync(filter);
            }
        }

        private static async Task UpdateDinos(DbServer server, UpdateRevisionIdRequestData payload)
        {
            //Update on server
            var updateBuilder = Builders<DbServer>.Update;
            var update = updateBuilder.Set("revision_id_dinos", payload.value);
            await server.ExplicitUpdateAsync(update);

            //Find and remove older revisions
            {
                var filterBuilder = Builders<DbDino>.Filter;
                var filter = filterBuilder.Lt("revision_id", payload.value) & filterBuilder.Eq("server_id", server.id);
                await Program.conn.content_dinos.DeleteManyAsync(filter);
            }
            {
                var filterBuilder = Builders<DbItem>.Filter;
                var filter = filterBuilder.Lt("revision_id", payload.value) & filterBuilder.Eq("server_id", server.id) & filterBuilder.Eq("parent_type", DbInventoryParentType.Dino);
                await Program.conn.content_items.DeleteManyAsync(filter);
            }
        }
    }
}
