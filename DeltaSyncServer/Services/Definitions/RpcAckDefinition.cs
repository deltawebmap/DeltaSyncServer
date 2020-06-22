using DeltaSyncServer.Services.v2;
using LibDeltaSystem;
using LibDeltaSystem.WebFramework;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Services.Definitions
{
    public class RpcAckDefinition : DeltaWebServiceDefinition
    {
        public override string GetTemplateUrl()
        {
            return "/v1/ack_event";
        }

        public override DeltaWebService OpenRequest(DeltaConnection conn, HttpContext e)
        {
            return new RpcAckRequestV2(conn, e);
        }
    }
}
