namespace System.IO.BACnet.Serialize;

public class ASN1
{
    public const int BACNET_MAX_OBJECT = 0x3FF;
    public const int BACNET_INSTANCE_BITS = 22;
    public const int BACNET_MAX_INSTANCE = 0x3FFFFF;
    public const int MAX_BITSTRING_BYTES = 15;
    public const uint BACNET_ARRAY_ALL = 0xFFFFFFFFU;
    public const uint BACNET_NO_PRIORITY = 0;
    public const uint BACNET_MIN_PRIORITY = 1;
    public const uint BACNET_MAX_PRIORITY = 16;

    /// <summary>
    /// You can provide a function to resolve a given tag within a property to a different tag (e.g. for custom properties)
    /// The address of the remote device is provided so that you can support devices from multiple vendors
    /// (the same custom property# has different meaning for each vendor)
    /// </summary>
    public static Func<BacnetAddress, BacnetPropertyIds, byte, BacnetApplicationTags> CustomTagResolver;

    public interface IEncode
    {
        void Encode(EncodeBuffer buffer);
    }

    public interface IDecode
    {
        int Decode(byte[] buffer, int offset, uint count);
    }

    public static void encode_bacnet_object_id(EncodeBuffer buffer, BacnetObjectTypes objectType, uint instance)
    {
        var type = (uint)objectType;
        var value = ((type & BACNET_MAX_OBJECT) << BACNET_INSTANCE_BITS) | (instance & BACNET_MAX_INSTANCE);
        encode_unsigned32(buffer, value);
    }

    public static void encode_tag(EncodeBuffer buffer, byte tagNumber, bool contextSpecific, uint lenValueType)
    {
        var len = 1;
        var tmp = new byte[3];

        tmp[0] = 0;
        if (contextSpecific) tmp[0] |= 0x8;

        /* additional tag byte after this byte */
        /* for extended tag byte */
        if (tagNumber <= 14)
        {
            tmp[0] |= (byte)(tagNumber << 4);
        }
        else
        {
            tmp[0] |= 0xF0;
            tmp[1] = tagNumber;
            len++;
        }

        /* NOTE: additional len byte(s) after extended tag byte */
        /* if larger than 4 */
        if (lenValueType <= 4)
        {
            tmp[0] |= (byte)lenValueType;
            buffer.Add(tmp, len);
        }
        else
        {
            tmp[0] |= 5;
            if (lenValueType <= 253)
            {
                tmp[len++] = (byte)lenValueType;
                buffer.Add(tmp, len);
            }
            else if (lenValueType <= 65535)
            {
                tmp[len++] = 254;
                buffer.Add(tmp, len);
                encode_unsigned16(buffer, (ushort)lenValueType);
            }
            else
            {
                tmp[len++] = 255;
                buffer.Add(tmp, len);
                encode_unsigned32(buffer, lenValueType);
            }
        }
    }

    public static void encode_bacnet_enumerated(EncodeBuffer buffer, uint value)
    {
        encode_bacnet_unsigned(buffer, value);
    }

    public static void encode_application_object_id(EncodeBuffer buffer, BacnetObjectTypes objectType, uint instance)
    {
        var tmp1 = new EncodeBuffer();
        encode_bacnet_object_id(tmp1, objectType, instance);
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, false, (uint)tmp1.offset);
        buffer.Add(tmp1.buffer, tmp1.offset);
    }

    public static void encode_application_unsigned(EncodeBuffer buffer, uint value)
    {
        var tmp1 = new EncodeBuffer();
        encode_bacnet_unsigned(tmp1, value);
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, false, (uint)tmp1.offset);
        buffer.Add(tmp1.buffer, tmp1.offset);
    }

    public static void encode_application_enumerated(EncodeBuffer buffer, uint value)
    {
        var tmp1 = new EncodeBuffer();
        encode_bacnet_enumerated(tmp1, value);
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, false, (uint)tmp1.offset);
        buffer.Add(tmp1.buffer, tmp1.offset);
    }

    public static void encode_application_signed(EncodeBuffer buffer, int value)
    {
        var tmp1 = new EncodeBuffer();
        encode_bacnet_signed(tmp1, value);
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT, false, (uint)tmp1.offset);
        buffer.Add(tmp1.buffer, tmp1.offset);
    }

    public static void encode_bacnet_unsigned(EncodeBuffer buffer, uint value)
    {
        if (value < 0x100)
        {
            buffer.Add((byte)value);
        }
        else if (value < 0x10000)
        {
            encode_unsigned16(buffer, (ushort)value);
        }
        else if (value < 0x1000000)
        {
            encode_unsigned24(buffer, value);
        }
        else
        {
            encode_unsigned32(buffer, value);
        }
    }

    public static void encode_context_boolean(EncodeBuffer buffer, byte tagNumber, bool boolean_value)
    {
        encode_tag(buffer, tagNumber, true, 1);
        buffer.Add(boolean_value ? (byte)1 : (byte)0);
    }

    public static void encode_context_real(EncodeBuffer buffer, byte tagNumber, float value)
    {
        encode_tag(buffer, tagNumber, true, 4);
        encode_bacnet_real(buffer, value);
    }

    public static void encode_context_unsigned(EncodeBuffer buffer, byte tagNumber, uint value)
    {
        int len;

        /* length of unsigned is variable, as per 20.2.4 */
        if (value < 0x100)
            len = 1;
        else if (value < 0x10000)
            len = 2;
        else if (value < 0x1000000)
            len = 3;
        else
            len = 4;

        encode_tag(buffer, tagNumber, true, (uint)len);
        encode_bacnet_unsigned(buffer, value);
    }

    public static void encode_context_character_string(EncodeBuffer buffer, byte tagNumber, string value)
    {
        var tmp = new EncodeBuffer();
        encode_bacnet_character_string(tmp, value);

        encode_tag(buffer, tagNumber, true, (uint)tmp.offset);
        buffer.Add(tmp.buffer, tmp.offset);
    }

    public static void encode_context_enumerated(EncodeBuffer buffer, byte tagNumber, uint value)
    {
        int len; /* return value */

        if (value < 0x100)
            len = 1;
        else if (value < 0x10000)
            len = 2;
        else if (value < 0x1000000)
            len = 3;
        else
            len = 4;

        encode_tag(buffer, tagNumber, true, (uint)len);
        encode_bacnet_enumerated(buffer, value);
    }

    public static void encode_bacnet_signed(EncodeBuffer buffer, long value)
    {
        /* don't encode the leading X'FF' or X'00' of the two's compliment.
           That is, the first octet of any multi-octet encoded value shall
           not be X'00' if the most significant bit (bit 7) of the second
           octet is 0, and the first octet shall not be X'FF' if the most
           significant bit of the second octet is 1. */
        if (value >= -128 && value < 128)
            buffer.Add((byte)(sbyte)value);
        else if (value >= -32768 && value < 32768)
            encode_signed16(buffer, (short)value);
        else if (value > -8388607 && value < 8388608)
            encode_signed24(buffer, (int)value);
        else if (value > -2147483648 && value < 2147483648)
            encode_signed32(buffer, (int)value);
        else
            encode_signed64(buffer, value);
    }

    public static void encode_octetString(EncodeBuffer buffer, byte[] octetString, int octetOffset, int octetCount)
    {
        if (octetString != null)
        {
            for (var i = octetOffset; i < octetOffset + octetCount; i++)
                buffer.Add(octetString[i]);
        }
    }

    public static void encode_application_octet_string(EncodeBuffer buffer, byte[] octetString, int octetOffset, int octetCount)
    {
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING, false, (uint)octetCount);
        encode_octetString(buffer, octetString, octetOffset, octetCount);
    }

    public static void encode_application_boolean(EncodeBuffer buffer, bool booleanValue)
    {
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN, false,
            booleanValue ? 1 : (uint)0);
    }

    public static void encode_bacnet_real(EncodeBuffer buffer, float value)
    {
        var data = BitConverter.GetBytes(value);
        buffer.Add(data[3]);
        buffer.Add(data[2]);
        buffer.Add(data[1]);
        buffer.Add(data[0]);
    }

    public static void encode_bacnet_double(EncodeBuffer buffer, double value)
    {
        var data = BitConverter.GetBytes(value);
        buffer.Add(data[7]);
        buffer.Add(data[6]);
        buffer.Add(data[5]);
        buffer.Add(data[4]);
        buffer.Add(data[3]);
        buffer.Add(data[2]);
        buffer.Add(data[1]);
        buffer.Add(data[0]);
    }

    public static void encode_application_real(EncodeBuffer buffer, float value)
    {
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, false, 4);
        encode_bacnet_real(buffer, value);
    }

    public static void encode_application_double(EncodeBuffer buffer, double value)
    {
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE, false, 8);
        encode_bacnet_double(buffer, value);
    }

    private static byte bitstring_bytesUsed(BacnetBitString bitString)
    {
        byte len = 0; /* return value */

        if (bitString.bits_used <= 0)
            return len;

        var lastBit = (byte)(bitString.bits_used - 1);
        var usedBytes = (byte)(lastBit / 8);
        /* add one for the first byte */
        usedBytes++;
        len = usedBytes;

        return len;
    }

    private static byte byte_reverse_bits(byte inByte)
    {
        byte outByte = 0;

        if ((inByte & 1) > 0)
        {
            outByte |= 0x80;
        }
        if ((inByte & 2) > 0)
        {
            outByte |= 0x40;
        }
        if ((inByte & 4) > 0)
        {
            outByte |= 0x20;
        }
        if ((inByte & 8) > 0)
        {
            outByte |= 0x10;
        }
        if ((inByte & 16) > 0)
        {
            outByte |= 0x8;
        }
        if ((inByte & 32) > 0)
        {
            outByte |= 0x4;
        }
        if ((inByte & 64) > 0)
        {
            outByte |= 0x2;
        }
        if ((inByte & 128) > 0)
        {
            outByte |= 1;
        }

        return outByte;
    }

    private static byte bitstring_octet(BacnetBitString bitString, byte octetIndex)
    {
        byte octet = 0;

        if (bitString.value == null)
            return octet;

        if (octetIndex < MAX_BITSTRING_BYTES)
            octet = bitString.value[octetIndex];

        return octet;
    }

    public static void encode_bitstring(EncodeBuffer buffer, BacnetBitString bitString)
    {
        /* if the bit string is empty, then the first octet shall be zero */
        if (bitString.bits_used == 0)
        {
            buffer.Add(0);
        }
        else
        {
            var usedBytes = bitstring_bytesUsed(bitString);
            var remainingUsedBits = (byte)(bitString.bits_used - (usedBytes - 1) * 8);
            /* number of unused bits in the subsequent final octet */
            buffer.Add((byte)(8 - remainingUsedBits));
            for (byte i = 0; i < usedBytes; i++)
                buffer.Add(byte_reverse_bits(bitstring_octet(bitString, i)));
        }
    }

    public static void encode_application_bitstring(EncodeBuffer buffer, BacnetBitString bitString)
    {
        uint bitStringEncodedLength = 1; /* 1 for the bits remaining octet */

        /* bit string may use more than 1 octet for the tag, so find out how many */
        bitStringEncodedLength += bitstring_bytesUsed(bitString);
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING, false, bitStringEncodedLength);
        encode_bitstring(buffer, bitString);
    }

    public static void EncodeApplicationDestination(EncodeBuffer buffer, BacnetObjectTypes objectType, uint instance)
    {
        var tempBuffer = new EncodeBuffer();
        encode_bacnet_object_id(tempBuffer, objectType, instance);
        buffer.Add(0x0C);
        buffer.Add(tempBuffer.buffer, tempBuffer.offset);
    }

    public static void EncodeApplicationDestination(EncodeBuffer buffer, BacnetAddress address)
    {
        address.Encode(buffer);
    }

    public static void bacapp_encode_application_data(EncodeBuffer buffer, BacnetValue value)
    {
        if (value.Value == null)
        {
            // Modif FC
            buffer.Add((byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL);
            return;
        }

        switch (value.Tag)
        {
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                /* don't encode anything */
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                encode_application_boolean(buffer, (bool)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                encode_application_unsigned(buffer, (uint)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                encode_application_signed(buffer, (int)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                encode_application_real(buffer, (float)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                encode_application_double(buffer, (double)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                encode_application_octet_string(buffer, (byte[])value.Value, 0, ((byte[])value.Value).Length);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                encode_application_character_string(buffer, (string)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                encode_application_bitstring(buffer, (BacnetBitString)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                encode_application_enumerated(buffer, (uint)(dynamic)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                encode_application_date(buffer, (DateTime)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                encode_application_time(buffer, (DateTime)value.Value);
                break;
            // Added for EventTimeStamp
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP:
                bacapp_encode_timestamp(buffer, (BacnetGenericTime)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME:
                bacapp_encode_datetime(buffer, (DateTime)value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                encode_application_object_id(buffer, ((BacnetObjectId)value.Value).type,
                    ((BacnetObjectId)value.Value).instance);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_COV_SUBSCRIPTION:
                encode_cov_subscription(buffer, (BacnetCOVSubscription)value.Value);
                //is this the right way to do it, I wonder?
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_RESULT:
                encode_read_access_result(buffer, (BacnetReadAccessResult)value.Value);
                //is this the right way to do it, I wonder?
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION:
                encode_read_access_specification(buffer, (BacnetReadAccessSpecification)value.Value);
                //is this the right way to do it, I wonder?
                break;
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DESTINATION:
                switch (value.Value)
                {
                    case BacnetObjectId oid:
                        EncodeApplicationDestination(buffer, oid.type, oid.instance);
                        break;
                    case BacnetAddress address:
                        EncodeApplicationDestination(buffer, address);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported destination value '{value.Value}' (type {value.GetType()})");
                }
                break;
            default:
                //context specific
                if (value.Value is byte[] arr)
                {
                    buffer?.Add(arr, arr.Length);
                }
                else
                {
                    try
                    {
                        var oType = value.Value.GetType();
                        if (oType.IsGenericType && oType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            // last chance to encode a List<object>
                            var t = (List<object>)value.Value;
                            foreach (var o in t)
                                ((IEncode)o).Encode(buffer);
                        }
                        else
                        {
                            // last chance to encode a value
                            ((IEncode)value.Value).Encode(buffer);
                        }
                    }
                    catch
                    {
                        throw new Exception("I cannot encode this");
                    }
                }
                break;
        }
    }

    public static void bacapp_encode_device_obj_property_ref(EncodeBuffer buffer, BacnetDeviceObjectPropertyReference value)
    {
        encode_context_object_id(buffer, 0, value.objectIdentifier.type, value.objectIdentifier.instance);
        encode_context_enumerated(buffer, 1, (uint)value.propertyIdentifier);

        /* Array index is optional so check if needed before inserting */
        if (value.arrayIndex != BACNET_ARRAY_ALL)
            encode_context_unsigned(buffer, 2, value.arrayIndex);

        /* Likewise, device id is optional so see if needed
         * (set type to non device to omit */
        if (value.deviceIndentifier.type == BacnetObjectTypes.OBJECT_DEVICE)
            encode_context_object_id(buffer, 3, value.deviceIndentifier.type, value.deviceIndentifier.instance);
    }

    public static void bacapp_encode_context_device_obj_property_ref(EncodeBuffer buffer, byte tagNumber, BacnetDeviceObjectPropertyReference value)
    {
        encode_opening_tag(buffer, tagNumber);
        bacapp_encode_device_obj_property_ref(buffer, value);
        encode_closing_tag(buffer, tagNumber);
    }

    public static void bacapp_encode_property_state(EncodeBuffer buffer, BacnetPropertyState value)
    {
        switch (value.tag)
        {
            case BacnetPropertyState.BacnetPropertyStateTypes.BOOLEAN_VALUE:
                encode_context_boolean(buffer, 0, value.state.boolean_value);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.BINARY_VALUE:
                encode_context_enumerated(buffer, 1, (uint)value.state.binaryValue);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.EVENT_TYPE:
                encode_context_enumerated(buffer, 2, (uint)value.state.eventType);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.POLARITY:
                encode_context_enumerated(buffer, 3, (uint)value.state.polarity);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.PROGRAM_CHANGE:
                encode_context_enumerated(buffer, 4, (uint)value.state.programChange);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.PROGRAM_STATE:
                encode_context_enumerated(buffer, 5, (uint)value.state.programState);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.REASON_FOR_HALT:
                encode_context_enumerated(buffer, 6, (uint)value.state.programError);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.RELIABILITY:
                encode_context_enumerated(buffer, 7, (uint)value.state.reliability);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.STATE:
                encode_context_enumerated(buffer, 8, (uint)value.state.state);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.SYSTEM_STATUS:
                encode_context_enumerated(buffer, 9, (uint)value.state.systemStatus);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.UNITS:
                encode_context_enumerated(buffer, 10, (uint)value.state.units);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.UNSIGNED_VALUE:
                encode_context_unsigned(buffer, 11, value.state.unsignedValue);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.LIFE_SAFETY_MODE:
                encode_context_enumerated(buffer, 12, (uint)value.state.lifeSafetyMode);
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.LIFE_SAFETY_STATE:
                encode_context_enumerated(buffer, 13, (uint)value.state.lifeSafetyState);
                break;

            default:
                /* FIXME: assert(0); - return a negative len? */
                break;
        }
    }

    public static void encode_context_bitstring(EncodeBuffer buffer, byte tagNumber, BacnetBitString bitString)
    {
        uint bitStringEncodedLength = 1; /* 1 for the bits remaining octet */

        /* bit string may use more than 1 octet for the tag, so find out how many */
        bitStringEncodedLength += bitstring_bytesUsed(bitString);
        encode_tag(buffer, tagNumber, true, bitStringEncodedLength);
        encode_bitstring(buffer, bitString);
    }

    public static void encode_opening_tag(EncodeBuffer buffer, byte tagNumber)
    {
        var len = 1;
        var tmp = new byte[2];

        /* set class field to context specific */
        tmp[0] = 0x8;
        /* additional tag byte after this byte for extended tag byte */
        if (tagNumber <= 14)
        {
            tmp[0] |= (byte)(tagNumber << 4);
        }
        else
        {
            tmp[0] |= 0xF0;
            tmp[1] = tagNumber;
            len++;
        }
        /* set type field to opening tag */
        tmp[0] |= 6;

        buffer.Add(tmp, len);
    }

    public static void encode_context_signed(EncodeBuffer buffer, byte tagNumber, int value)
    {
        int len; /* return value */

        /* length of signed int is variable, as per 20.2.11 */
        if (value >= -128 && value < 128)
            len = 1;
        else if (value >= -32768 && value < 32768)
            len = 2;
        else if (value > -8388608 && value < 8388608)
            len = 3;
        else
            len = 4;

        encode_tag(buffer, tagNumber, true, (uint)len);
        encode_bacnet_signed(buffer, value);
    }

    public static void encode_context_object_id(EncodeBuffer buffer, byte tagNumber, BacnetObjectTypes objectType, uint instance)
    {
        encode_tag(buffer, tagNumber, true, 4);
        encode_bacnet_object_id(buffer, objectType, instance);
    }

    public static void encode_closing_tag(EncodeBuffer buffer, byte tagNumber)
    {
        var len = 1;
        var tmp = new byte[2];

        /* set class field to context specific */
        tmp[0] = 0x8;
        /* additional tag byte after this byte for extended tag byte */
        if (tagNumber <= 14)
        {
            tmp[0] |= (byte)(tagNumber << 4);
        }
        else
        {
            tmp[0] |= 0xF0;
            tmp[1] = tagNumber;
            len++;
        }
        /* set type field to closing tag */
        tmp[0] |= 7;

        buffer.Add(tmp, len);
    }

    public static void encode_bacnet_time(EncodeBuffer buffer, DateTime value)
    {
        buffer.Add((byte)value.Hour);
        buffer.Add((byte)value.Minute);
        buffer.Add((byte)value.Second);
        buffer.Add((byte)(value.Millisecond / 10));
    }

    public static void encode_context_time(EncodeBuffer buffer, byte tagNumber, DateTime value)
    {
        encode_tag(buffer, tagNumber, true, 4);
        encode_bacnet_time(buffer, value);
    }

    public static void encode_bacnet_date(EncodeBuffer buffer, DateTime value)
    {
        if (value == new DateTime(1, 1, 1)) // this is the way decode do for 'Date any' = DateTime(0)
        {
            buffer.Add(0xFF);
            buffer.Add(0xFF);
            buffer.Add(0xFF);
            buffer.Add(0xFF);
            return;
        }

        /* allow 2 digit years */
        if (value.Year >= 1900)
            buffer.Add((byte)(value.Year - 1900));
        else if (value.Year < 0x100)
            buffer.Add((byte)value.Year);
        else
            throw new Exception("Date is rubbish");

        buffer.Add((byte)value.Month);
        buffer.Add((byte)value.Day);
        buffer.Add(value.DayOfWeek != DayOfWeek.Sunday
            ? (byte)value.DayOfWeek
            : (byte)7);
    }

    public static void encode_application_date(EncodeBuffer buffer, DateTime value)
    {
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, false, 4);
        encode_bacnet_date(buffer, value);
    }

    public static void encode_application_time(EncodeBuffer buffer, DateTime value)
    {
        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, false, 4);
        encode_bacnet_time(buffer, value);
    }

    public static void bacapp_encode_datetime(EncodeBuffer buffer, DateTime value)
    {
        if (value != new DateTime(1, 1, 1))
        {
            encode_application_date(buffer, value);
            encode_application_time(buffer, value);
        }
    }

    public static void bacapp_encode_context_datetime(EncodeBuffer buffer, byte tagNumber, DateTime value)
    {
        if (value != new DateTime(1, 1, 1))
        {
            encode_opening_tag(buffer, tagNumber);
            bacapp_encode_datetime(buffer, value);
            encode_closing_tag(buffer, tagNumber);
        }
    }

    public static void bacapp_encode_timestamp(EncodeBuffer buffer, BacnetGenericTime value)
    {
        switch (value.Tag)
        {
            case BacnetTimestampTags.TIME_STAMP_TIME:
                encode_context_time(buffer, 0, value.Time);
                break;

            case BacnetTimestampTags.TIME_STAMP_SEQUENCE:
                encode_context_unsigned(buffer, 1, value.Sequence);
                break;

            case BacnetTimestampTags.TIME_STAMP_DATETIME:
                bacapp_encode_context_datetime(buffer, 2, value.Time);
                break;

            case BacnetTimestampTags.TIME_STAMP_NONE:
                break;

            default:
                throw new NotImplementedException();
        }
    }

    public static void bacapp_encode_context_timestamp(EncodeBuffer buffer, byte tagNumber, BacnetGenericTime value)
    {
        if (value.Tag != BacnetTimestampTags.TIME_STAMP_NONE)
        {
            encode_opening_tag(buffer, tagNumber);
            bacapp_encode_timestamp(buffer, value);
            encode_closing_tag(buffer, tagNumber);
        }
    }

    public static void encode_application_character_string(EncodeBuffer buffer, string value)
    {
        var tmp = new EncodeBuffer();
        encode_bacnet_character_string(tmp, value);

        encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING, false,
            (uint)tmp.offset);
        buffer.Add(tmp.buffer, tmp.offset);
    }

    public static void encode_bacnet_character_string(EncodeBuffer buffer, string value)
    {
        buffer.Add((byte)BacnetCharacterStringEncodings.CHARACTER_UTF8);
        var bufUtf8 = Encoding.UTF8.GetBytes(value); // Encoding.ASCII depreciated : Addendum 135-2008k
        buffer.Add(bufUtf8, bufUtf8.Length);
    }

    public static void encode_unsigned16(EncodeBuffer buffer, ushort value)
    {
        buffer.Add((byte)((value & 0xff00) >> 8));
        buffer.Add((byte)((value & 0x00ff) >> 0));
    }

    public static void encode_unsigned24(EncodeBuffer buffer, uint value)
    {
        buffer.Add((byte)((value & 0xff0000) >> 16));
        buffer.Add((byte)((value & 0x00ff00) >> 8));
        buffer.Add((byte)((value & 0x0000ff) >> 0));
    }

    public static void encode_unsigned32(EncodeBuffer buffer, uint value)
    {
        buffer.Add((byte)((value & 0xff000000) >> 24));
        buffer.Add((byte)((value & 0x00ff0000) >> 16));
        buffer.Add((byte)((value & 0x0000ff00) >> 8));
        buffer.Add((byte)((value & 0x000000ff) >> 0));
    }

    public static void encode_signed16(EncodeBuffer buffer, short value)
    {
        buffer.Add((byte)((value & 0xff00) >> 8));
        buffer.Add((byte)((value & 0x00ff) >> 0));
    }

    public static void encode_signed24(EncodeBuffer buffer, int value)
    {
        buffer.Add((byte)((value & 0xff0000) >> 16));
        buffer.Add((byte)((value & 0x00ff00) >> 8));
        buffer.Add((byte)((value & 0x0000ff) >> 0));
    }

    public static void encode_signed32(EncodeBuffer buffer, int value)
    {
        buffer.Add((byte)((value & 0xff000000) >> 24));
        buffer.Add((byte)((value & 0x00ff0000) >> 16));
        buffer.Add((byte)((value & 0x0000ff00) >> 8));
        buffer.Add((byte)((value & 0x000000ff) >> 0));
    }

    public static void encode_signed64(EncodeBuffer buffer, long value)
    {
        buffer.Add((byte)(value >> 56));
        buffer.Add((byte)((value & 0xff000000000000) >> 48));
        buffer.Add((byte)((value & 0xff0000000000) >> 40));
        buffer.Add((byte)((value & 0xff00000000) >> 32));

        buffer.Add((byte)((value & 0xff000000) >> 24));
        buffer.Add((byte)((value & 0x00ff0000) >> 16));
        buffer.Add((byte)((value & 0x0000ff00) >> 8));
        buffer.Add((byte)((value & 0x000000ff) >> 0));
    }

    public static void encode_read_access_specification(EncodeBuffer buffer, BacnetReadAccessSpecification value)
    {
        /* Tag 0: BACnetObjectIdentifier */
        encode_context_object_id(buffer, 0, value.objectIdentifier.type, value.objectIdentifier.instance);

        /* Tag 1: sequence of BACnetPropertyReference */
        encode_opening_tag(buffer, 1);
        foreach (var p in value.propertyReferences)
        {
            encode_context_enumerated(buffer, 0, p.propertyIdentifier);

            /* optional array index */
            if (p.propertyArrayIndex != BACNET_ARRAY_ALL)
                encode_context_unsigned(buffer, 1, p.propertyArrayIndex);
        }
        encode_closing_tag(buffer, 1);
    }

    public static void encode_read_access_result(EncodeBuffer buffer, BacnetReadAccessResult value)
    {
        /* Tag 0: BACnetObjectIdentifier */
        encode_context_object_id(buffer, 0, value.objectIdentifier.type, value.objectIdentifier.instance);

        /* Tag 1: listOfResults */
        encode_opening_tag(buffer, 1);
        foreach (var pValue in value.values)
        {
            /* Tag 2: propertyIdentifier */
            encode_context_enumerated(buffer, 2, pValue.property.propertyIdentifier);
            /* Tag 3: optional propertyArrayIndex */
            if (pValue.property.propertyArrayIndex != BACNET_ARRAY_ALL)
                encode_context_unsigned(buffer, 3, pValue.property.propertyArrayIndex);

            if (pValue.value != null && pValue.value[0].Value is BacnetError)
            {
                /* Tag 5: Error */
                encode_opening_tag(buffer, 5);
                encode_application_enumerated(buffer, (uint)((BacnetError)pValue.value[0].Value).error_class);
                encode_application_enumerated(buffer, (uint)((BacnetError)pValue.value[0].Value).error_code);
                encode_closing_tag(buffer, 5);
            }
            else
            {
                /* Tag 4: Value */
                encode_opening_tag(buffer, 4);
                foreach (var v in pValue.value)
                {
                    bacapp_encode_application_data(buffer, v);
                }
                encode_closing_tag(buffer, 4);
            }
        }
        encode_closing_tag(buffer, 1);
    }

    public static int decode_read_access_result(BacnetAddress address, byte[] buffer, int offset, int apdu_len, out BacnetReadAccessResult value)
    {
        var len = 0;

        value = new BacnetReadAccessResult();

        if (!decode_is_context_tag(buffer, offset + len, 0))
            return -1;
        len = 1;
        len += decode_object_id(buffer, offset + len, out value.objectIdentifier.type,
            out value.objectIdentifier.instance);

        /* Tag 1: listOfResults */
        if (!decode_is_opening_tag_number(buffer, offset + len, 1))
            return -1;
        len++;

        var valueList = new List<BacnetPropertyValue>();
        while (apdu_len - len > 0)
        {
            var new_entry = new BacnetPropertyValue();

            /* end */
            if (decode_is_closing_tag_number(buffer, offset + len, 1))
            {
                len++;
                break;
            }

            /* Tag 2: propertyIdentifier */
            len += decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != 2)
                return -1;
            len += decode_enumerated(buffer, offset + len, lenValueType, out new_entry.property.propertyIdentifier);
            /* Tag 3: Optional Array Index */
            var tagLen = decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            if (tagNumber == 3)
            {
                len += tagLen;
                len += decode_unsigned(buffer, offset + len, lenValueType,
                    out new_entry.property.propertyArrayIndex);
            }
            else
                new_entry.property.propertyArrayIndex = BACNET_ARRAY_ALL;

            /* Tag 4: Value */
            tagLen = decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            len += tagLen;
            if (tagNumber == 4)
            {
                var localValueList = new List<BacnetValue>();
                while (!decode_is_closing_tag_number(buffer, offset + len, 4))
                {
                    tagLen = bacapp_decode_application_data(address, buffer, offset + len, apdu_len + offset - 1,
                        value.objectIdentifier.type, (BacnetPropertyIds)new_entry.property.propertyIdentifier, out var v);
                    if (tagLen < 0) return -1;
                    len += tagLen;
                    localValueList.Add(v);
                }
                // FC : two values one Date & one Time => change to one datetime
                if (localValueList.Count == 2 && localValueList[0].Tag == BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE && localValueList[1].Tag == BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME)
                {
                    var date = (DateTime)localValueList[0].Value;
                    var time = (DateTime)localValueList[1].Value;
                    var bdatetime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
                    localValueList.Clear();
                    localValueList.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME, bdatetime));
                    new_entry.value = localValueList;
                }
                else
                    new_entry.value = localValueList;
                len++;
            }
            else if (tagNumber == 5)
            {
                /* Tag 5: Error */
                var err = new BacnetError();
                len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += decode_enumerated(buffer, offset + len, lenValueType, out err.error_class);
                len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len += decode_enumerated(buffer, offset + len, lenValueType, out err.error_code);
                if (!decode_is_closing_tag_number(buffer, offset + len, 5))
                    return -1;
                len++;

                new_entry.value = new[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ERROR, err) };
            }

            valueList.Add(new_entry);
        }
        value.values = valueList;

        return len;
    }

    public static int decode_read_access_specification(byte[] buffer, int offset, int apdu_len, out BacnetReadAccessSpecification value)
    {
        var len = 0;

        value = new BacnetReadAccessSpecification();

        /* Tag 0: Object ID */
        if (!decode_is_context_tag(buffer, offset + len, 0))
            return -1;
        len++;
        len += decode_object_id(buffer, offset + len, out value.objectIdentifier.type,
            out value.objectIdentifier.instance);

        /* Tag 1: sequence of ReadAccessSpecification */
        if (!decode_is_opening_tag_number(buffer, offset + len, 1))
            return -1;
        len++; /* opening tag is only one octet */

        /* properties */
        var propertyIdAndArrayIndex = new List<BacnetPropertyReference>();
        while (apdu_len - len > 1 && !decode_is_closing_tag_number(buffer, offset + len, 1))
        {
            var p_ref = new BacnetPropertyReference();

            /* Tag 0: propertyIdentifier */
            if (!IS_CONTEXT_SPECIFIC(buffer[offset + len]))
                return -1;

            len += decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValue_type);
            if (tagNumber != 0)
                return -1;

            /* Should be at least the unsigned value + 1 tag left */
            if (len + lenValue_type >= apdu_len)
                return -1;
            len += decode_enumerated(buffer, offset + len, lenValue_type, out p_ref.propertyIdentifier);
            /* Assume most probable outcome */
            p_ref.propertyArrayIndex = BACNET_ARRAY_ALL;
            /* Tag 1: Optional propertyArrayIndex */
            if (IS_CONTEXT_SPECIFIC(buffer[offset + len]) && !IS_CLOSING_TAG(buffer[offset + len]))
            {
                var tmp = decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValue_type);
                if (tagNumber == 1)
                {
                    len += tmp;
                    /* Should be at least the unsigned array index + 1 tag left */
                    if (len + lenValue_type >= apdu_len)
                        return -1;
                    len += decode_unsigned(buffer, offset + len, lenValue_type, out p_ref.propertyArrayIndex);
                }
            }
            propertyIdAndArrayIndex.Add(p_ref);
        }

        /* closing tag */
        if (!decode_is_closing_tag_number(buffer, offset + len, 1))
            return -1;
        len++;

        value.propertyReferences = propertyIdAndArrayIndex;
        return len;
    }

    public static int decode_device_obj_property_ref(byte[] buffer, int offset, int apdu_len, out BacnetDeviceObjectPropertyReference value)
    {
        var len = 0;

        value = new BacnetDeviceObjectPropertyReference { arrayIndex = BACNET_ARRAY_ALL };

        /* Tag 0: Object ID */
        if (!decode_is_context_tag(buffer, offset + len, 0))
            return -1;

        len++;
        len += decode_object_id(buffer, offset + len, out value.objectIdentifier.type,
            out value.objectIdentifier.instance);

        /* Tag 1 : Property identifier */
        len += decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != 1)
            return -1;

        len += decode_enumerated(buffer, offset + len, lenValueType, out value.propertyIdentifier);

        /* Tag 2: Optional Array Index */
        var tagLen = decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber == 2)
        {
            len += tagLen;
            len += decode_unsigned(buffer, offset + len, lenValueType, out value.arrayIndex);
        }

        /* Tag 3 : Optional Device Identifier */
        if (!decode_is_context_tag(buffer, offset + len, 3))
            return len;
        if (IS_CLOSING_TAG(buffer[offset + len])) return len;

        len++;

        len += decode_object_id(buffer, offset + len, out value.deviceIndentifier.type,
            out value.deviceIndentifier.instance);

        return len;
    }

    public static int decode_unsigned(byte[] buffer, int offset, uint lenValue, out uint value)
    {
        switch (lenValue)
        {
            case 1:
                value = buffer[offset];
                break;

            case 2:
                decode_unsigned16(buffer, offset, out var unsigned16Value);
                value = unsigned16Value;
                break;

            case 3:
                decode_unsigned24(buffer, offset, out value);
                break;

            case 4:
                decode_unsigned32(buffer, offset, out value);
                break;

            default:
                value = 0;
                break;
        }

        return (int)lenValue;
    }

    public static int decode_unsigned32(byte[] buffer, int offset, out uint value)
    {
        value = ((uint)buffer[offset + 0] << 24) & 0xff000000;
        value |= ((uint)buffer[offset + 1] << 16) & 0x00ff0000;
        value |= ((uint)buffer[offset + 2] << 8) & 0x0000ff00;
        value |= (uint)buffer[offset + 3] & 0x000000ff;
        return 4;
    }

    public static int decode_unsigned24(byte[] buffer, int offset, out uint value)
    {
        value = ((uint)buffer[offset + 0] << 16) & 0x00ff0000;
        value |= ((uint)buffer[offset + 1] << 8) & 0x0000ff00;
        value |= (uint)buffer[offset + 2] & 0x000000ff;
        return 3;
    }

    public static int decode_unsigned16(byte[] buffer, int offset, out ushort value)
    {
        value = (ushort)(((uint)buffer[offset + 0] << 8) & 0x0000ff00);
        value |= (ushort)((uint)buffer[offset + 1] & 0x000000ff);
        return 2;
    }

    public static int decode_unsigned8(byte[] buffer, int offset, out byte value)
    {
        value = buffer[offset + 0];
        return 1;
    }

    public static int decode_signed32(byte[] buffer, int offset, out int value)
    {
        value = (int)((buffer[offset + 0] << 24) & 0xff000000);
        value |= (buffer[offset + 1] << 16) & 0x00ff0000;
        value |= (buffer[offset + 2] << 8) & 0x0000ff00;
        value |= buffer[offset + 3] & 0x000000ff;
        return 4;
    }

    public static int decode_signed24(byte[] buffer, int offset, out int value)
    {
        value = (buffer[offset + 0] << 16) & 0x00ff0000;
        value |= (buffer[offset + 1] << 8) & 0x0000ff00;
        value |= buffer[offset + 2] & 0x000000ff;
        if ((value & 0x800000) != 0) value |= 0xff << 24; // set sign
        return 3;
    }

    public static int decode_signed16(byte[] buffer, int offset, out short value)
    {
        value = (short)((buffer[offset + 0] << 8) & 0x0000ff00);
        value |= (short)(buffer[offset + 1] & 0x000000ff);
        return 2;
    }

    public static int decode_signed8(byte[] buffer, int offset, out sbyte value)
    {
        value = (sbyte)buffer[offset + 0];
        return 1;
    }

    public static bool IS_EXTENDED_TAG_NUMBER(byte x)
    {
        return (x & 0xF0) == 0xF0;
    }

    public static bool IS_EXTENDED_VALUE(byte x)
    {
        return (x & 0x07) == 5;
    }

    public static bool IS_CONTEXT_SPECIFIC(byte x)
    {
        return (x & 0x8) == 0x8;
    }

    public static bool IS_APPLICATION_TAG(byte x)
    {
        return !IS_CONTEXT_SPECIFIC(x);
    }

    public static bool IS_OPENING_TAG(byte x)
    {
        return (x & 0x07) == 6;
    }

    public static bool IS_CLOSING_TAG(byte x)
    {
        return (x & 0x07) == 7;
    }

    public static int decode_tag_number(byte[] buffer, int offset, out byte tagNumber)
    {
        var len = 1; /* return value */

        /* decode the tag number first */
        if (IS_EXTENDED_TAG_NUMBER(buffer[offset]))
        {
            /* extended tag */
            tagNumber = buffer[offset + 1];
            len++;
        }
        else
        {
            tagNumber = (byte)(buffer[offset] >> 4);
        }

        return len;
    }

    public static int decode_signed(byte[] buffer, int offset, uint lenValue, out int value)
    {
        switch (lenValue)
        {
            case 1:
                decode_signed8(buffer, offset, out var sbyteValue);
                value = sbyteValue;
                break;

            case 2:
                decode_signed16(buffer, offset, out var shortValue);
                value = shortValue;
                break;

            case 3:
                decode_signed24(buffer, offset, out value);
                break;

            case 4:
                decode_signed32(buffer, offset, out value);
                break;

            default:
                value = 0;
                break;
        }

        return (int)lenValue;
    }

    public static int decode_real(byte[] buffer, int offset, out float value)
    {
        byte[] tmp = { buffer[offset + 3], buffer[offset + 2], buffer[offset + 1], buffer[offset + 0] };
        value = BitConverter.ToSingle(tmp, 0);
        return 4;
    }

    public static int decode_real_safe(byte[] buffer, int offset, uint lenValue, out float value)
    {
        if (lenValue == 4)
            return decode_real(buffer, offset, out value);

        value = 0.0f;
        return (int)lenValue;
    }

    public static int decode_double(byte[] buffer, int offset, out double value)
    {
        byte[] tmp =
        {
                buffer[offset + 7], buffer[offset + 6], buffer[offset + 5], buffer[offset + 4],
                buffer[offset + 3], buffer[offset + 2], buffer[offset + 1], buffer[offset + 0]
            };
        value = BitConverter.ToDouble(tmp, 0);
        return 8;
    }

    public static int decode_double_safe(byte[] buffer, int offset, uint lenValue, out double value)
    {
        if (lenValue == 8)
            return decode_double(buffer, offset, out value);

        value = 0.0f;
        return (int)lenValue;
    }

    private static bool octetstring_copy(byte[] buffer, int offset, int maxOffset, byte[] octetString, int octetStringOffset, uint octetStringLength)
    {
        var status = false; /* return value */

        if (octetStringLength <= maxOffset + offset)
        {
            if (octetString != null)
                Array.Copy(buffer, offset, octetString, octetStringOffset,
                    Math.Min(octetString.Length, buffer.Length - offset));
            status = true;
        }

        return status;
    }

    public static int decode_octet_string(byte[] buffer, int offset, int maxLength, byte[] octetString, int octetStringOffset, uint octetStringLength)
    {
        octetstring_copy(buffer, offset, maxLength, octetString, octetStringOffset, octetStringLength);
        var len = (int)octetStringLength;

        return len;
    }

    public static int decode_context_octet_string(byte[] buffer, int offset, int maxLength, byte tagNumber, byte[] octetString, int octetStringOffset)
    {
        var len = 0; /* return value */

        if (decode_is_context_tag(buffer, offset, tagNumber))
        {
            len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out var lenValue);

            if (octetstring_copy(buffer, offset + len, maxLength, octetString, octetStringOffset, lenValue))
            {
                len += (int)lenValue;
            }
        }
        else
            len = -1;

        return len;
    }

    private static bool multi_charset_characterstring_decode(byte[] buffer, int offset, byte encoding, uint length, out string charString)
    {
        try
        {
            Encoding e;

            switch ((BacnetCharacterStringEncodings)encoding)
            {
                // 'normal' encoding, backward compatible ANSI_X34 (for decoding only)
                case BacnetCharacterStringEncodings.CHARACTER_UTF8:
                    e = Encoding.UTF8;
                    break;

                // UCS2 is backward compatible UTF16 (for decoding only)
                // http://hackipedia.org/Character%20sets/Unicode,%20UTF%20and%20UCS%20encodings/UCS-2.htm
                // https://en.wikipedia.org/wiki/Byte_order_mark
                case BacnetCharacterStringEncodings.CHARACTER_UCS2:
                    if (buffer[offset] == 0xFF && buffer[offset + 1] == 0xFE) // Byte Order Mark
                        e = Encoding.Unicode; // little endian encoding
                    else
                        e = Encoding.BigEndianUnicode; // big endian encoding if BOM is not set, or 0xFE-0xFF
                    break;

                // eq. UTF32. In usage somewhere for transmission ? A bad idea !
                case BacnetCharacterStringEncodings.CHARACTER_UCS4:
                    if (buffer[offset] == 0xFF && buffer[offset + 1] == 0xFE && buffer[offset + 2] == 0 &&
                        buffer[offset + 3] == 0)
                        e = Encoding.UTF32; // UTF32 little endian encoding
                    else
                        e = Encoding.GetEncoding(12001);
                    // UTF32 big endian encoding if BOM is not set, or 0-0-0xFE-0xFF
                    break;

                case BacnetCharacterStringEncodings.CHARACTER_ISO8859:
                    e = Encoding.GetEncoding(28591); // "iso-8859-1"
                    break;

                // FIXME: somebody in Japan (or elsewhere) could help,test&validate if such devices exist ?
                // http://cgproducts.johnsoncontrols.com/met_pdf/1201531.pdf?ref=binfind.com/web page 18
                case BacnetCharacterStringEncodings.CHARACTER_MS_DBCS:
                    e = Encoding.GetEncoding("shift_jis");
                    break;

                // FIXME: somebody in Japan (or elsewhere) could help,test&validate if such devices exist ?
                // http://www.sljfaq.org/afaq/encodings.html
                case BacnetCharacterStringEncodings.CHARACTER_JISX_0208:
                    e = Encoding.GetEncoding("shift_jis"); // maybe "iso-2022-jp" ?
                    break;

                // unknown code (wrong code, experimental, ...)
                // decoded as ISO-8859-1 (removing controls) : displays certainly a strange content !
                default:
                    var sb = new StringBuilder();
                    for (var i = 0; i < length; i++)
                    {
                        var oneChar = (char)buffer[offset + i]; // byte to char on .NET : ISO-8859-1
                        if (char.IsSymbol(oneChar)) sb.Append(oneChar);
                    }
                    charString = sb.ToString();
                    return true;
            }

            charString = e.GetString(buffer, offset, (int)length);
        }
        catch
        {
            charString = "string decoding error !";
        }

        return true; // always OK
    }

    public static int decode_character_string(byte[] buffer, int offset, int maxLength, uint lenValue, out string charString)
    {
        var len = 0; /* return value */

        var status = multi_charset_characterstring_decode(buffer, offset + 1, buffer[offset], lenValue - 1, out charString);
        if (status)
        {
            len = (int)lenValue;
        }

        return len;
    }

    private static void bitstring_set_octet(ref BacnetBitString bitString, byte index, byte octet)
    {
        if (index < MAX_BITSTRING_BYTES)
            bitString.value[index] = octet;
    }

    private static void bitstring_set_bits_used(ref BacnetBitString bitString, byte bytesUsed, byte unusedBits)
    {
        /* FIXME: check that bytesUsed is at least one? */
        bitString.bits_used = (byte)(bytesUsed * 8);
        bitString.bits_used -= unusedBits;
    }

    public static int decode_bitstring(byte[] buffer, int offset, uint lenValue, out BacnetBitString bitString)
    {
        var len = 0;

        bitString = new BacnetBitString { value = new byte[MAX_BITSTRING_BYTES] };
        if (lenValue > 0)
        {
            /* the first octet contains the unused bits */
            var bytesUsed = lenValue - 1;
            if (bytesUsed <= MAX_BITSTRING_BYTES)
            {
                len = 1;
                for (uint i = 0; i < bytesUsed; i++)
                {
                    bitstring_set_octet(ref bitString, (byte)i, byte_reverse_bits(buffer[offset + len++]));
                }
                var unusedBits = (byte)(buffer[offset] & 0x07);
                bitstring_set_bits_used(ref bitString, (byte)bytesUsed, unusedBits);
            }
        }

        return len;
    }

    public static int decode_context_character_string(byte[] buffer, int offset, int maxLength, byte tagNumber, out string charString)
    {
        var len = 0; /* return value */

        charString = null;
        if (decode_is_context_tag(buffer, offset + len, tagNumber))
        {
            len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out var lenValue);

            var status = multi_charset_characterstring_decode(buffer, offset + 1 + len, buffer[offset + len], lenValue - 1, out charString);

            if (status)
                len += (int)lenValue;
        }
        else
            len = -1;

        return len;
    }

    public static int decode_date(byte[] buffer, int offset, out DateTime bdate)
    {
        int year = (ushort)(buffer[offset] + 1900);
        int month = buffer[offset + 1];
        int day = buffer[offset + 2];
        int wday = buffer[offset + 3];

        if (month == 0xFF && day == 0xFF && wday == 0xFF && year - 1900 == 0xFF)
            bdate = new DateTime(1, 1, 1);
        else
            bdate = new DateTime(year, month, day);

        return 4;
    }

    public static int decode_date_safe(byte[] buffer, int offset, uint lenValue, out DateTime bdate)
    {
        if (lenValue == 4)
            return decode_date(buffer, offset, out bdate);

        bdate = new DateTime(1, 1, 1);
        return (int)lenValue;
    }

    public static int decode_bacnet_time(byte[] buffer, int offset, out DateTime btime)
    {
        int hour = buffer[offset + 0];
        int min = buffer[offset + 1];
        int sec = buffer[offset + 2];
        int hundredths = buffer[offset + 3];
        if (hour == 0xFF && min == 0xFF && sec == 0xFF && hundredths == 0xFF)
        {
            btime = new DateTime(1, 1, 1);
        }
        else
        {
            if (hundredths > 100) hundredths = 0; // sometimes set to 255
            btime = new DateTime(1, 1, 1, hour, min, sec, hundredths * 10);
        }
        return 4;
    }

    public static int decode_bacnet_time_safe(byte[] buffer, int offset, uint lenValue, out DateTime btime)
    {
        if (lenValue == 4)
            return decode_bacnet_time(buffer, offset, out btime);

        btime = new DateTime(1, 1, 1);
        return (int)lenValue;
    }

    public static int decode_bacnet_datetime(byte[] buffer, int offset, out DateTime bdatetime)
    {
        var len = 0;
        len += decode_application_date(buffer, offset + len, out var date); // Date
        len += decode_application_time(buffer, offset + len, out var time); // Time
        bdatetime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
        return len;
    }

    public static int decode_object_id(byte[] buffer, int offset, out ushort objectType, out uint instance)
    {
        var len = decode_unsigned32(buffer, offset, out var value);
        objectType = (ushort)((value >> BACNET_INSTANCE_BITS) & BACNET_MAX_OBJECT);
        instance = value & BACNET_MAX_INSTANCE;

        return len;
    }

    public static int decode_object_id_safe(byte[] buffer, int offset, uint lenValue, out ushort objectType, out uint instance)
    {
        if (lenValue == 4)
            return decode_object_id(buffer, offset, out objectType, out instance);

        objectType = 0;
        instance = 0;
        return 0;
    }

    public static int decode_context_object_id(byte[] buffer, int offset, byte tagNumber, out ushort objectType, out uint instance)
    {
        var len = 0;

        if (decode_is_context_tag_with_length(buffer, offset + len, tagNumber, out len))
        {
            len += decode_object_id(buffer, offset + len, out objectType, out instance);
        }
        else
        {
            objectType = 0;
            instance = 0;
            len = -1;
        }
        return len;
    }

    public static int decode_application_time(byte[] buffer, int offset, out DateTime btime)
    {
        var len = 0;
        decode_tag_number(buffer, offset + len, out var tagNumber);

        if (tagNumber == (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME)
        {
            len++;
            len += decode_bacnet_time(buffer, offset + len, out btime);
        }
        else
        {
            btime = new DateTime(1, 1, 1);
            len = -1;
        }
        return len;
    }

    public static int decode_context_bacnet_time(byte[] buffer, int offset, byte tagNumber, out DateTime btime)
    {
        var len = 0;

        if (decode_is_context_tag_with_length(buffer, offset + len, tagNumber, out len))
        {
            len += decode_bacnet_time(buffer, offset + len, out btime);
        }
        else
        {
            btime = new DateTime(1, 1, 1);
            len = -1;
        }
        return len;
    }

    public static int decode_application_date(byte[] buffer, int offset, out DateTime bdate)
    {
        var len = 0;
        decode_tag_number(buffer, offset + len, out var tagNumber);

        if (tagNumber == (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE)
        {
            len++;
            len += decode_date(buffer, offset + len, out bdate);
        }
        else
        {
            bdate = new DateTime(1, 1, 1);
            len = -1;
        }
        return len;
    }

    public static bool decode_is_context_tag_with_length(byte[] buffer, int offset, byte tagNumber, out int tagLength)
    {
        tagLength = decode_tag_number(buffer, offset, out var myTagNumber);
        return IS_CONTEXT_SPECIFIC(buffer[offset]) && myTagNumber == tagNumber;
    }

    public static int decode_context_date(byte[] buffer, int offset, byte tagNumber, out DateTime bdate)
    {
        var len = 0;

        if (decode_is_context_tag_with_length(buffer, offset + len, tagNumber, out len))
        {
            len += decode_date(buffer, offset + len, out bdate);
        }
        else
        {
            bdate = new DateTime(1, 1, 1);
            len = -1;
        }
        return len;
    }

    public static int bacapp_decode_data(byte[] buffer, int offset, int maxLength, BacnetApplicationTags tagDataType, uint lenValueType, out BacnetValue value)
    {
        var len = 0;
        uint uintValue;

        value = new BacnetValue { Tag = tagDataType };

        switch (tagDataType)
        {
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                /* nothing else to do */
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                value.Value = lenValueType > 0;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                len = decode_unsigned(buffer, offset, lenValueType, out uintValue);
                value.Value = uintValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                len = decode_signed(buffer, offset, lenValueType, out var intValue);
                value.Value = intValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                len = decode_real_safe(buffer, offset, lenValueType, out var floatValue);
                value.Value = floatValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                len = decode_double_safe(buffer, offset, lenValueType, out var doubleValue);
                value.Value = doubleValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                var octetString = new byte[lenValueType];
                len = decode_octet_string(buffer, offset, maxLength, octetString, 0, lenValueType);
                value.Value = octetString;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                len = decode_character_string(buffer, offset, maxLength, lenValueType, out var stringValue);
                value.Value = stringValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                len = decode_bitstring(buffer, offset, lenValueType, out var bitValue);
                value.Value = bitValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                len = decode_enumerated(buffer, offset, lenValueType, out value.Value);
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                len = decode_date_safe(buffer, offset, lenValueType, out var dateValue);
                value.Value = dateValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                len = decode_bacnet_time_safe(buffer, offset, lenValueType, out var timeValue);
                value.Value = timeValue;
                break;

            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                len = decode_object_id_safe(buffer, offset, lenValueType, out var objectType, out var instance);
                value.Value = new BacnetObjectId((BacnetObjectTypes)objectType, instance);
                break;
        }

        return len;
    }

    /* returns the fixed tag type for certain context tagged properties */

    private static BacnetApplicationTags bacapp_context_tag_type(BacnetAddress address, BacnetPropertyIds property, byte tagNumber)
    {
        var tag = BacnetApplicationTags.MAX_BACNET_APPLICATION_TAG;

        switch (property)
        {
            case BacnetPropertyIds.PROP_ACTUAL_SHED_LEVEL:
            case BacnetPropertyIds.PROP_REQUESTED_SHED_LEVEL:
            case BacnetPropertyIds.PROP_EXPECTED_SHED_LEVEL:
                switch (tagNumber)
                {
                    case 0:
                    case 1:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                        break;

                    case 2:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;
                        break;
                }
                break;

            case BacnetPropertyIds.PROP_ACTION:
                switch (tagNumber)
                {
                    case 0:
                    case 1:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                        break;

                    case 2:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;
                        break;

                    case 3:
                    case 5:
                    case 6:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                        break;

                    case 7:
                    case 8:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
                        break;

                    case 4: /* propertyValue: abstract syntax */
                        break;
                }
                break;

            case BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS:
                /* Sequence of ReadAccessSpecification */
                switch (tagNumber)
                {
                    case 0:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                        break;
                }
                break;

            case BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE:
                switch (tagNumber)
                {
                    case 1:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                        break;

                    case 3:
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                        break;

                    case 0: /* calendarEntry: abstract syntax + context */
                    case 2: /* list of BACnetTimeValue: abstract syntax */
                        break;
                }
                break;

            case BacnetPropertyIds.PROP_LOG_DEVICE_OBJECT_PROPERTY:
                switch (tagNumber)
                {
                    case 0: /* Object ID */
                    case 3: /* Device ID */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                        break;

                    case 1: /* Property ID */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;
                        break;

                    case 2: /* Array index */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                        break;
                }
                break;

            case BacnetPropertyIds.PROP_SUBORDINATE_LIST:
            case BacnetPropertyIds.PROP_ZONE_MEMBERS:
                /* BACnetARRAY[N] of BACnetDeviceObjectReference */
                switch (tagNumber)
                {
                    case 0: /* Optional Device ID */
                    case 1: /* Object ID */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                        break;
                }
                break;

            case BacnetPropertyIds.PROP_RECIPIENT_LIST:
                /* List of BACnetDestination */
                switch (tagNumber)
                {
                    case 0: /* Device Object ID */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                        break;
                }
                break;

            case BacnetPropertyIds.PROP_ACTIVE_COV_SUBSCRIPTIONS:
                /* BACnetCOVSubscription */
                switch (tagNumber)
                {
                    case 0: /* BACnetRecipientProcess */
                    case 1: /* BACnetObjectPropertyReference */
                        break;

                    case 2: /* issueConfirmedNotifications */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
                        break;

                    case 3: /* timeRemaining */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                        break;

                    case 4: /* covIncrement */
                        tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;
                        break;
                }
                break;
            default:
                tag = CustomTagResolver?.Invoke(address, property, tagNumber) ?? tag;
                break;
        }

        return tag;
    }

    public static int bacapp_decode_context_data(byte[] buffer, int offset, uint maxAPDULen, BacnetApplicationTags propertyTag, out BacnetValue value)
    {
        int apduLen = 0, len = 0;

        value = new BacnetValue();

        if (IS_CONTEXT_SPECIFIC(buffer[offset]))
        {
            //value->context_specific = true;
            var tagLen = decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            apduLen = tagLen;
            /* Empty construct : (closing tag) => returns NULL value */
            if (tagLen > 0 && tagLen <= maxAPDULen &&
                !decode_is_closing_tag_number(buffer, offset + len, tagNumber))
            {
                //value->context_tag = tagNumber;
                if (propertyTag < BacnetApplicationTags.MAX_BACNET_APPLICATION_TAG)
                {
                    len = bacapp_decode_data(buffer, offset + apduLen, (int)maxAPDULen, propertyTag,
                        lenValueType, out value);
                    apduLen += len;
                }
                else if (lenValueType > 0)
                {
                    /* Unknown value : non null size (elementary type) */
                    apduLen += (int)lenValueType;
                    /* SHOULD NOT HAPPEN, EXCEPTED WHEN READING UNKNOWN CONTEXTUAL PROPERTY */
                }
                else
                    apduLen = -1;
            }
            else if (tagLen == 1) /* and is a Closing tag */
                apduLen = 0; /* Don't advance over that closing tag. */
        }

        return apduLen;
    }

    public static int bacapp_decode_application_data(BacnetAddress address, byte[] buffer, int offset, int maxOffset, BacnetObjectTypes objectType, BacnetPropertyIds propertyId, out BacnetValue value)
    {
        var len = 0;

        value = new BacnetValue();

        /* FIXME: use max_apdu_len! */
        if (!IS_CONTEXT_SPECIFIC(buffer[offset]))
        {
            var tagLen = decode_tag_number_and_value(buffer, offset, out BacnetApplicationTags tagNumber, out uint lenValueType);
            if (tagLen > 0)
            {
                len += tagLen;
                var decodeLen = bacapp_decode_data(buffer, offset + len, maxOffset, tagNumber, lenValueType, out value);
                if (decodeLen < 0) return decodeLen;
                len += decodeLen;
            }
        }
        else
        {
            return bacapp_decode_context_application_data(address, buffer, offset, maxOffset, objectType, propertyId,
                out value);
        }

        return len;
    }

    public static int bacapp_decode_context_application_data(BacnetAddress address, byte[] buffer, int offset, int maxOffset, BacnetObjectTypes objectType, BacnetPropertyIds propertyId, out BacnetValue value)
    {
        var len = 0;

        value = new BacnetValue();

        if (IS_CONTEXT_SPECIFIC(buffer[offset]))
        {
            int tagLen;
            uint lenValueType;
            byte tagNumber;

            //this seems to be a strange way to determine object encodings
            if (propertyId == BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS)
            {
                tagLen = decode_read_access_specification(buffer, offset, maxOffset, out var v);
                if (tagLen < 0) return -1;
                value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION;
                value.Value = v;
                return tagLen;
            }
            if (propertyId == BacnetPropertyIds.PROP_ACTIVE_COV_SUBSCRIPTIONS)
            {
                tagLen = decode_cov_subscription(buffer, offset, maxOffset, out var v);
                if (tagLen < 0) return -1;
                value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_COV_SUBSCRIPTION;
                value.Value = v;
                return tagLen;
            }
            if (objectType == BacnetObjectTypes.OBJECT_GROUP && propertyId == BacnetPropertyIds.PROP_PRESENT_VALUE)
            {
                tagLen = decode_read_access_result(address, buffer, offset, maxOffset, out BacnetReadAccessResult v);
                if (tagLen < 0) return -1;
                value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_RESULT;
                value.Value = v;
                return tagLen;
            }
            if (propertyId == BacnetPropertyIds.PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES ||
                propertyId == BacnetPropertyIds.PROP_LOG_DEVICE_OBJECT_PROPERTY ||
                propertyId == BacnetPropertyIds.PROP_OBJECT_PROPERTY_REFERENCE)
            {
                tagLen = decode_device_obj_property_ref(buffer, offset, maxOffset, out var v);
                if (tagLen < 0) return -1;
                value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE;
                value.Value = v;
                return tagLen;
            }
            if (propertyId == BacnetPropertyIds.PROP_DATE_LIST)
            {
                var v = new BACnetCalendarEntry();
                tagLen = v.Decode(buffer, offset, (uint)maxOffset);
                if (tagLen < 0) return -1;
                value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED;
                value.Value = v;
                return tagLen;
            }
            if (propertyId == BacnetPropertyIds.PROP_EVENT_TIME_STAMPS)
            {
                decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                len++; // skip Tag

                if (tagNumber == 0) // Time without date
                {
                    len += decode_bacnet_time(buffer, offset + len, out var dt);
                    value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP;
                    value.Value = dt;
                }
                else if (tagNumber == 1) // sequence number
                {
                    len += decode_unsigned(buffer, offset + len, lenValueType, out var val);
                    value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                    value.Value = val;
                }
                else if (tagNumber == 2) // date + time
                {
                    len += decode_bacnet_datetime(buffer, offset + len, out var dt);
                    value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP;
                    len++;  // closing Tag
                    value.Value = dt;
                }
                else
                    return -1;

                return len;
            }

            value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED;
            var list = new List<BacnetValue>();

            decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            // If an opening tag is not present, no loop to get the values
            var multiplValue = IS_OPENING_TAG(buffer[offset + len]);

            while (len + offset <= maxOffset && !IS_CLOSING_TAG(buffer[offset + len]))
            {
                tagLen = decode_tag_number_and_value(buffer, offset + len, out var subTagNumber, out lenValueType);
                if (tagLen < 0) return -1;

                // DAL need to check explicitly for an opening tag, because,
                // lenValueType could also be zero for example if we are parsing
                // an empty octect string, or an integer value of zero
                // The former happens when parsing the BACnetAddress within a RecipientList
                // if the BACnetAddress has a broadcast MAC address
                if (IS_OPENING_TAG(buffer[offset + len]))
                {
                    len += tagLen;
                    tagLen = bacapp_decode_application_data(address, buffer, offset + len, maxOffset,
                        BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE, BacnetPropertyIds.MAX_BACNET_PROPERTY_ID,
                        out var subValue);
                    if (tagLen < 0) return -1;
                    list.Add(subValue);
                    len += tagLen;
                }
                else
                {
                    //override tagNumber
                    var overrideTagNumber = bacapp_context_tag_type(address, propertyId, subTagNumber);
                    if (overrideTagNumber != BacnetApplicationTags.MAX_BACNET_APPLICATION_TAG)
                        subTagNumber = (byte)overrideTagNumber;

                    //try app decode
                    var subTagLen = bacapp_decode_data(buffer, offset + len + tagLen, maxOffset,
                        (BacnetApplicationTags)subTagNumber, lenValueType, out var subValue);
                    if (subTagLen == (int)lenValueType)
                    {
                        list.Add(subValue);
                        len += tagLen + (int)lenValueType;
                    }
                    else
                    {
                        //fallback to copy byte array
                        var contextSpecific = new byte[(int)lenValueType];
                        Array.Copy(buffer, offset + len + tagLen, contextSpecific, 0, (int)lenValueType);
                        subValue =
                            new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_ENCODED,
                                contextSpecific);

                        list.Add(subValue);
                        len += tagLen + (int)lenValueType;
                    }
                }

                if (multiplValue == false)
                {
                    value = list[0];
                    return len;
                }
            }
            if (len + offset > maxOffset) return -1;

            //end tag
            if (decode_is_closing_tag_number(buffer, offset + len, tagNumber))
                len++;

            //context specifique is array of BACNET_VALUE
            value.Value = list.ToArray();
        }
        else
        {
            return -1;
        }

        return len;
    }

    public static int decode_object_id(byte[] buffer, int offset, out BacnetObjectTypes objectType, out uint instance)
    {
        var len = decode_unsigned32(buffer, offset, out var value);
        objectType = (BacnetObjectTypes)((value >> BACNET_INSTANCE_BITS) & BACNET_MAX_OBJECT);
        instance = value & BACNET_MAX_INSTANCE;

        return len;
    }

    public static int decode_enumerated(byte[] buffer, int offset, uint lenValue, out uint value)
    {
        var len = decode_unsigned(buffer, offset, lenValue, out value);
        return len;
    }

    public static int decode_enumerated<TEnum>(byte[] buffer, int offset, uint lenValue, out TEnum value)
    {
        var len = decode_enumerated(buffer, offset, lenValue, out var rawValue);
        value = (TEnum)(dynamic)rawValue;
        return len;
    }

    public static bool decode_is_context_tag(byte[] buffer, int offset, byte tagNumber)
    {
        decode_tag_number(buffer, offset, out var myTagNumber);
        return IS_CONTEXT_SPECIFIC(buffer[offset]) && myTagNumber == tagNumber;
    }

    public static bool decode_is_application_tag(byte[] buffer, int offset, BacnetApplicationTags tagNumber)
    {
        decode_tag_number(buffer, offset, out var myTagNumber);
        return IS_APPLICATION_TAG(buffer[offset]) && myTagNumber == (byte)tagNumber;
    }

    public static bool decode_is_opening_tag_number(byte[] buffer, int offset, byte tagNumber)
    {
        decode_tag_number(buffer, offset, out var myTagNumber);
        return IS_OPENING_TAG(buffer[offset]) && myTagNumber == tagNumber;
    }

    public static bool decode_is_closing_tag_number(byte[] buffer, int offset, byte tagNumber)
    {
        decode_tag_number(buffer, offset, out var myTagNumber);
        return IS_CLOSING_TAG(buffer[offset]) && myTagNumber == tagNumber;
    }

    public static bool decode_is_closing_tag(byte[] buffer, int offset)
    {
        return (buffer[offset] & 0x07) == 7;
    }

    public static bool decode_is_opening_tag(byte[] buffer, int offset)
    {
        return (buffer[offset] & 0x07) == 6;
    }

    public static int decode_tag_number_and_value(byte[] buffer, int offset, out byte tagNumber, out uint value)
    {
        var len = decode_tag_number(buffer, offset, out tagNumber);
        if (IS_EXTENDED_VALUE(buffer[offset]))
        {
            /* tagged as uint32_t */
            if (buffer[offset + len] == 255)
            {
                len++;
                len += decode_unsigned32(buffer, offset + len, out var value32);
                value = value32;
            }
            /* tagged as uint16_t */
            else if (buffer[offset + len] == 254)
            {
                len++;
                len += decode_unsigned16(buffer, offset + len, out var value16);
                value = value16;
            }
            /* no tag - must be uint8_t */
            else
            {
                value = buffer[offset + len];
                len++;
            }
        }
        else if (IS_OPENING_TAG(buffer[offset]))
        {
            value = 0;
        }
        else if (IS_CLOSING_TAG(buffer[offset]))
        {
            /* closing tag */
            value = 0;
        }
        else
        {
            /* small value */
            value = (uint)(buffer[offset] & 0x07);
        }

        return len;
    }

    public static int decode_tag_number_and_value<TTag, TValue>(byte[] buffer, int offset, out TTag tag, out TValue value)
    {
        var len = decode_tag_number_and_value(buffer, offset, out var rawByte, out var rawValue);
        tag = (TTag)(dynamic)rawByte;
        value = (TValue)(dynamic)rawValue;
        return len;
    }

    /// <summary>
    ///     This is used by the Active_COV_Subscriptions property in DEVICE
    /// </summary>
    public static void encode_cov_subscription(EncodeBuffer buffer, BacnetCOVSubscription value)
    {
        /* Recipient [0] BACnetRecipientProcess - opening */
        encode_opening_tag(buffer, 0);

        /*  recipient [0] BACnetRecipient - opening */
        encode_opening_tag(buffer, 0);
        /* CHOICE - device [0] BACnetObjectIdentifier - opening */
        /* CHOICE - address [1] BACnetAddress - opening */
        encode_opening_tag(buffer, 1);
        /* network-number Unsigned16, */
        /* -- A value of 0 indicates the local network */
        encode_application_unsigned(buffer, value.Recipient.net);
        /* mac-address OCTET STRING */
        /* -- A string of length 0 indicates a broadcast */
        if (value.Recipient.net == 0xFFFF)
            encode_application_octet_string(buffer, new byte[0], 0, 0);
        else
            encode_application_octet_string(buffer, value.Recipient.adr, 0, value.Recipient.adr.Length);
        /* CHOICE - address [1] BACnetAddress - closing */
        encode_closing_tag(buffer, 1);
        /*  recipient [0] BACnetRecipient - closing */
        encode_closing_tag(buffer, 0);

        /* processIdentifier [1] Unsigned32 */
        encode_context_unsigned(buffer, 1, value.subscriptionProcessIdentifier);
        /* Recipient [0] BACnetRecipientProcess - closing */
        encode_closing_tag(buffer, 0);

        /*  MonitoredPropertyReference [1] BACnetObjectPropertyReference, */
        encode_opening_tag(buffer, 1);
        /* objectIdentifier [0] */
        encode_context_object_id(buffer, 0, value.monitoredObjectIdentifier.type,
            value.monitoredObjectIdentifier.instance);
        /* propertyIdentifier [1] */
        /* FIXME: we are monitoring 2 properties! How to encode? */
        encode_context_enumerated(buffer, 1, value.monitoredProperty.propertyIdentifier);
        if (value.monitoredProperty.propertyArrayIndex != BACNET_ARRAY_ALL)
            encode_context_unsigned(buffer, 2, value.monitoredProperty.propertyArrayIndex);
        /* MonitoredPropertyReference [1] - closing */
        encode_closing_tag(buffer, 1);

        /* IssueConfirmedNotifications [2] BOOLEAN, */
        encode_context_boolean(buffer, 2, value.IssueConfirmedNotifications);
        /* TimeRemaining [3] Unsigned, */
        encode_context_unsigned(buffer, 3, value.TimeRemaining);
        /* COVIncrement [4] REAL OPTIONAL, */
        if (value.COVIncrement > 0)
            encode_context_real(buffer, 4, value.COVIncrement);
    }

    public static int decode_cov_subscription(byte[] buffer, int offset, int apduLen, out BacnetCOVSubscription value)
    {
        var len = 0;

        value = new BacnetCOVSubscription { Recipient = new BacnetAddress(BacnetAddressTypes.None, 0, null) };

        if (!decode_is_opening_tag_number(buffer, offset + len, 0))
            return -1;
        len++;
        if (!decode_is_opening_tag_number(buffer, offset + len, 0))
            return -1;
        len++;
        if (!decode_is_opening_tag_number(buffer, offset + len, 1))
            return -1;
        len++;
        len += decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
            return -1;
        len += decode_unsigned(buffer, offset + len, lenValueType, out var tmp);
        value.Recipient.net = (ushort)tmp;
        len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
            return -1;
        value.Recipient.adr = new byte[lenValueType];
        len += decode_octet_string(buffer, offset + len, apduLen, value.Recipient.adr, 0, lenValueType);
        if (!decode_is_closing_tag_number(buffer, offset + len, 1))
            return -1;
        len++;
        if (!decode_is_closing_tag_number(buffer, offset + len, 0))
            return -1;
        len++;

        len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber != 1)
            return -1;
        len += decode_unsigned(buffer, offset + len, lenValueType, out value.subscriptionProcessIdentifier);
        if (!decode_is_closing_tag_number(buffer, offset + len, 0))
            return -1;
        len++;

        if (!decode_is_opening_tag_number(buffer, offset + len, 1))
            return -1;
        len++;
        len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber != 0)
            return -1;
        len += decode_object_id(buffer, offset + len, out value.monitoredObjectIdentifier.type,
            out value.monitoredObjectIdentifier.instance);
        len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber != 1)
            return -1;
        len += decode_enumerated(buffer, offset + len, lenValueType,
            out value.monitoredProperty.propertyIdentifier);
        var tagLen = decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber == 2)
        {
            len += tagLen;
            len += decode_unsigned(buffer, offset + len, lenValueType,
                out value.monitoredProperty.propertyArrayIndex);
        }
        else
            value.monitoredProperty.propertyArrayIndex = BACNET_ARRAY_ALL;
        if (!decode_is_closing_tag_number(buffer, offset + len, 1))
            return -1;
        len++;

        len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber != 2)
            return -1;
        value.IssueConfirmedNotifications = buffer[offset + len] > 0;
        len++;

        len += decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
        if (tagNumber != 3)
            return -1;
        len += decode_unsigned(buffer, offset + len, lenValueType, out value.TimeRemaining);

        if (len < apduLen && !IS_CLOSING_TAG(buffer[offset + len]))
        {
            decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            if (tagNumber != 4)
                return len;
            len++;
            len += decode_real(buffer, offset + len, out value.COVIncrement);
        }

        return len;
    }

    public static int decode_context_property_state(byte[] buffer, int offset, byte tagNumber, out BacnetPropertyState value)
    {
        if (!decode_is_opening_tag_number(buffer, offset, tagNumber))
        {
            value = default;
            return -1;
        }

        var len = 1;
        var sectionLength = decode_property_state(buffer, offset + len, out value);
        if (sectionLength == -1)
            return -1;

        len += sectionLength;
        if (!decode_is_closing_tag_number(buffer, offset + len, tagNumber))
            return -1;

        return len + 1;
    }

    public static int decode_property_state(byte[] buffer, int offset, out BacnetPropertyState value)
    {
        value = default;

        var len = decode_tag_number_and_value(buffer, offset, out value.tag, out uint lenValueType);
        if (len == -1)
            return -1;

        var sectionLength = 0;
        switch (value.tag)
        {
            case BacnetPropertyState.BacnetPropertyStateTypes.BOOLEAN_VALUE:
                value.state.boolean_value = lenValueType == 1;
                sectionLength++;
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.BINARY_VALUE:
                sectionLength = decode_enumerated(buffer, offset + len, lenValueType, out value.state.binaryValue);
                if (sectionLength == -1)
                    return -1;
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.EVENT_TYPE:
                sectionLength = decode_enumerated(buffer, offset + len, lenValueType, out value.state.eventType);
                if (sectionLength == -1)
                    return -1;
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.STATE:
                sectionLength = decode_enumerated(buffer, offset + len, lenValueType, out value.state.state);
                if (sectionLength == -1)
                    return -1;
                break;

            case BacnetPropertyState.BacnetPropertyStateTypes.UNSIGNED_VALUE:
                sectionLength = decode_unsigned(buffer, offset + len, lenValueType, out value.state.unsignedValue);
                if (sectionLength == -1)
                    return -1;
                break;

            default:
                return -1;
        }

        return len + sectionLength;
    }

    public static int decode_context_bitstring(byte[] buffer, int offset, byte tagNumber, out BacnetBitString value)
    {
        if (!decode_is_context_tag(buffer, offset, tagNumber) || decode_is_closing_tag(buffer, offset))
        {
            value = new BacnetBitString();
            return -1; // BACNET_STATUS_ERROR
        }

        var len = decode_tag_number_and_value(buffer, offset, out _, out var lenValue);
        return len + decode_bitstring(buffer, offset + len, lenValue, out value);
    }

    public static int decode_context_real(byte[] buffer, int offset, byte tagNumber, out float value)
    {
        if (!decode_is_context_tag(buffer, offset, tagNumber))
        {
            value = 0;
            return -1;
        }

        var len = decode_tag_number_and_value(buffer, offset, out _, out _);
        return len + decode_real(buffer, offset + len, out value);
    }

    public static int decode_context_enumerated<TEnum>(byte[] buffer, int offset, byte tagNumber, out TEnum value)
    {
        if (!decode_is_context_tag(buffer, offset, tagNumber) || decode_is_closing_tag(buffer, offset))
        {
            value = default;
            return -1;
        }

        var len = decode_tag_number_and_value(buffer, offset, out _, out var lenValue);
        return len + decode_enumerated(buffer, offset + len, lenValue, out value);
    }

    public static int decode_context_unsigned(byte[] buffer, int offset, byte tagNumber, out uint value)
    {
        if (!decode_is_context_tag(buffer, offset, tagNumber) || decode_is_closing_tag(buffer, offset))
        {
            value = 0;
            return -1;
        }

        var len = decode_tag_number_and_value(buffer, offset, out _, out var lenValue);
        return len + decode_unsigned(buffer, offset + len, lenValue, out value);
    }
}
