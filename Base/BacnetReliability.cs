namespace System.IO.BACnet;

public enum BacnetReliability : uint
{
    RELIABILITY_NO_FAULT_DETECTED = 0,
    RELIABILITY_NO_SENSOR = 1,
    RELIABILITY_OVER_RANGE = 2,
    RELIABILITY_UNDER_RANGE = 3,
    RELIABILITY_OPEN_LOOP = 4,
    RELIABILITY_SHORTED_LOOP = 5,
    RELIABILITY_NO_OUTPUT = 6,
    RELIABILITY_UNRELIABLE_OTHER = 7,
    RELIABILITY_PROCESS_ERROR = 8,
    RELIABILITY_MULTI_STATE_FAULT = 9,
    RELIABILITY_CONFIGURATION_ERROR = 10,
    RELIABILITY_MEMBER_FAULT = 11,
    RELIABILITY_COMMUNICATION_FAILURE = 12,
    RELIABILITY_TRIPPED = 13,
    /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
    /* Enumerated values 64-65535 may be used by others subject to  */
    /* the procedures and constraints described in Clause 23. */
    /* do the max range inside of enum so that
       compilers will allocate adequate sized datatype for enum
       which is used to store decoding */
    RELIABILITY_PROPRIETARY_MIN = 64,
    RELIABILITY_PROPRIETARY_MAX = 65535
}
