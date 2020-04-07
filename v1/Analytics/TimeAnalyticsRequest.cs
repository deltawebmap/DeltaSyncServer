using DeltaSyncServer.Entities.Analytics.TimeAnalyticsPayload;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.Db.System.Analytics;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v1.Analytics
{
    public static class TimeAnalyticsRequest
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Authenticate state
            DbSyncSavedState state = await Program.ForceAuthSessionState(e);
            if (state == null)
                return;

            //Decode
            Dictionary<string, TimeAnalyticsRequestItem> request = Program.DecodeStreamAsJson<Dictionary<string, TimeAnalyticsRequestItem>>(e.Request.Body);

            //Add each analytics option
            List<WriteModel<DbModTimeAnalyticsObject>> actions = new List<WriteModel<DbModTimeAnalyticsObject>>();
            foreach (var r in request.Values)
            {
                //Create
                DbModTimeAnalyticsObject o = new DbModTimeAnalyticsObject
                {
                    action = r.action,
                    client_session = state._id,
                    client_version = state.mod_version,
                    key = r.key,
                    server_id = server._id,
                    time = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(r.time),
                    payload = new DbModTimeAnalyticsObject.DbModTimeAnalyticsObject_Payload
                    {
                        duration = r.durationms,
                        extras = r.extras,
                        length = r.resultlength
                    }
                };

                //Add
                actions.Add(new InsertOneModel<DbModTimeAnalyticsObject>(o));
            }

            //Apply
            if (actions.Count > 0)
                await Program.conn.system_analytics_time.BulkWriteAsync(actions);
        }
    }
}
