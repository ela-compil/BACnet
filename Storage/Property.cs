namespace System.IO.BACnet.Storage;

[Serializable]
public class Property
{
    [XmlIgnore]
    public BacnetPropertyIds Id { get; set; }

    [XmlAttribute("Id")]
    public string IdText
    {
        get
        {
            return Id.ToString();
        }
        set
        {
            Id = (BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), value);
        }
    }

    [XmlAttribute]
    public BacnetApplicationTags Tag { get; set; }

    [XmlElement]
    public string[] Value { get; set; }

    public static BacnetValue DeserializeValue(string value, BacnetApplicationTags type)
    {
        switch (type)
        {
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                return value == ""
                    ? new BacnetValue(type, null)
                    : new BacnetValue(value);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                return new BacnetValue(type, bool.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                return new BacnetValue(type, uint.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                return new BacnetValue(type, int.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                return new BacnetValue(type, float.Parse(value, CultureInfo.InvariantCulture));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                return new BacnetValue(type, double.Parse(value, CultureInfo.InvariantCulture));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                try
                {
                    return new BacnetValue(type, Convert.FromBase64String(value));
                }
                catch
                {
                    return new BacnetValue(type, value);
                }
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                try
                {
                    return new BacnetValue(type, Convert.FromBase64String(value));
                }
                catch
                {
                    return new BacnetValue(type, value);
                }
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                return new BacnetValue(type, value);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                return new BacnetValue(type, BacnetBitString.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                return new BacnetValue(type, uint.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                // Format: yyyy/MM/dd (bacnet-stack compatible); date patterns are stored as base64
                try
                {
                    return new BacnetValue(type, DateTime.ParseExact(value, "yyyy/MM/dd", CultureInfo.InvariantCulture));
                }
                catch (FormatException)
                {
                    return DeserializeEncoded(type, value, new BacnetDate());
                }
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                // Format: HH:mm:ss.hh where hh = hundredths (0-99) (bacnet-stack compatible);
                // partially-wildcarded times are stored as base64
                try
                {
                    return new BacnetValue(type, DateTime.ParseExact(value, "HH:mm:ss.ff", CultureInfo.InvariantCulture));
                }
                catch (FormatException)
                {
                    return DeserializeEncoded(type, value, new BacnetTime());
                }
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME:
                // Format: yyyy/MM/dd-HH:mm:ss.hh where hh = hundredths (0-99) (bacnet-stack compatible)
                return new BacnetValue(type, DateTime.ParseExact(value, "yyyy/MM/dd-HH:mm:ss.ff", CultureInfo.InvariantCulture));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                return new BacnetValue(type, BacnetObjectId.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION:
                return new BacnetValue(type, BacnetReadAccessSpecification.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE:
                return new BacnetValue(type, BacnetDeviceObjectPropertyReference.Parse(value));
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_WEEKLY_SCHEDULE:
                return DeserializeEncoded(type, value, new BacnetDailySchedule());
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_SPECIAL_EVENT:
                return DeserializeEncoded(type, value, new BacnetSpecialEvent());
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_CALENDAR_ENTRY:
                return DeserializeEncoded(type, value, new BacnetCalendarEntry());
            default:
                return new BacnetValue(type, null);
        }
    }

    private static string SerializeEncoded(ASN1.IEncode value)
    {
        var buffer = new EncodeBuffer();
        value.Encode(buffer);
        return Convert.ToBase64String(buffer.ToArray());
    }

    // schedule values are stored as base64 of their ASN.1 encoding, like octet strings
    private static BacnetValue DeserializeEncoded(BacnetApplicationTags type, string value, ASN1.IDecode target)
    {
        var bytes = Convert.FromBase64String(value);
        return target.Decode(bytes, 0, (uint)bytes.Length) < 0
            ? new BacnetValue(type, null)
            : new BacnetValue(type, target);
    }

    public static string SerializeValue(BacnetValue value, BacnetApplicationTags type)
    {
        switch (type)
        {
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                return value.ToString(); // Modif FC
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                // Convert.ToSingle handles a value boxed as any numeric type (avoids an
                // InvalidCastException when e.g. a double is boxed under a REAL tag).
                return Convert.ToSingle(value.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                return Convert.ToDouble(value.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                return Convert.ToUInt32(value.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                return Convert.ToBase64String((byte[])value.Value);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                {
                    return value.Value is byte[]? Convert.ToBase64String((byte[])value.Value)
                        : string.Join(";", ((BacnetValue[])value.Value)
                            .Select(v => SerializeValue(v, v.Tag)));
                }
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                if (value.Value is BacnetDate bacnetDate)
                    return SerializeEncoded(bacnetDate); // a date pattern has no textual form
                // Format: yyyy/MM/dd (bacnet-stack compatible)
                return ((DateTime)value.Value).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                if (value.Value is BacnetTime bacnetTime)
                    return SerializeEncoded(bacnetTime); // a partially-wildcarded time has no textual form
                // Format: HH:mm:ss.hh where hh = hundredths (0-99) (bacnet-stack compatible)
                return ((DateTime)value.Value).ToString("HH:mm:ss.", CultureInfo.InvariantCulture)
                    + (((DateTime)value.Value).Millisecond / 10).ToString("D2", CultureInfo.InvariantCulture);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME:
                // Format: yyyy/MM/dd-HH:mm:ss.hh where hh = hundredths (0-99) (bacnet-stack compatible)
                return ((DateTime)value.Value).ToString("yyyy/MM/dd-HH:mm:ss.", CultureInfo.InvariantCulture)
                    + (((DateTime)value.Value).Millisecond / 10).ToString("D2", CultureInfo.InvariantCulture);
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_WEEKLY_SCHEDULE:
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_SPECIAL_EVENT:
            case BacnetApplicationTags.BACNET_APPLICATION_TAG_CALENDAR_ENTRY:
                return SerializeEncoded((ASN1.IEncode)value.Value);
            default:
                return value.Value.ToString();
        }
    }

    [XmlIgnore]
    public IList<BacnetValue> BacnetValue
    {
        get
        {
            if (Value == null)
                return new BacnetValue[0];

            var ret = new BacnetValue[Value.Length];
            for (var i = 0; i < ret.Length; i++)
                ret[i] = DeserializeValue(Value[i], Tag);

            return ret;
        }
        set
        {
            Value = value.Select(v => SerializeValue(v, Tag)).ToArray();
        }
    }
}
