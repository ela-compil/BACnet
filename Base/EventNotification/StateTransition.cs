using System.IO.BACnet.EventNotification.EventValues;
using System.IO.BACnet.Helpers;
using System.Linq;

namespace System.IO.BACnet.EventNotification
{
    public class StateTransition : NotificationData
    {
        public BacnetEventTypes EventType { get; set; }
        public bool AckRequired { get; set; }
        public BacnetEventStates FromState { get; set; }
    }

    public class StateTransition<TEventValuesBase> : StateTransition
        where TEventValuesBase : EventValuesBase
    {
        public TEventValuesBase EventValues { get; }

        public StateTransition(TEventValuesBase eventValues)
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