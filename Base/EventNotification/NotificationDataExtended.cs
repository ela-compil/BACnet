using System.IO.BACnet.EventNotification.EventValues;
using System.IO.BACnet.Helpers;
using System.Linq;

namespace System.IO.BACnet.EventNotification
{
    public class NotificationDataExtended : NotificationData
    {
        public bool AckRequired { get; set; }
        public BacnetEventTypes EventType { get; set; }
        public BacnetEventStates FromState { get; set; }
        public EventValuesBase EventValues { get; protected set; }
    }

    public class NotificationData<TEventValuesBase> : NotificationDataExtended
        where TEventValuesBase : EventValuesBase
    {
        public new TEventValuesBase EventValues
        {
            get => base.EventValues as TEventValuesBase;
            private set => base.EventValues = value;
        }

        public NotificationData(TEventValuesBase eventValues)
        {
            EventValues = eventValues ?? throw new ArgumentNullException(nameof(eventValues));
        }

        public override string ToString()
        {
            return string.Join(", ", this.PropertiesWithValues(except: nameof(EventValues))
                .Concat(EventValues.PropertiesWithValues()));
        }
    }
}