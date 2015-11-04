using System.Diagnostics;

namespace Almanac.Model.Abstractions
{
    [DebuggerDisplay("{Latitude};{Longitude}")]
    public class Position
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}