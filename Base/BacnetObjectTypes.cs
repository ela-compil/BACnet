namespace System.IO.BACnet;

public enum BacnetObjectTypes : uint
{
    OBJECT_ANALOG_INPUT = 0,
    OBJECT_ANALOG_OUTPUT = 1,
    OBJECT_ANALOG_VALUE = 2,
    OBJECT_BINARY_INPUT = 3,
    OBJECT_BINARY_OUTPUT = 4,
    OBJECT_BINARY_VALUE = 5,
    OBJECT_CALENDAR = 6,
    OBJECT_COMMAND = 7,
    OBJECT_DEVICE = 8,
    OBJECT_EVENT_ENROLLMENT = 9,
    OBJECT_FILE = 10,
    OBJECT_GROUP = 11,
    OBJECT_LOOP = 12,
    OBJECT_MULTI_STATE_INPUT = 13,
    OBJECT_MULTI_STATE_OUTPUT = 14,
    OBJECT_NOTIFICATION_CLASS = 15,
    OBJECT_PROGRAM = 16,
    OBJECT_SCHEDULE = 17,
    OBJECT_AVERAGING = 18,
    OBJECT_MULTI_STATE_VALUE = 19,
    OBJECT_TRENDLOG = 20,
    OBJECT_LIFE_SAFETY_POINT = 21,
    OBJECT_LIFE_SAFETY_ZONE = 22,
    OBJECT_ACCUMULATOR = 23,
    OBJECT_PULSE_CONVERTER = 24,
    OBJECT_EVENT_LOG = 25,
    OBJECT_GLOBAL_GROUP = 26,
    OBJECT_TREND_LOG_MULTIPLE = 27,
    OBJECT_LOAD_CONTROL = 28,
    OBJECT_STRUCTURED_VIEW = 29,
    OBJECT_ACCESS_DOOR = 30,
    OBJECT_TIMER = 31,                  /* Addendum 135-2012ay */
    OBJECT_ACCESS_CREDENTIAL = 32,      /* Addendum 2008-j */
    OBJECT_ACCESS_POINT = 33,
    OBJECT_ACCESS_RIGHTS = 34,
    OBJECT_ACCESS_USER = 35,
    OBJECT_ACCESS_ZONE = 36,
    OBJECT_CREDENTIAL_DATA_INPUT = 37,  /* authentication-factor-input */
    OBJECT_NETWORK_SECURITY = 38,       /* Addendum 2008-g */
    OBJECT_BITSTRING_VALUE = 39,        /* Addendum 2008-w */
    OBJECT_CHARACTERSTRING_VALUE = 40,  /* Addendum 2008-w */
    OBJECT_DATE_PATTERN_VALUE = 41,     /* Addendum 2008-w */
    OBJECT_DATE_VALUE = 42,             /* Addendum 2008-w */
    OBJECT_DATETIME_PATTERN_VALUE = 43, /* Addendum 2008-w */
    OBJECT_DATETIME_VALUE = 44,         /* Addendum 2008-w */
    OBJECT_INTEGER_VALUE = 45,          /* Addendum 2008-w */
    OBJECT_LARGE_ANALOG_VALUE = 46,     /* Addendum 2008-w */
    OBJECT_OCTETSTRING_VALUE = 47,      /* Addendum 2008-w */
    OBJECT_POSITIVE_INTEGER_VALUE = 48, /* Addendum 2008-w */
    OBJECT_TIME_PATTERN_VALUE = 49,     /* Addendum 2008-w */
    OBJECT_TIME_VALUE = 50,             /* Addendum 2008-w */
    OBJECT_NOTIFICATION_FORWARDER = 51, /* Addendum 2010-af */
    OBJECT_ALERT_ENROLLMENT = 52,       /* Addendum 2010-af */
    OBJECT_CHANNEL = 53,                /* Addendum 2010-aa */
    OBJECT_LIGHTING_OUTPUT = 54,        /* Addendum 2010-i */
    OBJECT_BINARY_LIGHTING_OUTPUT = 55, /* Addendum 135-2012az */
    OBJECT_NETWORK_PORT = 56,           /* Addendum 135-2012az */
    OBJECT_ELEVATOR_GROUP = 57,         /* Addendum 135-2012aq */
    OBJECT_ESCALATOR = 58,              /* Addendum 135-2012aq */
    OBJECT_LIFT = 59,                   /* Addendum 135-2012aq */
    OBJECT_STAGING = 60,                /* Addendum 135-2016bd */
    OBJECT_AUDIT_LOG = 61,              /* Addendum 135-2016bi */
    OBJECT_AUDIT_REPORTER = 62,         /* Addendum 135-2016bi */
    OBJECT_COLOR = 63,                  /* Addendum 135-2020ca */
    OBJECT_COLOR_TEMPERATURE = 64,      /* Addendum 135-2020ca */

    /* Enumerated values 0-127 are reserved for definition by ASHRAE. */
    /* Enumerated values 128-1023 may be used by others subject to  */
    /* the procedures and constraints described in Clause 23. */
    /* do the max range inside of enum so that
       compilers will allocate adequate sized datatype for enum
       which is used to store decoding */

    OBJECT_PROPRIETARY_MIN = 128,
    OBJECT_PROPRIETARY_MAX = 1023,
    MAX_BACNET_OBJECT_TYPE = 1024,
    MAX_ASHRAE_OBJECT_TYPE = 65
}
