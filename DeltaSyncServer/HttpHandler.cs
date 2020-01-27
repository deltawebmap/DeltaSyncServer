using DeltaSyncServer.Services;
using DeltaSyncServer.Services.v1;
using DeltaSyncServer.Services.v1.Analytics;
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
                else if (e.Request.Path == "/v1/items")
                    await ItemsRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/eggs")
                    await EggsRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/settings")
                    await SettingsRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/update_revision_id")
                    await UpdateRevisionIdRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/live")
                    await LiveRequest.OnHttpRequest(e);
                else if (e.Request.Path == "/v1/analytics/time")
                    await TimeAnalyticsRequest.OnHttpRequest(e);
                else
                {
                    e.Response.StatusCode = 404;
                    await Program.WriteStringToStream(e.Response.Body, "Not Found");
                }
            } catch (Exception ex)
            {
                e.Response.StatusCode = 500;
                await Program.WriteStringToStream(e.Response.Body, "ERROR\r\n\r\n"+ex.Message+"\r\n\r\n"+ex.StackTrace+ "\r\n\r\n(C) DeltaWebMap 2020, RomanPort 2020 - https://github.com/deltawebmap/");
                return;
            }
        }
    }
}
