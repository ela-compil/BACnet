namespace System.IO.BACnet;

public enum BacnetPtpDisconnectReasons : byte
{
    PTP_DISCONNECT_NO_MORE_DATA = 0,
    PTP_DISCONNECT_PREEMPTED = 1,
    PTP_DISCONNECT_INVALID_PASSWORD = 2,
    PTP_DISCONNECT_OTHER = 3,
}
