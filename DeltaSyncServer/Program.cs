using DeltaSyncServer.Entities;
using DeltaSyncServer.Services.Definitions;
using DeltaSyncServer.Services.Definitions.Tests;
using LibDeltaSystem;
using LibDeltaSystem.Db.Content;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.WebFramework;
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
        public static ModRemoteConfig clientConfig;

        public const byte VERSION_MAJOR = 0;
        public const byte VERSION_MINOR = 15;

        static void Main(string[] args)
        {
            //Connect to database
            conn = DeltaConnection.InitDeltaManagedApp(args, DeltaCoreNetServerType.API_SYNC, VERSION_MAJOR, VERSION_MINOR);

            //Load client config
            clientConfig = new ModRemoteConfig(); // conn.GetUserConfig("sync_clientconfig.json", new ModRemoteConfig()).GetAwaiter().GetResult();

            //Start server
            DeltaWebServer server = new DeltaWebServer(conn, conn.GetUserPort(0));
            server.AddService(new ConfigRequestDefinition());
            server.AddService(new RegisterRequestDefinition());
            server.AddService(new PlayerProfilesRequestDefinition());
            server.AddService(new CleanIdsDefinition());
            server.AddService(new RpcAckDefinition());
            server.AddService(new PingRequestDefinition());
            server.AddService(new RealtimePlayersDefinition());
            //server.AddService(new TestRequestDefinition());

            //Run
            server.RunAsync().GetAwaiter().GetResult();
        }

        public static async Task WriteStringToStream(Stream s, string r)
        {
            byte[] b = Encoding.UTF8.GetBytes(r);
            await s.WriteAsync(b);
        }

        public static string TrimArkClassname(string name)
        {
            if (name.EndsWith("_C"))
                return name.Substring(0, name.Length - 2);
            return name;
        }

        public static ulong GetMultipartID(uint u1, uint u2)
        {
            return DbDino.ZipperDinoId(u1, u2);
        }
    }
}
