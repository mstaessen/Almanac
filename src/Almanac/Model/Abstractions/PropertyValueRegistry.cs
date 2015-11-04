using System;
using System.Collections.Generic;

namespace Almanac.Model.Abstractions
{
    public class PropertyValueRegistry<T> 
        where T : PropertyValue, new()
    {
        private readonly IDictionary<string, T> values = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

        public void Register(T value)
        {
            values[value.Value] = value;
        }

        public T FromString(string value)
        {
            value = value.ToUpperInvariant();
            if (!values.ContainsKey(value)) {
                Register(Create(value));
            }
            return values[value];
        }

        private T Create(string value)
        {
            var item = new T {
                Value = value
            };
            return item;
        }
    }
}