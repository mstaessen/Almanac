using System.Linq;

namespace Almanac.Serialization.Abstractions
{
    public interface IGroupCollection<TKey, TValue> : ILookup<TKey, TValue>
    {
        void Add(TValue value);
    }
}