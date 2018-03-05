using System.Collections.Generic;
using System.Linq;

namespace System.IO.BACnet
{
    public struct BacnetReadAccessResult : IEquatable<BacnetReadAccessResult>
    {
        public BacnetObjectId objectIdentifier;
        public IList<BacnetPropertyValue> values;

        public BacnetReadAccessResult(BacnetObjectId objectIdentifier, IList<BacnetPropertyValue> values)
        {
            this.objectIdentifier = objectIdentifier;
            this.values = values;
        }

        public bool Equals(BacnetReadAccessResult other)
        {
            return objectIdentifier.Equals(other.objectIdentifier) && values.SequenceEqual(other.values);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BacnetReadAccessResult && Equals((BacnetReadAccessResult) obj);
        }

        // TODO BUG FIXME fields used in GetHashCode MUST be immutable or you will break dictionaries & co
        public override int GetHashCode()
        {
            unchecked
            {
                return (objectIdentifier.GetHashCode() * 397) ^ (values != null ? values.GetHashCode() : 0);
            }
        }
    }
}