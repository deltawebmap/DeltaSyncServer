using DeltaSyncServer.Services.v2;
using LibDeltaSystem;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Services.Definitions
{
    public class CleanIdsDefinition : DeltaWebServiceDefinition
    {
        public override string GetTemplateUrl()
        {
            return "/v1/clean_ids";
        }

        public override DeltaWebService OpenRequest(DeltaConnection conn, HttpContext e)
        {
            return new CleanIdsRequestV2(conn, e);
        }
    }
}
