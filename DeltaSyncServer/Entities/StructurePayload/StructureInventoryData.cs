using DeltaSyncServer.Entities.DinoPayload;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.StructurePayload
{
    public class StructureInventoryData
    {
        public DinoItem[] items;
        public int max;
        public string name;
    }
}
