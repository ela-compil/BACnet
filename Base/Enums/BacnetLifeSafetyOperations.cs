namespace System.IO.BACnet;

public enum BacnetLifeSafetyOperations
{
    LIFE_SAFETY_OP_NONE = 0,
    LIFE_SAFETY_OP_SILENCE = 1,
    LIFE_SAFETY_OP_SILENCE_AUDIBLE = 2,
    LIFE_SAFETY_OP_SILENCE_VISUAL = 3,
    LIFE_SAFETY_OP_RESET = 4,
    LIFE_SAFETY_OP_RESET_ALARM = 5,
    LIFE_SAFETY_OP_RESET_FAULT = 6,
    LIFE_SAFETY_OP_UNSILENCE = 7,
    LIFE_SAFETY_OP_UNSILENCE_AUDIBLE = 8,
    LIFE_SAFETY_OP_UNSILENCE_VISUAL = 9,
    /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
    /* Enumerated values 64-65535 may be used by others subject to  */
    /* procedures and constraints described in Clause 23. */
    /* do the max range inside of enum so that
       compilers will allocate adequate sized datatype for enum
       which is used to store decoding */
    LIFE_SAFETY_OP_PROPRIETARY_MIN = 64,
    LIFE_SAFETY_OP_PROPRIETARY_MAX = 65535
}
