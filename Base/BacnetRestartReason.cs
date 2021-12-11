namespace System.IO.BACnet;

// From Loren Van Spronsen csharp-bacnet
public enum BacnetRestartReason
{
    UNKNOWN = 0,
    COLD_START = 1,
    WARM_START = 2,
    DETECTED_POWER_LOST = 3,
    DETECTED_POWER_OFF = 4,
    HARDWARE_WATCHDOG = 5,
    SOFTWARE_WATCHDOG = 6,
    SUSPENDED = 7
}
