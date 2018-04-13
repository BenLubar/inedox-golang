using System;

namespace Inedo.Extensions.Golang
{
    internal static class TimeSpanExtensions
    {
        public static string ToGoString(this TimeSpan ts)
        {
            if (ts < TimeSpan.Zero)
            {
                return $"-{ts.Negate().ToGoString()}";
            }

            if (ts == TimeSpan.Zero)
            {
                return "0s";
            }

            if (ts.Ticks < TimeSpan.TicksPerSecond)
            {
                return $"{ts.TotalMilliseconds}ms";
            }

            return string.Concat(
                ts >= TimeSpan.FromHours(1) ? $"{(int)ts.TotalHours}h" : "",
                ts.Minutes != 0 ? $"{ts.Minutes}m" : "",
                ts.Ticks % TimeSpan.TicksPerMinute != 0 ? $"{ts.TotalSeconds % 60}s" : ""
            );
        }
    }
}
