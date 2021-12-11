namespace System.IO.BACnet.Base;

public enum BacnetProgramError : ushort
{
    PROGRAM_ERROR_NORMAL = 0,
    PROGRAM_ERROR_LOAD_FAILED = 1,
    PROGRAM_ERROR_INTERNAL = 2,
    PROGRAM_ERROR_PROGRAM = 3,
    PROGRAM_ERROR_OTHER = 4,
    /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
    /* Enumerated values 64-65535 may be used by others subject to  */
    /* the procedures and constraints described in Clause 23. */
    /* do the max range inside of enum so that
       compilers will allocate adequate sized datatype for enum
       which is used to store decoding */
    PROGRAM_ERROR_PROPRIETARY_MIN = 64,
    PROGRAM_ERROR_PROPRIETARY_MAX = 65535
}
