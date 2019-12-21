using ARKDinoDataReader.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARKDinoDataReader.Entities
{
    public class DotArkLocationData
    {
        public float x;
        public float y;
        public float z;
        public float pitch;
        public float yaw;
        public float roll;

        public static DotArkLocationData ReadLocationData(IOMemoryStream da)
        {
            var stream = da;
            DotArkLocationData l = new DotArkLocationData();
            l.x = stream.ReadFloat();
            l.y = stream.ReadFloat();
            l.z = stream.ReadFloat();

            l.pitch = stream.ReadFloat();
            l.yaw = stream.ReadFloat();
            l.roll = stream.ReadFloat();

            return l;
        }

        public DotArkLocationData()
        {

        }

        public DotArkLocationData(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public string ToString
        {
            get
            {
                return $"X:{x} Y:{y} Z:{z} Pitch:{pitch} Yaw:{yaw} Roll:{roll}";
            }
        }
    }
}
