using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class Classification : PropertyValue
    {
        private static readonly PropertyValueRegistry<Classification> Registry = new PropertyValueRegistry<Classification>();

        public static Classification Public { get; } = FromString("PUBLIC");

        public static Classification Private { get; } = FromString("PRIVATE");

        public static Classification Confidential { get; } = FromString("CONFIDENTIAL");

        public static Classification FromString(string value)
        {
            return Registry.FromString(value);
        }
    }
}