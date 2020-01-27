using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Entities.Analytics.TimeAnalyticsPayload
{
    public class TimeAnalyticsRequestItem
    {
        public string key;
        public float durationms;
        public int resultlength;
        public string action;
        public string extras;
        public double time;
    }
}
