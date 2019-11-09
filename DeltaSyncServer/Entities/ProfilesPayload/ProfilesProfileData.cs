using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.ProfilesPayload
{
    public class ProfilesProfileData
    {
        public string steam_id;
        public string ark_id;
        public string ark_name;
        public int tribe_id;
        public double last_login;
        public bool tribe_valid;
    }
}
