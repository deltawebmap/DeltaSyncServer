using DeltaSyncServer.Services.Templates;
using LibDeltaSystem;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaSyncServer.Services.v2.Tests
{
    public class TestRequest : DeltaWebService
    {
        public TestRequest(DeltaConnection conn, HttpContext e) : base(conn, e)
        {
        }

        public override async Task OnRequest()
        {
            /*using (FileStream fs = new FileStream("E:\\test.bin", FileMode.Create))
                await e.Request.Body.CopyToAsync(fs);
            Console.WriteLine("Written");*/
        }

        public override async Task<bool> SetArgs(Dictionary<string, string> args)
        {
            return true;
        }
    }
}
