using System;
using System.Globalization;

namespace Almanac.Model.Abstractions
{
    public class LocalizedString : IEquatable<LocalizedString>
    {
        public string Text { get; }

        public CultureInfo CultureInfo { get; set; }

        public LocalizedString(string text) : this(text, null) {}

        public LocalizedString(string text, CultureInfo cultureInfo)
        {
            Text = text;
            CultureInfo = cultureInfo;
        }

        public bool Equals(LocalizedString other)
        {
            return String.Equals(Text, other.Text) 
                && Equals(CultureInfo, other.CultureInfo);
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
            return Equals((LocalizedString) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return (Text?.GetHashCode() ?? 0) * 397;
            }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}