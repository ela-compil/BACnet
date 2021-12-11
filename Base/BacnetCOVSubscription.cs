namespace System.IO.BACnet;

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
}
