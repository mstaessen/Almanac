using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class AttendeeType : PropertyValue
    {
        private static readonly PropertyValueRegistry<AttendeeType> Registry = new PropertyValueRegistry<AttendeeType>();

        public static AttendeeType Unknown { get; } = FromString("UNKNOWN");
        public static AttendeeType Individual { get; } = FromString("INDIVIDUAL");
        public static AttendeeType Group { get; } = FromString("GROUP");
        public static AttendeeType Resource { get; } = FromString("RESOURCE");
        public static AttendeeType Room { get; } = FromString("ROOM");

        public static AttendeeType FromString(string value)
        {
            return Registry.FromString(value);
        }
    }
}