using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.ResponsePayload
{
    public class StandardResponseData
    {
        public List<StandardResponseData_Event> events;
    }

    public class StandardResponseData_Event
    {
        public int op;
        public JObject payload;
    }
}
