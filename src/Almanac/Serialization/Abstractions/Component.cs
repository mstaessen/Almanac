using System;
using System.Collections.Generic;
using System.Linq;

namespace Almanac.Serialization.Abstractions
{
    public class Component : IEquatable<Component>
    {
        private readonly IList<Component> components = new List<Component>();

        private readonly IList<Property> properties = new List<Property>();

        public string Name { get; }

        public virtual IEnumerable<Property> Properties => properties;

        public virtual IEnumerable<Component> Components => components;

        public Component(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
        }

        public virtual void AddProperty(Property property)
        {
            properties.Add(property);
        }

        public virtual void AddComponent(Component component)
        {
            components.Add(component);
        }

        public bool Equals(Component other)
        {
            return String.Equals(Name, other.Name)
                && properties.SequenceEqual(other.properties)
                && components.SequenceEqual(other.components);
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
            return Equals((Component) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                var hashCode = components?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (properties?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (Name?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}