using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2
{
    public class CleanIdsRequestV2 : InjestServerAuthDeltaService
    {
        public CleanIdsRequestV2(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            //Decode
            RequestData request = await DecodePOSTBody<RequestData>();

            //If there is no data, skip
            if (request.ids.Length == 0)
            {
                await WriteIngestEndOfRequest();
                return;
            }

            //Based on type, execute
            if (request.type == "STRUCTURES")
                await ExecuteStructures(request.ids);

            await WriteIngestEndOfRequest();
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }

        private async Task ExecuteStructures(int[] ids)
        {
            //Create filter
            var structureFilterBuilder = Builders<DbStructure>.Filter;
            List<FilterDefinition<DbStructure>> idFilters = new List<FilterDefinition<DbStructure>>();
            foreach (int i in ids)
                idFilters.Add(structureFilterBuilder.Eq("structure_id", i));
            var structureFilter = structureFilterBuilder.Eq("server_id", server._id) & structureFilterBuilder.Not(structureFilterBuilder.Or(idFilters));

            //Execute
            var removed = await RunKeepFilter(structureFilter, conn.content_structures);

            //Convert to list of IDs to send over RPC
            List<string> removeIds = new List<string>();
            foreach (var r in removed)
                removeIds.Add(r.structure_id.ToString());
            await SendRemoveRPC(2, removeIds);
        }

        private async Task<List<T>> RunKeepFilter<T>(FilterDefinition<T> filter, IMongoCollection<T> collection)
        {
            //Find elements
            var delete = await (await collection.FindAsync(filter)).ToListAsync();
            
            //Remove
            if(delete.Count > 0)
                await collection.DeleteManyAsync(filter);

            return delete;
        }

        private async Task SendRemoveRPC(int type, List<string> ids)
        {

        }

        class RequestData
        {
            public string type;
            public int[] ids;
        }
    }
}
