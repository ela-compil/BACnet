namespace System.IO.BACnet;

public enum BacnetEventTypes
{
    EVENT_CHANGE_OF_BITSTRING = 0,
    EVENT_CHANGE_OF_STATE = 1,
    EVENT_CHANGE_OF_VALUE = 2,
    EVENT_COMMAND_FAILURE = 3,
    EVENT_FLOATING_LIMIT = 4,
    EVENT_OUT_OF_RANGE = 5,
    /*  complex-event-type        (6), -- see comment below */
    /*  event-buffer-ready   (7), -- context tag 7 is deprecated */
    EVENT_CHANGE_OF_LIFE_SAFETY = 8,
    EVENT_EXTENDED = 9,
    EVENT_BUFFER_READY = 10,
    EVENT_UNSIGNED_RANGE = 11,
    /* -- enumeration value 12 is reserved for future addenda */
    EVENT_ACCESS_EVENT = 13,
    EVENT_DOUBLE_OUT_OF_RANGE = 14,
    EVENT_SIGNED_OUT_OF_RANGE = 15,
    EVENT_UNSIGNED_OUT_OF_RANGE = 16,
    EVENT_CHANGE_OF_CHARACTERSTRING = 17,
    EVENT_CHANGE_OF_STATUS_FLAGS = 18,
    EVENT_CHANGE_OF_RELIABILITY = 19,
    EVENT_NONE = 20,
    EVENT_CHANGE_OF_DISCRETE_VALUE = 21,
    EVENT_CHANGE_OF_TIMER = 22,
    /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
    /* Enumerated values 64-65535 may be used by others subject to  */
    /* the procedures and constraints described in Clause 23.  */
    /* It is expected that these enumerated values will correspond to  */
    /* the use of the complex-event-type CHOICE [6] of the  */
    /* BACnetNotificationParameters production. */
    /* The last enumeration used in this version is 11. */
    /* do the max range inside of enum so that
       compilers will allocate adequate sized datatype for enum
       which is used to store decoding */
    EVENT_PROPRIETARY_MIN = 64,
    EVENT_PROPRIETARY_MAX = 65535
}
