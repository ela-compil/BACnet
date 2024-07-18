namespace System.IO.BACnet;

public struct BacnetEventNotificationData
{
    public uint processIdentifier;
    public BacnetObjectId initiatingObjectIdentifier;
    public BacnetObjectId eventObjectIdentifier;
    public BacnetGenericTime timeStamp;
    public uint notificationClass;
    public byte priority;
    public BacnetEventTypes eventType;
    public string messageText;       /* OPTIONAL - Set to NULL if not being used */
    public BacnetNotifyTypes notifyType;
    public bool ackRequired;
    public BacnetEventStates fromState;
    public BacnetEventStates toState;

    /*
     ** Each of these structures in the union maps to a particular eventtype
     ** Based on BACnetNotificationParameters
     */

    /*
     ** EVENT_CHANGE_OF_BITSTRING
     */
    public BacnetBitString changeOfBitstring_referencedBitString;
    public BacnetBitString changeOfBitstring_statusFlags;
    /*
     ** EVENT_CHANGE_OF_STATE
     */
    public BacnetPropertyState changeOfState_newState;
    public BacnetBitString changeOfState_statusFlags;
    /*
     ** EVENT_CHANGE_OF_VALUE
     */
    public BacnetBitString changeOfValue_changedBits;
    public float changeOfValue_changeValue;
    public BacnetCOVTypes? changeOfValue_tag;
    public BacnetBitString changeOfValue_statusFlags;
    /*
     ** EVENT_COMMAND_FAILURE
     */
    public uint commandFailure_commandValue;
    public BacnetBitString commandFailure_statusFlags;
    public uint commandFailure_feedbackValue;
    /*
     ** EVENT_FLOATING_LIMIT
     */
    public float floatingLimit_referenceValue;
    public BacnetBitString floatingLimit_statusFlags;
    public float floatingLimit_setPointValue;
    public float floatingLimit_errorLimit;
    /*
     ** EVENT_OUT_OF_RANGE
     */
    public float outOfRange_exceedingValue;
    public BacnetBitString outOfRange_statusFlags;
    public float outOfRange_deadband;
    public float outOfRange_exceededLimit;
    /*
     ** EVENT_CHANGE_OF_LIFE_SAFETY
     */
    public BacnetLifeSafetyStates? changeOfLifeSafety_newState;
    public BacnetLifeSafetyModes? changeOfLifeSafety_newMode;
    public BacnetBitString changeOfLifeSafety_statusFlags;
    public BacnetLifeSafetyOperations? changeOfLifeSafety_operationExpected;
    /*
     ** EVENT_EXTENDED
     **
     ** Not Supported!
     */
    /*
     ** EVENT_BUFFER_READY
     */
    public BacnetDeviceObjectPropertyReference bufferReady_bufferProperty;
    public uint bufferReady_previousNotification;
    public uint bufferReady_currentNotification;
    /*
     ** EVENT_UNSIGNED_RANGE
     */
    public uint unsignedRange_exceedingValue;
    public BacnetBitString unsignedRange_statusFlags;
    public uint unsignedRange_exceededLimit;
    /*
     ** EVENT_EXTENDED
     */
    public uint extended_vendorId;
    public uint extended_eventType;
    public object[] extended_parameters;
    /*
     ** EVENT_CHANGE_OF_RELIABILITY
     */
    public BacnetReliability changeOfReliability_reliability;
    public BacnetBitString changeOfReliability_statusFlags;
    public BacnetPropertyValue[] changeOfReliability_propertyValues;

    public override string ToString()
    {
        return $"initiatingObject: {initiatingObjectIdentifier}, eventObject: {eventObjectIdentifier}, "
             + $"eventType: {eventType}, notifyType: {notifyType}, timeStamp: {timeStamp}, "
             + $"fromState: {fromState}, toState: {toState}"
             + (notifyType != BacnetNotifyTypes.NOTIFY_ACK_NOTIFICATION ? $", {GetEventDetails()}" : "");
    }

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

            case BacnetEventTypes.EVENT_EXTENDED:
                return $"vendorId: {extended_vendorId}, extendedEventType: {extended_eventType}, parameters: [{extended_parameters?.Length ?? 0}]";

            case BacnetEventTypes.EVENT_CHANGE_OF_RELIABILITY:
                var properties = string.Join(", ", changeOfReliability_propertyValues?.Select(p => $"{p.property}"));
                return $"reliability: {changeOfReliability_reliability}, statusFlags: {changeOfReliability_statusFlags}, properties: [{properties}]";

            default:
                return "no details";
        }
    }
};
