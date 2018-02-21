using System.IO.BACnet.Helpers;

namespace System.IO.BACnet.EventNotification
{
    public class NotificationData
    {
        public virtual uint ProcessIdentifier { get; set; }
        // public BacnetObjectId initiatingDeviceIdentifier; // TODO?
        public virtual BacnetObjectId InitiatingObjectIdentifier { get; set; }
        public virtual BacnetObjectId EventObjectIdentifier { get; set; }
        public virtual BacnetGenericTime TimeStamp { get; set; }
        public virtual uint NotificationClass { get; set; }
        public virtual byte Priority { get; set; }
        public virtual string MessageText { get; set; }       /* OPTIONAL - Set to NULL if not being used */
        public virtual BacnetNotifyTypes NotifyType { get; set; }
        public virtual BacnetEventStates ToState { get; set; }

        public override string ToString()
        {
            return string.Join(", ", this.PropertiesWithValues());
        }
    };
}