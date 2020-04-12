using DeltaSyncServer.Services.v2;
using LibDeltaSystem;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Services.Definitions
{
    public class PlayerProfilesRequestDefinition : DeltaWebServiceDefinition
    {
        public override string GetTemplateUrl()
        {
            return "/v1/profiles";
        }

        public override DeltaWebService OpenRequest(DeltaConnection conn, HttpContext e)
        {
            return new PlayerProfilesRequestV2(conn, e);
        }
    }
}
