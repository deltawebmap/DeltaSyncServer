using DeltaSyncServer.Services.Definitions;
using DeltaSyncServer.Services.Definitions.Tests;
using LibDeltaSystem;
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

        public const byte VERSION_MAJOR = 0;
        public const byte VERSION_MINOR = 5;

        static void Main(string[] args)
        {
            //Connect to database
            conn = DeltaConnection.InitDeltaManagedApp(args, VERSION_MAJOR, VERSION_MINOR, new SyncCoreNet());

            //Start server
            DeltaWebServer server = new DeltaWebServer(conn, conn.GetUserPort(0));
            server.AddService(new ConfigRequestDefinition());
            server.AddService(new RegisterRequestDefinition());
            server.AddService(new DinosRequestDefinition());
            server.AddService(new LiveRequestDefinition());
            server.AddService(new PlayerProfilesRequestDefinition());
            server.AddService(new StructuresRequestDefinition());
            server.AddService(new CleanIdsDefinition());
            server.AddService(new RpcAckDefinition());
            server.AddService(new LiveDinosDefinition());
            server.AddService(new TestRequestDefinition());

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
            byte[] buf = new byte[8];
            BitConverter.GetBytes(u1).CopyTo(buf, 0);
            BitConverter.GetBytes(u2).CopyTo(buf, 4);
            return BitConverter.ToUInt64(buf, 0);
        }
    }
}
