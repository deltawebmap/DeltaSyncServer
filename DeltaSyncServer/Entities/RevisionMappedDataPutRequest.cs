using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities
{
    /// <summary>
    /// Represents revision mapped data (dinos, structures, etc) incoming.
    /// </summary>
    public class RevisionMappedDataPutRequest<T>
    {
        public T[] data;
        public ulong revision_id;
        public byte revision_index;
    }
}
