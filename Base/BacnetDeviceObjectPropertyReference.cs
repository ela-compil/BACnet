using System.IO.BACnet.Serialize;

namespace System.IO.BACnet
{
    public struct BacnetDeviceObjectPropertyReference : ASN1.IEncode
    {
        public BacnetObjectId objectIdentifier;
        public BacnetPropertyIds propertyIdentifier;
        public uint arrayIndex;
        public BacnetObjectId deviceIndentifier;

        public BacnetDeviceObjectPropertyReference(BacnetObjectId objectIdentifier, BacnetPropertyIds propertyIdentifier, BacnetObjectId? deviceIndentifier = null, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            this.objectIdentifier = objectIdentifier;
            this.propertyIdentifier = propertyIdentifier;
            this.arrayIndex = arrayIndex;
            this.deviceIndentifier = deviceIndentifier ?? new BacnetObjectId(BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE, 0);
        }

        public void Encode(EncodeBuffer buffer)
        {
            ASN1.bacapp_encode_device_obj_property_ref(buffer, this);
        }

        public BacnetObjectId ObjectId
        {
            get { return objectIdentifier; }
            set { objectIdentifier = value; }
        }

        public int ArrayIndex // shows -1 when it's ASN1.BACNET_ARRAY_ALL
        {
            get
            {
                return arrayIndex != ASN1.BACNET_ARRAY_ALL 
                    ? (int)arrayIndex 
                    : -1;
            }
            set
            {
                arrayIndex = value < 0
                    ? ASN1.BACNET_ARRAY_ALL
                    : (uint)value;
            }
        }
        public BacnetObjectId? DeviceId  // shows null when it's not OBJECT_DEVICE
        {
            get
            {
                return deviceIndentifier.type == BacnetObjectTypes.OBJECT_DEVICE
                    ? (BacnetObjectId?)deviceIndentifier
                    : null;
            }
            set
            {
                deviceIndentifier = value ?? new BacnetObjectId();
            }
        }

        public BacnetPropertyIds PropertyId
        {
            get { return propertyIdentifier; }
            set { propertyIdentifier = value; }
        }
    }
}