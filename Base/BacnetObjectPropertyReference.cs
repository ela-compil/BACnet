namespace System.IO.BACnet
{
    public class BacnetObjectPropertyReference
    {
        public BacnetObjectId ObjectId { get; }
        public BacnetPropertyReference[] PropertyReferences { get; }

        public BacnetObjectPropertyReference(BacnetObjectId objectId, params BacnetPropertyReference[] propertyReferences)
        {
            if((propertyReferences?.Length ?? 0) == 0)
                throw new ArgumentOutOfRangeException($"{nameof(propertyReferences)} count must be > 0");

            ObjectId = objectId;
            PropertyReferences = propertyReferences;
        }
    }
}
