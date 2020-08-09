using DeltaSyncServer.Entities;
using DeltaSyncServer.Tools.RpcSyncEngine;
using LibDeltaSystem;
using LibDeltaSystem.Tools;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.Templates
{
    /// <summary>
    /// Service for v2 sync
    /// </summary>
    /// <typeparam name="I">The input data type, wrapped in a RevisionMappedDataPutRequest<I></typeparam>
    /// <typeparam name="T">The actual, db, data source</typeparam>
    public abstract class InjestServerV2SyncService<I, T> : InjestServerAuthDeltaService
    {
        public InjestServerV2SyncService(DeltaConnection conn, HttpContext e, BaseRpcSyncEngine<I> rpcEngine) : base(conn, e)
        {
            writes = new List<WriteModel<T>>();
            this.rpcEngine = rpcEngine;
        }

        private BaseRpcSyncEngine<I> rpcEngine;
        private List<WriteModel<T>> writes;

        public RevisionMappedDataPutRequest<I> request;
        public IMongoCollection<T> collec;

        public override async Task OnRequest()
        {
            //Begin
            await OnProcessingBegin();

            //Run updates
            foreach(var r in request.data)
            {
                HandleItem(r);
            }

            //End
            await OnProcessingBegin();

            //Finish
            await WriteInjestEndOfRequest();
        }

        public virtual async Task OnProcessingBegin()
        {
            //Decode content
            request = await DecodePOSTBody<RevisionMappedDataPutRequest<I>>();

            //Get the collection
            collec = GetMongoCollection();
        }

        public virtual async Task OnProcessingEnd()
        {
            //Write
            if (writes.Count > 0)
                await collec.BulkWriteAsync(writes);

            //Send RPC
            rpcEngine.SendAll(conn, server);
        }

        public virtual void HandleItem(I item)
        {
            //Make command
            var command = new UpdateOneModel<T>(CreateFilterDefinition(item), CreateUpdateDefinition(item, request.revision_id, request.revision_index));
            command.IsUpsert = true;

            //Add
            writes.Add(command);

            //Add RPC command
            rpcEngine.AddItem(item);
        }

        /// <summary>
        /// Creates an update definition from the primitive data we get
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract UpdateDefinition<T> CreateUpdateDefinition(I data, ulong revision_id, byte revision_index);

        /// <summary>
        /// Creates an filter definition to access this item uniquely
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract FilterDefinition<T> CreateFilterDefinition(I data);

        /// <summary>
        /// Gets the collection we will be doing work in
        /// </summary>
        /// <returns></returns>
        public abstract IMongoCollection<T> GetMongoCollection();

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
