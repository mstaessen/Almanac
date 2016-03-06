using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class Priority : PropertyValue
    {
        private static readonly PropertyValueRegistry<Priority> Registry = new PropertyValueRegistry<Priority>();

        public static Priority Undefined { get; } = FromString("0");
        public static Priority High { get; } = FromString("1");
        public static Priority Low { get; } = FromString("9");
        public static Priority Medium { get; } = FromString("5");

        public static Priority FromString(string value)
        {
            return Registry.FromString(value);
        }
    }

}