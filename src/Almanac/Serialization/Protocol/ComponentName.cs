namespace Almanac.Serialization.Protocol
{
    /// <summary>
    /// http://www.iana.org/assignments/icalendar/icalendar.xhtml#components
    /// </summary>
    public static class ComponentName
    {
        public const string Calendar = "VCALENDAR";
        public const string Event = "VEVENT";
        public const string Todo = "VTODO";
        public const string Journal = "VJOURNAL";
        public const string FreeBusy = "VFREEBUSY";
        public const string TimeZone = "VTIMEZONE";
        public const string Alarm = "VALARM";
        public const string TimeZoneStandard = "STANDARD";
        public const string TimeZoneDaylight = "DAYLIGHT";
    }
}