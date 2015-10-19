using System;
using System.Collections.Generic;
using System.Linq;

namespace Almanac.Serialization.Abstractions
{
    public class Property : IEquatable<Property>
    {
        private readonly IList<Parameter> parameters = new List<Parameter>();

        public string Name { get; }

        public IEnumerable<Parameter> Parameters => parameters;

        public virtual string Value { get; internal set; }

        public Property(string name)
        {
            Name = name;
        }

        public void AddParameter(Parameter parameter)
        {
            parameters.Add(parameter);
        }

        public bool Equals(Property other)
        {
            return String.Equals(Name, other.Name)
                && Equals(Value, other.Value)
                && parameters.SequenceEqual(other.parameters);
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
            return Equals((Property) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                var hashCode = Name?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (parameters?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}