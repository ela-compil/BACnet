namespace System.IO.BACnet;

[Flags]
public enum BacnetStatusFlags
{
    STATUS_FLAG_IN_ALARM = 1,
    STATUS_FLAG_FAULT = 2,
    STATUS_FLAG_OVERRIDDEN = 4,
    STATUS_FLAG_OUT_OF_SERVICE = 8
}
