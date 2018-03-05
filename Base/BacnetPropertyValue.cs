using System.Collections.Generic;
using System.Linq;

namespace System.IO.BACnet
{
    // TODO you have been flagged for refactoring due to un-C#-iness
    public struct BacnetPropertyValue : IEquatable<BacnetPropertyValue>
    {
        public BacnetPropertyReference property;
        public IList<BacnetValue> value;
        public byte priority;

        public override string ToString()
        {
            return property.ToString();
        }

        public bool Equals(BacnetPropertyValue other)
            => property.Equals(other.property)
               && value.SequenceEqual(other.value)
               && priority == other.priority;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BacnetPropertyValue propertyValue && Equals(propertyValue);
        }

        // TODO BUG FIXME fields used in GetHashCode MUST be immutable or you will break dictionaries & co
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = property.GetHashCode();
                hashCode = (hashCode * 397) ^ (value != null ? value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ priority.GetHashCode();
                return hashCode;
            }
        }
    }
}