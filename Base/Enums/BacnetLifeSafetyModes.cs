namespace System.IO.BACnet;

public enum BacnetLifeSafetyModes
{
    MIN_LIFE_SAFETY_MODE = 0,
    LIFE_SAFETY_MODE_OFF = 0,
    LIFE_SAFETY_MODE_ON = 1,
    LIFE_SAFETY_MODE_TEST = 2,
    LIFE_SAFETY_MODE_MANNED = 3,
    LIFE_SAFETY_MODE_UNMANNED = 4,
    LIFE_SAFETY_MODE_ARMED = 5,
    LIFE_SAFETY_MODE_DISARMED = 6,
    LIFE_SAFETY_MODE_PREARMED = 7,
    LIFE_SAFETY_MODE_SLOW = 8,
    LIFE_SAFETY_MODE_FAST = 9,
    LIFE_SAFETY_MODE_DISCONNECTED = 10,
    LIFE_SAFETY_MODE_ENABLED = 11,
    LIFE_SAFETY_MODE_DISABLED = 12,
    LIFE_SAFETY_MODE_AUTOMATIC_RELEASE_DISABLED = 13,
    LIFE_SAFETY_MODE_DEFAULT = 14,
    MAX_LIFE_SAFETY_MODE = 15,
    /* Enumerated values 0-255 are reserved for definition by ASHRAE.  */
    /* Enumerated values 256-65535 may be used by others subject to  */
    /* procedures and constraints described in Clause 23. */
    /* do the max range inside of enum so that
       compilers will allocate adequate sized datatype for enum
       which is used to store decoding */
    LIFE_SAFETY_MODE_PROPRIETARY_MIN = 256,
    LIFE_SAFETY_MODE_PROPRIETARY_MAX = 65535
}
