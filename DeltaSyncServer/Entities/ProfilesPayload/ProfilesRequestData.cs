using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.ProfilesPayload
{
    public class ProfilesRequestData
    {
        public ProfilesTribeData[] tribes;
        public ProfilesProfileData[] player_profiles;
    }
}
