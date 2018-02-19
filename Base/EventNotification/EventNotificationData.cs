namespace System.IO.BACnet.EventNotification
{
    public class EventNotificationData
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
            return $"initiatingObject: {InitiatingObjectIdentifier}, eventObject: {EventObjectIdentifier}, "
                 + $"notifyType: {NotifyType}, timeStamp: {TimeStamp}, "
                 + $"toState: {ToState}";
        }

        /*
        private string GetEventDetails()
        {
            switch (eventType)
            {
                case BacnetEventTypes.EVENT_CHANGE_OF_BITSTRING:
                    return $"referencedBitString: {changeOfBitstring_referencedBitString}, statusFlags: {changeOfBitstring_statusFlags}";

                case BacnetEventTypes.EVENT_CHANGE_OF_STATE:
                    return $"newState: {changeOfState_newState}, statusFlags: {changeOfState_statusFlags}";

                case BacnetEventTypes.EVENT_CHANGE_OF_VALUE:
                    return $"changedBits: {changeOfValue_changedBits}, changeValue: {changeOfValue_changeValue}, "
                           + $"tag: {changeOfValue_tag}, statusFlags: {changeOfValue_statusFlags}";

                case BacnetEventTypes.EVENT_FLOATING_LIMIT:
                    return $"referenceValue: {floatingLimit_referenceValue}, statusFlags: {floatingLimit_statusFlags}, "
                           + $"setPointValue: {floatingLimit_setPointValue}, errorLimit: {floatingLimit_errorLimit}";

                case BacnetEventTypes.EVENT_OUT_OF_RANGE:
                    return $"exceedingValue: {outOfRange_exceedingValue}, statusFlags: {outOfRange_statusFlags}, "
                           + $"deadband: {outOfRange_deadband}, exceededLimit: {outOfRange_exceededLimit}";

                case BacnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY:
                    return $"newState: {changeOfLifeSafety_newState}, newMode: {changeOfLifeSafety_newMode}, "
                           +
                           $"statusFlags: {changeOfLifeSafety_statusFlags}, operationExpected: {changeOfLifeSafety_operationExpected}";

                case BacnetEventTypes.EVENT_BUFFER_READY:
                    return $"bufferProperty: {bufferReady_bufferProperty}, previousNotification: {bufferReady_previousNotification}, "
                           + $"currentNotification: {bufferReady_currentNotification}";

                case BacnetEventTypes.EVENT_UNSIGNED_RANGE:
                    return $"exceedingValue: {unsignedRange_exceedingValue}, statusFlags: {unsignedRange_statusFlags}, "
                           + $"exceededLimit: {unsignedRange_exceededLimit}";

                default:
                    return null;
            }
        }
        */
    };
}