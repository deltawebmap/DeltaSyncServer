using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using ArkWebMapGatewayClient.Sender;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer
{
    class Program
    {
        public static DeltaConnection conn;
        public static DeltaPrimalDataCache primal_data;

        public const string CLIENT_NAME = "sync-mod-prod";
        public const int VERSION_MAJOR = 0;
        public const int VERSION_MINOR = 1;

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            //Connect to database
            conn = new DeltaConnection(@"E:\database_config.json", CLIENT_NAME, VERSION_MAJOR, VERSION_MINOR);
            await conn.Connect();

            //Set up primal data
            primal_data = new DeltaPrimalDataCache();

            //Start HTTP server
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    IPAddress addr = IPAddress.Any;
                    options.Listen(addr, 43289);
                })
                .UseStartup<Program>()
                .Build();

            await host.RunAsync();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(HttpHandler.OnHttpRequest);
        }

        public static T DecodeStreamAsJson<T>(Stream s)
        {
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(s))
            using (var reader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<T>(reader);
            }
        }

        public static async Task WriteStringToStream(Stream s, string r)
        {
            byte[] b = Encoding.UTF8.GetBytes(r);
            await s.WriteAsync(b);
        }

        public static async Task<DbServer> ForceAuthServer(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate server
            DbServer server = await Program.conn.AuthenticateServerTokenAsync(e.Request.Query["token"]);

            //Fail if this isn't correct
            if(server == null)
            {
                e.Response.StatusCode = 401;
                await WriteStringToStream(e.Response.Body, "Not Authenticated");
            }

            return server;
        }

        public static string TrimArkClassname(string name)
        {
            if (name.EndsWith("_C"))
                return name.Substring(0, name.Length - 2);
            return name;
        }

        public static ulong GetMultipartID(uint u1, uint u2)
        {
            byte[] buf = new byte[8];
            BitConverter.GetBytes(u1).CopyTo(buf, 0);
            BitConverter.GetBytes(u2).CopyTo(buf, 4);
            return BitConverter.ToUInt64(buf, 0);
        }
    }
}
