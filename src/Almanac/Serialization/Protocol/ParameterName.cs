namespace Almanac.Serialization.Protocol
{
    /// <summary>
    /// http://www.iana.org/assignments/icalendar/icalendar.xhtml#parameters
    /// </summary>
    public static class ParameterName
    {
        public static readonly string AlternateTextRepresentation = "ALTREP";
        public static readonly string CommonName = "CN";
        public static readonly string CalendarUserType = "CUTYPE";
        public static readonly string Delegatees = "DELEGATED-FROM";
        public static readonly string Delegators = "DELEGATED-TO";
        public static readonly string DirectoryEntryReference = "DIR";
        public static readonly string InlineEncoding = "ENCODING";
        public static readonly string FormatType = "FMTTYPE";
        public static readonly string FreeBusyType = "FBTYPE";
        public static readonly string Language = "LANGUAGE";
        public static readonly string Membership = "MEMBER";
        public static readonly string ParticipationStatus = "PARTSTAT";
        public static readonly string RecurrenceIdentifierRange = "RANGE";
        public static readonly string AlarmTriggerRelationship = "RELATED";
        public static readonly string RelationshipType = "RELTYPE";
        public static readonly string ParticipationRole = "ROLE";
        public static readonly string RsvpExpectation = "RSVP";
        public static readonly string SentBy = "SENT-BY";
        public static readonly string TimeZoneIdentifier = "TZID";
        public static readonly string ValueDataType = "VALUE";
    }
}