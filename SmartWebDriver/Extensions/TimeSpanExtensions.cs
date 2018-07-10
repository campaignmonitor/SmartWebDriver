using System;

namespace SmartWebDriver.Extensions
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Milliseconds(this int @int)
        {
            return new TimeSpan(0, 0, 0, 0, @int);
        }

        public static TimeSpan Seconds(this int @int)
        {
            return new TimeSpan(0, 0, 0, @int, 0);
        }

        public static TimeSpan Minutes(this int @int)
        {
            return new TimeSpan(0, 0, @int, 0);
        }
    }
}
