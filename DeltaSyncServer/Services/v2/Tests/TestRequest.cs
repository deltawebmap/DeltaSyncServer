using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2.Tests
{
    public class TestRequest : InjestServerAuthDeltaService
    {
        public TestRequest(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            Console.WriteLine("TEST TO " + e.Request.Path);
            string r;
            using (StreamReader sr = new StreamReader(e.Request.Body))
                r = await sr.ReadToEndAsync();
            Console.WriteLine(r);
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
