using System.IO.BACnet.Serialize;

namespace System.IO.BACnet
{
    // TODO you have been flagged for refactoring due to un-C#-iness
    public struct BacnetPropertyReference
    {
        public uint propertyIdentifier;
        public uint propertyArrayIndex;        /* optional */

        public BacnetPropertyReference(BacnetPropertyIds id, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        : this((uint)id, arrayIndex)
        {
        }

        public BacnetPropertyReference(uint id, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            propertyIdentifier = id;
            propertyArrayIndex = arrayIndex;
        }

        public BacnetPropertyIds GetPropertyId()
        {
            return (BacnetPropertyIds)propertyIdentifier;
        }

        public override string ToString()
        {
            return $"{GetPropertyId()}";
        }
    }
}