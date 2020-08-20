using DeltaSyncServer.Entities.ResponsePayload;
using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            server = await Program.conn.GetServerByIdAsync(session.server_id);
            if (server == null)
            {
                await WriteString("Server authentication failed!", "text/plain", 401);
                return false;
            }

            return true;
        }



        /// <summary>
        /// Decodes the request data, but patches ARK encoding errors
        /// </summary>
        /// <typeparam name="T">The type of data to serialize to</typeparam>
        /// <returns></returns>
        public async Task<T> DecodePOSTBodyArkPatch<T>()
        {
            //Read stream
            string buffer;
            using (StreamReader sr = new StreamReader(e.Request.Body))
                buffer = await sr.ReadToEndAsync();

            //Look to see if the content contains "nan,". This is sent as an invalid float from the ARK serializer. I don't know why it happens.
            //I asked about this on the ARK Modding Community Discord server here: https://discordapp.com/channels/153690873186484224/154037794585575424/745828554935107621
            //It isn't a nice solution, but we'll find and replace "nan," with "0". This may catch names containing "nan,", but the likelyhood of this happening unintentionally is near zero and even if it does happen it's a very safe patch
            //We'll log it if it does appear in case the problem solves itself
            if (buffer.Contains("nan,"))
            {
                //Log
                conn.Log("DecodePOSTBodyArkPatch", "Found invalid NAN data in ARK serialized json. Patching...", DeltaLogLevel.Alert);

                //Patch
                buffer = buffer.Replace("nan,", "0,");
            }

            //Assume this is JSON
            return JsonConvert.DeserializeObject<T>(buffer);
        }

        /// <summary>
        /// Should be called to finish injest responses
        /// </summary>
        /// <returns></returns>
        public async Task WriteIngestEndOfRequest()
        {
            //Get next events
            var events = await conn.GetQueuedSyncCommandsForServerById(server._id, true, 10);

            //If there are events, send them
            if(events.Count > 0)
            {
                //Create event payloads
                StandardResponseData p = new StandardResponseData
                {
                    events = new List<StandardResponseData_Event>()
                };
                foreach(var e in events)
                {
                    p.events.Add(new StandardResponseData_Event
                    {
                        op = e.opcode,
                        payload = e.DecodePayloadAsJObject(),
                        ack_token = e.id+"@"+e.sender_id
                    });
                }

                //Write
                await WriteString("D003" + JsonConvert.SerializeObject(p), "text/plain", 200);
            } else
            {
                //Write "OK"
                await WriteString("D004", "text/plain", 200);
            }
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
