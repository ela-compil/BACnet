namespace System.IO.BACnet;

public struct BacnetWriteAccessSpecification
{
    public BacnetObjectId objectIdentifier;
    public ICollection<BacnetPropertyValue> propertyValues;

    public BacnetWriteAccessSpecification(BacnetObjectId objectIdentifier, ICollection<BacnetPropertyValue> propertyValues)
    {
        this.objectIdentifier = objectIdentifier;
        this.propertyValues = propertyValues;
    }
}
