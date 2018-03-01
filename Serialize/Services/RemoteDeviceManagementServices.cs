namespace System.IO.BACnet.Serialize
{
    public static class RemoteDeviceManagementServices
    {
        public static void EncodeDeviceCommunicationControl(EncodeBuffer buffer, uint timeDuration, EnableDisable enableDisable, string password)
        {
            /* optional timeDuration */
            if (timeDuration > 0)
                ASN1.encode_context_unsigned(buffer, 0, timeDuration);

            /* enable disable */
            ASN1.encode_context_unsigned(buffer, 1, (uint)enableDisable);

            /* optional password */
            if (!String.IsNullOrEmpty(password))
            {
                /* FIXME: must be at least 1 character, limited to 20 characters */
                ASN1.encode_context_character_string(buffer, 2, password);
            }
        }

        public static int DecodeDeviceCommunicationControl(byte[] buffer, int offset, int apduLen, out uint timeDuration, out uint enableDisable, out string password)
        {
            var len = 0;
            uint lenValueType;

            timeDuration = 0;
            enableDisable = 0;
            password = "";

            /* Tag 0: timeDuration, in minutes --optional--
             * But if not included, take it as indefinite,
             * which we return as "very large" */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValueType);
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out timeDuration);
            }

            /* Tag 1: enable_disable */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 1))
                return -1;
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValueType);
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out enableDisable);

            /* Tag 2: password --optional-- */
            if (len < apduLen)
            {
                if (!ASN1.decode_is_context_tag(buffer, offset + len, 2))
                    return -1;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValueType);
                len += ASN1.decode_character_string(buffer, offset + len, apduLen - (offset + len), lenValueType, out password);
            }

            return len;
        }

        public static void EncodePrivateTransferConfirmed(EncodeBuffer buffer, uint vendorID, uint serviceNumber, byte[] data)
        {
            ASN1.encode_context_unsigned(buffer, 0, vendorID);
            ASN1.encode_context_unsigned(buffer, 1, serviceNumber);
            ASN1.encode_opening_tag(buffer, 2);
            buffer.Add(data, data.Length);
            ASN1.encode_closing_tag(buffer, 2);
        }

        public static void EncodePrivateTransferUnconfirmed(EncodeBuffer buffer, uint vendorID, uint serviceNumber, byte[] data)
        {
            ASN1.encode_context_unsigned(buffer, 0, vendorID);
            ASN1.encode_context_unsigned(buffer, 1, serviceNumber);
            ASN1.encode_opening_tag(buffer, 2);
            buffer.Add(data, data.Length);
            ASN1.encode_closing_tag(buffer, 2);
        }

        public static void EncodePrivateTransferAcknowledge(EncodeBuffer buffer, uint vendorID, uint serviceNumber, byte[] data)
        {
            ASN1.encode_context_unsigned(buffer, 0, vendorID);
            ASN1.encode_context_unsigned(buffer, 1, serviceNumber);

            if (data == null || data.Length == 0)
                return; // that's ok.

            ASN1.encode_opening_tag(buffer, 2);
            buffer.Add(data, data.Length);
            ASN1.encode_closing_tag(buffer, 2);
        }

        public static void EncodeReinitializeDevice(EncodeBuffer buffer, BacnetReinitializedStates state, string password)
        {
            ASN1.encode_context_enumerated(buffer, 0, (uint)state);

            /* optional password */
            if (!String.IsNullOrEmpty(password))
            {
                /* FIXME: must be at least 1 character, limited to 20 characters */
                ASN1.encode_context_character_string(buffer, 1, password);
            }
        }

        public static int DecodeReinitializeDevice(byte[] buffer, int offset, int apduLen, out BacnetReinitializedStates state, out string password)
        {
            var len = 0;

            state = BacnetReinitializedStates.BACNET_REINIT_IDLE;
            password = "";

            /* Tag 0: reinitializedStateOfDevice */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out var lenValueType);
            len += EnumUtils.DecodeEnumerated(buffer, offset + len, lenValueType, out state);
            /* Tag 1: password - optional */
            if (len < apduLen)
            {
                if (!ASN1.decode_is_context_tag(buffer, offset + len, 1))
                    return -1;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValueType);
                len += ASN1.decode_character_string(buffer, offset + len, apduLen - (offset + len), lenValueType, out password);
            }

            return len;
        }

        public static void EncodeTimeSync(EncodeBuffer buffer, DateTime time)
        {
            ASN1.encode_application_date(buffer, time);
            ASN1.encode_application_time(buffer, time);
        }

        public static int DecodeTimeSync(byte[] buffer, int offset, int length, out DateTime dateTime)
        {
            var len = 0;
            dateTime = new DateTime(1, 1, 1);

            /* date */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out _);
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE)
                return -1;
            len += ASN1.decode_date(buffer, offset + len, out var date);
            /* time */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out _);
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME)
                return -1;
            len += ASN1.decode_bacnet_time(buffer, offset + len, out var time);

            //merge
            dateTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

            return len;
        }

        public static void EncodeWhoHasBroadcast(EncodeBuffer buffer, int lowLimit, int highLimit, BacnetObjectId objectId, string objectName)
        {
            /* optional limits - must be used as a pair */
            if (lowLimit >= 0 && lowLimit <= ASN1.BACNET_MAX_INSTANCE && highLimit >= 0 && highLimit <= ASN1.BACNET_MAX_INSTANCE)
            {
                ASN1.encode_context_unsigned(buffer, 0, (uint)lowLimit);
                ASN1.encode_context_unsigned(buffer, 1, (uint)highLimit);
            }
            if (!String.IsNullOrEmpty(objectName))
            {
                ASN1.encode_context_character_string(buffer, 3, objectName);
            }
            else
            {
                ASN1.encode_context_object_id(buffer, 2, objectId.Type, objectId.Instance);
            }
        }

        // Added by thamersalek
        public static int DecodeWhoHasBroadcast(byte[] buffer, int offset, int apduLen, out int lowLimit, out int highLimit, out BacnetObjectId objId, out string objName)
        {
            var len = 0;
            uint decodedValue;

            objName = null;
            objId = new BacnetObjectId(BacnetObjectTypes.OBJECT_BINARY_OUTPUT, 0x3FFFFF);
            lowLimit = -1;
            highLimit = -1;

            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);

            if (tagNumber == 0)
            {
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out decodedValue);
                if (decodedValue <= ASN1.BACNET_MAX_INSTANCE)
                    lowLimit = (int)decodedValue;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            }

            if (tagNumber == 1)
            {
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out decodedValue);
                if (decodedValue <= ASN1.BACNET_MAX_INSTANCE)
                    highLimit = (int)decodedValue;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            }

            if (tagNumber == 2)
            {
                len += ASN1.decode_object_id(buffer, offset + len, out ushort objType, out var objInst);
                objId = new BacnetObjectId((BacnetObjectTypes)objType, objInst);
            }

            if (tagNumber == 3)
                len += ASN1.decode_character_string(buffer, offset + len, apduLen - (offset + len), lenValue, out objName);

            return len;
        }

        public static void EncodeIhaveBroadcast(EncodeBuffer buffer, BacnetObjectId deviceId, BacnetObjectId objectId, string objectName)
        {
            /* deviceIdentifier */
            ASN1.encode_application_object_id(buffer, deviceId.Type, deviceId.Instance);
            /* objectIdentifier */
            ASN1.encode_application_object_id(buffer, objectId.Type, objectId.Instance);
            /* objectName */
            ASN1.encode_application_character_string(buffer, objectName);
        }

        public static void EncodeWhoIsBroadcast(EncodeBuffer buffer, int lowLimit, int highLimit)
        {
            /* optional limits - must be used as a pair */
            if (lowLimit >= 0 && lowLimit <= ASN1.BACNET_MAX_INSTANCE &&
                highLimit >= 0 && highLimit <= ASN1.BACNET_MAX_INSTANCE)
            {
                ASN1.encode_context_unsigned(buffer, 0, (uint)lowLimit);
                ASN1.encode_context_unsigned(buffer, 1, (uint)highLimit);
            }
        }

        public static int DecodeWhoIsBroadcast(byte[] buffer, int offset, int apduLen, out int lowLimit, out int highLimit)
        {
            var len = 0;

            lowLimit = -1;
            highLimit = -1;

            if (apduLen <= 0) return len;

            /* optional limits - must be used as a pair */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);
            if (tagNumber != 0)
                return -1;
            if (apduLen > len)
            {
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint decodedValue);
                if (decodedValue <= ASN1.BACNET_MAX_INSTANCE)
                    lowLimit = (int)decodedValue;
                if (apduLen > len)
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                    if (tagNumber != 1)
                        return -1;
                    if (apduLen > len)
                    {
                        len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out decodedValue);
                        if (decodedValue <= ASN1.BACNET_MAX_INSTANCE)
                            highLimit = (int)decodedValue;
                    }
                    else
                        return -1;
                }
                else
                    return -1;
            }
            else
                return -1;

            return len;
        }

        public static void EncodeIamBroadcast(EncodeBuffer buffer, uint deviceId, uint maxApdu, BacnetSegmentations segmentation, ushort vendorId)
        {
            ASN1.encode_application_object_id(buffer, BacnetObjectTypes.OBJECT_DEVICE, deviceId);
            ASN1.encode_application_unsigned(buffer, maxApdu);
            ASN1.encode_application_enumerated(buffer, (uint)segmentation);
            ASN1.encode_application_unsigned(buffer, vendorId);
        }

        public static int DecodeIamBroadcast(byte[] buffer, int offset, out uint deviceId, out uint maxApdu, out BacnetSegmentations segmentation, out ushort vendorId)
        {
            var apduLen = 0;
            var orgOffset = offset;

            deviceId = 0;
            maxApdu = 0;
            segmentation = BacnetSegmentations.SEGMENTATION_NONE;
            vendorId = 0;

            /* OBJECT ID - object id */
            var len = ASN1.decode_tag_number_and_value(buffer, offset + apduLen, out var tagNumber, out var lenValue);
            apduLen += len;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
                return -1;
            len = ASN1.decode_object_id(buffer, offset + apduLen, out BacnetObjectTypes type, out var instance);
            apduLen += len;
            var objectId = new BacnetObjectId(type, instance);
            if (objectId.Type != BacnetObjectTypes.OBJECT_DEVICE)
                return -1;
            deviceId = objectId.Instance;
            /* MAX APDU - unsigned */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apduLen, out tagNumber, out lenValue);
            apduLen += len;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len = ASN1.decode_unsigned(buffer, offset + apduLen, lenValue, out uint decodedValue);
            apduLen += len;
            maxApdu = decodedValue;
            /* Segmentation - enumerated */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apduLen, out tagNumber, out lenValue);
            apduLen += len;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)
                return -1;
            len = ASN1.decode_unsigned(buffer, offset + apduLen, lenValue, out decodedValue);
            apduLen += len;
            if (decodedValue > (uint)BacnetSegmentations.SEGMENTATION_NONE)
                return -1;
            segmentation = (BacnetSegmentations)decodedValue;
            /* Vendor ID - unsigned16 */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apduLen, out tagNumber, out lenValue);
            apduLen += len;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            ASN1.decode_unsigned(buffer, offset + apduLen, lenValue, out decodedValue);
            if (decodedValue > 0xFFFF)
                return -1;
            vendorId = (ushort)decodedValue;

            return offset - orgOffset;
        }
    }
}
