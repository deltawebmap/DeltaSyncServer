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

            //Clear all entries with a revision id lower than this, confining this to our selected revision index
            await RunDelete(Program.conn.content_structures, server.id, s.value, s.key);
            await RunDelete(Program.conn.content_dinos, server.id, s.value, s.key);
            await RunDelete(Program.conn.content_items, server.id, s.value, s.key);

            //Write OK
            e.Response.StatusCode = 200;
            await Program.WriteStringToStream(e.Response.Body, "OK");
        }

        private static async Task RunDelete<T>(IMongoCollection<T> collec, string serverId, ulong revision_id, byte revision_index)
        {
            var filterBuilder = Builders<T>.Filter;
            var filter = filterBuilder.Lt("revision_id", revision_id) & filterBuilder.Eq("server_id", serverId) & filterBuilder.Eq("revision_type", revision_index);
            await collec.DeleteManyAsync(filter);
        }
    }
}
