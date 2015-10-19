using System.Collections.Generic;

namespace Almanac.Model.Abstractions
{
    public class PropertyValueRegistry<T, TValue> where T : PropertyValue<TValue>
    {
        private static readonly IDictionary<TValue, T> Registry = new Dictionary<TValue, T>();

        public static void Register(T value)
        {
            Registry[value.Value] = value;
        }

        public static T FromString(TValue value)
        {
            if (Registry.ContainsKey(value)) {
                return Registry[value];
            }
            return null;
        }
    }
}