using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.Templates
{
    /// <summary>
    /// Authenticates a server for use for injest
    /// </summary>
    public abstract class InjestServerAuthDeltaService : DeltaWebService
    {
        public DbServer server;
        public DbSyncSavedState session;

        public InjestServerAuthDeltaService(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task<bool> OnPreRequest()
        {
            //Authenticate session
            session = await Program.conn.AuthenticateServerSessionTokenAsync(e.Request.Query["session_token"]);

            //Check if auth failed
            if (session == null)
            {
                await WriteString("Server authentication failed!", "text/plain", 401);
                return false;
            }

            //Get server
            server = await Program.conn.GetServerByIdAsync(MongoDB.Bson.ObjectId.Parse(session.server_id));
            if (server == null)
            {
                await WriteString("Server authentication failed!", "text/plain", 401);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Should be called to finish injest responses
        /// </summary>
        /// <returns></returns>
        public async Task WriteInjestEndOfRequest()
        {
            //This will likely be replaced with ACTUAL data sent at some point
            await WriteString($"OK~Delta Web Map Injest@{conn.system_version_major}.{conn.system_version_minor}", "text/plain", 200);
        }

        public async Task<List<D>> CreateOrUpdateItems<T, D>(T[] items, IMongoCollection<D> collection, CreateOrUpdateItems_GetFilter<T, D> getFilter, CreateOrUpdateItems_GetUpdate<T, D> getUpdate, CreateOrUpdateItems_GetNew<T, D> getNew)
        {
            //Attempt to update parts
            Task<D>[] updates = new Task<D>[items.Length];
            for (int i = 0; i < items.Length; i += 1)
            {
                updates[i] = collection.FindOneAndUpdateAsync(getFilter(items[i]), getUpdate(items[i]), new FindOneAndUpdateOptions<D, D>
                {
                    IsUpsert = false,
                    ReturnDocument = ReturnDocument.After
                });
            }

            //Wait for these to finish
            await Task.WhenAll(updates);

            //Identify items that need to be created (they haven't been in the db)
            List<D> writes = new List<D>();
            for (int i = 0; i < items.Length; i += 1)
            {
                //Determine if we must create data
                var result = updates[i].Result;
                bool exists = result != null;
                if (exists)
                    continue;

                //Create
                D input = getNew(items[i]);

                //Apply
                writes.Add(input);
            }

            //Apply writes (if any)
            if (writes.Count > 0)
                await collection.InsertManyAsync(writes);

            return writes;
        }
        public delegate FilterDefinition<D> CreateOrUpdateItems_GetFilter<T, D>(T item);
        public delegate UpdateDefinition<D> CreateOrUpdateItems_GetUpdate<T, D>(T item);
        public delegate D CreateOrUpdateItems_GetNew<T, D>(T item);
    }
}
