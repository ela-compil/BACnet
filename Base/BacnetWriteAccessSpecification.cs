using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.IO.BACnet
{
    public class BacnetWriteAccessSpecification
    {
        public BacnetObjectId ObjectId { get; }
        public ReadOnlyCollection<Property> Properties { get; }

        public BacnetWriteAccessSpecification(BacnetObjectId objectId, IEnumerable<Property> properties)
        {
            ObjectId = objectId;
            Properties = properties.ToList().AsReadOnly();
        }

        public class Property
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
        }
    }
}
