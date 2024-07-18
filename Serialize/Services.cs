namespace System.IO.BACnet.Serialize;

public class Services
{
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
        BacnetObjectId objectId;

        deviceId = 0;
        maxApdu = 0;
        segmentation = BacnetSegmentations.SEGMENTATION_NONE;
        vendorId = 0;

        /* OBJECT ID - object id */
        var len = ASN1.decode_tag_number_and_value(buffer, offset + apduLen, out var tagNumber, out var lenValue);
        apduLen += len;
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
            return -1;
        len = ASN1.decode_object_id(buffer, offset + apduLen, out objectId.type, out objectId.instance);
        apduLen += len;
        if (objectId.type != BacnetObjectTypes.OBJECT_DEVICE)
            return -1;
        deviceId = objectId.instance;
        /* MAX APDU - unsigned */
        len =
            ASN1.decode_tag_number_and_value(buffer, offset + apduLen, out tagNumber, out lenValue);
        apduLen += len;
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
            return -1;
        len = ASN1.decode_unsigned(buffer, offset + apduLen, lenValue, out var decodedValue);
        apduLen += len;
        maxApdu = decodedValue;
        /* Segmentation - enumerated */
        len =
            ASN1.decode_tag_number_and_value(buffer, offset + apduLen, out tagNumber, out lenValue);
        apduLen += len;
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)
            return -1;
        len = ASN1.decode_enumerated(buffer, offset + apduLen, lenValue, out decodedValue);
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

    public static void EncodeIhaveBroadcast(EncodeBuffer buffer, BacnetObjectId deviceId, BacnetObjectId objectId, string objectName)
    {
        /* deviceIdentifier */
        ASN1.encode_application_object_id(buffer, deviceId.type, deviceId.instance);
        /* objectIdentifier */
        ASN1.encode_application_object_id(buffer, objectId.type, objectId.instance);
        /* objectName */
        ASN1.encode_application_character_string(buffer, objectName);
    }

    public static void EncodeWhoHasBroadcast(EncodeBuffer buffer, int lowLimit, int highLimit, BacnetObjectId? objectId, string objectName)
    {
        /* optional limits - must be used as a pair */
        if (lowLimit >= 0 && lowLimit <= ASN1.BACNET_MAX_INSTANCE && highLimit >= 0 && highLimit <= ASN1.BACNET_MAX_INSTANCE)
        {
            ASN1.encode_context_unsigned(buffer, 0, (uint)lowLimit);
            ASN1.encode_context_unsigned(buffer, 1, (uint)highLimit);
        }
        if (objectId != null)
        {
            ASN1.encode_context_object_id(buffer, 2, objectId.Value.type, objectId.Value.instance);
        }
        else
        {
            ASN1.encode_context_character_string(buffer, 3, objectName);
        }
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
            len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out var decodedValue);
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

    // Added by thamersalek
    public static int DecodeWhoHasBroadcast(byte[] buffer, int offset, int apduLen, out int lowLimit, out int highLimit, out BacnetObjectId? objId, out string objName)
    {
        var len = 0;
        uint decodedValue;

        objName = null;
        objId = null;
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

    public static void EncodeAlarmAcknowledge(EncodeBuffer buffer, uint ackProcessIdentifier, BacnetObjectId eventObjectIdentifier, uint eventStateAcked, string ackSource, BacnetGenericTime eventTimeStamp, BacnetGenericTime ackTimeStamp)
    {
        ASN1.encode_context_unsigned(buffer, 0, ackProcessIdentifier);
        ASN1.encode_context_object_id(buffer, 1, eventObjectIdentifier.type, eventObjectIdentifier.instance);
        ASN1.encode_context_enumerated(buffer, 2, eventStateAcked);
        ASN1.bacapp_encode_context_timestamp(buffer, 3, eventTimeStamp);
        ASN1.encode_context_character_string(buffer, 4, ackSource);
        ASN1.bacapp_encode_context_timestamp(buffer, 5, ackTimeStamp);
    }
    // DAL
    public static int DecodeAlarmAcknowledge(byte[] buffer, int offset, int apduLen, out uint ackProcessIdentifier, out BacnetObjectId eventObjectIdentifier, out uint eventStateAcked, out string ackSource, out BacnetGenericTime eventTimeStamp, out BacnetGenericTime ackTimeStamp)
    {
        eventTimeStamp = default;
        ackTimeStamp = default;
        ackSource = null;
        var len = 0;
        len += ASN1.decode_context_unsigned(buffer, offset + len, 0, out ackProcessIdentifier);
        len += ASN1.decode_context_object_id(buffer, offset + len, 1, out ushort type, out eventObjectIdentifier.instance);
        eventObjectIdentifier.type = (BacnetObjectTypes)type;
        len += ASN1.decode_context_enumerated(buffer, offset + len, 2, out eventStateAcked);
        if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
        {
            len += 2; // opening Tag 3 then 2
            len += ASN1.decode_application_date(buffer, offset + len, out var date);
            len += ASN1.decode_application_time(buffer, offset + len, out var time);
            eventTimeStamp.Time = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute,
                time.Second, time.Millisecond);

            len += 2; // closing tag 2 then 3
        }
        else
            return -1;
        len += ASN1.decode_context_character_string(buffer, offset + len, 256, 4, out ackSource);
        if (ASN1.decode_is_context_tag(buffer, offset + len, 5))
        {
            len += 2; // opening Tag 5 then 2
            len += ASN1.decode_application_date(buffer, offset + len, out var date);
            len += ASN1.decode_application_time(buffer, offset + len, out var time);
            ackTimeStamp.Time = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute,
                time.Second, time.Millisecond);
            len += 2; // closing tag 2 then 5
        }
        else
            return -1;
        return len;
    }

    public static void EncodeAtomicReadFile(EncodeBuffer buffer, bool isStream, BacnetObjectId objectId, int position, uint count)
    {
        ASN1.encode_application_object_id(buffer, objectId.type, objectId.instance);
        var tagNumber = (byte)(isStream ? 0 : 1);
        ASN1.encode_opening_tag(buffer, tagNumber);
        ASN1.encode_application_signed(buffer, position);
        ASN1.encode_application_unsigned(buffer, count);
        ASN1.encode_closing_tag(buffer, tagNumber);
    }

    public static int DecodeAtomicReadFile(byte[] buffer, int offset, int apduLen, out bool isStream, out BacnetObjectId objectId, out int position, out uint count)
    {
        var len = 0;
        int tagLen;

        isStream = true;
        objectId = new BacnetObjectId();
        position = -1;
        count = 0;

        len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
            return -1;
        len += ASN1.decode_object_id(buffer, offset + len, out objectId.type, out objectId.instance);
        if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
        {
            /* a tag number is not extended so only one octet */
            len++;
            /* fileStartPosition */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                return -1;
            len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
            /* requestedOctetCount */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out count);
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                return -1;
            /* a tag number is not extended so only one octet */
            len++;
        }
        else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
        {
            isStream = false;
            /* a tag number is not extended so only one octet */
            len++;
            /* fileStartRecord */
            tagLen =
                ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber,
                out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                return -1;
            len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
            /* RecordCount */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out count);
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            /* a tag number is not extended so only one octet */
            len++;
        }
        else
            return -1;

        return len;
    }

    public static void EncodeAtomicReadFileAcknowledge(EncodeBuffer buffer, bool isStream, bool endOfFile, int position, uint blockCount, byte[][] blocks, int[] counts)
    {
        ASN1.encode_application_boolean(buffer, endOfFile);
        var tagNumber = (byte)(isStream ? 0 : 1);
        ASN1.encode_opening_tag(buffer, tagNumber);
        ASN1.encode_application_signed(buffer, position);

        if (isStream)
        {
            ASN1.encode_application_octet_string(buffer, blocks[0], 0, counts[0]);
        }
        else
        {
            ASN1.encode_application_unsigned(buffer, blockCount);
            for (var i = 0; i < blockCount; i++)
                ASN1.encode_application_octet_string(buffer, blocks[i], 0, counts[i]);
        }

        ASN1.encode_closing_tag(buffer, tagNumber);
    }

    public static void EncodeAtomicWriteFile(EncodeBuffer buffer, bool isStream, BacnetObjectId objectId, int position, uint blockCount, byte[][] blocks, int[] counts)
    {
        ASN1.encode_application_object_id(buffer, objectId.type, objectId.instance);
        var tagNumber = (byte)(isStream ? 0 : 1);

        ASN1.encode_opening_tag(buffer, tagNumber);
        ASN1.encode_application_signed(buffer, position);

        if (isStream)
        {
            ASN1.encode_application_octet_string(buffer, blocks[0], 0, counts[0]);
        }
        else
        {
            ASN1.encode_application_unsigned(buffer, blockCount);
            for (var i = 0; i < blockCount; i++)
                ASN1.encode_application_octet_string(buffer, blocks[i], 0, counts[i]);
        }

        ASN1.encode_closing_tag(buffer, tagNumber);
    }

    public static int DecodeAtomicWriteFile(byte[] buffer, int offset, int apduLen, out bool isStream, out BacnetObjectId objectId, out int position, out uint blockCount, out byte[][] blocks, out int[] counts)
    {
        var len = 0;
        int tagLen;

        objectId = new BacnetObjectId();
        isStream = true;
        position = -1;
        blockCount = 0;
        blocks = null;
        counts = null;

        len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
            return -1;
        len += ASN1.decode_object_id(buffer, offset + len, out objectId.type, out objectId.instance);
        if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
        {
            /* a tag number of 2 is not extended so only one octet */
            len++;
            /* fileStartPosition */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                return -1;
            len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
            /* fileData */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                return -1;
            blockCount = 1;
            blocks = new byte[1][];
            blocks[0] = new byte[lenValueType];
            counts = new[] { (int)lenValueType };
            len += ASN1.decode_octet_string(buffer, offset + len, apduLen, blocks[0], 0, lenValueType);
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                return -1;
            /* a tag number is not extended so only one octet */
            len++;
        }
        else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
        {
            isStream = false;
            /* a tag number is not extended so only one octet */
            len++;
            /* fileStartRecord */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                return -1;
            len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
            /* returnedRecordCount */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out blockCount);
            /* fileData */
            blocks = new byte[blockCount][];
            counts = new int[blockCount];
            for (var i = 0; i < blockCount; i++)
            {
                tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += tagLen;
                blocks[i] = new byte[lenValueType];
                counts[i] = (int)lenValueType;
                if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                    return -1;
                len += ASN1.decode_octet_string(buffer, offset + len, apduLen, blocks[i], 0, lenValueType);
            }
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            /* a tag number is not extended so only one octet */
            len++;
        }
        else
            return -1;

        return len;
    }

    // by Christopher Günter
    public static void EncodeCreateProperty(EncodeBuffer buffer, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList)
    {
        /* Tag 1: sequence of WriteAccessSpecification */
        ASN1.encode_opening_tag(buffer, 0);
        ASN1.encode_context_object_id(buffer, 1, objectId.type, objectId.instance);
        ASN1.encode_closing_tag(buffer, 0);

        if (valueList == null)
            return;

        ASN1.encode_opening_tag(buffer, 1);

        foreach (var pValue in valueList)
        {
            ASN1.encode_context_enumerated(buffer, 0, pValue.property.propertyIdentifier);

            if (pValue.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                ASN1.encode_context_unsigned(buffer, 1, pValue.property.propertyArrayIndex);

            ASN1.encode_opening_tag(buffer, 2);
            foreach (var value in pValue.value)
            {
                ASN1.bacapp_encode_application_data(buffer, value);
            }
            ASN1.encode_closing_tag(buffer, 2);

            if (pValue.priority != ASN1.BACNET_NO_PRIORITY)
                ASN1.encode_context_unsigned(buffer, 3, pValue.priority);
        }

        ASN1.encode_closing_tag(buffer, 1);
    }

    public static void EncodeAddListElement(EncodeBuffer buffer, BacnetObjectId objectId, uint propertyId, uint arrayIndex, IList<BacnetValue> valueList)
    {
        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        ASN1.encode_context_enumerated(buffer, 1, propertyId);

        if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
        {
            ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
        }

        ASN1.encode_opening_tag(buffer, 3);
        foreach (var value in valueList)
        {
            ASN1.bacapp_encode_application_data(buffer, value);
        }

        ASN1.encode_closing_tag(buffer, 3);
    }

    public static void EncodeAtomicWriteFileAcknowledge(EncodeBuffer buffer, bool isStream, int position)
    {
        ASN1.encode_context_signed(buffer, (byte)(isStream ? 0 : 1), position);
    }

    public static void EncodeCOVNotifyConfirmed(EncodeBuffer buffer, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IEnumerable<BacnetPropertyValue> values)
    {
        /* tag 0 - subscriberProcessIdentifier */
        ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
        /* tag 1 - initiatingDeviceIdentifier */
        ASN1.encode_context_object_id(buffer, 1, BacnetObjectTypes.OBJECT_DEVICE, initiatingDeviceIdentifier);
        /* tag 2 - monitoredObjectIdentifier */
        ASN1.encode_context_object_id(buffer, 2, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
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

    public static void EncodeCOVNotifyUnconfirmed(EncodeBuffer buffer, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IEnumerable<BacnetPropertyValue> values)
    {
        /* tag 0 - subscriberProcessIdentifier */
        ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
        /* tag 1 - initiatingDeviceIdentifier */
        ASN1.encode_context_object_id(buffer, 1, BacnetObjectTypes.OBJECT_DEVICE, initiatingDeviceIdentifier);
        /* tag 2 - monitoredObjectIdentifier */
        ASN1.encode_context_object_id(buffer, 2, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
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

    public static void EncodeSubscribeCOV(EncodeBuffer buffer, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime)
    {
        /* tag 0 - subscriberProcessIdentifier */
        ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
        /* tag 1 - monitoredObjectIdentifier */
        ASN1.encode_context_object_id(buffer, 1, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
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
        monitoredObjectIdentifier = new BacnetObjectId();
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
            len += ASN1.decode_object_id(buffer, offset + len, out monitoredObjectIdentifier.type, out monitoredObjectIdentifier.instance);
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

    public static void EncodeSubscribeProperty(EncodeBuffer buffer, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetPropertyReference monitoredProperty, bool covIncrementPresent, float covIncrement)
    {
        /* tag 0 - subscriberProcessIdentifier */
        ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
        /* tag 1 - monitoredObjectIdentifier */
        ASN1.encode_context_object_id(buffer, 1, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
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

    public static int DecodeSubscribeProperty(byte[] buffer, int offset, int apduLen, out uint subscriberProcessIdentifier, out BacnetObjectId monitoredObjectIdentifier, out BacnetPropertyReference monitoredProperty, out bool cancellationRequest, out bool issueConfirmedNotifications, out uint lifetime, out float covIncrement)
    {
        var len = 0;
        uint lenValue;
        uint decodedValue;

        subscriberProcessIdentifier = 0;
        monitoredObjectIdentifier = new BacnetObjectId();
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
            len += ASN1.decode_object_id(buffer, offset + len, out monitoredObjectIdentifier.type, out monitoredObjectIdentifier.instance);
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
            len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out decodedValue);
            monitoredProperty.propertyIdentifier = decodedValue;
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
        if (len < apduLen && ASN1.decode_is_context_tag(buffer, offset + len, 5))
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

    // F Chaxel
    public static int DecodeEventNotifyData(byte[] buffer, int offset, int apduLen, out BacnetEventNotificationData eventData)
    {
        var len = 0;
        uint lenValue;

        eventData = new BacnetEventNotificationData();

        /* tag 0 - processIdentifier */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out eventData.processIdentifier);
        }
        else
            return -1;

        /*  tag 1 - initiatingObjectIdentifier */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_object_id(buffer, offset + len, out eventData.initiatingObjectIdentifier.type,
                out eventData.initiatingObjectIdentifier.instance);
        }
        else
            return -1;

        /*  tag 2 - eventObjectIdentifier */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_object_id(buffer, offset + len, out eventData.eventObjectIdentifier.type,
                out eventData.eventObjectIdentifier.instance);
        }
        else
            return -1;

        /*  tag 3 - timeStamp */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
        {
            len += 2; // opening Tag 3 then 2
            len += ASN1.decode_application_date(buffer, offset + len, out var date);
            len += ASN1.decode_application_time(buffer, offset + len, out var time);
            eventData.timeStamp.Time = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute,
                time.Second, time.Millisecond);

            len += 2; // closing tag 2 then 3
        }
        else
            return -1;

        /* tag 4 - noticicationClass */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 4))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out eventData.notificationClass);
        }
        else
            return -1;

        /* tag 5 - priority */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 5))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out var priority);
            if (priority > 0xFF) return -1;
            eventData.priority = (byte)priority;
        }
        else
            return -1;

        /* tag 6 - eventType */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 6))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out eventData.eventType);
        }
        else
            return -1;

        /* optional tag 7 - messageText  : never tested */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 7))
        {
            // max_lenght 20000 sound like a joke
            len += ASN1.decode_context_character_string(buffer, offset + len, 20000, 7, out eventData.messageText);
        }

        /* tag 8 - notifyType */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 8))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out eventData.notifyType);
        }
        else
            return -1;

        switch (eventData.notifyType)
        {
            case BacnetNotifyTypes.NOTIFY_ALARM:
            case BacnetNotifyTypes.NOTIFY_EVENT:
                /* tag 9 - ackRequired */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_unsigned8(buffer, offset + len, out var val);
                eventData.ackRequired = Convert.ToBoolean(val);

                /* tag 10 - fromState */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out eventData.fromState);
                break;
        }

        /* tag 11 - toState */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 11))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out eventData.toState);
        }
        else
            return -1;

        /* tag 12 - event values */
        switch (eventData.notifyType)
        {
            // In cases other than alarm and event there's no data,
            // so do not return an error but break and continue normally
            case BacnetNotifyTypes.NOTIFY_ACK_NOTIFICATION:
                break;

            case BacnetNotifyTypes.NOTIFY_ALARM:
            case BacnetNotifyTypes.NOTIFY_EVENT:
                if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 12))
                    return -1;

                len += ASN1.decode_tag_number(buffer, offset + len, out _);
                if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, (byte)eventData.eventType))
                    return -1;

                len += ASN1.decode_tag_number(buffer, offset + len, out _);
                switch (eventData.eventType)
                {
                    case BacnetEventTypes.EVENT_CHANGE_OF_BITSTRING:
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 0, out eventData.changeOfBitstring_referencedBitString);
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out eventData.changeOfBitstring_statusFlags);
                        break;

                    case BacnetEventTypes.EVENT_CHANGE_OF_STATE:
                        len += ASN1.decode_context_property_state(buffer, offset + len, 0, out eventData.changeOfState_newState);
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out eventData.changeOfState_statusFlags);
                        break;

                    case BacnetEventTypes.EVENT_CHANGE_OF_VALUE:
                        if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
                            return -1;

                        len++;
                        if (ASN1.decode_is_context_tag(buffer, offset + len, (byte)BacnetCOVTypes.CHANGE_OF_VALUE_BITS))
                        {
                            len += ASN1.decode_context_bitstring(buffer, offset + len, 0, out eventData.changeOfValue_changedBits);
                            eventData.changeOfValue_tag = BacnetCOVTypes.CHANGE_OF_VALUE_BITS;
                        }
                        else if (ASN1.decode_is_context_tag(buffer, offset + len, (byte)BacnetCOVTypes.CHANGE_OF_VALUE_REAL))
                        {
                            len += ASN1.decode_context_real(buffer, offset + len, 1, out eventData.changeOfValue_changeValue);
                            eventData.changeOfValue_tag = BacnetCOVTypes.CHANGE_OF_VALUE_REAL;
                        }
                        else
                        {
                            return -1;
                        }

                        if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                            return -1;

                        len++;
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 0, out eventData.changeOfValue_statusFlags);
                        break;

                    case BacnetEventTypes.EVENT_COMMAND_FAILURE:
                        if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
                            return -1;

                        len++;
                        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                        len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out eventData.commandFailure_commandValue);

                        if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                            return -1;

                        len++;
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out eventData.commandFailure_statusFlags);

                        if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 2))
                            return -1;
                        
                        len++;
                        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
                        len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out eventData.commandFailure_feedbackValue);

                        if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
                            return -1;

                        len++;
                        break;

                    case BacnetEventTypes.EVENT_FLOATING_LIMIT:
                        len += ASN1.decode_context_real(buffer, offset + len, 0, out eventData.floatingLimit_referenceValue);
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out eventData.floatingLimit_statusFlags);
                        len += ASN1.decode_context_real(buffer, offset + len, 2, out eventData.floatingLimit_setPointValue);
                        len += ASN1.decode_context_real(buffer, offset + len, 3, out eventData.floatingLimit_errorLimit);
                        break;

                    case BacnetEventTypes.EVENT_OUT_OF_RANGE:
                        len += ASN1.decode_context_real(buffer, offset + len, 0, out eventData.outOfRange_exceedingValue);
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out eventData.outOfRange_statusFlags);
                        len += ASN1.decode_context_real(buffer, offset + len, 2, out eventData.outOfRange_deadband);
                        len += ASN1.decode_context_real(buffer, offset + len, 3, out eventData.outOfRange_exceededLimit);
                        break;

                    case BacnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY:
                        len += ASN1.decode_context_enumerated(buffer, offset + len, 0, out eventData.changeOfLifeSafety_newState);
                        len += ASN1.decode_context_enumerated(buffer, offset + len, 1, out eventData.changeOfLifeSafety_newMode);
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 2, out eventData.changeOfLifeSafety_statusFlags);
                        len += ASN1.decode_context_enumerated(buffer, offset + len, 3, out eventData.changeOfLifeSafety_operationExpected);
                        break;

                    case BacnetEventTypes.EVENT_BUFFER_READY:
                        // Too lazy for this one and not sure if really needed, somebody want to do it ? :)
                        break;

                    case BacnetEventTypes.EVENT_UNSIGNED_RANGE:
                        len += ASN1.decode_context_unsigned(buffer, offset + len, 0, out eventData.unsignedRange_exceedingValue);
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out eventData.unsignedRange_statusFlags);
                        len += ASN1.decode_context_unsigned(buffer, offset + len, 2, out eventData.unsignedRange_exceededLimit);
                        break;

                    case BacnetEventTypes.EVENT_EXTENDED:
                        len += ASN1.decode_context_unsigned(buffer, offset + len, 0, out eventData.extended_vendorId);
                        len += ASN1.decode_context_unsigned(buffer, offset + len, 1, out eventData.extended_eventType);
                        if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 2))
                            return -1;

                        len++;
                        eventData.extended_parameters = new object[0];

                        while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
                        {
                            var paramCount = eventData.extended_parameters.Length + 1;
                            Array.Resize(ref eventData.extended_parameters, paramCount);

                            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out lenValue);

                            switch ((BacnetApplicationTags)tagNumber)
                            {
                                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                                    len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out var enumeratedValue);
                                    eventData.extended_parameters[paramCount - 1] = enumeratedValue;
                                    break;

                                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                                    len += ASN1.decode_bitstring(buffer, offset + len, lenValue, out var bitStringValue);
                                    eventData.extended_parameters[paramCount - 1] = bitStringValue;
                                    break;

                                case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out var unsignedValue);
                                    eventData.extended_parameters[paramCount - 1] = unsignedValue;
                                    break;

                                default:
                                    var octetStringValue = new byte[lenValue];
                                    len += ASN1.decode_octet_string(buffer, offset + len, apduLen, octetStringValue, 0, lenValue);
                                    eventData.extended_parameters[paramCount - 1] = octetStringValue;
                                    break;
                            }
                        }

                        len++;
                        break;

                    case BacnetEventTypes.EVENT_CHANGE_OF_RELIABILITY:
                        len += ASN1.decode_context_enumerated(buffer, offset + len, 0, out eventData.changeOfReliability_reliability);
                        len += ASN1.decode_context_bitstring(buffer, offset + len, 1, out eventData.changeOfReliability_statusFlags);

                        // Below decoding was only tested for events coming from OBJECT_LIFE_SAFETY_POINT and OBJECT_LIFE_SAFETY_ZONE
                        // Handling should be universal but it requires testing for other types of objects

                        eventData.changeOfReliability_propertyValues = new BacnetPropertyValue[0];

                        if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 2))
                        {
                            len += ASN1.decode_tag_number(buffer, offset + len, out _);
                            var currentIndex = -1;

                            while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
                            {
                                currentIndex++;

                                // add new object property
                                Array.Resize(ref eventData.changeOfReliability_propertyValues, currentIndex + 1);
                                
                                // read property identifier
                                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out var valueLength);                            
                                len += ASN1.decode_enumerated(buffer, offset + len, valueLength,
                                    out eventData.changeOfReliability_propertyValues[currentIndex].property.propertyIdentifier);

                                // read property values
                                var values = new List<BacnetValue>();
                                eventData.changeOfReliability_propertyValues[currentIndex].value = values;
                                if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 2))
                                {
                                    len += ASN1.decode_tag_number(buffer, offset + len, out _);
                                    while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
                                    {
                                        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out BacnetApplicationTags tagNumber, out valueLength);
                                        if (valueLength > 0)
                                        {
                                            len += ASN1.bacapp_decode_data(buffer, offset + len, apduLen, tagNumber, valueLength, out var value);
                                            values.Add(value);
                                        }
                                    }
                                    len += ASN1.decode_tag_number(buffer, offset + len, out _);
                                }
                            }
                            len += ASN1.decode_tag_number(buffer, offset + len, out _);
                        }
                        break;

                    default:
                        return -1;
                }

                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, (byte)eventData.eventType))
                    return -1;

                len += ASN1.decode_tag_number(buffer, offset + len, out _);
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 12))
                    return -1;

                len += ASN1.decode_tag_number(buffer, offset + len, out _);
                break;
        }

        return len;
    }

    private static void EncodeEventNotifyData(EncodeBuffer buffer, BacnetEventNotificationData data)
    {
        /* tag 0 - processIdentifier */
        ASN1.encode_context_unsigned(buffer, 0, data.processIdentifier);
        /* tag 1 - initiatingObjectIdentifier */
        ASN1.encode_context_object_id(buffer, 1, data.initiatingObjectIdentifier.type, data.initiatingObjectIdentifier.instance);

        /* tag 2 - eventObjectIdentifier */
        ASN1.encode_context_object_id(buffer, 2, data.eventObjectIdentifier.type, data.eventObjectIdentifier.instance);

        /* tag 3 - timeStamp */
        ASN1.bacapp_encode_context_timestamp(buffer, 3, data.timeStamp);

        /* tag 4 - noticicationClass */
        ASN1.encode_context_unsigned(buffer, 4, data.notificationClass);

        /* tag 5 - priority */
        ASN1.encode_context_unsigned(buffer, 5, data.priority);

        /* tag 6 - eventType */
        ASN1.encode_context_enumerated(buffer, 6, (uint)data.eventType);

        /* tag 7 - messageText */
        if (!string.IsNullOrEmpty(data.messageText))
            ASN1.encode_context_character_string(buffer, 7, data.messageText);

        /* tag 8 - notifyType */
        ASN1.encode_context_enumerated(buffer, 8, (uint)data.notifyType);

        switch (data.notifyType)
        {
            case BacnetNotifyTypes.NOTIFY_ALARM:
            case BacnetNotifyTypes.NOTIFY_EVENT:
                /* tag 9 - ackRequired */
                ASN1.encode_context_boolean(buffer, 9, data.ackRequired);

                /* tag 10 - fromState */
                ASN1.encode_context_enumerated(buffer, 10, (uint)data.fromState);
                break;
        }

        /* tag 11 - toState */
        ASN1.encode_context_enumerated(buffer, 11, (uint)data.toState);

        switch (data.notifyType)
        {
            case BacnetNotifyTypes.NOTIFY_ALARM:
            case BacnetNotifyTypes.NOTIFY_EVENT:
                /* tag 12 - event values */
                ASN1.encode_opening_tag(buffer, 12);

                switch (data.eventType)
                {
                    case BacnetEventTypes.EVENT_CHANGE_OF_BITSTRING:
                        ASN1.encode_opening_tag(buffer, 0);
                        ASN1.encode_context_bitstring(buffer, 0, data.changeOfBitstring_referencedBitString);
                        ASN1.encode_context_bitstring(buffer, 1, data.changeOfBitstring_statusFlags);
                        ASN1.encode_closing_tag(buffer, 0);
                        break;

                    case BacnetEventTypes.EVENT_CHANGE_OF_STATE:
                        ASN1.encode_opening_tag(buffer, 1);
                        ASN1.encode_opening_tag(buffer, 0);
                        ASN1.bacapp_encode_property_state(buffer, data.changeOfState_newState);
                        ASN1.encode_closing_tag(buffer, 0);
                        ASN1.encode_context_bitstring(buffer, 1, data.changeOfState_statusFlags);
                        ASN1.encode_closing_tag(buffer, 1);
                        break;

                    case BacnetEventTypes.EVENT_CHANGE_OF_VALUE:
                        ASN1.encode_opening_tag(buffer, 2);
                        ASN1.encode_opening_tag(buffer, 0);

                        switch (data.changeOfValue_tag)
                        {
                            case BacnetCOVTypes.CHANGE_OF_VALUE_REAL:
                                ASN1.encode_context_real(buffer, 1, data.changeOfValue_changeValue);
                                break;

                            case BacnetCOVTypes.CHANGE_OF_VALUE_BITS:
                                ASN1.encode_context_bitstring(buffer, 0, data.changeOfValue_changedBits);
                                break;

                            default:
                                throw new Exception("Hmm?");
                        }

                        ASN1.encode_closing_tag(buffer, 0);
                        ASN1.encode_context_bitstring(buffer, 1, data.changeOfValue_statusFlags);
                        ASN1.encode_closing_tag(buffer, 2);
                        break;

                    case BacnetEventTypes.EVENT_FLOATING_LIMIT:
                        ASN1.encode_opening_tag(buffer, 4);
                        ASN1.encode_context_real(buffer, 0, data.floatingLimit_referenceValue);
                        ASN1.encode_context_bitstring(buffer, 1, data.floatingLimit_statusFlags);
                        ASN1.encode_context_real(buffer, 2, data.floatingLimit_setPointValue);
                        ASN1.encode_context_real(buffer, 3, data.floatingLimit_errorLimit);
                        ASN1.encode_closing_tag(buffer, 4);
                        break;

                    case BacnetEventTypes.EVENT_OUT_OF_RANGE:
                        ASN1.encode_opening_tag(buffer, 5);
                        ASN1.encode_context_real(buffer, 0, data.outOfRange_exceedingValue);
                        ASN1.encode_context_bitstring(buffer, 1, data.outOfRange_statusFlags);
                        ASN1.encode_context_real(buffer, 2, data.outOfRange_deadband);
                        ASN1.encode_context_real(buffer, 3, data.outOfRange_exceededLimit);
                        ASN1.encode_closing_tag(buffer, 5);
                        break;

                    case BacnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY:
                        ASN1.encode_opening_tag(buffer, 8);
                        ASN1.encode_context_enumerated(buffer, 0, (uint)data.changeOfLifeSafety_newState);
                        ASN1.encode_context_enumerated(buffer, 1, (uint)data.changeOfLifeSafety_newMode);
                        ASN1.encode_context_bitstring(buffer, 2, data.changeOfLifeSafety_statusFlags);
                        ASN1.encode_context_enumerated(buffer, 3, (uint)data.changeOfLifeSafety_operationExpected);
                        ASN1.encode_closing_tag(buffer, 8);
                        break;

                    case BacnetEventTypes.EVENT_BUFFER_READY:
                        ASN1.encode_opening_tag(buffer, 10);
                        ASN1.bacapp_encode_context_device_obj_property_ref(buffer, 0, data.bufferReady_bufferProperty);
                        ASN1.encode_context_unsigned(buffer, 1, data.bufferReady_previousNotification);
                        ASN1.encode_context_unsigned(buffer, 2, data.bufferReady_currentNotification);
                        ASN1.encode_closing_tag(buffer, 10);

                        break;

                    case BacnetEventTypes.EVENT_UNSIGNED_RANGE:
                        ASN1.encode_opening_tag(buffer, 11);
                        ASN1.encode_context_unsigned(buffer, 0, data.unsignedRange_exceedingValue);
                        ASN1.encode_context_bitstring(buffer, 1, data.unsignedRange_statusFlags);
                        ASN1.encode_context_unsigned(buffer, 2, data.unsignedRange_exceededLimit);
                        ASN1.encode_closing_tag(buffer, 11);
                        break;

                    default:
                        throw new NotImplementedException();
                }
                ASN1.encode_closing_tag(buffer, 12);
                break;

            case BacnetNotifyTypes.NOTIFY_ACK_NOTIFICATION:
                /* FIXME: handle this case */
                break;
        }
    }

    public static void EncodeEventNotifyConfirmed(EncodeBuffer buffer, BacnetEventNotificationData data)
    {
        EncodeEventNotifyData(buffer, data);
    }

    public static void EncodeEventNotifyUnconfirmed(EncodeBuffer buffer, BacnetEventNotificationData data)
    {
        EncodeEventNotifyData(buffer, data);
    }

    public static void EncodeAlarmSummary(EncodeBuffer buffer, BacnetObjectId objectIdentifier, uint alarmState, BacnetBitString acknowledgedTransitions)
    {
        /* tag 0 - Object Identifier */
        ASN1.encode_application_object_id(buffer, objectIdentifier.type, objectIdentifier.instance);
        /* tag 1 - Alarm State */
        ASN1.encode_application_enumerated(buffer, alarmState);
        /* tag 2 - Acknowledged Transitions */
        ASN1.encode_application_bitstring(buffer, acknowledgedTransitions);
    }

    // FChaxel
    public static int DecodeAlarmSummaryOrEvent(byte[] buffer, int offset, int apduLen, bool getEvent, ref IList<BacnetGetEventInformationData> alarms, out bool moreEvent)
    {
        var len = 0; ;

        if (getEvent)
            len++;  // peut être tag 0

        while (apduLen - 3 - len > 0)
        {
            var value = new BacnetGetEventInformationData();

            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);
            len += ASN1.decode_object_id(buffer, offset + len, out value.objectIdentifier.type, out value.objectIdentifier.instance);
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out value.eventState);
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            len += ASN1.decode_bitstring(buffer, offset + len, lenValue, out value.acknowledgedTransitions);

            if (getEvent)
            {
                len++;  // opening Tag 3
                value.eventTimeStamps = new BacnetGenericTime[3];

                for (var i = 0; i < 3; i++)
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue); // opening tag

                    if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                    {
                        len += ASN1.decode_application_date(buffer, offset + len, out var date);
                        len += ASN1.decode_application_time(buffer, offset + len, out var time);
                        var timestamp = date.Date + time.TimeOfDay;
                        value.eventTimeStamps[i] = new BacnetGenericTime(timestamp, BacnetTimestampTags.TIME_STAMP_DATETIME);
                        len++; // closing tag
                    }
                    else
                        len += (int)lenValue;
                }

                len++;  // closing Tag 3

                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out value.notifyType);
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
            }

            alarms.Add(value);
        }

        if (getEvent)
            moreEvent = buffer[apduLen - 1] == 1;
        else
            moreEvent = false;

        return len;
    }
    // DAL
    public static int DecodeAlarmSummaryOrEventRequest(byte[] buffer, int offset, int apduLen, bool getEvent, ref BacnetObjectId id)
    {
        var len = 0;
        // object id may be there for get event info, but it is optional
        if (getEvent && apduLen != 0)
        {
            len++; // past opening tag 0
            len += ASN1.decode_object_id(buffer, offset + len, out id.type, out id.instance);
        }
        return len;
    }

    public static void EncodeGetEventInformation(EncodeBuffer buffer, bool sendLast, BacnetObjectId lastReceivedObjectIdentifier)
    {
        /* encode optional parameter */
        if (sendLast)
            ASN1.encode_context_object_id(buffer, 0, lastReceivedObjectIdentifier.type, lastReceivedObjectIdentifier.instance);
    }

    public static void EncodeGetEventInformationAcknowledge(EncodeBuffer buffer, BacnetGetEventInformationData[] events, bool moreEvents)
    {
        /* service ack follows */
        /* Tag 0: listOfEventSummaries */
        ASN1.encode_opening_tag(buffer, 0);
        foreach (var eventData in events)
        {
            /* Tag 0: objectIdentifier */
            ASN1.encode_context_object_id(buffer, 0, eventData.objectIdentifier.type, eventData.objectIdentifier.instance);
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

    public static void EncodeLifeSafetyOperation(EncodeBuffer buffer, uint processId, string requestingSrc, uint operation, BacnetObjectId targetObject)
    {
        /* tag 0 - requestingProcessId */
        ASN1.encode_context_unsigned(buffer, 0, processId);
        /* tag 1 - requestingSource */
        ASN1.encode_context_character_string(buffer, 1, requestingSrc);
        /* Operation */
        ASN1.encode_context_enumerated(buffer, 2, operation);
        /* Object ID */
        ASN1.encode_context_object_id(buffer, 3, targetObject.type, targetObject.instance);
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
        ASN1.encode_opening_tag(buffer, 2);
        buffer.Add(data, data.Length);
        ASN1.encode_closing_tag(buffer, 2);
    }

    public static void EncodeDeviceCommunicationControl(EncodeBuffer buffer, uint timeDuration, uint enableDisable, string password)
    {
        /* optional timeDuration */
        if (timeDuration > 0)
            ASN1.encode_context_unsigned(buffer, 0, timeDuration);

        /* enable disable */
        ASN1.encode_context_enumerated(buffer, 1, enableDisable);

        /* optional password */
        if (!string.IsNullOrEmpty(password))
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
        len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out enableDisable);

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

    public static void EncodeReinitializeDevice(EncodeBuffer buffer, BacnetReinitializedStates state, string password)
    {
        ASN1.encode_context_enumerated(buffer, 0, (uint)state);

        /* optional password */
        if (!string.IsNullOrEmpty(password))
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
        len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out state);
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

    public static void EncodeReadRange(EncodeBuffer buffer, BacnetObjectId objectId, uint propertyId, uint arrayIndex, BacnetReadRangeRequestTypes requestType, uint position, DateTime time, int count)
    {
        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        ASN1.encode_context_enumerated(buffer, 1, propertyId);

        /* optional array index */
        if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
        {
            ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
        }

        /* Build the appropriate (optional) range parameter based on the request type */
        switch (requestType)
        {
            case BacnetReadRangeRequestTypes.RR_BY_POSITION:
                ASN1.encode_opening_tag(buffer, 3);
                ASN1.encode_application_unsigned(buffer, position);
                ASN1.encode_application_signed(buffer, count);
                ASN1.encode_closing_tag(buffer, 3);
                break;

            case BacnetReadRangeRequestTypes.RR_BY_SEQUENCE:
                ASN1.encode_opening_tag(buffer, 6);
                ASN1.encode_application_unsigned(buffer, position);
                ASN1.encode_application_signed(buffer, count);
                ASN1.encode_closing_tag(buffer, 6);
                break;

            case BacnetReadRangeRequestTypes.RR_BY_TIME:
                ASN1.encode_opening_tag(buffer, 7);
                ASN1.encode_application_date(buffer, time);
                ASN1.encode_application_time(buffer, time);
                ASN1.encode_application_signed(buffer, count);
                ASN1.encode_closing_tag(buffer, 7);
                break;

            case BacnetReadRangeRequestTypes.RR_READ_ALL:  /* to attempt a read of the whole array or list, omit the range parameter */
                break;
        }
    }

    public static int DecodeReadRange(byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out BacnetPropertyReference property, out BacnetReadRangeRequestTypes requestType, out uint position, out DateTime time, out int count)
    {
        var len = 0;

        objectId = new BacnetObjectId();
        property = new BacnetPropertyReference();
        requestType = BacnetReadRangeRequestTypes.RR_READ_ALL;
        position = 0;
        time = new DateTime(1, 1, 1);
        count = -1;

        /* Tag 0: Object ID          */
        if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
            return -1;
        len++;
        len += ASN1.decode_object_id(buffer, offset + len, out ushort type, out objectId.instance);
        objectId.type = (BacnetObjectTypes)type;
        /* Tag 1: Property ID */
        len +=
            ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != 1)
            return -1;
        len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out property.propertyIdentifier);

        /* Tag 2: Optional Array Index */
        if (len < apduLen && ASN1.decode_is_context_tag(buffer, offset + len, 0))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out property.propertyArrayIndex);
        }
        else
            property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

        /* optional request type */
        if (len < apduLen)
        {
            len += ASN1.decode_tag_number(buffer, offset + len, out tagNumber);    //opening tag
            switch (tagNumber)
            {
                case 3:
                    requestType = BacnetReadRangeRequestTypes.RR_BY_POSITION;
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out position);
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                    len += ASN1.decode_signed(buffer, offset + len, lenValueType, out count);
                    break;

                case 6:
                    requestType = BacnetReadRangeRequestTypes.RR_BY_SEQUENCE;
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out position);
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                    len += ASN1.decode_signed(buffer, offset + len, lenValueType, out count);
                    break;

                case 7:
                    requestType = BacnetReadRangeRequestTypes.RR_BY_TIME;
                    len += ASN1.decode_application_date(buffer, offset + len, out var date);
                    len += ASN1.decode_application_time(buffer, offset + len, out time);
                    time = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                    len += ASN1.decode_signed(buffer, offset + len, lenValueType, out count);
                    break;

                default:
                    return -1;  //don't know this type yet
            }
            len += ASN1.decode_tag_number(buffer, offset + len, out tagNumber);    //closing tag
        }
        return len;
    }

    public static void EncodeReadRangeAcknowledge(EncodeBuffer buffer, BacnetObjectId objectId, uint propertyId, uint arrayIndex, BacnetBitString resultFlags, uint itemCount, byte[] applicationData, BacnetReadRangeRequestTypes requestType, uint firstSequence)
    {
        /* service ack follows */
        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        ASN1.encode_context_enumerated(buffer, 1, propertyId);
        /* context 2 array index is optional */
        if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
        {
            ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
        }
        /* Context 3 BACnet Result Flags */
        ASN1.encode_context_bitstring(buffer, 3, resultFlags);
        /* Context 4 Item Count */
        ASN1.encode_context_unsigned(buffer, 4, itemCount);
        /* Context 5 Property list - reading the standard it looks like an empty list still
         * requires an opening and closing tag as the tagged parameter is not optional
         */
        ASN1.encode_opening_tag(buffer, 5);
        if (itemCount != 0)
        {
            buffer.Add(applicationData, applicationData.Length);
        }
        ASN1.encode_closing_tag(buffer, 5);

        if (itemCount != 0 && requestType != BacnetReadRangeRequestTypes.RR_BY_POSITION && requestType != BacnetReadRangeRequestTypes.RR_READ_ALL)
        {
            /* Context 6 Sequence number of first item */
            ASN1.encode_context_unsigned(buffer, 6, firstSequence);
        }
    }

    // FC
    public static uint DecodeReadRangeAcknowledge(byte[] buffer, int offset, int apduLen, out byte[] rangeBuffer)
    {
        var len = 0;
        rangeBuffer = null;

        /* Tag 0: Object ID          */
        if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
            return 0;

        len++;
        len += ASN1.decode_object_id(buffer, offset + len, out ushort _, out _);

        /* Tag 1: Property ID */
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != 1)
            return 0;
        len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out _);

        /* Tag 2: Optional Array Index or Tag 3:  BACnet Result Flags */
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber == 2 && len < apduLen)
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out _);
        else
            /* Tag 3:  BACnet Result Flags */
            len += ASN1.decode_bitstring(buffer, offset + len, 2, out _);

        /* Tag 4 Item Count */
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out var itemCount);

        if (!ASN1.decode_is_opening_tag(buffer, offset + len))
            return 0;
        len += 1;

        rangeBuffer = new byte[buffer.Length - offset - len - 1];

        Array.Copy(buffer, offset + len, rangeBuffer, 0, rangeBuffer.Length);

        return itemCount;
    }

    public static void EncodeReadProperty(EncodeBuffer buffer, BacnetObjectId objectId, uint propertyId, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        if ((int)objectId.type <= ASN1.BACNET_MAX_OBJECT)
        {
            /* check bounds so that we could create malformed
               messages for testing */
            ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        }
        if (propertyId <= (uint)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID)
        {
            /* check bounds so that we could create malformed
               messages for testing */
            ASN1.encode_context_enumerated(buffer, 1, propertyId);
        }
        /* optional array index */
        if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
        {
            ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
        }
    }

    public static int DecodeAtomicWriteFileAcknowledge(byte[] buffer, int offset, int apduLen, out bool isStream, out int position)
    {
        var len = 0;

        isStream = false;
        position = 0;

        len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        switch (tagNumber)
        {
            case 0:
                isStream = true;
                len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                break;

            case 1:
                len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
                break;

            default:
                return -1;
        }

        return len;
    }

    public static int DecodeAtomicReadFileAcknowledge(byte[] buffer, int offset, int apduLen, out bool endOfFile, out bool isStream, out int position, out uint count, out byte[] targetBuffer, out int targetOffset)
    {
        var len = 0;

        endOfFile = false;
        isStream = false;
        position = -1;
        count = 0;
        targetBuffer = null;
        targetOffset = -1;

        len = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)
            return -1;

        endOfFile = lenValueType > 0;
        if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
        {
            isStream = true;
            /* a tag number is not extended so only one octet */
            len++;
            /* fileStartPosition */
            var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                return -1;
            len += ASN1.decode_signed(buffer, offset + len, lenValueType, out position);
            /* fileData */
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                return -1;
            //len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
            targetBuffer = buffer;
            targetOffset = offset + len;
            count = lenValueType;
            len += (int)count;
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                return -1;
            /* a tag number is not extended so only one octet */
            len++;
        }
        else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
        {
            throw new NotImplementedException("Non stream File transfers are not supported");
            ///* a tag number is not extended so only one octet */
            //len++;
            ///* fileStartRecord */
            //tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            //len += tag_len;
            //if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
            //    return -1;
            //len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
            ///* returnedRecordCount */
            //tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            //len += tag_len;
            //if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
            //    return -1;
            //len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out count);
            //for (i = 0; i < count; i++)
            //{
            //    /* fileData */
            //    tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            //    len += tag_len;
            //    if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
            //        return -1;
            //    len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
            //}
            //if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
            //    return -1;
            ///* a tag number is not extended so only one octet */
            //len++;
        }
        else
            return -1;

        return len;
    }

    public static int DecodeReadProperty(byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out BacnetPropertyReference property)
    {
        var len = 0;

        objectId = new BacnetObjectId();
        property = new BacnetPropertyReference();

        // must have at least 2 tags , otherwise return reject code: Missing required parameter
        if (apduLen < 7)
            return -1;

        /* Tag 0: Object ID */
        if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
            return -2;

        len++;
        len += ASN1.decode_object_id(buffer, offset + len, out ushort type, out objectId.instance);
        objectId.type = (BacnetObjectTypes)type;

        /* Tag 1: Property ID */
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != 1)
            return -2;

        len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out property.propertyIdentifier);

        /* Tag 2: Optional Array Index */
        if (len < apduLen)
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            if (tagNumber == 2 && len < apduLen)
            {
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out property.propertyArrayIndex);
            }
            else
                return -2;
        }
        else
            property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

        if (len < apduLen)
            /* If something left over now, we have an invalid request */
            return -3;

        return len;
    }

    public static void EncodeReadPropertyAcknowledge(EncodeBuffer buffer, BacnetObjectId objectId, uint propertyId, uint arrayIndex, IEnumerable<BacnetValue> valueList)
    {
        /* service ack follows */
        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        ASN1.encode_context_enumerated(buffer, 1, propertyId);
        /* context 2 array index is optional */
        if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
        {
            ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
        }

        /* Value */
        ASN1.encode_opening_tag(buffer, 3);
        foreach (BacnetValue value in valueList)
        {
            ASN1.bacapp_encode_application_data(buffer, value);
        }
        ASN1.encode_closing_tag(buffer, 3);
    }

    public static int DecodeReadPropertyAcknowledge(BacnetAddress address, byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out BacnetPropertyReference property, out IList<BacnetValue> valueList)
    {
        objectId = new BacnetObjectId();
        property = new BacnetPropertyReference();
        valueList = new List<BacnetValue>();

        /* FIXME: check apduLen against the len during decode   */
        /* Tag 0: Object ID */
        if (!ASN1.decode_is_context_tag(buffer, offset, 0))
            return -1;
        var len = 1;
        len += ASN1.decode_object_id(buffer, offset + len, out objectId.type, out objectId.instance);
        /* Tag 1: Property ID */
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != 1)
            return -1;
        len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out property.propertyIdentifier);
        /* Tag 2: Optional Array Index */
        var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber == 2)
        {
            len += tagLen;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out property.propertyArrayIndex);
        }
        else
            property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

        /* Tag 3: opening context tag */
        if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 3))
        {
            /* a tag number of 3 is not extended so only one octet */
            len++;

            while (apduLen - len > 1)
            {
                tagLen = ASN1.bacapp_decode_application_data(address, buffer, offset + len, apduLen + offset, objectId.type, (BacnetPropertyIds)property.propertyIdentifier, out var value);
                if (tagLen < 0) return -1;
                len += tagLen;
                valueList.Add(value);
            }
        }
        else
            return -1;

        if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
            return -1;
        len++;

        return len;
    }

    public static void EncodeReadPropertyMultiple(EncodeBuffer buffer, IList<BacnetReadAccessSpecification> properties)
    {
        foreach (var value in properties)
            ASN1.encode_read_access_specification(buffer, value);
    }

    public static void EncodeReadPropertyMultiple(EncodeBuffer buffer, BacnetObjectId objectId, IList<BacnetPropertyReference> properties)
    {
        EncodeReadPropertyMultiple(buffer, new[] { new BacnetReadAccessSpecification(objectId, properties) });
    }

    public static int DecodeReadPropertyMultiple(byte[] buffer, int offset, int apduLen, out IList<BacnetReadAccessSpecification> properties)
    {
        var len = 0;

        var values = new List<BacnetReadAccessSpecification>();
        properties = null;

        while (apduLen - len > 0)
        {
            var tmp = ASN1.decode_read_access_specification(buffer, offset + len, apduLen - len, out var value);
            if (tmp < 0) return -1;
            len += tmp;
            values.Add(value);
        }

        properties = values;
        return len;
    }

    public static void EncodeReadPropertyMultipleAcknowledge(EncodeBuffer buffer, IList<BacnetReadAccessResult> values)
    {
        foreach (var value in values)
            ASN1.encode_read_access_result(buffer, value);
    }

    public static int DecodeReadPropertyMultipleAcknowledge(BacnetAddress address, byte[] buffer, int offset, int apduLen, out IList<BacnetReadAccessResult> values)
    {
        var len = 0;

        var result = new List<BacnetReadAccessResult>();

        while (apduLen - len > 0)
        {
            var tmp = ASN1.decode_read_access_result(address, buffer, offset + len, apduLen - len, out var value);
            if (tmp < 0)
            {
                values = null;
                return -1;
            }
            len += tmp;
            result.Add(value);
        }

        values = result;
        return len;
    }

    public static void EncodeWriteProperty(EncodeBuffer buffer, BacnetObjectId objectId, uint propertyId, uint arrayIndex, uint priority, IEnumerable<BacnetValue> valueList)
    {
        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        ASN1.encode_context_enumerated(buffer, 1, propertyId);

        /* optional array index; ALL is -1 which is assumed when missing */
        if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
        {
            ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
        }

        /* propertyValue */
        ASN1.encode_opening_tag(buffer, 3);
        foreach (var value in valueList)
        {
            ASN1.bacapp_encode_application_data(buffer, value);
        }
        ASN1.encode_closing_tag(buffer, 3);

        /* optional priority - 0 if not set, 1..16 if set */
        if (priority != ASN1.BACNET_NO_PRIORITY)
        {
            ASN1.encode_context_unsigned(buffer, 4, priority);
        }
    }

    public static int DecodeCOVNotifyUnconfirmed(BacnetAddress address, byte[] buffer, int offset, int apduLen, out uint subscriberProcessIdentifier, out BacnetObjectId initiatingDeviceIdentifier, out BacnetObjectId monitoredObjectIdentifier, out uint timeRemaining, out ICollection<BacnetPropertyValue> values)
    {
        var len = 0;
        uint lenValue;

        subscriberProcessIdentifier = 0;
        initiatingDeviceIdentifier = new BacnetObjectId();
        monitoredObjectIdentifier = new BacnetObjectId();
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
            len += ASN1.decode_object_id(buffer, offset + len, out initiatingDeviceIdentifier.type, out initiatingDeviceIdentifier.instance);
        }
        else
            return -1;

        /* tag 2 - monitoredObjectIdentifier */
        if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out _, out lenValue);
            len += ASN1.decode_object_id(buffer, offset + len, out monitoredObjectIdentifier.type, out monitoredObjectIdentifier.instance);
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
                len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out newEntry.property.propertyIdentifier);
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
                var tmp = ASN1.bacapp_decode_application_data(address, buffer, offset + len, apduLen + offset, monitoredObjectIdentifier.type, (BacnetPropertyIds)newEntry.property.propertyIdentifier, out var bValue);
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
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out var decodedValue);
                newEntry.priority = (byte)decodedValue;
            }
            else
                newEntry.priority = (byte)ASN1.BACNET_NO_PRIORITY;

            _values.AddLast(newEntry);
        }

        values = _values;
        return len;
    }

    public static int DecodeWriteProperty(BacnetAddress address, byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out BacnetPropertyValue value)
    {
        var len = 0;

        objectId = new BacnetObjectId();
        value = new BacnetPropertyValue();

        /* Tag 0: Object ID          */
        if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
            return -1;
        len++;
        len += ASN1.decode_object_id(buffer, offset + len, out objectId.type, out objectId.instance);
        /* Tag 1: Property ID */
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != 1)
            return -1;
        len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out value.property.propertyIdentifier);
        /* Tag 2: Optional Array Index */
        /* note: decode without incrementing len so we can check for opening tag */
        var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber == 2)
        {
            len += tagLen;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out value.property.propertyArrayIndex);
        }
        else
            value.property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;
        /* Tag 3: opening context tag */
        if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 3))
            return -1;
        len++;

        //data
        var valueList = new List<BacnetValue>();
        while (apduLen - len > 1 && !ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
        {
            var l = ASN1.bacapp_decode_application_data(address, buffer, offset + len, apduLen + offset, objectId.type, (BacnetPropertyIds)value.property.propertyIdentifier, out var bValue);
            if (l <= 0) return -1;
            len += l;
            valueList.Add(bValue);
        }
        value.value = valueList;

        if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
            return -2;
        /* a tag number of 3 is not extended so only one octet */
        len++;
        /* Tag 4: optional Priority - assumed MAX if not explicitly set */
        value.priority = (byte)ASN1.BACNET_MAX_PRIORITY;
        if (len < apduLen)
        {
            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            if (tagNumber == 4)
            {
                len += tagLen;
                len = ASN1.decode_unsigned(buffer, offset + len, lenValueType, out var unsignedValue);
                if (unsignedValue >= ASN1.BACNET_MIN_PRIORITY && unsignedValue <= ASN1.BACNET_MAX_PRIORITY)
                    value.priority = (byte)unsignedValue;
                else
                    return -1;
            }
        }

        return len;
    }

    public static void EncodeWritePropertyMultiple(EncodeBuffer buffer, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList)
    {
        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        /* Tag 1: sequence of WriteAccessSpecification */
        ASN1.encode_opening_tag(buffer, 1);

        foreach (var pValue in valueList)
        {
            /* Tag 0: Property */
            ASN1.encode_context_enumerated(buffer, 0, pValue.property.propertyIdentifier);

            /* Tag 1: array index */
            if (pValue.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                ASN1.encode_context_unsigned(buffer, 1, pValue.property.propertyArrayIndex);

            /* Tag 2: Value */
            ASN1.encode_opening_tag(buffer, 2);
            foreach (var value in pValue.value)
            {
                ASN1.bacapp_encode_application_data(buffer, value);
            }
            ASN1.encode_closing_tag(buffer, 2);

            /* Tag 3: Priority */
            if (pValue.priority != ASN1.BACNET_NO_PRIORITY)
                ASN1.encode_context_unsigned(buffer, 3, pValue.priority);
        }

        ASN1.encode_closing_tag(buffer, 1);
    }

    public static void EncodeWriteObjectMultiple(EncodeBuffer buffer, ICollection<BacnetReadAccessResult> valueList)
    {
        foreach (var value in valueList)
            EncodeWritePropertyMultiple(buffer, value.objectIdentifier, value.values);
    }

    // By C. Gunter
    // quite the same as DecodeWritePropertyMultiple
    public static int DecodeCreateObject(BacnetAddress address, byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out ICollection<BacnetPropertyValue> valuesRefs)
    {
        var len = 0;

        objectId = new BacnetObjectId();
        valuesRefs = null;

        //object id
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);

        if (tagNumber == 0 && apduLen > len)
        {
            apduLen -= len;
            if (apduLen >= 4)
            {
                len += ASN1.decode_context_object_id(buffer, offset + len, 1, out var typenr, out objectId.instance);
                objectId.type = (BacnetObjectTypes)typenr;
            }
            else
                return -1;
        }
        else
            return -1;
        if (ASN1.decode_is_closing_tag(buffer, offset + len))
            len++;
        //end objectid

        // No initial values ?
        if (buffer.Length == offset + len)
            return len;

        /* Tag 1: sequence of WriteAccessSpecification */
        if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
            return -1;
        len++;

        var _values = new LinkedList<BacnetPropertyValue>();
        while (apduLen - len > 1)
        {
            var newEntry = new BacnetPropertyValue();

            /* tag 0 - Property Identifier */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            uint propertyId;
            if (tagNumber == 0)
                len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out propertyId);
            else
                return -1;

            /* tag 1 - Property Array Index - optional */
            var ulVal = ASN1.BACNET_ARRAY_ALL;
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            if (tagNumber == 1)
            {
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out ulVal);
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            }
            newEntry.property = new BacnetPropertyReference(propertyId, ulVal);

            /* tag 2 - Property Value */
            if (tagNumber == 2 && ASN1.decode_is_opening_tag(buffer, offset + len - 1))
            {
                var values = new List<BacnetValue>();
                while (!ASN1.decode_is_closing_tag(buffer, offset + len))
                {
                    var l = ASN1.bacapp_decode_application_data(address, buffer, offset + len, apduLen + offset, objectId.type, (BacnetPropertyIds)propertyId, out var value);
                    if (l <= 0) return -1;
                    len += l;
                    values.Add(value);
                }
                len++;
                newEntry.value = values;
            }
            else
                return -1;

            _values.AddLast(newEntry);
        }

        /* Closing tag 1 - List of Properties */
        if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
            return -1;
        len++;

        valuesRefs = _values;

        return len;
    }

    public static int DecodeDeleteObject(byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId)
    {
        objectId = new BacnetObjectId();
        ASN1.decode_tag_number_and_value(buffer, offset, out var tagNumber, out _);

        if (tagNumber != 12)
            return -1;

        var len = 1;
        len += ASN1.decode_object_id(buffer, offset + len, out objectId.type, out objectId.instance);

        if (len == apduLen) //check if packet was correct!
            return len;

        return -1;
    }

    public static void EncodeCreateObjectAcknowledge(EncodeBuffer buffer, BacnetObjectId objectId)
    {
        ASN1.encode_application_object_id(buffer, objectId.type, objectId.instance);
    }

    public static int DecodeWritePropertyMultiple(BacnetAddress address, byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out ICollection<BacnetPropertyValue> valuesRefs)
    {
        var len = 0;
        objectId = new BacnetObjectId();
        valuesRefs = null;

        /* Context tag 0 - Object ID */
        len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);
        if (tagNumber == 0 && apduLen > len)
        {
            apduLen -= len;
            if (apduLen >= 4)
            {
                len += ASN1.decode_object_id(buffer, offset + len, out objectId.type, out objectId.instance);
            }
            else
                return -1;
        }
        else
            return -1;

        /* Tag 1: sequence of WriteAccessSpecification */
        if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
            return -1;
        len++;

        var _values = new LinkedList<BacnetPropertyValue>();
        while (apduLen - len > 1)
        {
            var newEntry = new BacnetPropertyValue();

            /* tag 0 - Property Identifier */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            uint propertyId;
            if (tagNumber == 0)
                len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out propertyId);
            else
                return -1;

            /* tag 1 - Property Array Index - optional */
            var ulVal = ASN1.BACNET_ARRAY_ALL;
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            if (tagNumber == 1)
            {
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out ulVal);
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            }
            newEntry.property = new BacnetPropertyReference(propertyId, ulVal);

            /* tag 2 - Property Value */
            if (tagNumber == 2 && ASN1.decode_is_opening_tag(buffer, offset + len - 1))
            {
                var values = new List<BacnetValue>();
                while (!ASN1.decode_is_closing_tag(buffer, offset + len))
                {
                    var l = ASN1.bacapp_decode_application_data(address, buffer, offset + len, apduLen + offset, objectId.type, (BacnetPropertyIds)propertyId, out var value);
                    if (l <= 0) return -1;
                    len += l;
                    values.Add(value);
                }
                len++;
                newEntry.value = values;
            }
            else
                return -1;

            /* tag 3 - Priority - optional */
            ulVal = ASN1.BACNET_NO_PRIORITY;
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
            if (tagNumber == 3)
                len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out ulVal);
            else
                len--;
            newEntry.priority = (byte)ulVal;

            _values.AddLast(newEntry);
        }

        /* Closing tag 1 - List of Properties */
        if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
            return -1;
        len++;

        valuesRefs = _values;

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

    public static void EncodeError(EncodeBuffer buffer, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, byte tagNumber = 0)
    {
        ASN1.encode_opening_tag(buffer, tagNumber);
        ASN1.encode_application_enumerated(buffer, (uint)errorClass);
        ASN1.encode_application_enumerated(buffer, (uint)errorCode);
        ASN1.encode_closing_tag(buffer, tagNumber);
    }

    public static int DecodeError(byte[] buffer, int offset, out BacnetErrorClasses errorClass, out BacnetErrorCodes errorCode)
    {
        var orgOffset = offset;
        errorClass = default;
        errorCode = default;

        if (ASN1.decode_is_opening_tag(buffer, offset))
            offset += ASN1.decode_tag_number(buffer, offset, out _);

        if (!ASN1.decode_is_application_tag(buffer, offset, BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED))
            return -1;

        offset += ASN1.decode_tag_number_and_value(buffer, offset, out _, out var lenValueType);
        offset += ASN1.decode_enumerated(buffer, offset, lenValueType, out errorClass);

        if (!ASN1.decode_is_application_tag(buffer, offset, BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED))
            return -1;

        offset += ASN1.decode_tag_number_and_value(buffer, offset, out _, out lenValueType);
        offset += ASN1.decode_enumerated(buffer, offset, lenValueType, out errorCode);

        if (buffer.Length > offset && ASN1.decode_is_closing_tag(buffer, offset))
            offset += ASN1.decode_tag_number(buffer, offset, out _);

        return offset - orgOffset;
    }

    public static void EncodeLogRecord(EncodeBuffer buffer, BacnetLogRecord record)
    {
        /* Tag 0: timestamp */
        ASN1.encode_opening_tag(buffer, 0);
        ASN1.encode_application_date(buffer, record.timestamp);
        ASN1.encode_application_time(buffer, record.timestamp);
        ASN1.encode_closing_tag(buffer, 0);

        /* Tag 1: logDatum */
        if (record.type != BacnetTrendLogValueType.TL_TYPE_NULL)
        {
            if (record.type == BacnetTrendLogValueType.TL_TYPE_ERROR)
            {
                ASN1.encode_opening_tag(buffer, 1);
                var err = record.GetValue<BacnetError>();
                EncodeError(buffer, err.error_class, err.error_code, 8);
                ASN1.encode_closing_tag(buffer, 1);
                return;
            }

            ASN1.encode_opening_tag(buffer, 1);
            var tmp1 = new EncodeBuffer();
            switch (record.type)
            {
                case BacnetTrendLogValueType.TL_TYPE_ANY:
                    throw new NotImplementedException();
                case BacnetTrendLogValueType.TL_TYPE_BITS:
                    ASN1.encode_bitstring(tmp1, record.GetValue<BacnetBitString>());
                    break;

                case BacnetTrendLogValueType.TL_TYPE_BOOL:
                    tmp1.Add(record.GetValue<bool>() ? (byte)1 : (byte)0);
                    break;

                case BacnetTrendLogValueType.TL_TYPE_DELTA:
                    ASN1.encode_bacnet_real(tmp1, record.GetValue<float>());
                    break;

                case BacnetTrendLogValueType.TL_TYPE_ENUM:
                    ASN1.encode_application_enumerated(tmp1, record.GetValue<uint>());
                    break;

                case BacnetTrendLogValueType.TL_TYPE_REAL:
                    ASN1.encode_bacnet_real(tmp1, record.GetValue<float>());
                    break;

                case BacnetTrendLogValueType.TL_TYPE_SIGN:
                    ASN1.encode_bacnet_signed(tmp1, record.GetValue<int>());
                    break;

                case BacnetTrendLogValueType.TL_TYPE_STATUS:
                    ASN1.encode_bitstring(tmp1, record.GetValue<BacnetBitString>());
                    break;

                case BacnetTrendLogValueType.TL_TYPE_UNSIGN:
                    ASN1.encode_bacnet_unsigned(tmp1, record.GetValue<uint>());
                    break;
            }
            ASN1.encode_tag(buffer, (byte)record.type, true, (uint)tmp1.offset);
            buffer.Add(tmp1.buffer, tmp1.offset);
            ASN1.encode_closing_tag(buffer, 1);
        }

        /* Tag 2: status */
        if (record.statusFlags.bits_used > 0)
        {
            ASN1.encode_opening_tag(buffer, 2);
            ASN1.encode_application_bitstring(buffer, record.statusFlags);
            ASN1.encode_closing_tag(buffer, 2);
        }
    }

    public static int DecodeLogRecord(byte[] buffer, int offset, int length, int nCurves, out BacnetLogRecord[] records)
    {
        var len = 0;
        records = new BacnetLogRecord[nCurves];

        len += ASN1.decode_tag_number(buffer, offset + len, out var tagNumber);
        if (tagNumber != 0) return -1;

        // Date and Time in Tag 0
        len += ASN1.decode_application_date(buffer, offset + len, out var date);
        len += ASN1.decode_application_time(buffer, offset + len, out var time);

        var dt = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

        if (!ASN1.decode_is_closing_tag(buffer, offset + len)) return -1;
        len++;

        // Value or error in Tag 1
        len += ASN1.decode_tag_number(buffer, offset + len, out tagNumber);
        if (tagNumber != 1) return -1;

        // Not test for TrendLogMultiple
        // Seems to be encoded like this somewhere in an Ashrae document
        for (var curveNumber = 0; curveNumber < nCurves; curveNumber++)
        {
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var contextTagType, out var lenValue);
            records[curveNumber] = new BacnetLogRecord
            {
                timestamp = dt,
                type = (BacnetTrendLogValueType)contextTagType
            };

            switch ((BacnetTrendLogValueType)contextTagType)
            {
                case BacnetTrendLogValueType.TL_TYPE_STATUS:
                    len += ASN1.decode_bitstring(buffer, offset + len, lenValue, out var sval);
                    records[curveNumber].Value = sval;
                    break;

                case BacnetTrendLogValueType.TL_TYPE_BOOL:
                    records[curveNumber].Value = buffer[offset + len] > 0;
                    len++;
                    break;

                case BacnetTrendLogValueType.TL_TYPE_REAL:
                    len += ASN1.decode_real(buffer, offset + len, out var rval);
                    records[curveNumber].Value = rval;
                    break;

                case BacnetTrendLogValueType.TL_TYPE_ENUM:
                    len += ASN1.decode_enumerated(buffer, offset + len, lenValue, out var eval);
                    records[curveNumber].Value = eval;
                    break;

                case BacnetTrendLogValueType.TL_TYPE_SIGN:
                    len += ASN1.decode_signed(buffer, offset + len, lenValue, out var ival);
                    records[curveNumber].Value = ival;
                    break;

                case BacnetTrendLogValueType.TL_TYPE_UNSIGN:
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out var uinval);
                    records[curveNumber].Value = uinval;
                    break;

                case BacnetTrendLogValueType.TL_TYPE_ERROR:
                    len += DecodeError(buffer, offset + len, out var errclass, out var errcode);
                    records[curveNumber].Value = new BacnetError(errclass, errcode);
                    break;

                case BacnetTrendLogValueType.TL_TYPE_NULL:
                    len++;
                    records[curveNumber].Value = null;
                    break;
                // Time change (Automatic or Synch time) Delta in seconds
                case BacnetTrendLogValueType.TL_TYPE_DELTA:
                    len += ASN1.decode_real(buffer, offset + len, out var dval);
                    records[curveNumber].Value = dval;
                    break;
                // No way to handle these data types, sure it's the end of this download !
                case BacnetTrendLogValueType.TL_TYPE_ANY:
                    throw new NotImplementedException();
                case BacnetTrendLogValueType.TL_TYPE_BITS:
                    len += ASN1.decode_bitstring(buffer, offset + len, lenValue, out var bval);
                    records[curveNumber].Value = bval;
                    break;

                default:
                    return 0;
            }
        }

        if (!ASN1.decode_is_closing_tag(buffer, offset + len))
            return -1;

        len++;

        if (len >= length)
            return len;

        var l = ASN1.decode_tag_number(buffer, offset + len, out tagNumber);

        // Optional Tag 2
        if (tagNumber != 2)
            return len;

        len += l;
        len += ASN1.decode_bitstring(buffer, offset + len, 2, out var statusFlags);

        //set status to all returns
        for (var curveNumber = 0; curveNumber < nCurves; curveNumber++)
            records[curveNumber].statusFlags = statusFlags;

        return len;
    }
}
