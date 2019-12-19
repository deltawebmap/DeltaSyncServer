using DeltaSyncServer.Services;
using DeltaSyncServer.Services.v1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer
{
    public static class HttpHandler
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            Console.WriteLine(e.Request.Path);

            try
            {
                if (e.Request.Path == "/config")
                    await ConfigService.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/dinos")
                    await DinosRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/structures")
                    await StructuresRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/profiles")
                    await ProfilesRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/players")
                    await PlayersRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/eggs")
                    await EggsRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/settings")
                    await SettingsRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/update_revision_id")
                    await UpdateRevisionIdRequest.OnHttpRequest(e);
                else
                {
                    e.Response.StatusCode = 404;
                    await Program.WriteStringToStream(e.Response.Body, "Not Found");
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }
    }
}
