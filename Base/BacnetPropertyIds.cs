namespace System.IO.BACnet;

public enum BacnetPropertyIds
{
    PROP_ACKED_TRANSITIONS = 0,
    PROP_ACK_REQUIRED = 1,
    PROP_ACTION = 2,
    PROP_ACTION_TEXT = 3,
    PROP_ACTIVE_TEXT = 4,
    PROP_ACTIVE_VT_SESSIONS = 5,
    PROP_ALARM_VALUE = 6,
    PROP_ALARM_VALUES = 7,
    PROP_ALL = 8,
    PROP_ALL_WRITES_SUCCESSFUL = 9,
    PROP_APDU_SEGMENT_TIMEOUT = 10,
    PROP_APDU_TIMEOUT = 11,
    PROP_APPLICATION_SOFTWARE_VERSION = 12,
    PROP_ARCHIVE = 13,
    PROP_BIAS = 14,
    PROP_CHANGE_OF_STATE_COUNT = 15,
    PROP_CHANGE_OF_STATE_TIME = 16,
    PROP_NOTIFICATION_CLASS = 17,
    PROP_BLANK_1 = 18,
    PROP_CONTROLLED_VARIABLE_REFERENCE = 19,
    PROP_CONTROLLED_VARIABLE_UNITS = 20,
    PROP_CONTROLLED_VARIABLE_VALUE = 21,
    PROP_COV_INCREMENT = 22,
    PROP_DATE_LIST = 23,
    PROP_DAYLIGHT_SAVINGS_STATUS = 24,
    PROP_DEADBAND = 25,
    PROP_DERIVATIVE_CONSTANT = 26,
    PROP_DERIVATIVE_CONSTANT_UNITS = 27,
    PROP_DESCRIPTION = 28,
    PROP_DESCRIPTION_OF_HALT = 29,
    PROP_DEVICE_ADDRESS_BINDING = 30,
    PROP_DEVICE_TYPE = 31,
    PROP_EFFECTIVE_PERIOD = 32,
    PROP_ELAPSED_ACTIVE_TIME = 33,
    PROP_ERROR_LIMIT = 34,
    PROP_EVENT_ENABLE = 35,
    PROP_EVENT_STATE = 36,
    PROP_EVENT_TYPE = 37,
    PROP_EXCEPTION_SCHEDULE = 38,
    PROP_FAULT_VALUES = 39,
    PROP_FEEDBACK_VALUE = 40,
    PROP_FILE_ACCESS_METHOD = 41,
    PROP_FILE_SIZE = 42,
    PROP_FILE_TYPE = 43,
    PROP_FIRMWARE_REVISION = 44,
    PROP_HIGH_LIMIT = 45,
    PROP_INACTIVE_TEXT = 46,
    PROP_IN_PROCESS = 47,
    PROP_INSTANCE_OF = 48,
    PROP_INTEGRAL_CONSTANT = 49,
    PROP_INTEGRAL_CONSTANT_UNITS = 50,
    PROP_ISSUE_CONFIRMED_NOTIFICATIONS = 51,
    PROP_LIMIT_ENABLE = 52,
    PROP_LIST_OF_GROUP_MEMBERS = 53,
    PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES = 54,
    PROP_LIST_OF_SESSION_KEYS = 55,
    PROP_LOCAL_DATE = 56,
    PROP_LOCAL_TIME = 57,
    PROP_LOCATION = 58,
    PROP_LOW_LIMIT = 59,
    PROP_MANIPULATED_VARIABLE_REFERENCE = 60,
    PROP_MAXIMUM_OUTPUT = 61,
    PROP_MAX_APDU_LENGTH_ACCEPTED = 62,
    PROP_MAX_INFO_FRAMES = 63,
    PROP_MAX_MASTER = 64,
    PROP_MAX_PRES_VALUE = 65,
    PROP_MINIMUM_OFF_TIME = 66,
    PROP_MINIMUM_ON_TIME = 67,
    PROP_MINIMUM_OUTPUT = 68,
    PROP_MIN_PRES_VALUE = 69,
    PROP_MODEL_NAME = 70,
    PROP_MODIFICATION_DATE = 71,
    PROP_NOTIFY_TYPE = 72,
    PROP_NUMBER_OF_APDU_RETRIES = 73,
    PROP_NUMBER_OF_STATES = 74,
    PROP_OBJECT_IDENTIFIER = 75,
    PROP_OBJECT_LIST = 76,
    PROP_OBJECT_NAME = 77,
    PROP_OBJECT_PROPERTY_REFERENCE = 78,
    PROP_OBJECT_TYPE = 79,
    PROP_OPTIONAL = 80,
    PROP_OUT_OF_SERVICE = 81,
    PROP_OUTPUT_UNITS = 82,
    PROP_EVENT_PARAMETERS = 83,
    PROP_POLARITY = 84,
    PROP_PRESENT_VALUE = 85,
    PROP_PRIORITY = 86,
    PROP_PRIORITY_ARRAY = 87,
    PROP_PRIORITY_FOR_WRITING = 88,
    PROP_PROCESS_IDENTIFIER = 89,
    PROP_PROGRAM_CHANGE = 90,
    PROP_PROGRAM_LOCATION = 91,
    PROP_PROGRAM_STATE = 92,
    PROP_PROPORTIONAL_CONSTANT = 93,
    PROP_PROPORTIONAL_CONSTANT_UNITS = 94,
    PROP_PROTOCOL_CONFORMANCE_CLASS = 95,       /* deleted in version 1 revision 2 */
    PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED = 96,
    PROP_PROTOCOL_SERVICES_SUPPORTED = 97,
    PROP_PROTOCOL_VERSION = 98,
    PROP_READ_ONLY = 99,
    PROP_REASON_FOR_HALT = 100,
    PROP_RECIPIENT = 101,
    PROP_RECIPIENT_LIST = 102,
    PROP_RELIABILITY = 103,
    PROP_RELINQUISH_DEFAULT = 104,
    PROP_REQUIRED = 105,
    PROP_RESOLUTION = 106,
    PROP_SEGMENTATION_SUPPORTED = 107,
    PROP_SETPOINT = 108,
    PROP_SETPOINT_REFERENCE = 109,
    PROP_STATE_TEXT = 110,
    PROP_STATUS_FLAGS = 111,
    PROP_SYSTEM_STATUS = 112,
    PROP_TIME_DELAY = 113,
    PROP_TIME_OF_ACTIVE_TIME_RESET = 114,
    PROP_TIME_OF_STATE_COUNT_RESET = 115,
    PROP_TIME_SYNCHRONIZATION_RECIPIENTS = 116,
    PROP_UNITS = 117,
    PROP_UPDATE_INTERVAL = 118,
    PROP_UTC_OFFSET = 119,
    PROP_VENDOR_IDENTIFIER = 120,
    PROP_VENDOR_NAME = 121,
    PROP_VT_CLASSES_SUPPORTED = 122,
    PROP_WEEKLY_SCHEDULE = 123,
    PROP_ATTEMPTED_SAMPLES = 124,
    PROP_AVERAGE_VALUE = 125,
    PROP_BUFFER_SIZE = 126,
    PROP_CLIENT_COV_INCREMENT = 127,
    PROP_COV_RESUBSCRIPTION_INTERVAL = 128,
    PROP_CURRENT_NOTIFY_TIME = 129,
    PROP_EVENT_TIME_STAMPS = 130,
    PROP_LOG_BUFFER = 131,
    PROP_LOG_DEVICE_OBJECT_PROPERTY = 132,
    /* The enable property is renamed from log-enable in
       Addendum b to ANSI/ASHRAE 135-2004(135b-2) */
    PROP_ENABLE = 133,
    PROP_LOG_INTERVAL = 134,
    PROP_MAXIMUM_VALUE = 135,
    PROP_MINIMUM_VALUE = 136,
    PROP_NOTIFICATION_THRESHOLD = 137,
    PROP_PREVIOUS_NOTIFY_TIME = 138,
    PROP_PROTOCOL_REVISION = 139,
    PROP_RECORDS_SINCE_NOTIFICATION = 140,
    PROP_RECORD_COUNT = 141,
    PROP_START_TIME = 142,
    PROP_STOP_TIME = 143,
    PROP_STOP_WHEN_FULL = 144,
    PROP_TOTAL_RECORD_COUNT = 145,
    PROP_VALID_SAMPLES = 146,
    PROP_WINDOW_INTERVAL = 147,
    PROP_WINDOW_SAMPLES = 148,
    PROP_MAXIMUM_VALUE_TIMESTAMP = 149,
    PROP_MINIMUM_VALUE_TIMESTAMP = 150,
    PROP_VARIANCE_VALUE = 151,
    PROP_ACTIVE_COV_SUBSCRIPTIONS = 152,
    PROP_BACKUP_FAILURE_TIMEOUT = 153,
    PROP_CONFIGURATION_FILES = 154,
    PROP_DATABASE_REVISION = 155,
    PROP_DIRECT_READING = 156,
    PROP_LAST_RESTORE_TIME = 157,
    PROP_MAINTENANCE_REQUIRED = 158,
    PROP_MEMBER_OF = 159,
    PROP_MODE = 160,
    PROP_OPERATION_EXPECTED = 161,
    PROP_SETTING = 162,
    PROP_SILENCED = 163,
    PROP_TRACKING_VALUE = 164,
    PROP_ZONE_MEMBERS = 165,
    PROP_LIFE_SAFETY_ALARM_VALUES = 166,
    PROP_MAX_SEGMENTS_ACCEPTED = 167,
    PROP_PROFILE_NAME = 168,
    PROP_AUTO_SLAVE_DISCOVERY = 169,
    PROP_MANUAL_SLAVE_ADDRESS_BINDING = 170,
    PROP_SLAVE_ADDRESS_BINDING = 171,
    PROP_SLAVE_PROXY_ENABLE = 172,
    PROP_LAST_NOTIFY_RECORD = 173,
    PROP_SCHEDULE_DEFAULT = 174,
    PROP_ACCEPTED_MODES = 175,
    PROP_ADJUST_VALUE = 176,
    PROP_COUNT = 177,
    PROP_COUNT_BEFORE_CHANGE = 178,
    PROP_COUNT_CHANGE_TIME = 179,
    PROP_COV_PERIOD = 180,
    PROP_INPUT_REFERENCE = 181,
    PROP_LIMIT_MONITORING_INTERVAL = 182,
    PROP_LOGGING_OBJECT = 183,
    PROP_LOGGING_RECORD = 184,
    PROP_PRESCALE = 185,
    PROP_PULSE_RATE = 186,
    PROP_SCALE = 187,
    PROP_SCALE_FACTOR = 188,
    PROP_UPDATE_TIME = 189,
    PROP_VALUE_BEFORE_CHANGE = 190,
    PROP_VALUE_SET = 191,
    PROP_VALUE_CHANGE_TIME = 192,
    /* enumerations 193-206 are new */
    PROP_ALIGN_INTERVALS = 193,
    /* enumeration 194 is unassigned */
    PROP_INTERVAL_OFFSET = 195,
    PROP_LAST_RESTART_REASON = 196,
    PROP_LOGGING_TYPE = 197,
    /* enumeration 198-201 is unassigned */
    PROP_RESTART_NOTIFICATION_RECIPIENTS = 202,
    PROP_TIME_OF_DEVICE_RESTART = 203,
    PROP_TIME_SYNCHRONIZATION_INTERVAL = 204,
    PROP_TRIGGER = 205,
    PROP_UTC_TIME_SYNCHRONIZATION_RECIPIENTS = 206,
    /* enumerations 207-211 are used in Addendum d to ANSI/ASHRAE 135-2004 */
    PROP_NODE_SUBTYPE = 207,
    PROP_NODE_TYPE = 208,
    PROP_STRUCTURED_OBJECT_LIST = 209,
    PROP_SUBORDINATE_ANNOTATIONS = 210,
    PROP_SUBORDINATE_LIST = 211,
    /* enumerations 212-225 are used in Addendum e to ANSI/ASHRAE 135-2004 */
    PROP_ACTUAL_SHED_LEVEL = 212,
    PROP_DUTY_WINDOW = 213,
    PROP_EXPECTED_SHED_LEVEL = 214,
    PROP_FULL_DUTY_BASELINE = 215,
    /* enumerations 216-217 are unassigned */
    /* enumerations 212-225 are used in Addendum e to ANSI/ASHRAE 135-2004 */
    PROP_REQUESTED_SHED_LEVEL = 218,
    PROP_SHED_DURATION = 219,
    PROP_SHED_LEVEL_DESCRIPTIONS = 220,
    PROP_SHED_LEVELS = 221,
    PROP_STATE_DESCRIPTION = 222,
    /* enumerations 223-225 are unassigned  */
    /* enumerations 226-235 are used in Addendum f to ANSI/ASHRAE 135-2004 */
    PROP_DOOR_ALARM_STATE = 226,
    PROP_DOOR_EXTENDED_PULSE_TIME = 227,
    PROP_DOOR_MEMBERS = 228,
    PROP_DOOR_OPEN_TOO_LONG_TIME = 229,
    PROP_DOOR_PULSE_TIME = 230,
    PROP_DOOR_STATUS = 231,
    PROP_DOOR_UNLOCK_DELAY_TIME = 232,
    PROP_LOCK_STATUS = 233,
    PROP_MASKED_ALARM_VALUES = 234,
    PROP_SECURED_STATUS = 235,
    /* enumerations 236-243 are unassigned  */
    /* enumerations 244-311 are used in Addendum j to ANSI/ASHRAE 135-2004 */
    PROP_ABSENTEE_LIMIT = 244,
    PROP_ACCESS_ALARM_EVENTS = 245,
    PROP_ACCESS_DOORS = 246,
    PROP_ACCESS_EVENT = 247,
    PROP_ACCESS_EVENT_AUTHENTICATION_FACTOR = 248,
    PROP_ACCESS_EVENT_CREDENTIAL = 249,
    PROP_ACCESS_EVENT_TIME = 250,
    PROP_ACCESS_TRANSACTION_EVENTS = 251,
    PROP_ACCOMPANIMENT = 252,
    PROP_ACCOMPANIMENT_TIME = 253,
    PROP_ACTIVATION_TIME = 254,
    PROP_ACTIVE_AUTHENTICATION_POLICY = 255,
    PROP_ASSIGNED_ACCESS_RIGHTS = 256,
    PROP_AUTHENTICATION_FACTORS = 257,
    PROP_AUTHENTICATION_POLICY_LIST = 258,
    PROP_AUTHENTICATION_POLICY_NAMES = 259,
    PROP_AUTHENTICATION_STATUS = 260,
    PROP_AUTHORIZATION_MODE = 261,
    PROP_BELONGS_TO = 262,
    PROP_CREDENTIAL_DISABLE = 263,
    PROP_CREDENTIAL_STATUS = 264,
    PROP_CREDENTIALS = 265,
    PROP_CREDENTIALS_IN_ZONE = 266,
    PROP_DAYS_REMAINING = 267,
    PROP_ENTRY_POINTS = 268,
    PROP_EXIT_POINTS = 269,
    PROP_EXPIRY_TIME = 270,
    PROP_EXTENDED_TIME_ENABLE = 271,
    PROP_FAILED_ATTEMPT_EVENTS = 272,
    PROP_FAILED_ATTEMPTS = 273,
    PROP_FAILED_ATTEMPTS_TIME = 274,
    PROP_LAST_ACCESS_EVENT = 275,
    PROP_LAST_ACCESS_POINT = 276,
    PROP_LAST_CREDENTIAL_ADDED = 277,
    PROP_LAST_CREDENTIAL_ADDED_TIME = 278,
    PROP_LAST_CREDENTIAL_REMOVED = 279,
    PROP_LAST_CREDENTIAL_REMOVED_TIME = 280,
    PROP_LAST_USE_TIME = 281,
    PROP_LOCKOUT = 282,
    PROP_LOCKOUT_RELINQUISH_TIME = 283,
    PROP_MASTER_EXEMPTION = 284,
    PROP_MAX_FAILED_ATTEMPTS = 285,
    PROP_MEMBERS = 286,
    PROP_MUSTER_POINT = 287,
    PROP_NEGATIVE_ACCESS_RULES = 288,
    PROP_NUMBER_OF_AUTHENTICATION_POLICIES = 289,
    PROP_OCCUPANCY_COUNT = 290,
    PROP_OCCUPANCY_COUNT_ADJUST = 291,
    PROP_OCCUPANCY_COUNT_ENABLE = 292,
    PROP_OCCUPANCY_EXEMPTION = 293,
    PROP_OCCUPANCY_LOWER_LIMIT = 294,
    PROP_OCCUPANCY_LOWER_LIMIT_ENFORCED = 295,
    PROP_OCCUPANCY_STATE = 296,
    PROP_OCCUPANCY_UPPER_LIMIT = 297,
    PROP_OCCUPANCY_UPPER_LIMIT_ENFORCED = 298,
    PROP_PASSBACK_EXEMPTION = 299,
    PROP_PASSBACK_MODE = 300,
    PROP_PASSBACK_TIMEOUT = 301,
    PROP_POSITIVE_ACCESS_RULES = 302,
    PROP_REASON_FOR_DISABLE = 303,
    PROP_SUPPORTED_FORMATS = 304,
    PROP_SUPPORTED_FORMAT_CLASSES = 305,
    PROP_THREAT_AUTHORITY = 306,
    PROP_THREAT_LEVEL = 307,
    PROP_TRACE_FLAG = 308,
    PROP_TRANSACTION_NOTIFICATION_CLASS = 309,
    PROP_USER_EXTERNAL_IDENTIFIER = 310,
    PROP_USER_INFORMATION_REFERENCE = 311,
    /* enumerations 312-316 are unassigned */
    PROP_USER_NAME = 317,
    PROP_USER_TYPE = 318,
    PROP_USES_REMAINING = 319,
    PROP_ZONE_FROM = 320,
    PROP_ZONE_TO = 321,
    PROP_ACCESS_EVENT_TAG = 322,
    PROP_GLOBAL_IDENTIFIER = 323,
    /* enumerations 324-325 are unassigned */
    PROP_VERIFICATION_TIME = 326,
    PROP_BASE_DEVICE_SECURITY_POLICY = 327,
    PROP_DISTRIBUTION_KEY_REVISION = 328,
    PROP_DO_NOT_HIDE = 329,
    PROP_KEY_SETS = 330,
    PROP_LAST_KEY_SERVER = 331,
    PROP_NETWORK_ACCESS_SECURITY_POLICIES = 332,
    PROP_PACKET_REORDER_TIME = 333,
    PROP_SECURITY_PDU_TIMEOUT = 334,
    PROP_SECURITY_TIME_WINDOW = 335,
    PROP_SUPPORTED_SECURITY_ALGORITHM = 336,
    PROP_UPDATE_KEY_SET_TIMEOUT = 337,
    PROP_BACKUP_AND_RESTORE_STATE = 338,
    PROP_BACKUP_PREPARATION_TIME = 339,
    PROP_RESTORE_COMPLETION_TIME = 340,
    PROP_RESTORE_PREPARATION_TIME = 341,
    /* enumerations 342-344 are defined in Addendum 2008-w */
    PROP_BIT_MASK = 342,
    PROP_BIT_TEXT = 343,
    PROP_IS_UTC = 344,
    PROP_GROUP_MEMBERS = 345,
    PROP_GROUP_MEMBER_NAMES = 346,
    PROP_MEMBER_STATUS_FLAGS = 347,
    PROP_REQUESTED_UPDATE_INTERVAL = 348,
    PROP_COVU_PERIOD = 349,
    PROP_COVU_RECIPIENTS = 350,
    PROP_EVENT_MESSAGE_TEXTS = 351,
    /* enumerations 352-363 are defined in Addendum 2010-af */
    PROP_EVENT_MESSAGE_TEXTS_CONFIG = 352,
    PROP_EVENT_DETECTION_ENABLE = 353,
    PROP_EVENT_ALGORITHM_INHIBIT = 354,
    PROP_EVENT_ALGORITHM_INHIBIT_REF = 355,
    PROP_TIME_DELAY_NORMAL = 356,
    PROP_RELIABILITY_EVALUATION_INHIBIT = 357,
    PROP_FAULT_PARAMETERS = 358,
    PROP_FAULT_TYPE = 359,
    PROP_LOCAL_FORWARDING_ONLY = 360,
    PROP_PROCESS_IDENTIFIER_FILTER = 361,
    PROP_SUBSCRIBED_RECIPIENTS = 362,
    PROP_PORT_FILTER = 363,
    /* enumeration 364 is defined in Addendum 2010-ae */
    PROP_AUTHORIZATION_EXEMPTIONS = 364,
    /* enumerations 365-370 are defined in Addendum 2010-aa */
    PROP_ALLOW_GROUP_DELAY_INHIBIT = 365,
    PROP_CHANNEL_NUMBER = 366,
    PROP_CONTROL_GROUPS = 367,
    PROP_EXECUTION_DELAY = 368,
    PROP_LAST_PRIORITY = 369,
    PROP_WRITE_STATUS = 370,
    /* enumeration 371 is defined in Addendum 2010-ao */
    PROP_PROPERTY_LIST = 371,
    /* enumeration 372 is defined in Addendum 2010-ak */
    PROP_SERIAL_NUMBER = 372,
    /* enumerations 373-386 are defined in Addendum 2010-i */
    PROP_BLINK_WARN_ENABLE = 373,
    PROP_DEFAULT_FADE_TIME = 374,
    PROP_DEFAULT_RAMP_RATE = 375,
    PROP_DEFAULT_STEP_INCREMENT = 376,
    PROP_EGRESS_TIME = 377,
    PROP_IN_PROGRESS = 378,
    PROP_INSTANTANEOUS_POWER = 379,
    PROP_LIGHTING_COMMAND = 380,
    PROP_LIGHTING_COMMAND_DEFAULT_PRIORITY = 381,
    PROP_MAX_ACTUAL_VALUE = 382,
    PROP_MIN_ACTUAL_VALUE = 383,
    PROP_POWER = 384,
    PROP_TRANSITION = 385,
    PROP_EGRESS_ACTIVE = 386,
    PROP_INTERFACE_VALUE = 387,
    PROP_FAULT_HIGH_LIMIT = 388,
    PROP_FAULT_LOW_LIMIT = 389,
    PROP_LOW_DIFF_LIMIT = 390,
    /* enumerations 391-392 are defined in Addendum 135-2012az */
    PROP_STRIKE_COUNT = 391,
    PROP_TIME_OF_STRIKE_COUNT_RESET = 392,
    /* enumerations 393-398 are defined in Addendum 135-2012ay */
    PROP_DEFAULT_TIMEOUT = 393,
    PROP_INITIAL_TIMEOUT = 394,
    PROP_LAST_STATE_CHANGE = 395,
    PROP_STATE_CHANGE_VALUES = 396,
    PROP_TIMER_RUNNING = 397,
    PROP_TIMER_STATE = 398,
    /* enumerations 399-427 are defined in Addendum 2012-ai */
    PROP_APDU_LENGTH = 399,
    PROP_IP_ADDRESS = 400,
    PROP_IP_DEFAULT_GATEWAY = 401,
    PROP_IP_DHCP_ENABLE = 402,
    PROP_IP_DHCP_LEASE_TIME = 403,
    PROP_IP_DHCP_LEASE_TIME_REMAINING = 404,
    PROP_IP_DHCP_SERVER = 405,
    PROP_IP_DNS_SERVER = 406,
    PROP_BACNET_IP_GLOBAL_ADDRESS = 407,
    PROP_BACNET_IP_MODE = 408,
    PROP_BACNET_IP_MULTICAST_ADDRESS = 409,
    PROP_BACNET_IP_NAT_TRAVERSAL = 410,
    PROP_IP_SUBNET_MASK = 411,
    PROP_BACNET_IP_UDP_PORT = 412,
    PROP_BBMD_ACCEPT_FD_REGISTRATIONS = 413,
    PROP_BBMD_BROADCAST_DISTRIBUTION_TABLE = 414,
    PROP_BBMD_FOREIGN_DEVICE_TABLE = 415,
    PROP_CHANGES_PENDING = 416,
    PROP_COMMAND = 417,
    PROP_FD_BBMD_ADDRESS = 418,
    PROP_FD_SUBSCRIPTION_LIFETIME = 419,
    PROP_LINK_SPEED = 420,
    PROP_LINK_SPEEDS = 421,
    PROP_LINK_SPEED_AUTONEGOTIATE = 422,
    PROP_MAC_ADDRESS = 423,
    PROP_NETWORK_INTERFACE_NAME = 424,
    PROP_NETWORK_NUMBER = 425,
    PROP_NETWORK_NUMBER_QUALITY = 426,
    PROP_NETWORK_TYPE = 427,
    PROP_ROUTING_TABLE = 428,
    PROP_VIRTUAL_MAC_ADDRESS_TABLE = 429,

    // Addendum-135-2012as
    PROP_COMMAND_TIME_ARRAY = 430,
    PROP_CURRENT_COMMAND_PRIORITY = 431,
    PROP_LAST_COMMAND_TIME = 432,
    PROP_VALUE_SOURCE = 433,
    PROP_VALUE_SOURCE_ARRAY = 434,
    PROP_BACNET_IPV6_MODE = 435,
    PROP_IPV6_ADDRESS = 436,
    PROP_IPV6_PREFIX_LENGTH = 437,
    PROP_BACNET_IPV6_UDP_PORT = 438,
    PROP_IPV6_DEFAULT_GATEWAY = 439,
    PROP_BACNET_IPV6_MULTICAST_ADDRESS = 440,
    PROP_IPV6_DNS_SERVER = 441,
    PROP_IPV6_AUTO_ADDRESSING_ENABLE = 442,
    PROP_IPV6_DHCP_LEASE_TIME = 443,
    PROP_IPV6_DHCP_LEASE_TIME_REMAINING = 444,
    PROP_IPV6_DHCP_SERVER = 445,
    PROP_IPV6_ZONE_INDEX = 446,
    PROP_ASSIGNED_LANDING_CALLS = 447,
    PROP_CAR_ASSIGNED_DIRECTION = 448,
    PROP_CAR_DOOR_COMMAND = 449,
    PROP_CAR_DOOR_STATUS = 450,
    PROP_CAR_DOOR_TEXT = 451,
    PROP_CAR_DOOR_ZONE = 452,
    PROP_CAR_DRIVE_STATUS = 453,
    PROP_CAR_LOAD = 454,
    PROP_CAR_LOAD_UNITS = 455,
    PROP_CAR_MODE = 456,
    PROP_CAR_MOVING_DIRECTION = 457,
    PROP_CAR_POSITION = 458,
    PROP_ELEVATOR_GROUP = 459,
    PROP_ENERGY_METER = 460,
    PROP_ENERGY_METER_REF = 461,
    PROP_ESCALATOR_MODE = 462,
    PROP_FAULT_SIGNALS = 463,
    PROP_FLOOR_TEXT = 464,
    PROP_GROUP_ID = 465,
    /* value 466 is unassigned */
    PROP_GROUP_MODE = 467,
    PROP_HIGHER_DECK = 468,
    PROP_INSTALLATION_ID = 469,
    PROP_LANDING_CALLS = 470,
    PROP_LANDING_CALL_CONTROL = 471,
    PROP_LANDING_DOOR_STATUS = 472,
    PROP_LOWER_DECK = 473,
    PROP_MACHINE_ROOM_ID = 474,
    PROP_MAKING_CAR_CALL = 475,
    PROP_NEXT_STOPPING_FLOOR = 476,
    PROP_OPERATION_DIRECTION = 477,
    PROP_PASSENGER_ALARM = 478,
    PROP_POWER_MODE = 479,
    PROP_REGISTERED_CAR_CALL = 480,
    PROP_ACTIVE_COV_MULTIPLE_SUBSCRIPTIONS = 481,
    PROP_PROTOCOL_LEVEL = 482,
    PROP_REFERENCE_PORT = 483,
    PROP_DEPLOYED_PROFILE_LOCATION = 484,
    PROP_PROFILE_LOCATION = 485,
    PROP_TAGS = 486,
    PROP_SUBORDINATE_NODE_TYPES = 487,
    PROP_SUBORDINATE_TAGS = 488,
    PROP_SUBORDINATE_RELATIONSHIPS = 489,
    PROP_DEFAULT_SUBORDINATE_RELATIONSHIP = 490,
    PROP_REPRESENTS = 491,
    PROP_DEFAULT_PRESENT_VALUE = 492,
    PROP_PRESENT_STAGE = 493,
    PROP_STAGES = 494,
    PROP_STAGE_NAMES = 495,
    PROP_TARGET_REFERENCES = 496,
    PROP_AUDIT_SOURCE_LEVEL = 497,
    PROP_AUDIT_LEVEL = 498,
    PROP_AUDIT_NOTIFICATION_RECIPIENT = 499,
    PROP_AUDIT_PRIORITY_FILTER = 500,
    PROP_AUDITABLE_OPERATIONS = 501,
    PROP_DELETE_ON_FORWARD = 502,
    PROP_MAXIMUM_SEND_DELAY = 503,
    PROP_MONITORED_OBJECTS = 504,
    PROP_SEND_NOW = 505,
    PROP_FLOOR_NUMBER = 506,
    PROP_DEVICE_UUID = 507,
    /* enumerations 508-511 are defined in Addendum 2020cc */
    PROP_ADDITIONAL_REFERENCE_PORTS = 508,
    PROP_CERTIFICATE_SIGNING_REQUEST_FILE = 509,
    PROP_COMMAND_VALIDATION_RESULT = 510,
    PROP_ISSUER_CERTIFICATE_FILES = 511,
    /* The special property identifiers all, optional, and required  */
    /* are reserved for use in the ReadPropertyConditional and */
    /* ReadPropertyMultiple services or services not defined in this standard.
     */
    /* Enumerated values 0-511 are reserved for definition by ASHRAE.  */
    /* Enumerated values 512-4194303 may be used by others subject to the  */
    /* procedures and constraints described in Clause 23.  */
    PROP_PROPRIETARY_RANGE_MIN = 512,
    PROP_PROPRIETARY_RANGE_MAX = 4194303,
    /* enumerations 4194304-4194327 are defined in Addendum 2020cc */
    PROP_MAX_BVLC_LENGTH_ACCEPTED = 4194304,
    PROP_MAX_NPDU_LENGTH_ACCEPTED = 4194305,
    PROP_OPERATIONAL_CERTIFICATE_FILE = 4194305,
    PROP_CURRENT_HEALTH = 4194307,
    PROP_SC_CONNECT_WAIT_TIMEOUT = 4194308,
    PROP_SC_DIRECT_CONNECT_ACCEPT_ENABLE = 4194309,
    PROP_SC_DIRECT_CONNECT_ACCEPT_URIS = 4194310,
    PROP_SC_DIRECT_CONNECT_BINDING = 4194311,
    PROP_SC_DIRECT_CONNECT_CONNECTION_STATUS = 4194312,
    PROP_SC_DIRECT_CONNECT_INITIATE_ENABLE = 4194313,
    PROP_SC_DISCONNECT_WAIT_TIMEOUT = 4194314,
    PROP_SC_FAILED_CONNECTION_REQUESTS = 4194315,
    PROP_SC_FAILOVER_HUB_CONNECTION_STATUS = 4194316,
    PROP_SC_FAILOVER_HUB_URI = 4194317,
    PROP_SC_HUB_CONNECTOR_STATE = 4194318,
    PROP_SC_HUB_FUNCTION_ACCEPT_URIS = 4194319,
    PROP_SC_HUB_FUNCTION_BINDING = 4194320,
    PROP_SC_HUB_FUNCTION_CONNECTION_STATUS = 4194321,
    PROP_SC_HUB_FUNCTION_ENABLE = 4194322,
    PROP_SC_HEARTBEAT_TIMEOUT = 4194323,
    PROP_SC_PRIMARY_HUB_CONNECTION_STATUS = 4194324,
    PROP_SC_PRIMARY_HUB_URI = 4194325,
    PROP_SC_MAXIMUM_RECONNECT_TIME = 4194326,
    PROP_SC_MINIMUM_RECONNECT_TIME = 4194327,
    /* enumerations 4194328-4194332 are defined in Addendum 2020ca */
    PROP_COLOR_OVERRIDE = 4194328,
    PROP_COLOR_REFERENCE = 4194329,
    PROP_DEFAULT_COLOR = 4194330,
    PROP_DEFAULT_COLOR_TEMPERATURE = 4194331,
    PROP_OVERRIDE_COLOR_REFERENCE = 4194332,
    PROP_COLOR_COMMAND = 4194334,
    PROP_HIGH_END_TRIM = 4194335,
    PROP_LOW_END_TRIM = 4194336,
    PROP_TRIM_FADE_TIME = 4194337,

    /* The special property identifiers all, optional, and required  */
    /* are reserved for use in the ReadPropertyConditional and */
    /* ReadPropertyMultiple services or services not defined in this standard. */
    /* Enumerated values 0-511 are reserved for definition by ASHRAE.  */
    /* Enumerated values 512-4194303 may be used by others subject to the  */
    /* procedures and constraints described in Clause 23.  */
    /* Enumerated values 4194303-16777215 are reserved
       for definition by ASHRAE.  */
    /* do the max range inside of enum so that
       compilers will allocate adequate sized datatype for enum
       which is used to store decoding */
    MAX_BACNET_PROPERTY_ID = 16777215
}
