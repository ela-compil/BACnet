using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.IO.BACnet
{
    public class BacnetWriteAccessSpecification : IEquatable<BacnetWriteAccessSpecification>
    {
        public BacnetObjectId ObjectId { get; }
        public ReadOnlyCollection<Property> Properties { get; }

        public BacnetWriteAccessSpecification(BacnetObjectId objectId, IEnumerable<Property> properties)
        {
            if(properties == null)
                throw new ArgumentNullException(nameof(properties));

            ObjectId = objectId;
            Properties = properties.ToList().AsReadOnly();
        }

        public bool Equals(BacnetWriteAccessSpecification other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ObjectId.Equals(other.ObjectId) && Properties.SequenceEqual(other.Properties);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BacnetWriteAccessSpecification) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ObjectId.GetHashCode() * 397) ^ Properties.GetHashCode();
            }
        }

        public class Property : IEquatable<Property>
        {
            public BacnetPropertyIds Id { get; }
            public uint? ArrayIndex { get; }
            public BacnetValue Value { get; }
            public uint? Priority { get; }

            public Property(BacnetPropertyIds id, BacnetValue value, uint? arrayIndex = null, uint? priority = null)
            {
                Id = id;
                ArrayIndex = arrayIndex;
                Value = value;
                Priority = priority;
            }

            public bool Equals(Property other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Id == other.Id && ArrayIndex == other.ArrayIndex && Value.Equals(other.Value) && Priority == other.Priority;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Property)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int)Id;
                    hashCode = (hashCode * 397) ^ ArrayIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ Value.GetHashCode();
                    hashCode = (hashCode * 397) ^ Priority.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
