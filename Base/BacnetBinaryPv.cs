namespace System.IO.BACnet.Base;

public enum BacnetBinaryPv : byte
{
    MIN_BINARY_PV = 0,  /* for validating incoming values */
    BINARY_INACTIVE = 0,
    BINARY_ACTIVE = 1,
    MAX_BINARY_PV = 1,  /* for validating incoming values */
    BINARY_NULL = 255   /* our homemade way of storing this info */
}
