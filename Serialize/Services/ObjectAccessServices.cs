using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace System.IO.BACnet.Serialize
{
    public static class ObjectAccessServices
    {
        public static void EncodeAddOrRemoveListElement(EncodeBuffer buffer, BacnetObjectId objectId, uint propertyId, uint arrayIndex, IList<BacnetValue> valueList)
        {
            ASN1.encode_context_object_id(buffer, 0, objectId.Type, objectId.Instance);
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

        public static void EncodeCreateObject(EncodeBuffer buffer, BacnetObjectTypes objectType, ICollection<BacnetPropertyValue> valueList)
        {
            ASN1.encode_opening_tag(buffer, 0);
            ASN1.encode_context_unsigned(buffer, 0, (uint)objectType);
            ASN1.encode_closing_tag(buffer, 0);

            EncodeCreateObjectInternal(buffer, valueList);
        }

        // by Christopher Günter
        public static void EncodeCreateObject(EncodeBuffer buffer, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList)
        {
            /* Tag 1: sequence of WriteAccessSpecification */
            ASN1.encode_opening_tag(buffer, 0);
            ASN1.encode_context_object_id(buffer, 1, objectId.Type, objectId.Instance);
            ASN1.encode_closing_tag(buffer, 0);

            EncodeCreateObjectInternal(buffer, valueList);
        }

        private static void EncodeCreateObjectInternal(EncodeBuffer buffer, ICollection<BacnetPropertyValue> valueList)
        {
            if (valueList == null || valueList.Count == 0)
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

        // By C. Gunter
        // quite the same as DecodeWritePropertyMultiple
        public static int DecodeCreateObject(BacnetAddress address, byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out ICollection<BacnetPropertyValue> valuesRefs)
        {
            var len = 0;

            objectId = default(BacnetObjectId);
            valuesRefs = null;

            //object id
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);

            if (tagNumber == 0 && apduLen > len)
            {
                apduLen -= len;
                if (apduLen >= 4)
                {
                    len += ASN1.decode_context_object_id(buffer, offset + len, 1, out var typenr, out var instance);
                    objectId = new BacnetObjectId((BacnetObjectTypes) typenr, instance);
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

            var linkedPropertyValues = new LinkedList<BacnetPropertyValue>();
            while (apduLen - len > 1)
            {
                var newEntry = new BacnetPropertyValue();

                /* tag 0 - Property Identifier */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                uint propertyId;
                if (tagNumber == 0)
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out propertyId);
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
                        var l = ASN1.bacapp_decode_application_data(
                            address, buffer, offset + len, apduLen + offset, objectId.Type,
                            (BacnetPropertyIds) propertyId, out var value);

                        if (l <= 0) return -1;
                        len += l;
                        values.Add(value);
                    }
                    len++;
                    newEntry.value = values;
                }
                else
                    return -1;

                linkedPropertyValues.AddLast(newEntry);
            }

            /* Closing tag 1 - List of Properties */
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            valuesRefs = linkedPropertyValues;

            return len;
        }

        public static void EncodeCreateObjectAcknowledge(EncodeBuffer buffer, BacnetObjectId objectId)
        {
            ASN1.encode_application_object_id(buffer, objectId.Type, objectId.Instance);
        }

        public static void EncodeDeleteObject(EncodeBuffer buffer, BacnetObjectId objectId)
        {
            ASN1.encode_application_object_id(buffer, objectId.Type, objectId.Instance);
        }

        public static int DecodeDeleteObject(byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId)
        {
            objectId = default;

            ASN1.decode_tag_number_and_value(buffer, offset, out var tagNumber, out _);

            if (tagNumber != 12)
                return -1;

            var len = 1;
            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
            objectId = new BacnetObjectId(type, instance);

            if (len == apduLen) //check if packet was correct!
                return len;

            return -1;
        }

        public static void EncodeReadProperty(EncodeBuffer buffer, BacnetObjectId objectId, BacnetPropertyIds propertyId, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            if ((int)objectId.Type <= ASN1.BACNET_MAX_OBJECT)
            {
                /* check bounds so that we could create malformed
                   messages for testing */
                ASN1.encode_context_object_id(buffer, 0, objectId.Type, objectId.Instance);
            }
            if (propertyId <= BacnetPropertyIds.MAX_BACNET_PROPERTY_ID)
            {
                /* check bounds so that we could create malformed
                   messages for testing */
                ASN1.encode_context_enumerated(buffer, 1, (uint)propertyId);
            }
            /* optional array index */
            if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
            }
        }

        public static int DecodeReadProperty(byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out BacnetPropertyReference property)
        {
            var len = 0;

            objectId = default(BacnetObjectId);
            property = new BacnetPropertyReference();

            // must have at least 2 tags , otherwise return reject code: Missing required parameter
            if (apduLen < 7)
                return -1;

            /* Tag 0: Object ID */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -2;

            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
            objectId = new BacnetObjectId(type, instance);

            /* Tag 1: Property ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != 1)
                return -2;

            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out property.propertyIdentifier);

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

        public static void EncodeReadPropertyAcknowledge(EncodeBuffer buffer, BacnetObjectId objectId,
            BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            /* service ack follows */
            ASN1.encode_context_object_id(buffer, 0, objectId.Type, objectId.Instance);
            ASN1.encode_context_unsigned(buffer, 1, (uint)propertyId);
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
            objectId = default(BacnetObjectId);
            property = new BacnetPropertyReference();
            valueList = new List<BacnetValue>();

            /* FIXME: check apduLen against the len during decode   */
            /* Tag 0: Object ID */
            if (!ASN1.decode_is_context_tag(buffer, offset, 0))
                return -1;
            var len = 1;
            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
            objectId = new BacnetObjectId(type, instance);
            /* Tag 1: Property ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != 1)
                return -1;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out property.propertyIdentifier);
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
                    tagLen = ASN1.bacapp_decode_application_data(address, buffer, offset + len, apduLen + offset, objectId.Type, (BacnetPropertyIds)property.propertyIdentifier, out var value);
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

        public static void EncodeReadRange(EncodeBuffer buffer, BacnetObjectId objectId, BacnetPropertyIds propertyId,
            BacnetReadRangeRequestTypes requestType, uint position, DateTime time, int count, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            ASN1.encode_context_object_id(buffer, 0, objectId.Type, objectId.Instance);
            ASN1.encode_context_unsigned(buffer, 1, (uint)propertyId);

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

            objectId = default(BacnetObjectId);
            property = new BacnetPropertyReference();
            requestType = BacnetReadRangeRequestTypes.RR_READ_ALL;
            position = 0;
            time = new DateTime(1, 1, 1);
            count = -1;

            /* Tag 0: Object ID          */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
            objectId = new BacnetObjectId(type, instance);
            /* Tag 1: Property ID */
            len +=
                ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != 1)
                return -1;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out property.propertyIdentifier);

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

        public static void EncodeReadRangeAcknowledge(EncodeBuffer buffer, BacnetObjectId objectId,
            BacnetPropertyIds propertyId, BacnetBitString resultFlags, uint itemCount, byte[] applicationData,
            BacnetReadRangeRequestTypes requestType, uint firstSequence, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            /* service ack follows */
            ASN1.encode_context_object_id(buffer, 0, objectId.Type, objectId.Instance);
            ASN1.encode_context_unsigned(buffer, 1, (uint)propertyId);
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
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out uint _);

            /* Tag 2: Optional Array Index or Tag 3:  BACnet Result Flags */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            if (tagNumber == 2 && len < apduLen)
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out uint _);
            else
                /* Tag 3:  BACnet Result Flags */
                len += ASN1.decode_bitstring(buffer, offset + len, 2, out _);

            /* Tag 4 Item Count */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out uint itemCount);

            if (!ASN1.decode_is_opening_tag(buffer, offset + len))
                return 0;
            len += 1;

            rangeBuffer = new byte[buffer.Length - offset - len - 1];

            Array.Copy(buffer, offset + len, rangeBuffer, 0, rangeBuffer.Length);

            return itemCount;
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
                    ASN1.encode_opening_tag(buffer, 8);
                    var err = record.GetValue<BacnetError>();
                    ASN1.EncodeError(buffer, err.error_class, err.error_code);
                    ASN1.encode_closing_tag(buffer, 8);
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
            var recordStatusFlags = BacnetBitString.ConvertFromInt((uint)record.statusFlags, 4);
            if (recordStatusFlags.BitsUsed > 0)
            {
                ASN1.encode_tag(buffer, 2, true, 2);
                ASN1.encode_bitstring(buffer, recordStatusFlags);
            }
        }

        public static int DecodeLogRecord(byte[] buffer, int offset, int length, int nCurves, out BacnetLogRecord[] records)
        {
            var len = 0;
            records = new BacnetLogRecord[nCurves];

            for (var curveNumber = 0; curveNumber < nCurves; curveNumber++)
            {
                len += ASN1.decode_tag_number(buffer, offset + len, out var tagNumber);
                if (tagNumber != 0) return -1;

                // Date and Time in Tag 0
                len += ASN1.decode_application_date(buffer, offset + len, out var date);
                len += ASN1.decode_application_time(buffer, offset + len, out var time);

                var dt = new DateTime(
                    date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

                if (!ASN1.decode_is_closing_tag(buffer, offset + len)) return -1;
                len++;

                // Value or error in Tag 1
                len += ASN1.decode_tag_number(buffer, offset + len, out tagNumber);
                if (tagNumber != 1) return -1;

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
                        len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint eval);
                        records[curveNumber].Value = eval;
                        break;

                    case BacnetTrendLogValueType.TL_TYPE_SIGN:
                        len += ASN1.decode_signed(buffer, offset + len, lenValue, out var ival);
                        records[curveNumber].Value = ival;
                        break;

                    case BacnetTrendLogValueType.TL_TYPE_UNSIGN:
                        len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out uint uinval);
                        records[curveNumber].Value = uinval;
                        break;

                    case BacnetTrendLogValueType.TL_TYPE_ERROR:
                        len += ASN1.DecodeError(buffer, offset + len, length, out var errclass, out var errcode);
                        records[curveNumber].Value = new BacnetError(errclass, errcode);
                        len++; // Closing Tag 8
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


                if (!ASN1.decode_is_closing_tag(buffer, offset + len))
                    return -1;

                len++;

                if (len >= length)
                    return len;

                ASN1.decode_tag_number(buffer, offset + len, out tagNumber);

                // Optional Tag 2
                if (tagNumber != 2)
                    return len;

                len++;
                len += ASN1.decode_bitstring(buffer, offset + len, 2, out var statusFlagsBits);

                //set status to all returns
                var statusFlags = (BacnetStatusFlags)statusFlagsBits.ConvertToInt();
                records[curveNumber].statusFlags = statusFlags;
            }

            return len;
        }

        public static void EncodeWriteProperty(EncodeBuffer buffer, BacnetObjectId objectId,
            BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, uint arrayIndex = ASN1.BACNET_ARRAY_ALL,
            uint priority=0)
        {
            ASN1.encode_context_object_id(buffer, 0, objectId.Type, objectId.Instance);
            ASN1.encode_context_unsigned(buffer, 1, (uint)propertyId);

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

        public static int DecodeWriteProperty(BacnetAddress address, byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out BacnetPropertyValue value)
        {
            var len = 0;

            objectId = default(BacnetObjectId);
            value = new BacnetPropertyValue();

            /* Tag 0: Object ID          */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
            objectId = new BacnetObjectId(type, instance);
            /* Tag 1: Property ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != 1)
                return -1;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out value.property.propertyIdentifier);
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
                var l = ASN1.bacapp_decode_application_data(
                    address, buffer, offset + len, apduLen + offset, objectId.Type,
                    (BacnetPropertyIds) value.property.propertyIdentifier, out var bValue);

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
                    len = ASN1.decode_unsigned(buffer, offset + len, lenValueType, out uint unsignedValue);
                    if (unsignedValue >= ASN1.BACNET_MIN_PRIORITY && unsignedValue <= ASN1.BACNET_MAX_PRIORITY)
                        value.priority = (byte)unsignedValue;
                    else
                        return -1;
                }
            }

            return len;
        }

        // TODO check if this still works and / or makes any sense.
        public static void EncodeWritePropertyMultiple(EncodeBuffer buffer, BacnetObjectId objectId,
            ICollection<BacnetPropertyValue> valueList)
            => EncodeWritePropertyMultiple(
                buffer,
                new BacnetWriteAccessSpecification(
                    objectId,
                    valueList.Select(
                        pv => new BacnetWriteAccessSpecification.Property(
                            pv.property.GetPropertyId(), pv.value.Single(), // TODO CHECK is someone really passing multiple values here? I don't see that in the spec
                            pv.property.propertyArrayIndex == ASN1.BACNET_ARRAY_ALL
                                ? null
                                : (uint?) pv.property.propertyArrayIndex,
                            pv.priority == 0 ? null : (uint?) pv.priority))));

        public static void EncodeWritePropertyMultiple(EncodeBuffer buffer,
            params BacnetWriteAccessSpecification[] writeAccessSpec)
        {
            foreach (var objectWithProps in writeAccessSpec)
            {

                ASN1.encode_context_object_id(buffer, 0, objectWithProps.ObjectId);
                /* Tag 1: sequence of WriteAccessSpecification */
                ASN1.encode_opening_tag(buffer, 1);

                foreach (var prop in objectWithProps.Properties)
                {
                    /* Tag 0: Property */
                    ASN1.encode_context_unsigned(buffer, 0, (uint) prop.Id);

                    /* Tag 1: array index */
                    if (prop.ArrayIndex != null)
                        ASN1.encode_context_unsigned(buffer, 1, prop.ArrayIndex.Value);

                    /* Tag 2: Value */
                    ASN1.encode_opening_tag(buffer, 2);
                    /*
                     * TODO CHECK: removed the loop, because I don't think we can have a list of values here.
                     * If we can, it should still only be a single BacnetValue which has its own type - e.g. ListOfBacnetValues,
                     * and the encode-call should handle it.
                     */
                    ASN1.bacapp_encode_application_data(buffer, prop.Value);
                    ASN1.encode_closing_tag(buffer, 2);

                    /* Tag 3: Priority */
                    if (prop.Priority != null)
                        ASN1.encode_context_unsigned(buffer, 3, prop.Priority.Value);
                }

                ASN1.encode_closing_tag(buffer, 1);
            }
        }

        public static int DecodeWritePropertyMultiple(BacnetAddress address, byte[] buffer, int offset, int apduLen, out BacnetObjectId objectId, out ICollection<BacnetPropertyValue> valuesRefs)
        {
            var len = 0;
            objectId = default(BacnetObjectId);
            valuesRefs = null;

            /* Context tag 0 - Object ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue);
            if (tagNumber == 0 && apduLen > len)
            {
                apduLen -= len;
                if (apduLen >= 4)
                {
                    len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes type, out var instance);
                    objectId = new BacnetObjectId(type, instance);
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

            var linkedList = new LinkedList<BacnetPropertyValue>();
            while (apduLen - len > 1)
            {
                var newEntry = new BacnetPropertyValue();

                /* tag 0 - Property Identifier */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue);
                uint propertyId;
                if (tagNumber == 0)
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValue, out propertyId);
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
                    var list = new List<BacnetValue>();
                    while (!ASN1.decode_is_closing_tag(buffer, offset + len))
                    {
                        var l = ASN1.bacapp_decode_application_data(
                            address, buffer, offset + len, apduLen + offset, objectId.Type,
                            (BacnetPropertyIds) propertyId, out var value);

                        if (l <= 0) return -1;
                        len += l;
                        list.Add(value);
                    }
                    len++;
                    newEntry.value = list;
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

                linkedList.AddLast(newEntry);
            }

            /* Closing tag 1 - List of Properties */
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            valuesRefs = linkedList;

            return len;
        }

        public static void EncodeWriteObjectMultiple(EncodeBuffer buffer, ICollection<BacnetReadAccessResult> valueList)
        {
            foreach (var value in valueList)
                EncodeWritePropertyMultiple(buffer, value.objectIdentifier, value.values);
        }
    }
}
