namespace System.IO.BACnet
{
    public struct BacnetCOVSubscription
    {
        /* BACnetRecipientProcess */
        public BacnetAddress Recipient;
        public uint subscriptionProcessIdentifier;
        /* BACnetObjectPropertyReference */
        public BacnetObjectId monitoredObjectIdentifier;
        public BacnetPropertyReference monitoredProperty;
        /* BACnetCOVSubscription */
        public bool IssueConfirmedNotifications;
        public uint TimeRemaining; 
        public float COVIncrement;

        public override string ToString()
        {
            var objType = $"{monitoredObjectIdentifier.Type}".Replace("OBJECT_", "");
            var objInstance = monitoredObjectIdentifier.Instance;
            return $"{objType}:{objInstance} by {Recipient}, remain {TimeRemaining}s";
        }
    }
}