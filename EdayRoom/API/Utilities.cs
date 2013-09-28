using System;

namespace EdayRoom.API
{
    public static class Utilities
    {
        public static double MilliTimeStamp(DateTime date)
        {
            var d1 = new DateTime(1970, 1, 1);
            DateTime d2 = date.ToUniversalTime();
            var ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return ts.TotalMilliseconds;
        }
    }
}