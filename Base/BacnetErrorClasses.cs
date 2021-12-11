namespace System.IO.BACnet;

public enum BacnetErrorClasses
{
    ERROR_CLASS_DEVICE = 0,
    ERROR_CLASS_OBJECT = 1,
    ERROR_CLASS_PROPERTY = 2,
    ERROR_CLASS_RESOURCES = 3,
    ERROR_CLASS_SECURITY = 4,
    ERROR_CLASS_SERVICES = 5,
    ERROR_CLASS_VT = 6,
    ERROR_CLASS_COMMUNICATION = 7,
    /* Enumerated values 0-63 are reserved for definition by ASHRAE. */
    /* Enumerated values 64-65535 may be used by others subject to */
    /* the procedures and constraints described in Clause 23. */
    MAX_BACNET_ERROR_CLASS = 8,
    /* do the MAX here instead of outside of enum so that
       compilers will allocate adequate sized datatype for enum */
    ERROR_CLASS_PROPRIETARY_FIRST = 64,
    ERROR_CLASS_PROPRIETARY_LAST = 65535
}
