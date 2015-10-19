using System;

namespace Almanac.Serialization
{
    public static class DateTimeExtensions
    {
        internal static string ToICalDateTime(this DateTime datetime)
        {
            return datetime.Kind == DateTimeKind.Utc 
                ? datetime.ToString("yyyyMMddTHHmmssZ")
                : datetime.ToString("yyyyMMddTHHmmss");
        }
    }
}