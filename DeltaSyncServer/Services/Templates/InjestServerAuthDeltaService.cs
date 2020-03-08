using LibDeltaSystem;
using LibDeltaSystem.Db.System;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
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
        
        public InjestServerAuthDeltaService(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task<bool> OnPreRequest()
        {
            //Authenticate server
            server = await Program.conn.AuthenticateServerTokenAsync(e.Request.Query["token"]);

            //Check if auth failed
            if (server == null)
            {
                await WriteString("Server authentication failed!", "text/plain", 401);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Should be called to finish injest responses
        /// </summary>
        /// <returns></returns>
        public async Task WriteInjestEndOfRequest()
        {
            //This will likely be replaced with ACTUAL data sent at some point
            await WriteString($"OK~Delta Web Map Injest@{conn.system_version_major}.{conn.system_version_minor}", "text/plain", 200);
        }
    }
}
