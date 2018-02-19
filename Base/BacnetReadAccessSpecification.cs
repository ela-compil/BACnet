using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.Linq;

namespace System.IO.BACnet
{
    public struct BacnetReadAccessSpecification
    {
        public BacnetObjectId objectIdentifier;
        public IList<BacnetPropertyReference> propertyReferences;

        public BacnetReadAccessSpecification(BacnetObjectId objectIdentifier, IList<BacnetPropertyReference> propertyReferences)
        {
            this.objectIdentifier = objectIdentifier;
            this.propertyReferences = propertyReferences;
        }

        public static object Parse(string value)
        {
            var ret = new BacnetReadAccessSpecification();
            if (string.IsNullOrEmpty(value)) return ret;
            var tmp = value.Split(':');
            if (tmp.Length < 2) return ret;
            ret.objectIdentifier = new BacnetObjectId((BacnetObjectTypes)Enum.Parse(typeof(BacnetObjectTypes), tmp[0]), uint.Parse(tmp[1]));
            var refs = new List<BacnetPropertyReference>();
            for (var i = 2; i < tmp.Length; i++)
            {
                refs.Add(new BacnetPropertyReference
                {
                    propertyArrayIndex = ASN1.BACNET_ARRAY_ALL,
                    propertyIdentifier = (uint)(BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), tmp[i])
                });
            }
            ret.propertyReferences = refs;
            return ret;
        }

        public override string ToString()
        {
            return propertyReferences.Aggregate(objectIdentifier.ToString(), (current, r) =>
                $"{current}:{(BacnetPropertyIds)r.propertyIdentifier}");
        }
    }
}