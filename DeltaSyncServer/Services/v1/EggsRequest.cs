using DeltaSyncServer.Entities.EggPayload;
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
    public static class EggsRequest
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Decode request data
            EggRequestData request = Program.DecodeStreamAsJson<EggRequestData>(e.Request.Body);

            //Generate salt
            int salt = Program.rand.Next();

            //Loop through and add eggs
            List<WriteModel<DbEgg>> actions = new List<WriteModel<DbEgg>>();
            foreach (var d in request.data)
            {
                //Get the egg ID
                ulong id = Program.GetMultipartID(d.id1, d.id2);

                //Attempt to find this already existing in the database
                DbEgg egg = await DbEgg.GetEggByItemID(Program.conn, server.id, id);
                if(egg == null)
                {
                    egg = new DbEgg
                    {
                        _id = MongoDB.Bson.ObjectId.GenerateNewId(),
                        sent_notifications = new List<string>(),
                        placed_time = DateTime.UtcNow,
                        tribe_id = d.tribe_id,
                        item_id = id,
                        server_id = server.id
                    };
                }

                //Write updated data
                egg.min_temperature = d.min_temp;
                egg.max_temperature = d.max_temp;
                egg.health = d.health;
                egg.incubation = d.incubation / 100;
                egg.tribe_id = d.tribe_id;
                egg.hatch_time = DateTime.UtcNow.AddSeconds(d.time_remaining);
                egg.updated_time = DateTime.UtcNow;
                egg.current_temperature = d.temp;
                egg.location = d.pos;
                egg.egg_type = d.type;
                egg.parents = d.parents;
                egg.updater_salt = salt;

                //Update
                var a = new ReplaceOneModel<DbEgg>(DbEgg.GetFilterDefinition(server.id, id), egg);
                a.IsUpsert = true;
                actions.Add(a);
            }

            if(actions.Count > 0)
            {
                //Commit actions
                await Program.conn.content_eggs.BulkWriteAsync(actions);

                //Remove old eggs
                var filterBuilder = Builders<DbEgg>.Filter;
                var filter = filterBuilder.Eq("server_id", server.id) & filterBuilder.Ne("updater_salt", salt);
                await Program.conn.content_eggs.DeleteManyAsync(filter);
            }
        }
    }
}
