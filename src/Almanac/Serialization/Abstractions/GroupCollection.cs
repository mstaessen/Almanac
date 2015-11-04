using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Almanac.Serialization.Abstractions
{
    public class GroupCollection<TKey, TValue> : IGroupCollection<TKey, TValue>
    {
        private readonly Func<TValue, TKey> keyAccessor;
        private readonly IDictionary<TKey, IList<TValue>> elements;

        public GroupCollection(Func<TValue, TKey> keyAccessor) : this(keyAccessor, EqualityComparer<TKey>.Default) {}

        public GroupCollection(Func<TValue, TKey> keyAccessor, IEqualityComparer<TKey> comparer)
        {
            this.keyAccessor = keyAccessor;
            elements = new Dictionary<TKey, IList<TValue>>(comparer);
        }

        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            return elements.Select(x => new Group(x.Key, x.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(TKey key)
        {
            return elements.ContainsKey(key);
        }

        public int Count => elements.Count;

        public IEnumerable<TValue> this[TKey key] => elements.ContainsKey(key)
            ? elements[key]
            : Enumerable.Empty<TValue>();

        public void Add(TValue value)
        {
            var key = keyAccessor(value);
            if (!elements.ContainsKey(key)) {
                elements[key] = new List<TValue>();
            }
            elements[key].Add(value);
        }

        private class Group : IGrouping<TKey, TValue>, IEquatable<Group>
        {
            public TKey Key { get; }

            private IEnumerable<TValue> Value { get; }

            public Group(TKey key, IEnumerable<TValue> value)
            {
                Key = key;
                Value = value;
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return Value.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Equals(Group other)
            {
                return EqualityComparer<TKey>.Default.Equals(Key, other.Key) 
                    && Value.SequenceEqual(other);
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
                return Equals((Group) obj);
            }

            public override int GetHashCode()
            {
                unchecked {
                    return (EqualityComparer<TKey>.Default.GetHashCode(Key) * 397) ^ (Value?.GetHashCode() ?? 0);
                }
            }
        }
    }
}