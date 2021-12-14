namespace System.IO.BACnet;

public struct BacnetLogRecord
{
    public DateTime timestamp;

    /* logDatum: CHOICE { */
    public BacnetTrendLogValueType type;
    //private BacnetBitString log_status;
    //private bool boolean_value;
    //private float real_value;
    //private uint enum_value;
    //private uint unsigned_value;
    //private int signed_value;
    //private BacnetBitString bitstring_value;
    //private bool null_value;
    //private BacnetError failure;
    //private float time_change;
    private object any_value;
    /* } */

    public BacnetBitString statusFlags;

    public BacnetLogRecord(BacnetTrendLogValueType type, object value, DateTime stamp, uint status)
    {
        this.type = type;
        timestamp = stamp;
        statusFlags = BacnetBitString.ConvertFromInt(status);
        any_value = null;
        Value = value;
    }

    public object Value
    {
        get
        {
            switch (type)
            {
                case BacnetTrendLogValueType.TL_TYPE_ANY:
                    return any_value;
                case BacnetTrendLogValueType.TL_TYPE_BITS:
                    return (BacnetBitString)Convert.ChangeType(any_value, typeof(BacnetBitString));
                case BacnetTrendLogValueType.TL_TYPE_BOOL:
                    return (bool)Convert.ChangeType(any_value, typeof(bool));
                case BacnetTrendLogValueType.TL_TYPE_DELTA:
                    return (float)Convert.ChangeType(any_value, typeof(float));
                case BacnetTrendLogValueType.TL_TYPE_ENUM:
                    return (uint)Convert.ChangeType(any_value, typeof(uint));
                case BacnetTrendLogValueType.TL_TYPE_ERROR:
                    if (any_value != null)
                        return (BacnetError)Convert.ChangeType(any_value, typeof(BacnetError));
                    else
                        return new BacnetError(BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                case BacnetTrendLogValueType.TL_TYPE_NULL:
                    return null;
                case BacnetTrendLogValueType.TL_TYPE_REAL:
                    return (float)Convert.ChangeType(any_value, typeof(float));
                case BacnetTrendLogValueType.TL_TYPE_SIGN:
                    return (int)Convert.ChangeType(any_value, typeof(int));
                case BacnetTrendLogValueType.TL_TYPE_STATUS:
                    return (BacnetBitString)Convert.ChangeType(any_value, typeof(BacnetBitString));
                case BacnetTrendLogValueType.TL_TYPE_UNSIGN:
                    return (uint)Convert.ChangeType(any_value, typeof(uint));
                default:
                    throw new NotSupportedException();
            }
        }
        set
        {
            switch (type)
            {
                case BacnetTrendLogValueType.TL_TYPE_ANY:
                    any_value = value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_BITS:
                    if (value == null) value = new BacnetBitString();
                    if (value.GetType() != typeof(BacnetBitString))
                        value = BacnetBitString.ConvertFromInt((uint)Convert.ChangeType(value, typeof(uint)));
                    any_value = (BacnetBitString)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_BOOL:
                    if (value == null) value = false;
                    if (value.GetType() != typeof(bool))
                        value = (bool)Convert.ChangeType(value, typeof(bool));
                    any_value = (bool)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_DELTA:
                    if (value == null) value = (float)0;
                    if (value.GetType() != typeof(float))
                        value = (float)Convert.ChangeType(value, typeof(float));
                    any_value = (float)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_ENUM:
                    if (value == null) value = (uint)0;
                    if (value.GetType() != typeof(uint))
                        value = (uint)Convert.ChangeType(value, typeof(uint));
                    any_value = (uint)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_ERROR:
                    if (value == null) value = new BacnetError();
                    if (value.GetType() != typeof(BacnetError))
                        throw new ArgumentException();
                    any_value = (BacnetError)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_NULL:
                    if (value != null) throw new ArgumentException();
                    any_value = value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_REAL:
                    if (value == null) value = (float)0;
                    if (value.GetType() != typeof(float))
                        value = (float)Convert.ChangeType(value, typeof(float));
                    any_value = (float)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_SIGN:
                    if (value == null) value = 0;
                    if (value.GetType() != typeof(int))
                        value = (int)Convert.ChangeType(value, typeof(int));
                    any_value = (int)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_STATUS:
                    if (value == null) value = new BacnetBitString();
                    if (value.GetType() != typeof(BacnetBitString))
                        value = BacnetBitString.ConvertFromInt((uint)Convert.ChangeType(value, typeof(uint)));
                    any_value = (BacnetBitString)value;
                    break;
                case BacnetTrendLogValueType.TL_TYPE_UNSIGN:
                    if (value == null) value = (uint)0;
                    if (value.GetType() != typeof(uint))
                        value = (uint)Convert.ChangeType(value, typeof(uint));
                    any_value = (uint)value;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public T GetValue<T>()
    {
        return (T)Convert.ChangeType(Value, typeof(T));
    }
}
