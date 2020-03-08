using LibDeltaSystem.Db.System;
using LibDeltaSystem.Db.System.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v1
{
    public static class SettingsRequest
    {
        /// <summary>
        /// To /v1/settings
        /// Used to put server settings
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;

            //Read data
            DbServerGameSettings s = Program.DecodeStreamAsJson<DbServerGameSettings>(e.Request.Body);
            server.game_settings = s;
            await server.UpdateAsync(Program.conn);

            //Write finished
            e.Response.StatusCode = 200;
            await Program.WriteStringToStream(e.Response.Body, "OK");
        }
    }
}
