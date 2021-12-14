namespace System.IO.BACnet;

public enum BacnetBackupState
{
    IDLE = 0,
    PREPARING_FOR_BACKUP = 1,
    PREPARING_FOR_RESTORE = 2,
    PERFORMING_A_BACKUP = 3,
    PERFORMING_A_RESTORE = 4
}
