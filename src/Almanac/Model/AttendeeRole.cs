using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class AttendeeRole : PropertyValue
    {
        private static readonly PropertyValueRegistry<AttendeeRole> Registry = new PropertyValueRegistry<AttendeeRole>();

        public static AttendeeRole Chair { get; } = FromString("CHAIR");
        public static AttendeeRole RequiredParticipant { get; } = FromString("REQ-PARTICIPANT");
        public static AttendeeRole OptionalParticipant { get; } = FromString("OPT-PARTICIPANT");
        public static AttendeeRole NonParticipant { get; } = FromString("NON-PARTICIPANT");

        public static AttendeeRole FromString(string value)
        {
            return Registry.FromString(value);
        }
    }
}