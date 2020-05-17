using DeltaSyncServer.Entities;
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
        public InjestServerV2SyncService(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            //Decode content
            RevisionMappedDataPutRequest<I> request = await DecodePOSTBody<RevisionMappedDataPutRequest<I>>();

            //Get primal data
            var primal = await Program.conn.GetPrimalDataPackage(server.mods);

            //Get the collection
            var collec = GetMongoCollection();

            //Run updates
            List<WriteModel<T>> writes = new List<WriteModel<T>>();
            foreach(var r in request.data)
            {
                //Make command
                var command = new UpdateOneModel<T>(CreateFilterDefinition(r), CreateUpdateDefinition(r, request.revision_id, request.revision_index));
                command.IsUpsert = true;

                //Add
                writes.Add(command);

                //Send RPC message
                FireRPCEvent(r);
            }

            //Run
            await collec.BulkWriteAsync(writes);

            //Post update
            await OnPostDbUpdate(request.data);

            //Finish
            await WriteInjestEndOfRequest();
        }

        /// <summary>
        /// Called after we push DB updates, right before the end. Can be used to add additional functonality
        /// </summary>
        /// <returns></returns>
        public virtual async Task OnPostDbUpdate(I[] data)
        {

        }

        /// <summary>
        /// Fires an RPC event for an item
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task FireRPCEvent(I data)
        {
            await RPCMessageTool.SendDbContentUpdateMessage(conn, GetRPCContentUpdateType(), GetRPCVersionOfItem(data), server._id, GetTribeIdFromItem(data));
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

        /// <summary>
        /// Returns the RPC content update type. 0 is dinos, for example
        /// </summary>
        /// <returns></returns>
        public abstract LibDeltaSystem.RPC.Payloads.Entities.RPCSyncType GetRPCContentUpdateType();

        /// <summary>
        /// Returns the tribe ID for a provided item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public abstract int GetTribeIdFromItem(I item);

        /// <summary>
        /// Gets the version of an item to send over RPC
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public abstract object GetRPCVersionOfItem(I item);

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
