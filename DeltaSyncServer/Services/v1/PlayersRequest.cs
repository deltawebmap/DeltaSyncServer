using LibDeltaSystem.Db.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v1
{
    public static class PlayersRequest
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate
            DbServer server = await Program.ForceAuthServer(e);
            if (server == null)
                return;
            return; //TODO

            using (StreamReader sr = new StreamReader(e.Request.Body))
                Console.WriteLine(await sr.ReadToEndAsync());
        }
    }
}
