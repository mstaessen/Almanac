using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class Method : PropertyValue
    {
        private static readonly PropertyValueRegistry<Method> Registry = new PropertyValueRegistry<Method>();
         
        public static Method Publish { get; } = FromString("PUBLISH");
        public static Method Request { get; } = FromString("REQUEST");
        public static Method Reply { get; } = FromString("REPLY");
        public static Method Add { get; } = FromString("ADD");
        public static Method Cancel { get; } = FromString("CANCEL");
        public static Method Refresh { get; } = FromString("REFRESH");
        public static Method Counter { get; } = FromString("COUNTER");
        public static Method DeclineCounter { get; } = FromString("DECLINECOUNTER");

        public static Method FromString(string value)
        {
            return Registry.FromString(value);
        }
    }
}