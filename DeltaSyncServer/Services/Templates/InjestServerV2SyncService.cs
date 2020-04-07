﻿using DeltaSyncServer.Entities;
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

            //We'll now attempt to just update these values
            Task<T>[] updates = new Task<T>[request.data.Length];
            for (int i = 0; i<request.data.Length; i+=1)
            {
                var r = request.data[i];
                updates[i] = collec.FindOneAndUpdateAsync(CreateFilterDefinition(r), CreateUpdateDefinition(r, request.revision_id, request.revision_index), new FindOneAndUpdateOptions<T, T>
                {
                    IsUpsert = false,
                    ReturnDocument = ReturnDocument.After
                });
            }

            //Wait for these to finish
            await Task.WhenAll(updates);

            //Identify items that need to be created (they haven't been in the db)
            List<T> writes = new List<T>();
            for (int i = 0; i < request.data.Length; i += 1)
            {
                //Determine if we must create data
                var r = request.data[i];
                var result = updates[i].Result;
                bool exists = result != null;
                if (exists)
                {
                    //Fire off RPC events
                    FireRPCEvent(result);

                    //Since we don't need to create, skip
                    continue;
                }

                //Create
                T input = CreateNewEntry(r, request.revision_id, request.revision_index);

                //Apply
                writes.Add(input);
            }

            //Apply writes (if any)
            if (writes.Count > 0)
                await collec.InsertManyAsync(writes);

            //Send new objects over RPC
            foreach (var w in writes)
                FireRPCEvent(w);

            //Finish
            Console.WriteLine(writes.Count);
            await WriteInjestEndOfRequest();
        }

        /// <summary>
        /// Fires an RPC event for an item
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task FireRPCEvent(T data)
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
        /// Creates a whole new entry
        /// </summary>
        /// <param name="data"></param>
        /// <param name="revision_id"></param>
        /// <param name="revision_index"></param>
        /// <returns></returns>
        public abstract T CreateNewEntry(I data, ulong revision_id, byte revision_index);

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
        public abstract int GetTribeIdFromItem(T item);

        /// <summary>
        /// Gets the version of an item to send over RPC
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public abstract object GetRPCVersionOfItem(T item);

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
