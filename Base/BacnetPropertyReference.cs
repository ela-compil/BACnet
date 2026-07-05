namespace System.IO.BACnet;

public struct BacnetPropertyReference
{
    public uint propertyIdentifier;
    public uint propertyArrayIndex;        /* optional */

    /// <param name="id">The property identifier to reference.</param>
    /// <param name="arrayIndex">
    /// Index of the array element to reference. Defaults to <see cref="ASN1.BACNET_ARRAY_ALL"/>, which
    /// omits the optional array index from the encoded request so the whole property is read/written.
    /// Only pass an explicit index for array properties; index 0 selects the array length, not a scalar
    /// value, and requesting it for a non-array property yields an error from the peer.
    /// </param>
    public BacnetPropertyReference(uint id, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        propertyIdentifier = id;
        propertyArrayIndex = arrayIndex;
    }

    /// <inheritdoc cref="BacnetPropertyReference(uint, uint)"/>
    public BacnetPropertyReference(BacnetPropertyIds id, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        : this((uint)id, arrayIndex)
    {
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
