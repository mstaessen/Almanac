using System;
using System.Diagnostics;

namespace Almanac.Model.Abstractions
{
    [DebuggerDisplay("{Name} <{Email}>")]
    public abstract class Address : IEquatable<Address>
    {
        public string Email { get; }

        public LocalizedString Name { get; set; }

        protected Address(string email, LocalizedString name)
        {
            Email = email;
            Name = name;
        }

        public bool Equals(Address other)
        {
            return string.Equals(Email, other.Email);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((Address) obj);
        }

        public override int GetHashCode()
        {
            return Email?.GetHashCode() ?? 0;
        }
    }
}