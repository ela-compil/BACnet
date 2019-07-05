using System.Collections.Generic;
using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using System.Linq;

namespace System.IO.BACnet.Serialize
{
    public static class AlarmAndEventServices
    {
        public static void EncodeAlarmAcknowledge(EncodeBuffer buffer, uint ackProcessIdentifier, BacnetObjectId eventObjectIdentifier, uint eventStateAcked, string ackSource, BacnetGenericTime eventTimeStamp, BacnetGenericTime ackTimeStamp)
        {
            ASN1.encode_context_unsigned(buffer, 0, ackProcessIdentifier);
            ASN1.encode_context_object_id(buffer, 1, eventObjectIdentifier.Type, eventObjectIdentifier.Instance);
            ASN1.encode_context_enumerated(buffer, 2, eventStateAcked);
            ASN1.bacapp_encode_context_timestamp(buffer, 3, eventTimeStamp);
            ASN1.encode_context_character_string(buffer, 4, ackSource);
            ASN1.bacapp_encode_context_timestamp(buffer, 5, ackTimeStamp);
        }

        public static void EncodeCOVNotify(EncodeBuffer buffer, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IEnumerable<BacnetPropertyValue> values)
        {
            /* tag 0 - subscriberProcessIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
            /* tag 1 - initiatingDeviceIdentifier */
            ASN1.encode_context_object_id(buffer, 1, BacnetObjectTypes.OBJECT_DEVICE, initiatingDeviceIdentifier);
            /* tag 2 - monitoredObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 2, monitoredObjectIdentifier.Type, monitoredObjectIdentifier.Instance);
            /* tag 3 - timeRemaining */
            ASN1.encode_context_unsigned(buffer, 3, timeRemaining);
            /* tag 4 - listOfValues */
            ASN1.encode_opening_tag(buffer, 4);
            foreach (var value in values)
            {
                /* tag 0 - propertyIdentifier */
                ASN1.encode_context_enumerated(buffer, 0, value.property.propertyIdentifier);
                /* tag 1 - propertyArrayIndex OPTIONAL */
                if (value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                {
                    ASN1.encode_context_unsigned(buffer, 1, value.property.propertyArrayIndex);
                }
                /* tag 2 - value */
                /* abstract syntax gets enclosed in a context tag */
                ASN1.encode_opening_tag(buffer, 2);
                foreach (var v in value.value)
                {
                    ASN1.bacapp_encode_application_data(buffer, v);
                }
                ASN1.encode_closing_tag(buffer, 2);
                /* tag 3 - priority OPTIONAL */
                if (value.priority != ASN1.BACNET_NO_PRIORITY)
                {
                    ASN1.encode_context_unsigned(buffer, 3, value.priority);
                }
                /* is there another one to encode? */
                /* FIXME: check to see if there is room in the APDU */
            }
            ASN1.encode_closing_tag(buffer, 4);
        }

        public static int DecodeCOVNotify(BacnetAddress address, byte[] buffer, int offset, int apduLen, out uint subscriberProcessIdentifier, out BacnetObjectId initiatingDeviceIdentifier, out BacnetObjectId monitoredObjectIdentifier, out uint timeRemaining, out ICollection<BacnetPropertyValue> values)
        {
            var len = 0;
            uint lenValue;

            subscriberProcessIdentifier = 0;
            initiatingDeviceIdentifier = default(BacnetObjectId);
            monitoredObjectIdentifier = default(BacnetObjectId);
            timeRemaining = 0;
            values = null;

            /* tag 0 - subscriberProcessIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out subscriberProcessIdentifier);
            }
            else
                return -1;

            /* tag 1 - initiatingDeviceIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                initiatingDeviceIdentifier = new BacnetObjectId(type, instance);
            }
            else
                return -1;

            /* tag 2 - monitoredObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                monitoredObjectIdentifier = new BacnetObjectId(type, instance);
            }
            else
                return -1;

            /* tag 3 - timeRemaining */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out timeRemaining);
            }
            else
                return -1;

            /* tag 4: opening context tag - listOfValues */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 4))
                return -1;

            /* a tag number of 4 is not extended so only one octet */
            len++;
            var _values = new LinkedList<BacnetPropertyValue>();
            while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 4))
            {
                var newEntry = new BacnetPropertyValue();

                /* tag 0 - propertyIdentifier */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out newEntry.property.propertyIdentifier);
                }
                else
                    return -1;

                /* tag 1 - propertyArrayIndex OPTIONAL */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out newEntry.property.propertyArrayIndex);
                }
                else
                    newEntry.property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

                /* tag 2: opening context tag - value */
                if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 2))
                    return -1;

                /* a tag number of 2 is not extended so only one octet */
                len++;
                var bValues = new List<BacnetValue>();
                while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
                {
                    var tmp = ASN1.bacapp_decode_application_data(
                        address, buffer, offset + len, apduLen + offset, monitoredObjectIdentifier.Type,
                        (BacnetPropertyIds) newEntry.property.propertyIdentifier, out var bValue);

                    if (tmp < 0) return -1;
                    len += tmp;
                    bValues.Add(bValue);
                }
                newEntry.value = bValues;

                /* a tag number of 2 is not extended so only one octet */
                len++;
                /* tag 3 - priority OPTIONAL */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint decodedValue);
                    newEntry.priority = (byte)decodedValue;
                }
                else
                    newEntry.priority = (byte)ASN1.BACNET_NO_PRIORITY;

                _values.AddLast(newEntry);
            }

            values = _values;
            return len;
        }

        public static void EncodeEventNotifyData(EncodeBuffer buffer, NotificationData data)
        {
            /* tag 0 - processIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, data.ProcessIdentifier);
            /* tag 1 - initiatingObjectIdentifier */
            ASN1.encode_context_object_id(
                buffer, 1, data.InitiatingObjectIdentifier.Type, data.InitiatingObjectIdentifier.Instance);

            /* tag 2 - eventObjectIdentifier */
            ASN1.encode_context_object_id(
                buffer, 2, data.EventObjectIdentifier.Type, data.EventObjectIdentifier.Instance);

            /* tag 3 - timeStamp */
            ASN1.bacapp_encode_context_timestamp(buffer, 3, data.TimeStamp);

            /* tag 4 - noticicationClass */
            ASN1.encode_context_unsigned(buffer, 4, data.NotificationClass);

            /* tag 5 - priority */
            ASN1.encode_context_unsigned(buffer, 5, data.Priority);

            var stateTransition = data as StateTransition;

            if (stateTransition != null)
            {
                /* tag 6 - eventType */
                ASN1.encode_context_enumerated(buffer, 6, (uint)stateTransition.EventType);
            }

            /* tag 7 - messageText */
            if (!String.IsNullOrEmpty(data.MessageText))
                ASN1.encode_context_character_string(buffer, 7, data.MessageText);

            /* tag 8 - notifyType */
            ASN1.encode_context_enumerated(buffer, 8, (uint)data.NotifyType);

            switch (stateTransition?.NotifyType)
            {
                case BacnetNotifyTypes.NOTIFY_ALARM:
                case BacnetNotifyTypes.NOTIFY_EVENT:
                    /* tag 9 - ackRequired */
                    ASN1.encode_context_boolean(buffer, 9, stateTransition.AckRequired);

                    /* tag 10 - fromState */
                    ASN1.encode_context_enumerated(buffer, 10, (uint)stateTransition.FromState);
                    break;
            }

            /* tag 11 - toState */
            ASN1.encode_context_enumerated(buffer, 11, (uint)data.ToState);

            if (stateTransition == null || !stateTransition.GetType().IsGenericType)
                return; // there are no EventValues if we're not processing a StateTransition

            ASN1.encode_opening_tag(buffer, 12);

            switch (stateTransition)
            {
                case StateTransition<ChangeOfBitString> changeOfBitString:
                    ASN1.encode_opening_tag(buffer, 0);
                    ASN1.encode_context_bitstring(buffer, 0, changeOfBitString.EventValues.ReferencedBitString);
                    ASN1.encode_context_bitstring(buffer, 1, changeOfBitString.EventValues.StatusFlags);
                    ASN1.encode_closing_tag(buffer, 0);
                    break;

                case StateTransition changeOfStateTransition when changeOfStateTransition.GetType().GetGenericArguments()[0].BaseType == typeof(ChangeOfState):
                    ASN1.encode_opening_tag(buffer, 1);
                    ASN1.encode_context_property_state(buffer, 0, changeOfStateTransition, out var changeOfState);
                    ASN1.encode_context_bitstring(buffer, 1, changeOfState.StatusFlags);
                    ASN1.encode_closing_tag(buffer, 1);
                    break;

                case StateTransition changeOfValueTransition when changeOfValueTransition.GetType().GetGenericArguments()[0].BaseType == typeof(ChangeOfValue):
                    ASN1.encode_opening_tag(buffer, 2);
                    ASN1.encode_opening_tag(buffer, 0);

                    BacnetBitString covStatusFlags;

                    switch (changeOfValueTransition)
                    {
                        case StateTransition<ChangeOfValue<float>> covFLoat:
                            ASN1.encode_context_real(buffer, 1, covFLoat.EventValues.ChangedValue);
                            covStatusFlags = covFLoat.EventValues.StatusFlags;
                            break;
                        case StateTransition<ChangeOfValue<BacnetBitString>> covBits:
                            ASN1.encode_context_bitstring(buffer, 0, covBits.EventValues.ChangedValue);
                            covStatusFlags = covBits.EventValues.StatusFlags;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Unexpected Type '{changeOfValueTransition.GetType()}'");
                    }

                    ASN1.encode_closing_tag(buffer, 0);
                    ASN1.encode_context_bitstring(buffer, 1, covStatusFlags);
                    ASN1.encode_closing_tag(buffer, 2);
                    break;

                case StateTransition<FloatingLimit> floatingLimit:
                    ASN1.encode_opening_tag(buffer, 4);
                    ASN1.encode_context_real(buffer, 0, floatingLimit.EventValues.ReferenceValue);
                    ASN1.encode_context_bitstring(buffer, 1, floatingLimit.EventValues.StatusFlags);
                    ASN1.encode_context_real(buffer, 2, floatingLimit.EventValues.SetPointValue);
                    ASN1.encode_context_real(buffer, 3, floatingLimit.EventValues.ErrorLimit);
                    ASN1.encode_closing_tag(buffer, 4);
                    break;

                case StateTransition<OutOfRange> outOfRange:
                    ASN1.encode_opening_tag(buffer, 5);
                    ASN1.encode_context_real(buffer, 0, outOfRange.EventValues.ExceedingValue);
                    ASN1.encode_context_bitstring(buffer, 1, outOfRange.EventValues.StatusFlags);
                    ASN1.encode_context_real(buffer, 2, outOfRange.EventValues.Deadband);
                    ASN1.encode_context_real(buffer, 3, outOfRange.EventValues.ExceededLimit);
                    ASN1.encode_closing_tag(buffer, 5);
                    break;

                case StateTransition<ChangeOfLifeSafety> changeOfLifeSafety:
                    ASN1.encode_opening_tag(buffer, 8);
                    ASN1.encode_context_enumerated(buffer, 0, (uint)changeOfLifeSafety.EventValues.NewState);
                    ASN1.encode_context_enumerated(buffer, 1, (uint)changeOfLifeSafety.EventValues.NewMode);
                    ASN1.encode_context_bitstring(buffer, 2, changeOfLifeSafety.EventValues.StatusFlags);
                    ASN1.encode_context_enumerated(buffer, 3, (uint)changeOfLifeSafety.EventValues.OperationExpected);
                    ASN1.encode_closing_tag(buffer, 8);
                    break;

                case StateTransition<BufferReady> bufferReady:
                    ASN1.encode_opening_tag(buffer, 10);
                    ASN1.bacapp_encode_context_device_obj_property_ref(buffer, 0, bufferReady.EventValues.BufferProperty);
                    ASN1.encode_context_unsigned(buffer, 1, bufferReady.EventValues.PreviousNotification);
                    ASN1.encode_context_unsigned(buffer, 2, bufferReady.EventValues.CurrentNotification);
                    ASN1.encode_closing_tag(buffer, 10);
                    break;

                case StateTransition<UnsignedRange> unsignedRange:
                    ASN1.encode_opening_tag(buffer, 11);
                    ASN1.encode_context_unsigned(buffer, 0, unsignedRange.EventValues.ExceedingValue);
                    ASN1.encode_context_bitstring(buffer, 1, unsignedRange.EventValues.StatusFlags);
                    ASN1.encode_context_unsigned(buffer, 2, unsignedRange.EventValues.ExceededLimit);
                    ASN1.encode_closing_tag(buffer, 11);
                    break;

                default:
                    var eventValuesType = stateTransition.GetType().GetGenericArguments().First();
                    throw new NotImplementedException($"EventValues of type {eventValuesType} is not implemented");
            }

            ASN1.encode_closing_tag(buffer, 12);
        }

        public static int DecodeEventNotifyData(byte[] buffer, int offset, int apduLen, out NotificationData eventData)
        {
            var len = 0;
            uint lenValue;

            eventData = default;
            BacnetNotifyTypes? notifyType;
            BacnetEventTypes? eventType = default;
            var decodedNotificationData = new List<Action<NotificationData>>();
            var decodedStateTransition = new List<Action<StateTransition>>();

            /* tag 0 - processIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint processIdentifier);
                decodedNotificationData.Add(e => e.ProcessIdentifier = processIdentifier);
            }
            else
                return -1;

            /*  tag 1 - initiatingObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                decodedNotificationData.Add(e => e.InitiatingObjectIdentifier = new BacnetObjectId(type, instance));
            }
            else
                return -1;

            /*  tag 2 - eventObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                decodedNotificationData.Add(e => e.EventObjectIdentifier = new BacnetObjectId(type, instance));
            }
            else
                return -1;

            /*  tag 3 - timeStamp */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
            {
                len += 1; // opening Tag 3
                /*
                len += ASN1.decode_application_date(buffer, offset + len, out var date);
                len += ASN1.decode_application_time(buffer, offset + len, out var time);
                decodedNotificationData.Add(e => e.TimeStamp = new BacnetGenericTime(new DateTime(
                        date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond),
                    BacnetTimestampTags.TIME_STAMP_DATETIME));
                len += 2; // closing tag 2 then 3
                */
                len += ASN1.bacapp_decode_timestamp(buffer, offset + len, out var genericTime);
                decodedNotificationData.Add(e => e.TimeStamp = genericTime);
                ++len; // closing tag 3
            }
            else
                return -1;

            /* tag 4 - noticicationClass */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 4))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint notificationClass);
                decodedNotificationData.Add(e => e.NotificationClass = notificationClass);
            }
            else
                return -1;

            /* tag 5 - priority */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 5))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint priority);
                if (priority > 0xFF) return -1;
                decodedNotificationData.Add(e => e.Priority = (byte) priority);
            }
            else
                return -1;

            /* tag 6 - eventType */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 6))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += EnumUtils.DecodeEnumerated(buffer, offset + len, lenValue, out BacnetEventTypes eventTypeValue);
                eventType = eventTypeValue;
            }
            //else
            //    return -1;
            // shouldn't be present in ack transitions (according to the spec), but still is with some hardware

            /* optional tag 7 - messageText  : never tested */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 7))
            {
                // max_lenght 20000 sound like a joke
                len += ASN1.decode_context_character_string(buffer, offset + len, 20000, 7, out var messageText);
                decodedNotificationData.Add(e => e.MessageText = messageText);
            }

            /* tag 8 - notifyType */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 8))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += EnumUtils.DecodeEnumerated(buffer, offset + len, lenValue, out BacnetNotifyTypes notifyTypeValue);
                decodedStateTransition.Add(e => e.NotifyType = notifyTypeValue);
                notifyType = notifyTypeValue;
            }
            else
                return -1;

            switch (notifyType)
            {
                case BacnetNotifyTypes.NOTIFY_ALARM:
                case BacnetNotifyTypes.NOTIFY_EVENT:
                    /* tag 9 - ackRequired */
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                    len += ASN1.decode_unsigned8(buffer, offset + len, out var val);
                    decodedStateTransition.Add(e => e.AckRequired = Convert.ToBoolean(val));

                    /* tag 10 - fromState */
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint fromstate);
                    decodedStateTransition.Add(e => e.FromState = (BacnetEventStates) fromstate);
                    break;
            }

            /* tag 11 - toState */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 11))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint toState);
                decodedNotificationData.Add(e => e.ToState = (BacnetEventStates) toState);
            }
            else
                return -1;

            /* tag 12 - event values */
            switch (notifyType)
            {
                case BacnetNotifyTypes.NOTIFY_ALARM when eventType.HasValue:
                case BacnetNotifyTypes.NOTIFY_EVENT when eventType.HasValue:
                    if (!DecodeEventValues(buffer, offset, eventType.Value, ref len, out var eventValues))
                        return -1;

                    var targetType = typeof(StateTransition<>).MakeGenericType(eventValues.GetType());
                    eventData = Activator.CreateInstance(targetType, eventValues) as StateTransition;
                    break;
            }

            eventData = eventData ?? (decodedStateTransition.Any()
                            ? new StateTransition(eventType ?? throw new ArgumentNullException($"Need eventType to initialize StateTransition"))
                            : new NotificationData());

            if (eventData is StateTransition stateTransition)
                foreach (var setValue in decodedStateTransition)
                    setValue(stateTransition);

            foreach (var setValue in decodedNotificationData)
                setValue(eventData);

            return len;
        }

        private static bool DecodeEventValues(byte[] buffer, int offset, BacnetEventTypes eventType, ref int len,
            out EventValuesBase eventValues)
        {
            eventValues = default;

            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 12))
                return false;

            len++;
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, (byte)eventType))
                return false;

            len++;
            switch (eventType)
            {
                case BacnetEventTypes.EVENT_CHANGE_OF_BITSTRING:
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 0, out var referencedBitString);
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out var changeOfBitStringStatusFlags);
                    eventValues = new ChangeOfBitString
                    {
                        ReferencedBitString = referencedBitString,
                        StatusFlags = changeOfBitStringStatusFlags
                    };
                    break;

                case BacnetEventTypes.EVENT_CHANGE_OF_STATE:
                    len += ASN1.decode_context_property_state(buffer, offset + len, 0, out var newState);
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out var changeOfStateStatusFlags);
                    eventValues = (newState as ChangeOfState).SetStatusFlags(changeOfStateStatusFlags);
                    break;

                case BacnetEventTypes.EVENT_CHANGE_OF_VALUE:
                    if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
                        return false;

                    len++;
                    if (ASN1.decode_is_context_tag(buffer, offset + len, (byte)BacnetCOVTypes.CHANGE_OF_VALUE_BITS))
                    {
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 0, out var changedBits);
                        eventValues = ChangeOfValueFactory.Create(changedBits);
                    }
                    else if (ASN1.decode_is_context_tag(buffer, offset + len, (byte)BacnetCOVTypes.CHANGE_OF_VALUE_REAL))
                    {
                        len += ASN1.decode_context_real(buffer, offset + len, 1, out var changeValue);
                        eventValues = ChangeOfValueFactory.Create(changeValue);
                    }
                    else
                    {
                        return false;
                    }

                    if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                        return false;

                    len++;
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out var changeOfValueStatusFlags);
                    ((ChangeOfValue)eventValues).StatusFlags = changeOfValueStatusFlags;
                    break;

                case BacnetEventTypes.EVENT_FLOATING_LIMIT:
                    len += ASN1.decode_context_real(buffer, offset + len, 0, out var referenceValue);
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out var floatingLimitStatusFlags);
                    len += ASN1.decode_context_real(buffer, offset + len, 2, out var setPointValue);
                    len += ASN1.decode_context_real(buffer, offset + len, 3, out var errorLimit);
                    eventValues = new FloatingLimit
                    {
                        ReferenceValue = referenceValue,
                        StatusFlags = floatingLimitStatusFlags,
                        SetPointValue = setPointValue,
                        ErrorLimit = errorLimit
                    };
                    break;

                case BacnetEventTypes.EVENT_OUT_OF_RANGE:
                    len += ASN1.decode_context_real(buffer, offset + len, 0, out var outOfRangeExceedingValue);
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out var outOfRangeStatusFlags);
                    len += ASN1.decode_context_real(buffer, offset + len, 2, out var deadband);
                    len += ASN1.decode_context_real(buffer, offset + len, 3, out var outOfRangeExceededLimit);
                    eventValues = new OutOfRange
                    {
                        ExceedingValue = outOfRangeExceedingValue,
                        StatusFlags = outOfRangeStatusFlags,
                        Deadband = deadband,
                        ExceededLimit = outOfRangeExceededLimit
                    };
                    break;

                case BacnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY:
                    len += EnumUtils.DecodeContextEnumerated(buffer, offset + len, 0, out BacnetLifeSafetyStates lifeSafetyNewState);
                    len += EnumUtils.DecodeContextEnumerated(buffer, offset + len, 1, out BacnetLifeSafetyModes lifeSafetyNewMode);
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 2, out var lifeSafetyStatusFlags);
                    len += EnumUtils.DecodeContextEnumerated(buffer, offset + len, 3, out BacnetLifeSafetyOperations operationExpected);
                    eventValues = new ChangeOfLifeSafety
                    {
                        NewState = lifeSafetyNewState,
                        NewMode = lifeSafetyNewMode,
                        StatusFlags = lifeSafetyStatusFlags,
                        OperationExpected = operationExpected
                    };
                    break;

                case BacnetEventTypes.EVENT_BUFFER_READY:
                    len += ASN1.decode_context_device_obj_property_ref(buffer, offset + len, 0, out var bufferProperty);
                    len += ASN1.decode_context_unsigned(buffer, offset + len, 1, out var previousNotification);
                    len += ASN1.decode_context_unsigned(buffer, offset + len, 2, out var currentNotification);
                    eventValues = new BufferReady
                    {
                        BufferProperty = bufferProperty,
                        CurrentNotification = currentNotification,
                        PreviousNotification = previousNotification
                    };
                    break;

                case BacnetEventTypes.EVENT_UNSIGNED_RANGE:
                    len += ASN1.decode_context_unsigned(buffer, offset + len, 0, out var unsignedRangeExceedingValue);
                    len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out var unsignedRangeStatusFlags);
                    len += ASN1.decode_context_unsigned(buffer, offset + len, 2, out var unsignedRangeExceededLimit);
                    eventValues = new UnsignedRange
                    {
                        ExceedingValue = unsignedRangeExceedingValue,
                        StatusFlags = unsignedRangeStatusFlags,
                        ExceededLimit = unsignedRangeExceededLimit
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Event-Type {eventType} is not supported");
            }

            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, (byte)eventType))
                return false;

            len++;
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 12))
                return false;

            len++;
            return eventValues != null;
        }

        public static void EncodeAlarmSummary(EncodeBuffer buffer, BacnetObjectId objectIdentifier, BacnetEventStates alarmState, BacnetBitString acknowledgedTransitions)
        {
            /* tag 0 - Object Identifier */
            ASN1.encode_application_object_id(buffer, objectIdentifier.Type, objectIdentifier.Instance);
            /* tag 1 - Alarm State */
            ASN1.encode_application_enumerated(buffer, (uint)alarmState);
            /* tag 2 - Acknowledged Transitions */
            ASN1.encode_application_bitstring(buffer, acknowledgedTransitions);
        }

        public static int DecodeAlarmSummary(byte[] buffer, int offset, int apduLen, ref IList<BacnetAlarmSummaryData> alarms)
        {
            var len = 0;

            while (apduLen - 3 - len > 0)
            {                
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                len += EnumUtils.DecodeEnumerated(buffer, offset + len, lenValue, out BacnetEventStates alarmState);
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                len += ASN1.decode_bitstring(buffer, offset + len, lenValue, out var acknowledgedTransitions);

                var value = new BacnetAlarmSummaryData(new BacnetObjectId(type, instance), alarmState, acknowledgedTransitions);

                alarms.Add(value);
            }

            return len;
        }

        public static void EncodeGetEventInformation(EncodeBuffer buffer, BacnetObjectId? lastReceivedObjectIdentifier)
        {
            /* encode optional parameter */
            if (lastReceivedObjectIdentifier != null)
                ASN1.encode_context_object_id(buffer, 0, lastReceivedObjectIdentifier.Value.Type, lastReceivedObjectIdentifier.Value.Instance);
        }

        public static void EncodeGetEventInformationAcknowledge(EncodeBuffer buffer, BacnetGetEventInformationData[] events, bool moreEvents)
        {
            /* service ack follows */
            /* Tag 0: listOfEventSummaries */
            ASN1.encode_opening_tag(buffer, 0);
            foreach (var eventData in events)
            {
                /* Tag 0: objectIdentifier */
                ASN1.encode_context_object_id(buffer, 0, eventData.objectIdentifier.Type, eventData.objectIdentifier.Instance);
                /* Tag 1: eventState */
                ASN1.encode_context_enumerated(buffer, 1, (uint)eventData.eventState);
                /* Tag 2: acknowledgedTransitions */
                ASN1.encode_context_bitstring(buffer, 2, eventData.acknowledgedTransitions);
                /* Tag 3: eventTimeStamps */
                ASN1.encode_opening_tag(buffer, 3);
                for (var i = 0; i < 3; i++)
                {
                    ASN1.bacapp_encode_timestamp(buffer, eventData.eventTimeStamps[i]);
                }
                ASN1.encode_closing_tag(buffer, 3);
                /* Tag 4: notifyType */
                ASN1.encode_context_enumerated(buffer, 4, (uint)eventData.notifyType);
                /* Tag 5: eventEnable */
                ASN1.encode_context_bitstring(buffer, 5, eventData.eventEnable);
                /* Tag 6: eventPriorities */
                ASN1.encode_opening_tag(buffer, 6);
                for (var i = 0; i < 3; i++)
                {
                    ASN1.encode_application_unsigned(buffer, eventData.eventPriorities[i]);
                }
                ASN1.encode_closing_tag(buffer, 6);
            }
            ASN1.encode_closing_tag(buffer, 0);
            ASN1.encode_context_boolean(buffer, 1, moreEvents);
        }

        public static int DecodeEventInformation(byte[] buffer, int offset, int apduLen, ref IList<BacnetGetEventInformationData> events, out bool moreEvent)
        {
            var len = 1; // tag 0

            while (apduLen - 3 - len > 0)
            {
                var value = new BacnetGetEventInformationData();

                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                value.objectIdentifier = new BacnetObjectId(type, instance);

                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                len += EnumUtils.DecodeEnumerated(buffer, offset + len, lenValue, out value.eventState);
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                len += ASN1.decode_bitstring(buffer, offset + len, lenValue, out value.acknowledgedTransitions);

                len++;  // opening Tag 3
                value.eventTimeStamps = new BacnetGenericTime[3];

                for (var i = 0; i < 3; i++)
                {
                    len += ASN1.bacapp_decode_timestamp(buffer, offset + len, out var timeStamp);
                    value.eventTimeStamps[i] = timeStamp;
                }

                len++;  // closing Tag 3

                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                len += EnumUtils.DecodeEnumerated(buffer, offset + len, lenValue, out value.notifyType);
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                len += ASN1.decode_bitstring(buffer, offset + len, lenValue, out value.eventEnable);

                len++; // opening tag 6;
                value.eventPriorities = new uint[3];
                for (var i = 0; i < 3; i++)
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out value.eventPriorities[i]);
                }
                len++;  // closing Tag 6

                events.Add(value);
            }

            moreEvent = buffer[offset+len++] == 1;
            return len;
        }

        public static void EncodeLifeSafetyOperation(EncodeBuffer buffer, uint processId, string requestingSrc, uint operation, BacnetObjectId targetObject)
        {
            /* tag 0 - requestingProcessId */
            ASN1.encode_context_unsigned(buffer, 0, processId);
            /* tag 1 - requestingSource */
            ASN1.encode_context_character_string(buffer, 1, requestingSrc);
            /* Operation */
            ASN1.encode_context_enumerated(buffer, 2, operation);
            /* Object ID */
            ASN1.encode_context_object_id(buffer, 3, targetObject.Type, targetObject.Instance);
        }

        public static void EncodeSubscribeCOV(EncodeBuffer buffer, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime)
        {
            /* tag 0 - subscriberProcessIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
            /* tag 1 - monitoredObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 1, monitoredObjectIdentifier.Type, monitoredObjectIdentifier.Instance);
            /*
               If both the 'Issue Confirmed Notifications' and
               'Lifetime' parameters are absent, then this shall
               indicate a cancellation request.
             */
            if (cancellationRequest)
                return;
            /* tag 2 - issueConfirmedNotifications */
            ASN1.encode_context_boolean(buffer, 2, issueConfirmedNotifications);
            /* tag 3 - lifetime */
            ASN1.encode_context_unsigned(buffer, 3, lifetime);
        }

        public static int DecodeSubscribeCOV(byte[] buffer, int offset, int apduLen, out uint subscriberProcessIdentifier, out BacnetObjectId monitoredObjectIdentifier, out bool cancellationRequest, out bool issueConfirmedNotifications, out uint lifetime)
        {
            var len = 0;
            uint lenValue;

            subscriberProcessIdentifier = 0;
            monitoredObjectIdentifier = default(BacnetObjectId);
            cancellationRequest = false;
            issueConfirmedNotifications = false;
            lifetime = 0;

            /* tag 0 - subscriberProcessIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out subscriberProcessIdentifier);
            }
            else
                return -1;
            /* tag 1 - monitoredObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                monitoredObjectIdentifier = new BacnetObjectId(type, instance);
            }
            else
                return -1;
            /* optional parameters - if missing, means cancellation */
            if (len < apduLen)
            {
                /* tag 2 - issueConfirmedNotifications - optional */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                    issueConfirmedNotifications = buffer[offset + len] > 0;
                    len += (int)lenValue;
                }
                else
                {
                    cancellationRequest = true;
                }
                /* tag 3 - lifetime - optional */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out lifetime);
                }
                else
                {
                    lifetime = 0;
                }
            }
            else
            {
                cancellationRequest = true;
            }

            return len;
        }

        // TODO CHECK: rename to EncodeSubscribeCovProperty ?
        public static void EncodeSubscribeProperty(EncodeBuffer buffer, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetPropertyReference monitoredProperty, bool covIncrementPresent, float covIncrement)
        {
            /* tag 0 - subscriberProcessIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
            /* tag 1 - monitoredObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 1, monitoredObjectIdentifier.Type, monitoredObjectIdentifier.Instance);
            if (!cancellationRequest)
            {
                /* tag 2 - issueConfirmedNotifications */
                ASN1.encode_context_boolean(buffer, 2, issueConfirmedNotifications);
                /* tag 3 - lifetime */
                ASN1.encode_context_unsigned(buffer, 3, lifetime);
            }
            /* tag 4 - monitoredPropertyIdentifier */
            ASN1.encode_opening_tag(buffer, 4);
            ASN1.encode_context_enumerated(buffer, 0, monitoredProperty.propertyIdentifier);
            if (monitoredProperty.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 1, monitoredProperty.propertyArrayIndex);
            }
            ASN1.encode_closing_tag(buffer, 4);

            /* tag 5 - covIncrement */
            if (covIncrementPresent)
                ASN1.encode_context_real(buffer, 5, covIncrement);
        }

        // TODO CHECK: rename to DecodeSubscribeCovProperty ?
        public static int DecodeSubscribeProperty(byte[] buffer, int offset, int apduLen, out uint subscriberProcessIdentifier, out BacnetObjectId monitoredObjectIdentifier, out BacnetPropertyReference monitoredProperty, out bool cancellationRequest, out bool issueConfirmedNotifications, out uint lifetime, out float covIncrement)
        {
            var len = 0;
            uint lenValue;
            uint decodedValue;

            subscriberProcessIdentifier = 0;
            monitoredObjectIdentifier = default(BacnetObjectId);
            cancellationRequest = false;
            issueConfirmedNotifications = false;
            lifetime = 0;
            covIncrement = 0;
            monitoredProperty = new BacnetPropertyReference();

            /* tag 0 - subscriberProcessIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out subscriberProcessIdentifier);
            }
            else
                return -1;

            /* tag 1 - monitoredObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                monitoredObjectIdentifier = new BacnetObjectId(type, instance);
            }
            else
                return -1;

            /* tag 2 - issueConfirmedNotifications - optional */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                issueConfirmedNotifications = buffer[offset + len] > 0;
                len++;
            }
            else
            {
                cancellationRequest = true;
            }

            /* tag 3 - lifetime - optional */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out lifetime);
            }
            else
            {
                lifetime = 0;
            }

            /* tag 4 - monitoredPropertyIdentifier */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 4))
                return -1;

            /* a tag number of 4 is not extended so only one octet */
            len++;
            /* the propertyIdentifier is tag 0 */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out monitoredProperty.propertyIdentifier);
            }
            else
                return -1;

            /* the optional array index is tag 1 */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out decodedValue);
                monitoredProperty.propertyArrayIndex = decodedValue;
            }
            else
            {
                monitoredProperty.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;
            }

            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 4))
                return -1;

            /* a tag number of 4 is not extended so only one octet */
            len++;
            /* tag 5 - covIncrement - optional */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 5))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_real(buffer, offset + len, out covIncrement);
            }
            else
            {
                covIncrement = 0;
            }

            return len;
        }
    }
}
