namespace System.IO.BACnet;

public enum BacnetDeviceStatus : byte
{
    OPERATIONAL = 0,
    OPERATIONAL_READONLY = 1,
    DOWNLOAD_REQUIRED = 2,
    DOWNLOAD_IN_PROGRESS = 3,
    NON_OPERATIONAL = 4,
    BACKUP_IN_PROGRESS = 5
}
