using System;
using System.Collections.Generic;
using System.Linq;

namespace Almanac.Serialization.Abstractions
{
    public class Parameter : IEquatable<Parameter>
    {
        private readonly IList<string> values = new List<string>();

        public string Name { get; }

        public IEnumerable<string> Values => values; 

        public bool Quoted { get; set; }

        public Parameter(string name)
        {
            Name = name;
        }

        public void AddValue(string value)
        {
            values.Add(value);
        }

        public bool Equals(Parameter other)
        {
            return String.Equals(Name, other.Name) 
                && values.SequenceEqual(other.values);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (GetType() != obj.GetType()) {
                return false;
            }
            return Equals((Parameter) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((Name?.GetHashCode() ?? 0) * 397) 
                    ^ (values?.GetHashCode() ?? 0);
            }
        }
    }
}