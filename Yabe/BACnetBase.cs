/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

/*
 * Ported from project at http://bacnet.sourceforge.net/
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO.BACnet
{
    /* note: these are not the real values, */
    /* but are shifted left for easy encoding */
    [Flags]
    public enum BACNET_PDU_TYPE : byte
    {
        PDU_TYPE_CONFIRMED_SERVICE_REQUEST = 0,
        SERVER_ABORT = 1,
        SEGMENTED_RESPONSE_ACCEPTED = 2,
        MORE_FOLLOWS = 4,
        SEGMENTED_MESSAGE = 8,
        PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST = 0x10,
        PDU_TYPE_SIMPLE_ACK = 0x20,
        PDU_TYPE_COMPLEX_ACK = 0x30,
        PDU_TYPE_SEGMENT_ACK = 0x40,
        PDU_TYPE_ERROR = 0x50,
        PDU_TYPE_REJECT = 0x60,
        PDU_TYPE_ABORT = 0x70,
        PDU_TYPE_MASK = 0xF0,
    };

    public enum BACNET_SEGMENTATION
    {
        SEGMENTATION_BOTH = 0,
        SEGMENTATION_TRANSMIT = 1,
        SEGMENTATION_RECEIVE = 2,
        SEGMENTATION_NONE = 3,
        MAX_BACNET_SEGMENTATION = 4
    };

    public enum BACNET_DEVICE_STATUS
    {
        STATUS_OPERATIONAL = 0,
        STATUS_OPERATIONAL_READ_ONLY = 1,
        STATUS_DOWNLOAD_REQUIRED = 2,
        STATUS_DOWNLOAD_IN_PROGRESS = 3,
        STATUS_NON_OPERATIONAL = 4,
        STATUS_BACKUP_IN_PROGRESS = 5,
        MAX_DEVICE_STATUS = 6
    };

    [Flags]
    public enum BACNET_STATUS_FLAGS
    {
        STATUS_FLAG_IN_ALARM = 1,
        STATUS_FLAG_FAULT = 2,
        STATUS_FLAG_OVERRIDDEN = 4,
        STATUS_FLAG_OUT_OF_SERVICE = 8,
    };

    public enum BACNET_SERVICES_SUPPORTED
    {
        /* Alarm and Event Services */
        SERVICE_SUPPORTED_ACKNOWLEDGE_ALARM = 0,
        SERVICE_SUPPORTED_CONFIRMED_COV_NOTIFICATION = 1,
        SERVICE_SUPPORTED_CONFIRMED_EVENT_NOTIFICATION = 2,
        SERVICE_SUPPORTED_GET_ALARM_SUMMARY = 3,
        SERVICE_SUPPORTED_GET_ENROLLMENT_SUMMARY = 4,
        SERVICE_SUPPORTED_GET_EVENT_INFORMATION = 39,
        SERVICE_SUPPORTED_SUBSCRIBE_COV = 5,
        SERVICE_SUPPORTED_SUBSCRIBE_COV_PROPERTY = 38,
        SERVICE_SUPPORTED_LIFE_SAFETY_OPERATION = 37,
        /* File Access Services */
        SERVICE_SUPPORTED_ATOMIC_READ_FILE = 6,
        SERVICE_SUPPORTED_ATOMIC_WRITE_FILE = 7,
        /* Object Access Services */
        SERVICE_SUPPORTED_ADD_LIST_ELEMENT = 8,
        SERVICE_SUPPORTED_REMOVE_LIST_ELEMENT = 9,
        SERVICE_SUPPORTED_CREATE_OBJECT = 10,
        SERVICE_SUPPORTED_DELETE_OBJECT = 11,
        SERVICE_SUPPORTED_READ_PROPERTY = 12,
        SERVICE_SUPPORTED_READ_PROP_CONDITIONAL = 13,
        SERVICE_SUPPORTED_READ_PROP_MULTIPLE = 14,
        SERVICE_SUPPORTED_READ_RANGE = 35,
        SERVICE_SUPPORTED_WRITE_PROPERTY = 15,
        SERVICE_SUPPORTED_WRITE_PROP_MULTIPLE = 16,
        SERVICE_SUPPORTED_WRITE_GROUP = 40,
        /* Remote Device Management Services */
        SERVICE_SUPPORTED_DEVICE_COMMUNICATION_CONTROL = 17,
        SERVICE_SUPPORTED_PRIVATE_TRANSFER = 18,
        SERVICE_SUPPORTED_TEXT_MESSAGE = 19,
        SERVICE_SUPPORTED_REINITIALIZE_DEVICE = 20,
        /* Virtual Terminal Services */
        SERVICE_SUPPORTED_VT_OPEN = 21,
        SERVICE_SUPPORTED_VT_CLOSE = 22,
        SERVICE_SUPPORTED_VT_DATA = 23,
        /* Security Services */
        SERVICE_SUPPORTED_AUTHENTICATE = 24,
        SERVICE_SUPPORTED_REQUEST_KEY = 25,
        SERVICE_SUPPORTED_I_AM = 26,
        SERVICE_SUPPORTED_I_HAVE = 27,
        SERVICE_SUPPORTED_UNCONFIRMED_COV_NOTIFICATION = 28,
        SERVICE_SUPPORTED_UNCONFIRMED_EVENT_NOTIFICATION = 29,
        SERVICE_SUPPORTED_UNCONFIRMED_PRIVATE_TRANSFER = 30,
        SERVICE_SUPPORTED_UNCONFIRMED_TEXT_MESSAGE = 31,
        SERVICE_SUPPORTED_TIME_SYNCHRONIZATION = 32,
        SERVICE_SUPPORTED_UTC_TIME_SYNCHRONIZATION = 36,
        SERVICE_SUPPORTED_WHO_HAS = 33,
        SERVICE_SUPPORTED_WHO_IS = 34,
        /* Other services to be added as they are defined. */
        /* All values in this production are reserved */
        /* for definition by ASHRAE. */
        MAX_BACNET_SERVICES_SUPPORTED = 41,
    };

    public enum BACNET_UNCONFIRMED_SERVICE : byte
    {
        SERVICE_UNCONFIRMED_I_AM = 0,
        SERVICE_UNCONFIRMED_I_HAVE = 1,
        SERVICE_UNCONFIRMED_COV_NOTIFICATION = 2,
        SERVICE_UNCONFIRMED_EVENT_NOTIFICATION = 3,
        SERVICE_UNCONFIRMED_PRIVATE_TRANSFER = 4,
        SERVICE_UNCONFIRMED_TEXT_MESSAGE = 5,
        SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION = 6,
        SERVICE_UNCONFIRMED_WHO_HAS = 7,
        SERVICE_UNCONFIRMED_WHO_IS = 8,
        SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION = 9,
        /* addendum 2010-aa */
        SERVICE_UNCONFIRMED_WRITE_GROUP = 10,
        /* Other services to be added as they are defined. */
        /* All choice values in this production are reserved */
        /* for definition by ASHRAE. */
        /* Proprietary extensions are made by using the */
        /* UnconfirmedPrivateTransfer service. See Clause 23. */
        MAX_BACNET_UNCONFIRMED_SERVICE = 11,
    };

    public enum BACNET_CONFIRMED_SERVICE : byte
    {
        /* Alarm and Event Services */
        SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM = 0,
        SERVICE_CONFIRMED_COV_NOTIFICATION = 1,
        SERVICE_CONFIRMED_EVENT_NOTIFICATION = 2,
        SERVICE_CONFIRMED_GET_ALARM_SUMMARY = 3,
        SERVICE_CONFIRMED_GET_ENROLLMENT_SUMMARY = 4,
        SERVICE_CONFIRMED_GET_EVENT_INFORMATION = 29,
        SERVICE_CONFIRMED_SUBSCRIBE_COV = 5,
        SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY = 28,
        SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION = 27,
        /* File Access Services */
        SERVICE_CONFIRMED_ATOMIC_READ_FILE = 6,
        SERVICE_CONFIRMED_ATOMIC_WRITE_FILE = 7,
        /* Object Access Services */
        SERVICE_CONFIRMED_ADD_LIST_ELEMENT = 8,
        SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT = 9,
        SERVICE_CONFIRMED_CREATE_OBJECT = 10,
        SERVICE_CONFIRMED_DELETE_OBJECT = 11,
        SERVICE_CONFIRMED_READ_PROPERTY = 12,
        SERVICE_CONFIRMED_READ_PROP_CONDITIONAL = 13,
        SERVICE_CONFIRMED_READ_PROP_MULTIPLE = 14,
        SERVICE_CONFIRMED_READ_RANGE = 26,
        SERVICE_CONFIRMED_WRITE_PROPERTY = 15,
        SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE = 16,
        /* Remote Device Management Services */
        SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL = 17,
        SERVICE_CONFIRMED_PRIVATE_TRANSFER = 18,
        SERVICE_CONFIRMED_TEXT_MESSAGE = 19,
        SERVICE_CONFIRMED_REINITIALIZE_DEVICE = 20,
        /* Virtual Terminal Services */
        SERVICE_CONFIRMED_VT_OPEN = 21,
        SERVICE_CONFIRMED_VT_CLOSE = 22,
        SERVICE_CONFIRMED_VT_DATA = 23,
        /* Security Services */
        SERVICE_CONFIRMED_AUTHENTICATE = 24,
        SERVICE_CONFIRMED_REQUEST_KEY = 25,
        /* Services added after 1995 */
        /* readRange (26) see Object Access Services */
        /* lifeSafetyOperation (27) see Alarm and Event Services */
        /* subscribeCOVProperty (28) see Alarm and Event Services */
        /* getEventInformation (29) see Alarm and Event Services */
        MAX_BACNET_CONFIRMED_SERVICE = 30
    };

    public enum BACNET_MAX_SEGMENTS : byte
    {
        MAX_SEG0 = 0,
        MAX_SEG2 = 0x10,
        MAX_SEG4 = 0x20,
        MAX_SEG8 = 0x30,
        MAX_SEG16 = 0x40,
        MAX_SEG32 = 0x50,
        MAX_SEG64 = 0x60,
        MAX_SEG65 = 0x70,
    };

    public enum BACNET_MAX_ADPU : byte
    {
        MAX_APDU50 = 0,
        MAX_APDU128 = 1,
        MAX_APDU206 = 2,
        MAX_APDU480 = 3,
        MAX_APDU1024 = 4,
        MAX_APDU1476 = 5,
    };

    public enum BACNET_OBJECT_TYPE
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
        /* 31 was lighting output, but BACnet editor changed it... */
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
        OBJECT_DATE_VALUE = 42,     /* Addendum 2008-w */
        OBJECT_DATETIME_PATTERN_VALUE = 43, /* Addendum 2008-w */
        OBJECT_DATETIME_VALUE = 44, /* Addendum 2008-w */
        OBJECT_INTEGER_VALUE = 45,  /* Addendum 2008-w */
        OBJECT_LARGE_ANALOG_VALUE = 46,     /* Addendum 2008-w */
        OBJECT_OCTETSTRING_VALUE = 47,      /* Addendum 2008-w */
        OBJECT_POSITIVE_INTEGER_VALUE = 48, /* Addendum 2008-w */
        OBJECT_TIME_PATTERN_VALUE = 49,     /* Addendum 2008-w */
        OBJECT_TIME_VALUE = 50,     /* Addendum 2008-w */
        OBJECT_NOTIFICATION_FORWARDER = 51, /* Addendum 2010-af */
        OBJECT_ALERT_ENROLLMENT = 52,       /* Addendum 2010-af */
        OBJECT_CHANNEL = 53,        /* Addendum 2010-aa */
        OBJECT_LIGHTING_OUTPUT = 54,        /* Addendum 2010-i */
        /* Enumerated values 0-127 are reserved for definition by ASHRAE. */
        /* Enumerated values 128-1023 may be used by others subject to  */
        /* the procedures and constraints described in Clause 23. */
        /* do the max range inside of enum so that
           compilers will allocate adequate sized datatype for enum
           which is used to store decoding */
        OBJECT_PROPRIETARY_MIN = 128,
        OBJECT_PROPRIETARY_MAX = 1023,
        MAX_BACNET_OBJECT_TYPE = 1024,
        MAX_ASHRAE_OBJECT_TYPE = 55,
    };

    public enum BACNET_APPLICATION_TAG
    {
        BACNET_APPLICATION_TAG_NULL = 0,
        BACNET_APPLICATION_TAG_BOOLEAN = 1,
        BACNET_APPLICATION_TAG_UNSIGNED_INT = 2,
        BACNET_APPLICATION_TAG_SIGNED_INT = 3,
        BACNET_APPLICATION_TAG_REAL = 4,
        BACNET_APPLICATION_TAG_DOUBLE = 5,
        BACNET_APPLICATION_TAG_OCTET_STRING = 6,
        BACNET_APPLICATION_TAG_CHARACTER_STRING = 7,
        BACNET_APPLICATION_TAG_BIT_STRING = 8,
        BACNET_APPLICATION_TAG_ENUMERATED = 9,
        BACNET_APPLICATION_TAG_DATE = 10,
        BACNET_APPLICATION_TAG_TIME = 11,
        BACNET_APPLICATION_TAG_OBJECT_ID = 12,
        BACNET_APPLICATION_TAG_RESERVE1 = 13,
        BACNET_APPLICATION_TAG_RESERVE2 = 14,
        BACNET_APPLICATION_TAG_RESERVE3 = 15,
        MAX_BACNET_APPLICATION_TAG = 16,

        /* Extra stuff - complex tagged data - not specifically enumerated */

        /* Means : "nothing", an empty list, not even a null character */
        BACNET_APPLICATION_TAG_EMPTYLIST,
        /* BACnetWeeknday */
        BACNET_APPLICATION_TAG_WEEKNDAY,
        /* BACnetDateRange */
        BACNET_APPLICATION_TAG_DATERANGE,
        /* BACnetDateTime */
        BACNET_APPLICATION_TAG_DATETIME,
        /* BACnetTimeStamp */
        BACNET_APPLICATION_TAG_TIMESTAMP,
        /* Error Class, Error Code */
        BACNET_APPLICATION_TAG_ERROR,
        /* BACnetDeviceObjectPropertyReference */
        BACNET_APPLICATION_TAG_DEVICE_OBJECT_PROPERTY_REFERENCE,
        /* BACnetDeviceObjectReference */
        BACNET_APPLICATION_TAG_DEVICE_OBJECT_REFERENCE,
        /* BACnetObjectPropertyReference */
        BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE,
        /* BACnetDestination (Recipient_List) */
        BACNET_APPLICATION_TAG_DESTINATION,
        /* BACnetRecipient */
        BACNET_APPLICATION_TAG_RECIPIENT,
        /* BACnetCOVSubscription */
        BACNET_APPLICATION_TAG_COV_SUBSCRIPTION,
        /* BACnetCalendarEntry */
        BACNET_APPLICATION_TAG_CALENDAR_ENTRY,
        /* BACnetWeeklySchedule */
        BACNET_APPLICATION_TAG_WEEKLY_SCHEDULE,
        /* BACnetSpecialEvent */
        BACNET_APPLICATION_TAG_SPECIAL_EVENT,
        /* BACnetReadAccessSpecification */
        BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION,
        /* BACnetLightingCommand */
        BACNET_APPLICATION_TAG_LIGHTING_COMMAND,
        BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC,
    };

    public enum BACNET_CHARACTER_STRING_ENCODING
    {
        CHARACTER_ANSI_X34 = 0,     /* deprecated */
        CHARACTER_UTF8 = 0,
        CHARACTER_MS_DBCS = 1,
        CHARACTER_JISC_6226 = 2,
        CHARACTER_UCS4 = 3,
        CHARACTER_UCS2 = 4,
        CHARACTER_ISO8859 = 5,
        MAX_CHARACTER_STRING_ENCODING = 6
    };

    public struct BACNET_PROPERTY_STATE
    {
        public enum BACNET_PROPERTY_STATE_TYPE
        {
            BOOLEAN_VALUE,
            BINARY_VALUE,
            EVENT_TYPE,
            POLARITY,
            PROGRAM_CHANGE,
            PROGRAM_STATE,
            REASON_FOR_HALT,
            RELIABILITY,
            STATE,
            SYSTEM_STATUS,
            UNITS,
            UNSIGNED_VALUE,
            LIFE_SAFETY_MODE,
            LIFE_SAFETY_STATE
        } ;

        public BACNET_PROPERTY_STATE_TYPE tag;
        public uint state;
    } ;

    public struct BACNET_DEVICE_OBJECT_PROPERTY_REFERENCE
    {
        public BACNET_OBJECT_ID objectIdentifier;
        public uint propertyIdentifier;
        public UInt32 arrayIndex;
        public BACNET_OBJECT_ID deviceIndentifier;
    } ;

    public struct BACNET_EVENT_NOTIFICATION_DATA
    {
        public UInt32 processIdentifier;
        public BACNET_OBJECT_ID initiatingObjectIdentifier;
        public BACNET_OBJECT_ID eventObjectIdentifier;
        public BACNET_GENERIC_TIME timeStamp;
        public UInt32 notificationClass;
        public byte priority;
        public BACNET_EVENT_TYPE eventType;
        public string messageText;       /* OPTIONAL - Set to NULL if not being used */
        public BACNET_NOTIFY_TYPE notifyType;
        public bool ackRequired;
        public BACNET_EVENT_STATE fromState;
        public BACNET_EVENT_STATE toState;

        public enum BACNET_EVENT_STATE
        {
            EVENT_STATE_NORMAL = 0,
            EVENT_STATE_FAULT = 1,
            EVENT_STATE_OFFNORMAL = 2,
            EVENT_STATE_HIGH_LIMIT = 3,
            EVENT_STATE_LOW_LIMIT = 4
        } ;

        public enum BACNET_NOTIFY_TYPE
        {
            NOTIFY_ALARM = 0,
            NOTIFY_EVENT = 1,
            NOTIFY_ACK_NOTIFICATION = 2
        } ;

        public enum BACNET_EVENT_TYPE
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
        };

        public enum CHANGE_OF_VALUE_TYPE
        {
            CHANGE_OF_VALUE_BITS,
            CHANGE_OF_VALUE_REAL
        } ;

        public enum BACNET_LIFE_SAFETY_STATE
        {
            MIN_LIFE_SAFETY_STATE = 0,
            LIFE_SAFETY_STATE_QUIET = 0,
            LIFE_SAFETY_STATE_PRE_ALARM = 1,
            LIFE_SAFETY_STATE_ALARM = 2,
            LIFE_SAFETY_STATE_FAULT = 3,
            LIFE_SAFETY_STATE_FAULT_PRE_ALARM = 4,
            LIFE_SAFETY_STATE_FAULT_ALARM = 5,
            LIFE_SAFETY_STATE_NOT_READY = 6,
            LIFE_SAFETY_STATE_ACTIVE = 7,
            LIFE_SAFETY_STATE_TAMPER = 8,
            LIFE_SAFETY_STATE_TEST_ALARM = 9,
            LIFE_SAFETY_STATE_TEST_ACTIVE = 10,
            LIFE_SAFETY_STATE_TEST_FAULT = 11,
            LIFE_SAFETY_STATE_TEST_FAULT_ALARM = 12,
            LIFE_SAFETY_STATE_HOLDUP = 13,
            LIFE_SAFETY_STATE_DURESS = 14,
            LIFE_SAFETY_STATE_TAMPER_ALARM = 15,
            LIFE_SAFETY_STATE_ABNORMAL = 16,
            LIFE_SAFETY_STATE_EMERGENCY_POWER = 17,
            LIFE_SAFETY_STATE_DELAYED = 18,
            LIFE_SAFETY_STATE_BLOCKED = 19,
            LIFE_SAFETY_STATE_LOCAL_ALARM = 20,
            LIFE_SAFETY_STATE_GENERAL_ALARM = 21,
            LIFE_SAFETY_STATE_SUPERVISORY = 22,
            LIFE_SAFETY_STATE_TEST_SUPERVISORY = 23,
            MAX_LIFE_SAFETY_STATE = 24,
            /* Enumerated values 0-255 are reserved for definition by ASHRAE.  */
            /* Enumerated values 256-65535 may be used by others subject to  */
            /* procedures and constraints described in Clause 23. */
            /* do the max range inside of enum so that
               compilers will allocate adequate sized datatype for enum
               which is used to store decoding */
            LIFE_SAFETY_STATE_PROPRIETARY_MIN = 256,
            LIFE_SAFETY_STATE_PROPRIETARY_MAX = 65535
        } ;

        public enum BACNET_LIFE_SAFETY_MODE
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
        } ;

        public enum BACNET_LIFE_SAFETY_OPERATION
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
        } ;

        /*
         ** Each of these structures in the union maps to a particular eventtype
         ** Based on BACnetNotificationParameters
         */

        /*
         ** EVENT_CHANGE_OF_BITSTRING
         */
        public BACNET_BIT_STRING changeOfBitstring_referencedBitString;
        public BACNET_BIT_STRING changeOfBitstring_statusFlags;
        /*
         ** EVENT_CHANGE_OF_STATE
         */
        public BACNET_PROPERTY_STATE changeOfState_newState;
        public BACNET_BIT_STRING changeOfState_statusFlags;
        /*
         ** EVENT_CHANGE_OF_VALUE
         */
        public BACNET_BIT_STRING changeOfValue_changedBits;
        public float changeOfValue_changeValue;
        public CHANGE_OF_VALUE_TYPE changeOfValue_tag;
        public BACNET_BIT_STRING changeOfValue_statusFlags;
        /*
         ** EVENT_COMMAND_FAILURE
         **
         ** Not Supported!
         */
        /*
         ** EVENT_FLOATING_LIMIT
         */
        public float floatingLimit_referenceValue;
        public BACNET_BIT_STRING floatingLimit_statusFlags;
        public float floatingLimit_setPointValue;
        public float floatingLimit_errorLimit;
        /*
         ** EVENT_OUT_OF_RANGE
         */
        public float outOfRange_exceedingValue;
        public BACNET_BIT_STRING outOfRange_statusFlags;
        public float outOfRange_deadband;
        public float outOfRange_exceededLimit;
        /*
         ** EVENT_CHANGE_OF_LIFE_SAFETY
         */
        public BACNET_LIFE_SAFETY_STATE changeOfLifeSafety_newState;
        public BACNET_LIFE_SAFETY_MODE changeOfLifeSafety_newMode;
        public BACNET_BIT_STRING changeOfLifeSafety_statusFlags;
        public BACNET_LIFE_SAFETY_OPERATION changeOfLifeSafety_operationExpected;
        /*
         ** EVENT_EXTENDED
         **
         ** Not Supported!
         */
        /*
         ** EVENT_BUFFER_READY
         */
        public BACNET_DEVICE_OBJECT_PROPERTY_REFERENCE bufferReady_bufferProperty;
        public uint bufferReady_previousNotification;
        public uint bufferReady_currentNotification;
        /*
         ** EVENT_UNSIGNED_RANGE
         */
        public uint unsignedRange_exceedingValue;
        public BACNET_BIT_STRING unsignedRange_statusFlags;
        public uint unsignedRange_exceededLimit;
    };

    public struct BACNET_VALUE
    {
        public BACNET_APPLICATION_TAG Tag;
        public object Value;
        public BACNET_VALUE(BACNET_APPLICATION_TAG tag, object value)
        {
            this.Tag = tag;
            this.Value = value;
        }
        public BACNET_VALUE(object value)
        {
            this.Value = value;
            Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL;

            //guess at the tag
            if (value != null)
            {
                Type t = value.GetType();
                if (t == typeof(string))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CHARACTER_STRING;
                else if (t == typeof(int) || t == typeof(short) || t == typeof(byte))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_SIGNED_INT;
                else if (t == typeof(uint) || t == typeof(ushort) || t == typeof(sbyte))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                else if (t == typeof(bool))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN;
                else if (t == typeof(float))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL;
                else if (t == typeof(double))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DOUBLE;
                else if (t == typeof(BACNET_BIT_STRING))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BIT_STRING;
                else if (t == typeof(BACNET_OBJECT_ID))
                    Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID;
                else
                    throw new NotImplementedException("This type (" + t.Name + ") is not yet implemented");
            }
        }
        public override string ToString()
        {
            if (Value == null)
                return "";
            else if (Value.GetType() == typeof(byte[]))
            {
                string ret = "";
                byte[] tmp = (byte[])Value;
                foreach (byte b in tmp)
                    ret += b.ToString("X2");
                return ret;
            }
            else 
                return Value.ToString();
        }
    }

    public struct BACNET_OBJECT_ID
    {
        public BACNET_OBJECT_TYPE type;
        public UInt32 instance;
        public BACNET_OBJECT_ID(BACNET_OBJECT_TYPE type, UInt32 instance)
        {
            this.type = type;
            this.instance = instance;
        }
        public override string ToString()
        {
            return type.ToString() + ": " + instance;
        }

        public static BACNET_OBJECT_ID Parse(string value)
        {
            BACNET_OBJECT_ID ret = new BACNET_OBJECT_ID();
            if (string.IsNullOrEmpty(value)) return ret;
            int p = value.IndexOf(": ");
            if (p < 0) return ret;
            string str_type = value.Substring(0, p);
            string str_instance = value.Substring(p + 2);
            ret.type = (BACNET_OBJECT_TYPE)Enum.Parse(typeof(BACNET_OBJECT_TYPE), str_type);
            ret.instance = uint.Parse(str_instance);
            return ret;
        }
    };

    public struct BACNET_PROPERTY_REFERENCE
    {
        public UInt32 propertyIdentifier;
        public UInt32 propertyArrayIndex;        /* optional */
        public BACNET_PROPERTY_REFERENCE(uint id, uint array_index)
        {
            propertyIdentifier = id;
            propertyArrayIndex = array_index;
        }
    };

    public struct BACNET_PROPERTY_VALUE
    {
        public BACNET_PROPERTY_REFERENCE property;
        public IList<BACNET_VALUE> value;
        public byte priority;
    }

    public struct BACNET_GENERIC_TIME
    {
        public BACNET_TIMESTAMP_TAG Tag;
        public DateTime Time;
        public UInt16 Sequence;

        public enum BACNET_TIMESTAMP_TAG
        {
            TIME_STAMP_NONE = -1,
            TIME_STAMP_TIME = 0,
            TIME_STAMP_SEQUENCE = 1,
            TIME_STAMP_DATETIME = 2
        };
    }

    public struct BACNET_GET_EVENT_INFORMATION_DATA
    {
        public BACNET_OBJECT_ID objectIdentifier;
        public BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_STATE eventState;
        public BACNET_BIT_STRING acknowledgedTransitions;
        public BACNET_GENERIC_TIME[] eventTimeStamps;    //3
        public BACNET_EVENT_NOTIFICATION_DATA.BACNET_NOTIFY_TYPE notifyType;
        public BACNET_BIT_STRING eventEnable;
        public uint[] eventPriorities;     //3
    };

    public struct BACNET_BIT_STRING
    {
        public byte bits_used;
        public byte[] value;

        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < bits_used; i++)
            {
                ret = ((value[i/8] & (i%8+1)) > 0 ? "1" : "0") + ret;
            }
            return ret;
        }

        public void SetBit(byte bit_number, bool v)
        {
            byte byte_number = (byte)(bit_number / 8);
            byte bit_mask = 1;

            if (value == null) value = new byte[System.IO.BACnet.Serialize.ASN1.MAX_BITSTRING_BYTES];

            if (byte_number < System.IO.BACnet.Serialize.ASN1.MAX_BITSTRING_BYTES)
            {
                /* set max bits used */
                if (bits_used < (bit_number + 1))
                    bits_used = (byte)(bit_number + 1);
                bit_mask = (byte)(bit_mask << (bit_number - (byte_number * 8)));
                if (v)
                    value[byte_number] |= bit_mask;
                else
                    value[byte_number] &= (byte)(~(bit_mask));
            }
        }

        public static BACNET_BIT_STRING Parse(string str)
        {
            BACNET_BIT_STRING ret = new BACNET_BIT_STRING();
            ret.value = new byte[System.IO.BACnet.Serialize.ASN1.MAX_BITSTRING_BYTES];

            if (!string.IsNullOrEmpty(str))
            {
                ret.bits_used = (byte)str.Length;
                for (int i = ret.bits_used-1, bit = 0; i >= 0; i--, bit++)
                {
                    bool is_set = str[i] == '1';
                    if (is_set) ret.value[bit / 8] |= (byte)(1 << (bit % 8));
                }
            }

            return ret;
        }

        public uint ConvertToInt()
        {
            return BitConverter.ToUInt32(value, 0);
        }
    };

    public enum AddressTypes
    {
        None,
        IP,
        MSTP,
        Ethernet,
        ArcNet,
        LonTalk,
    }

    public class BACNET_ADDRESS
    {
        public UInt16 net;
        public byte[] adr;
        public AddressTypes type;
        public BACNET_ADDRESS(AddressTypes type, UInt16 net, byte[] adr)
        {
            this.type = type;
            this.net = net;
            this.adr = adr;
            if (this.adr == null) this.adr = new byte[0];
        }
        public override int GetHashCode()
        {
            return adr.GetHashCode();
        }
        public override string ToString()
        {
            switch (type)
            {
                case AddressTypes.IP:
                    if(adr == null || adr.Length < 6) return "0.0.0.0";
                    ushort port = BitConverter.ToUInt16(adr, 4);
                    return adr[0] + "." + adr[1] + "." + adr[2] + "." + adr[3] + ":" + port;
                case AddressTypes.MSTP:
                    if(adr == null || adr.Length < 1) return "-1";
                    return adr[0].ToString();
                default:
                    return base.ToString();
            }
        }
        public override bool Equals(object obj)
        {
            if (!(obj is BACNET_ADDRESS)) return false;
            BACNET_ADDRESS d = (BACNET_ADDRESS)obj;
            if (adr == null && d.adr == null) return true;
            else if (adr == null || d.adr == null) return false;
            else if (adr.Length != d.adr.Length) return false;
            else
            {
                for (int i = 0; i < adr.Length; i++)
                    if (adr[i] != d.adr[i]) return false;
                return true;
            }
        }
    }

    /* MS/TP Frame Type */
    public enum MSTP_FRAME_TYPE : byte
    {
        /* Frame Types 8 through 127 are reserved by ASHRAE. */
        FRAME_TYPE_TOKEN = 0,
        FRAME_TYPE_POLL_FOR_MASTER = 1,
        FRAME_TYPE_REPLY_TO_POLL_FOR_MASTER = 2,
        FRAME_TYPE_TEST_REQUEST = 3,
        FRAME_TYPE_TEST_RESPONSE = 4,
        FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY = 5,
        FRAME_TYPE_BACNET_DATA_NOT_EXPECTING_REPLY = 6,
        FRAME_TYPE_REPLY_POSTPONED = 7,
        /* Frame Types 128 through 255: Proprietary Frames */
        /* These frames are available to vendors as proprietary (non-BACnet) frames. */
        /* The first two octets of the Data field shall specify the unique vendor */
        /* identification code, most significant octet first, for the type of */
        /* vendor-proprietary frame to be conveyed. The length of the data portion */
        /* of a Proprietary frame shall be in the range of 2 to 501 octets. */
        FRAME_TYPE_PROPRIETARY_MIN = 128,
        FRAME_TYPE_PROPRIETARY_MAX = 255,
    };

    public enum BACNET_PROPERTY_ID
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
        PROP_AUTHORIZATION_STATUS = 260,
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
        /* The special property identifiers all, optional, and required  */
        /* are reserved for use in the ReadPropertyConditional and */
        /* ReadPropertyMultiple services or services not defined in this standard. */
        /* Enumerated values 0-511 are reserved for definition by ASHRAE.  */
        /* Enumerated values 512-4194303 may be used by others subject to the  */
        /* procedures and constraints described in Clause 23.  */
        /* do the max range inside of enum so that
           compilers will allocate adequate sized datatype for enum
           which is used to store decoding */
        MAX_BACNET_PROPERTY_ID = 4194303,
    };

    public enum BACNET_BVLC_FUNCTION : byte
    {
        BVLC_RESULT = 0,
        BVLC_WRITE_BROADCAST_DISTRIBUTION_TABLE = 1,
        BVLC_READ_BROADCAST_DIST_TABLE = 2,
        BVLC_READ_BROADCAST_DIST_TABLE_ACK = 3,
        BVLC_FORWARDED_NPDU = 4,
        BVLC_REGISTER_FOREIGN_DEVICE = 5,
        BVLC_READ_FOREIGN_DEVICE_TABLE = 6,
        BVLC_READ_FOREIGN_DEVICE_TABLE_ACK = 7,
        BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY = 8,
        BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK = 9,
        BVLC_ORIGINAL_UNICAST_NPDU = 10,
        BVLC_ORIGINAL_BROADCAST_NPDU = 11,
        MAX_BVLC_FUNCTION = 12
    };

    [Flags]
    public enum BACNET_NPDU_CONTROL : byte
    {
        PriorityNormalMessage = 0,
        PriorityUrgentMessage = 1,
        PriorityCriticalMessage = 2,
        PriorityLifeSafetyMessage = 3,
        ExpectingReply = 4,
        SourceSpecified = 8,
        DestinationSpecified = 32,
        NetworkLayerMessage = 128,
    };

    /*Network Layer Message Type */
    /*If Bit 7 of the control octet described in 6.2.2 is 1, */
    /* a message type octet shall be present as shown in Figure 6-1. */
    /* The following message types are indicated: */
    public enum BACNET_NETWORK_MESSAGE_TYPE : byte
    {
        NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK = 0,
        NETWORK_MESSAGE_I_AM_ROUTER_TO_NETWORK = 1,
        NETWORK_MESSAGE_I_COULD_BE_ROUTER_TO_NETWORK = 2,
        NETWORK_MESSAGE_REJECT_MESSAGE_TO_NETWORK = 3,
        NETWORK_MESSAGE_ROUTER_BUSY_TO_NETWORK = 4,
        NETWORK_MESSAGE_ROUTER_AVAILABLE_TO_NETWORK = 5,
        NETWORK_MESSAGE_INIT_RT_TABLE = 6,
        NETWORK_MESSAGE_INIT_RT_TABLE_ACK = 7,
        NETWORK_MESSAGE_ESTABLISH_CONNECTION_TO_NETWORK = 8,
        NETWORK_MESSAGE_DISCONNECT_CONNECTION_TO_NETWORK = 9,
        /* X'0A' to X'7F': Reserved for use by ASHRAE, */
        /* X'80' to X'FF': Available for vendor proprietary messages */
    } ;
}

namespace System.IO.BACnet.Serialize
{
    public class BVLC
    {
        public const byte BVLL_TYPE_BACNET_IP = 0x81;
        public const byte BVLC_HEADER_LENGTH = 4;
        public const int BVLC_MAX_APDU = 1476;
        public const int MSTP_MAX_NDPU = 1497;

        public static int Encode(byte[] buffer, int offset, BACNET_BVLC_FUNCTION function, int msg_length)
        {
            buffer[offset + 0] = BVLL_TYPE_BACNET_IP;
            buffer[offset + 1] = (byte)function;
            buffer[offset + 2] = (byte)((msg_length & 0xFF00) >> 8);
            buffer[offset + 3] = (byte)((msg_length & 0x00FF) >> 0);
            return 4;
        }

        public static int Decode(byte[] buffer, int offset, out BACNET_BVLC_FUNCTION function, out int msg_length)
        {
            function = (BACNET_BVLC_FUNCTION)buffer[offset + 1];
            msg_length = (buffer[offset + 2] << 8) | (buffer[offset + 3] << 0);
            if (buffer[offset + 0] != BVLL_TYPE_BACNET_IP) return -1;
            return 4;
        }
    }

    public class MSTP
    {
        public const byte MSTP_PREAMBLE1 = 0x55;
        public const byte MSTP_PREAMBLE2 = 0xFF;
        public const int MSTP_MAX_APDU = 480;
        public const int MSTP_MAX_NDPU = 501;
        public const byte MSTP_HEADER_LENGTH = 8;

        public static byte CRC_Calc_Header(byte dataValue, byte crcValue)
        {
            ushort crc;

            crc = (ushort)(crcValue ^ dataValue); /* XOR C7..C0 with D7..D0 */

            /* Exclusive OR the terms in the table (top down) */
            crc = (ushort)(crc ^ (crc << 1) ^ (crc << 2) ^ (crc << 3) ^ (crc << 4) ^ (crc << 5) ^ (crc << 6) ^ (crc << 7));

            /* Combine bits shifted out left hand end */
            return (byte)((crc & 0xfe) ^ ((crc >> 8) & 1));
        }

        public static byte CRC_Calc_Header(byte[] buffer, int offset, int length)
        {
            byte crc = 0xff;
            for (int i = offset; i < (offset + length); i++)
                crc = CRC_Calc_Header(buffer[i], crc);
            return (byte)~crc;
        }

        public static ushort CRC_Calc_Data(byte dataValue, ushort crcValue)
        {
            ushort crcLow;

            crcLow = (ushort)((crcValue & 0xff) ^ dataValue);     /* XOR C7..C0 with D7..D0 */

            /* Exclusive OR the terms in the table (top down) */
            return (ushort)((crcValue >> 8) ^ (crcLow << 8) ^ (crcLow << 3)
                ^ (crcLow << 12) ^ (crcLow >> 4)
                ^ (crcLow & 0x0f) ^ ((crcLow & 0x0f) << 7));
        }

        public static ushort CRC_Calc_Data(byte[] buffer, int offset, int length)
        {
            ushort crc = 0xffff;
            for (int i = offset; i < (offset + length); i++)
                crc = CRC_Calc_Data(buffer[i], crc);
            return (ushort)~crc;
        }

        public static int Encode(byte[] buffer, int offset, MSTP_FRAME_TYPE frame_type, byte destination_address, byte source_address, int msg_length)
        {
            buffer[offset + 0] = MSTP_PREAMBLE1;
            buffer[offset + 1] = MSTP_PREAMBLE2;
            buffer[offset + 2] = (byte)frame_type;
            buffer[offset + 3] = destination_address;
            buffer[offset + 4] = source_address;
            buffer[offset + 5] = (byte)((msg_length & 0xFF00) >> 8);
            buffer[offset + 6] = (byte)((msg_length & 0x00FF) >> 0);
            buffer[offset + 7] = CRC_Calc_Header(buffer, offset + 2, 5);
            if (msg_length > 0)
            {
                //calculate data crc
                ushort data_crc = CRC_Calc_Data(buffer, offset + 8, msg_length);
                buffer[offset + 8 + msg_length + 0] = (byte)(data_crc & 0xFF);  //LSB first!
                buffer[offset + 8 + msg_length + 1] = (byte)(data_crc >> 8);
            }
            //optional pad (0xFF)
            return MSTP_HEADER_LENGTH + (msg_length) + (msg_length > 0 ? 2 : 0);
        }

        public static int Decode(byte[] buffer, int offset, int max_length, out MSTP_FRAME_TYPE frame_type, out byte destination_address, out byte source_address, out int msg_length)
        {
            frame_type = (MSTP_FRAME_TYPE)buffer[offset + 2];
            destination_address = buffer[offset + 3];
            source_address = buffer[offset + 4];
            msg_length = (buffer[offset + 5] << 8) | (buffer[offset + 6] << 0);
            byte crc_header = buffer[offset + 7];
            ushort crc_data = 0;
            if (max_length < MSTP_HEADER_LENGTH) return -1;     //not enough data
            if (msg_length > 0) crc_data = (ushort)((buffer[offset + 8 + msg_length + 1] << 8) | (buffer[offset + 8 + msg_length + 0] << 0));
            if (buffer[offset + 0] != MSTP_PREAMBLE1) return -1;
            if (buffer[offset + 1] != MSTP_PREAMBLE2) return -1;
            if (CRC_Calc_Header(buffer, offset + 2, 5) != crc_header) return -1;
            if (msg_length > 0 && max_length >= (MSTP_HEADER_LENGTH + msg_length + 2) && CRC_Calc_Data(buffer, offset + 8, msg_length) != crc_data) return -1;
            return 8 + (msg_length) + (msg_length > 0 ? 2 : 0);
        }
    }

    public class NPDU
    {
        public const byte BACNET_PROTOCOL_VERSION = 1;

        public static int Decode(byte[] buffer, int offset, out BACNET_NPDU_CONTROL function, out BACNET_ADDRESS destination, out BACNET_ADDRESS source, out byte hop_count, out BACNET_NETWORK_MESSAGE_TYPE network_msg_type, out ushort vendor_id)
        {
            int org_offset = offset;

            offset++;
            function = (BACNET_NPDU_CONTROL)buffer[offset++];

            destination = null;
            if ((function & BACNET_NPDU_CONTROL.DestinationSpecified) == BACNET_NPDU_CONTROL.DestinationSpecified)
            {
                destination = new BACNET_ADDRESS(AddressTypes.None, (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0)), null);
                int adr_len = buffer[offset++];
                if (adr_len > 0)
                {
                    destination.adr = new byte[adr_len];
                    for (int i = 0; i < destination.adr.Length; i++)
                        destination.adr[i] = buffer[offset++];
                }
            }

            source = null;
            if ((function & BACNET_NPDU_CONTROL.SourceSpecified) == BACNET_NPDU_CONTROL.SourceSpecified)
            {
                source = new BACNET_ADDRESS(AddressTypes.None, (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0)), null);
                int adr_len = buffer[offset++];
                if (adr_len > 0)
                {
                    source.adr = new byte[adr_len];
                    for (int i = 0; i < source.adr.Length; i++)
                        source.adr[i] = buffer[offset++];
                }
            }

            hop_count = 0;
            if ((function & BACNET_NPDU_CONTROL.DestinationSpecified) == BACNET_NPDU_CONTROL.DestinationSpecified)
            {
                hop_count = buffer[offset++];
            }

            network_msg_type = BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK;
            vendor_id = 0;
            if ((function & BACNET_NPDU_CONTROL.NetworkLayerMessage) == BACNET_NPDU_CONTROL.NetworkLayerMessage)
            {
                network_msg_type = (BACNET_NETWORK_MESSAGE_TYPE)buffer[offset++];
                if (((byte)network_msg_type) >= 0x80)
                {
                    vendor_id = (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0));
                }
            }

            if (buffer[org_offset + 0] != BACNET_PROTOCOL_VERSION) return -1;
            return offset - org_offset;
        }

        public static int Encode(byte[] buffer, int offset, BACNET_NPDU_CONTROL function, BACNET_ADDRESS destination, BACNET_ADDRESS source, byte hop_count, BACNET_NETWORK_MESSAGE_TYPE network_msg_type, ushort vendor_id)
        {
            int org_offset = offset;
            buffer[offset++] = BACNET_PROTOCOL_VERSION;
            buffer[offset++] = (byte)(function | (destination != null && destination.net > 0 ? BACNET_NPDU_CONTROL.DestinationSpecified : (BACNET_NPDU_CONTROL)0) | (source != null && source.net > 0 ? BACNET_NPDU_CONTROL.SourceSpecified : (BACNET_NPDU_CONTROL)0));

            if (destination != null && destination.net > 0)
            {
                buffer[offset++] = (byte)((destination.net & 0xFF00) >> 8);
                buffer[offset++] = (byte)((destination.net & 0x00FF) >> 0);
                buffer[offset++] = (byte)destination.adr.Length;
                if (destination.adr.Length > 0)
                {
                    for (int i = 0; i < destination.adr.Length; i++)
                        buffer[offset++] = destination.adr[i];
                }
            }

            if (source != null && source.net > 0)
            {
                buffer[offset++] = (byte)((source.net & 0xFF00) >> 8);
                buffer[offset++] = (byte)((source.net & 0x00FF) >> 0);
                buffer[offset++] = (byte)source.adr.Length;
                if (source.adr.Length > 0)
                {
                    for (int i = 0; i < source.adr.Length; i++)
                        buffer[offset++] = source.adr[i];
                }
            }

            if (destination != null && destination.net > 0)
            {
                buffer[offset++] = hop_count;
            }

            if ((function & BACNET_NPDU_CONTROL.NetworkLayerMessage) > 0)
            {
                buffer[offset++] = (byte)network_msg_type;
                if (((byte)network_msg_type) >= 0x80)
                {
                    buffer[offset++] = (byte)((vendor_id & 0xFF00) >> 8);
                    buffer[offset++] = (byte)((vendor_id & 0x00FF) >> 0);
                }
            }

            return offset - org_offset;
        }
    }

    public class APDU
    {
        public static BACNET_PDU_TYPE GetDecodedType(byte[] buffer, int offset)
        {
            return (BACNET_PDU_TYPE)buffer[offset];
        }

        public static int GetDecodedInvokeId(byte[] buffer, int offset)
        {
            BACNET_PDU_TYPE type = GetDecodedType(buffer, offset);
            switch (type & BACNET_PDU_TYPE.PDU_TYPE_MASK)
            {
                case BACNET_PDU_TYPE.PDU_TYPE_SIMPLE_ACK:
                case BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK:
                case BACNET_PDU_TYPE.PDU_TYPE_ERROR:
                case BACNET_PDU_TYPE.PDU_TYPE_REJECT:
                case BACNET_PDU_TYPE.PDU_TYPE_ABORT:
                    return buffer[offset + 1];
                case BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                    return buffer[offset + 2];
                default:
                    return -1;
            }
        }

        public static int EncodeConfirmedServiceRequest(byte[] buffer, int offset, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, BACNET_MAX_SEGMENTS max_segments, BACNET_MAX_ADPU max_adpu, byte invoke_id, byte sequence_number, byte proposed_window_number)
        {
            int org_offset = offset;

            buffer[offset++] = (byte)type;
            buffer[offset++] = (byte)((byte)max_segments | (byte)max_adpu);
            buffer[offset++] = invoke_id;

            if((type & BACNET_PDU_TYPE.SEGMENTED_MESSAGE) > 0)
            {
                buffer[offset++] = sequence_number;
                buffer[offset++] = proposed_window_number;
            }
            buffer[offset++] = (byte)service;

            return offset - org_offset;
        }

        public static int DecodeConfirmedServiceRequest(byte[] buffer, int offset, out BACNET_PDU_TYPE type, out BACNET_CONFIRMED_SERVICE service, out BACNET_MAX_SEGMENTS max_segments, out BACNET_MAX_ADPU max_adpu, out byte invoke_id, out byte sequence_number, out byte proposed_window_number)
        {
            int org_offset = offset;

            type = (BACNET_PDU_TYPE)buffer[offset++];
            max_segments = (BACNET_MAX_SEGMENTS)(buffer[offset] & 0xF0);
            max_adpu = (BACNET_MAX_ADPU)(buffer[offset++] & 0x0F);
            invoke_id = buffer[offset++];

            sequence_number = 0;
            proposed_window_number = 0;
            if ((type & BACNET_PDU_TYPE.SEGMENTED_MESSAGE) > 0)
            {
                sequence_number = buffer[offset++];
                proposed_window_number = buffer[offset++];
            }
            service = (BACNET_CONFIRMED_SERVICE)buffer[offset++];

            return offset - org_offset;
        }

        public static int EncodeUnconfirmedServiceRequest(byte[] buffer, int offset, BACNET_PDU_TYPE type, BACNET_UNCONFIRMED_SERVICE service)
        {
            int org_offset = offset;

            buffer[offset++] = (byte)type;
            buffer[offset++] = (byte)service;

            return offset - org_offset;
        }

        public static int DecodeUnconfirmedServiceRequest(byte[] buffer, int offset, out BACNET_PDU_TYPE type, out BACNET_UNCONFIRMED_SERVICE service)
        {
            int org_offset = offset;

            type = (BACNET_PDU_TYPE)buffer[offset++];
            service = (BACNET_UNCONFIRMED_SERVICE)buffer[offset++];

            return offset - org_offset;
        }

        public static int EncodeSimpleAck(byte[] buffer, int offset, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id)
        {
            int org_offset = offset;

            buffer[offset++] = (byte)type;
            buffer[offset++] = invoke_id;
            buffer[offset++] = (byte)service;

            return offset - org_offset;
        }

        public static int DecodeSimpleAck(byte[] buffer, int offset, out BACNET_PDU_TYPE type, out BACNET_CONFIRMED_SERVICE service, out byte invoke_id)
        {
            int org_offset = offset;

            type = (BACNET_PDU_TYPE)buffer[offset++];
            invoke_id = buffer[offset++];
            service = (BACNET_CONFIRMED_SERVICE)buffer[offset++];

            return offset - org_offset;
        }

        public static int EncodeComplexAck(byte[] buffer, int offset, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id, byte sequence_number, byte proposed_window_number)
        {
            int org_offset = offset;

            buffer[offset++] = (byte)type;
            buffer[offset++] = invoke_id;

            if ((type & BACNET_PDU_TYPE.SEGMENTED_MESSAGE) > 0)
            {
                buffer[offset++] = sequence_number;
                buffer[offset++] = proposed_window_number;
            }
            buffer[offset++] = (byte)service;

            return offset - org_offset;
        }

        public static int DecodeComplexAck(byte[] buffer, int offset, out BACNET_PDU_TYPE type, out BACNET_CONFIRMED_SERVICE service, out byte invoke_id, out byte sequence_number, out byte proposed_window_number)
        {
            int org_offset = offset;

            type = (BACNET_PDU_TYPE)buffer[offset++];
            invoke_id = buffer[offset++];

            sequence_number = 0;
            proposed_window_number = 0;
            if ((type & BACNET_PDU_TYPE.SEGMENTED_MESSAGE) > 0)
            {
                sequence_number = buffer[offset++];
                proposed_window_number = buffer[offset++];
            }
            service = (BACNET_CONFIRMED_SERVICE)buffer[offset++];

            return offset - org_offset;
        }

        public static int EncodeSegmentAck(byte[] buffer, int offset, BACNET_PDU_TYPE type)
        {
            int org_offset = offset;

            buffer[offset++] = (byte)type;

            return offset - org_offset;
        }

        public static int DecodeSegmentAck(byte[] buffer, int offset, out BACNET_PDU_TYPE type)
        {
            int org_offset = offset;

            type = (BACNET_PDU_TYPE)buffer[offset++];

            return offset - org_offset;
        }

        public static int EncodeError(byte[] buffer, int offset, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id)
        {
            int org_offset = offset;

            buffer[offset++] = (byte)type;
            buffer[offset++] = invoke_id;
            buffer[offset++] = (byte)service;

            return offset - org_offset;
        }

        public static int DecodeError(byte[] buffer, int offset, out BACNET_PDU_TYPE type, out BACNET_CONFIRMED_SERVICE service, out byte invoke_id)
        {
            int org_offset = offset;

            type = (BACNET_PDU_TYPE)buffer[offset++];
            invoke_id = buffer[offset++];
            service = (BACNET_CONFIRMED_SERVICE)buffer[offset++];

            return offset - org_offset;
        }

        /// <summary>
        /// Also EncodeReject
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="type"></param>
        /// <param name="invoke_id"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static int EncodeAbort(byte[] buffer, int offset, BACNET_PDU_TYPE type, byte invoke_id, byte reason)
        {
            int org_offset = offset;

            buffer[offset++] = (byte)type;
            buffer[offset++] = invoke_id;
            buffer[offset++] = reason;

            return offset - org_offset;
        }

        public static int DecodeAbort(byte[] buffer, int offset, out BACNET_PDU_TYPE type, out byte invoke_id, out byte reason)
        {
            int org_offset = offset;

            type = (BACNET_PDU_TYPE)buffer[offset++];
            invoke_id = buffer[offset++];
            reason = buffer[offset++];

            return offset - org_offset;
        }
    }

    public class ASN1
    {
        public const int BACNET_MAX_OBJECT = 0x3FF;
        public const int BACNET_INSTANCE_BITS = 22;
        public const int BACNET_MAX_INSTANCE = 0x3FFFFF;
        public const int MAX_BITSTRING_BYTES = 15;
        public const uint BACNET_ARRAY_ALL = 0xFFFFFFFFU;
        public const uint BACNET_NO_PRIORITY = 0;
        public const uint BACNET_MIN_PRIORITY = 1;
        public const uint BACNET_MAX_PRIORITY = 16;

        public static int encode_bacnet_object_id(byte[] buffer, int offset, BACNET_OBJECT_TYPE object_type, UInt32 instance)
        {
            UInt32 value = 0;
            UInt32 type = 0;
            int len = 0;

            type = (UInt32)object_type;
            value = ((type & BACNET_MAX_OBJECT) << BACNET_INSTANCE_BITS) | (instance & BACNET_MAX_INSTANCE);
            len = encode_unsigned32(buffer, offset, value);

            return len;
        }

        public static int encode_tag(byte[] buffer, int offset, byte tag_number, bool context_specific, UInt32 len_value_type)
        {
            int len = 1;
            buffer[offset] = 0;
            if (context_specific) buffer[offset] |= 0x8;

            /* additional tag byte after this byte */
            /* for extended tag byte */
            if (tag_number <= 14)
            {
                buffer[offset] |= (byte)(tag_number << 4);
            }
            else
            {
                buffer[offset] |= 0xF0;
                buffer[offset+1] = tag_number;
                len++;
            }

            /* NOTE: additional len byte(s) after extended tag byte */
            /* if larger than 4 */
            if (len_value_type <= 4)
            {
                buffer[offset] |= (byte)len_value_type;
            }
            else
            {
                buffer[offset] |= 5;
                if (len_value_type <= 253)
                {
                    buffer[offset + len++] = (byte)len_value_type;
                }
                else if (len_value_type <= 65535)
                {
                    buffer[offset + len++] = 254;
                    len += encode_unsigned16(buffer, offset + len, (UInt16)len_value_type);
                }
                else
                {
                    buffer[offset + len++] = 255;
                    len += encode_unsigned32(buffer, offset + len, len_value_type);
                }
            }

            return len;
        }

        public static int encode_application_object_id(byte[] buffer, int offset, BACNET_OBJECT_TYPE object_type, UInt32 instance)
        {
            int len = 0;
            len += encode_bacnet_object_id(buffer, offset + 1, object_type, instance);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID, false, (uint)len);
            return len;
        }

        public static int encode_application_unsigned(byte[] buffer, int offset, UInt32 value)
        {
            int len = 0;

            len = encode_bacnet_unsigned(buffer, offset +1, value);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT, false, (UInt32)len);

            return len;
        }

        public static int encode_bacnet_enumerated(byte[] buffer, int offset, UInt32 value)
        {
            return encode_bacnet_unsigned(buffer, offset, value);
        }

        public static int encode_application_enumerated(byte[] buffer, int offset, UInt32 value)
        {
            int len = 0;        /* return value */

            /* assumes that the tag only consumes 1 octet */
            len = encode_bacnet_enumerated(buffer, offset + 1, value);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_ENUMERATED, false, (UInt32)len);

            return len;
        }

        public static int encode_bacnet_unsigned(byte[] buffer, int offset, UInt32 value)
        {
            int len = 0;        /* return value */

            if (value < 0x100)
            {
                buffer[offset] = (byte)value;
                len = 1;
            }
            else if (value < 0x10000)
            {
                len = encode_unsigned16(buffer, offset, (UInt16)value);
            }
            else if (value < 0x1000000)
            {
                len = encode_unsigned24(buffer, offset, value);
            }
            else
            {
                len = encode_unsigned32(buffer, offset, value);
            }

            return len;
        }

        public static int encode_context_boolean(byte[] buffer, int offset, byte tag_number, bool boolean_value)
        {
            int len = 0;        /* return value */

            len = encode_tag(buffer, offset, (byte)tag_number, true, 1);
            buffer[offset + len++] = (boolean_value ? (byte)1 : (byte)0);

            return len;
        }

        public static int encode_context_real(byte[] buffer, int offset, byte tag_number, float value)
        {
            int len = 0;

            /* length of double is 4 octets, as per 20.2.6 */
            len = encode_tag(buffer, offset, tag_number, true, 4);
            len += encode_bacnet_real(buffer, offset + len, value);
            return len;
        }

        public static int encode_context_unsigned(byte[] buffer, int offset, byte tag_number, UInt32 value)
        {
            int len = 0;

            /* length of unsigned is variable, as per 20.2.4 */
            if (value < 0x100)
            {
                len = 1;
            }
            else if (value < 0x10000)
            {
                len = 2;
            }
            else if (value < 0x1000000)
            {
                len = 3;
            }
            else
            {
                len = 4;
            }

            len = encode_tag(buffer, offset, tag_number, true, (UInt32)len);
            len += encode_bacnet_unsigned(buffer, offset + len, value);

            return len;
        }

        public static int encode_context_character_string(byte[] buffer, int offset, int max_length, byte tag_number, string value)
        {
            int len = 0;
            int string_len = 0;

            string_len =
                value.Length + 1 /* for encoding */ ;
            len += encode_tag(buffer, offset, tag_number, true, (UInt32)string_len);
            if ((len + string_len) <= max_length)
            {
                len += encode_bacnet_character_string(buffer, offset, max_length, value);
            }
            else
            {
                len = 0;
            }

            return len;
        }

        public static int encode_context_enumerated(byte[] buffer, int offset, byte tag_number, UInt32 value)
        {
            int len = 0;        /* return value */

            /* length of enumerated is variable, as per 20.2.11 */
            if (value < 0x100)
            {
                len = 1;
            }
            else if (value < 0x10000)
            {
                len = 2;
            }
            else if (value < 0x1000000)
            {
                len = 3;
            }
            else
            {
                len = 4;
            }

            len = encode_tag(buffer, offset, tag_number, true, (uint)len);
            len += encode_bacnet_enumerated(buffer, offset + len, value);

            return len;
        }

        public static int encode_bacnet_signed(byte[] buffer, int offset, Int32 value)
        {
            int len = 0;        /* return value */

            /* don't encode the leading X'FF' or X'00' of the two's compliment.
               That is, the first octet of any multi-octet encoded value shall
               not be X'00' if the most significant bit (bit 7) of the second
               octet is 0, and the first octet shall not be X'FF' if the most
               significant bit of the second octet is 1. */
            if ((value >= -128) && (value < 128))
            {
                buffer[offset] = (byte)(sbyte)value;
            }
            else if ((value >= -32768) && (value < 32768))
            {
                len = encode_signed16(buffer, offset, (Int16)value);
            }
            else if ((value > -8388608) && (value < 8388608))
            {
                len = encode_signed24(buffer, offset, value);
            }
            else
            {
                len = encode_signed32(buffer, offset, value);
            }

            return len;
        }

        public static int encode_application_signed(byte[] buffer, int offset, Int32 value)
        {
            int len = 0;        /* return value */

            /* assumes that the tag only consumes 1 octet */
            len = encode_bacnet_signed(buffer, offset+1, value);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_SIGNED_INT, false, (uint)len);

            return len;
        }

        public static int encode_octet_string(byte[] buffer, int offset, byte[] octet_string, int octet_offset, int octet_count)
        {
            int len = 0;        /* return value */
            int i = 0;  /* loop counter */

            if (octet_string != null && octet_count > 0)
            {
                /* FIXME: might need to pass in the length of the APDU
                   to bounds check since it might not be the only data chunk */
                len = octet_count;
                for (i = 0; i < len; i++)
                {
                    buffer[offset + i] = octet_string[i + octet_offset];
                }
            }

            return len;
        }

        public static int encode_application_octet_string(byte[] buffer, int offset, int max_length, byte[] octet_string, int octet_offset, int octet_count)
        {
            int apdu_len = 0;

            if (octet_string != null && octet_count > 0)
            {
                apdu_len = encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING, false, (uint)octet_count);
                /* FIXME: probably need to pass in the length of the APDU
                   to bounds check since it might not be the only data chunk */
                if ((apdu_len + octet_string.Length) <= max_length)
                {
                    apdu_len += encode_octet_string(buffer, offset + apdu_len, octet_string, octet_offset, octet_count);
                }
                else
                {
                    apdu_len = 0;
                }
            }

            return apdu_len;
        }

        public static int encode_application_boolean(byte[] buffer, int offset, bool boolean_value)
        {
            int len = 0;
            len = encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN, false, boolean_value ? (uint)1 : (uint)0);
            return len;
        }

        public static int encode_bacnet_real(byte[] buffer, int offset, float value)
        {
            byte[] data = BitConverter.GetBytes(value);
            buffer[offset + 0] = data[3];
            buffer[offset + 1] = data[2];
            buffer[offset + 2] = data[1];
            buffer[offset + 3] = data[0];
            return 4;
        }

        public static int encode_bacnet_double(byte[] buffer, int offset, double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            buffer[offset + 0] = data[7];
            buffer[offset + 1] = data[6];
            buffer[offset + 2] = data[5];
            buffer[offset + 3] = data[4];
            buffer[offset + 4] = data[3];
            buffer[offset + 5] = data[2];
            buffer[offset + 6] = data[1];
            buffer[offset + 7] = data[0];
            return 4;
        }

        public static int encode_application_real(byte[] buffer, int offset, float value)
        {
            int len = 0;

            /* assumes that the tag only consumes 1 octet */
            len = encode_bacnet_real(buffer, offset +1, value);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL, false, (uint)len);

            return len;
        }

        public static int encode_application_double(byte[] buffer, int offset, double value)
        {
            int len = 0;

            /* assumes that the tag only consumes 2 octet */
            len = encode_bacnet_double(buffer, offset + 2, value);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DOUBLE, false, (uint)len);

            return len;
        }

        private static byte bitstring_bytes_used(BACNET_BIT_STRING bit_string)
        {
            byte len = 0;    /* return value */
            byte used_bytes = 0;
            byte last_bit = 0;

            if (bit_string.bits_used > 0)
            {
                last_bit = (byte)(bit_string.bits_used - 1);
                used_bytes = (byte)(last_bit / 8);
                /* add one for the first byte */
                used_bytes++;
                len = used_bytes;
            }

            return len;
        }


        private static byte byte_reverse_bits(byte in_byte)
        {
            byte out_byte = 0;

            if ((in_byte & 1) > 0)
            {
                out_byte |= 0x80;
            }
            if ((in_byte & 2) > 0)
            {
                out_byte |= 0x40;
            }
            if ((in_byte & 4) > 0)
            {
                out_byte |= 0x20;
            }
            if ((in_byte & 8) > 0)
            {
                out_byte |= 0x10;
            }
            if ((in_byte & 16)> 0) 
            {
                out_byte |= 0x8;
            }
            if ((in_byte & 32) > 0)
            {
                out_byte |= 0x4;
            }
            if ((in_byte & 64) > 0)
            {
                out_byte |= 0x2;
            }
            if ((in_byte & 128) > 0)
            {
                out_byte |= 1;
            }

            return out_byte;
        }

        private static byte bitstring_octet(BACNET_BIT_STRING bit_string, byte octet_index)
        {
            byte octet = 0;

            if (bit_string.value != null)
            {
                if (octet_index < MAX_BITSTRING_BYTES)
                {
                    octet = bit_string.value[octet_index];
                }
            }

            return octet;
        }

        public static int encode_bitstring(byte[] buffer, int offset, BACNET_BIT_STRING bit_string)
        {
            int len = 0;
            byte remaining_used_bits = 0;
            byte used_bytes = 0;
            byte i = 0;

            /* if the bit string is empty, then the first octet shall be zero */
            if (bit_string.bits_used == 0)
            {
                buffer[offset + len++] = 0;
            }
            else
            {
                used_bytes = bitstring_bytes_used(bit_string);
                remaining_used_bits = (byte)(bit_string.bits_used - ((used_bytes - 1) * 8));
                /* number of unused bits in the subsequent final octet */
                buffer[offset + len++] = (byte)(8 - remaining_used_bits);
                for (i = 0; i < used_bytes; i++)
                {
                    buffer[offset + len++] = byte_reverse_bits(bitstring_octet(bit_string, i));
                }
            }

            return len;
        }

        public static int encode_application_bitstring(byte[] buffer, int offset, BACNET_BIT_STRING bit_string)
        {
            int len = 0;
            uint bit_string_encoded_length = 1;     /* 1 for the bits remaining octet */

            /* bit string may use more than 1 octet for the tag, so find out how many */
            bit_string_encoded_length += bitstring_bytes_used(bit_string);
            len = encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BIT_STRING, false, bit_string_encoded_length);
            len += encode_bitstring(buffer, offset + len, bit_string);

            return len;
        }

        public static int bacapp_encode_application_data(byte[] buffer, int offset, int max_length, BACNET_VALUE value)
        {
            int apdu_len = 0;   /* total length of the apdu, return value */

            switch (value.Tag)
            {
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL:
                    buffer[offset] = (byte)value.Tag;
                    apdu_len++;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN:
                    apdu_len =
                        encode_application_boolean(buffer, offset, (bool)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    apdu_len =
                        encode_application_unsigned(buffer, offset, (uint)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_SIGNED_INT:
                    apdu_len =
                        encode_application_signed(buffer, offset, (int)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL:
                    apdu_len = encode_application_real(buffer, offset, (float)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DOUBLE:
                    apdu_len = encode_application_double(buffer, offset, (double)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING:
                    apdu_len =
                        encode_application_octet_string(buffer, offset, max_length, (byte[])value.Value, 0, ((byte[])value.Value).Length);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    apdu_len =
                        encode_application_character_string(buffer, offset, max_length, (string)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BIT_STRING:
                    apdu_len = encode_application_bitstring(buffer, offset, (BACNET_BIT_STRING)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_ENUMERATED:
                    apdu_len = encode_application_enumerated(buffer, offset, (uint)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DATE:
                    apdu_len =
                        encode_application_date(buffer, offset, (DateTime)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_TIME:
                    apdu_len =
                        encode_application_time(buffer, offset, (DateTime)value.Value);
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID:
                    apdu_len = encode_application_object_id(buffer, offset, ((BACNET_OBJECT_ID)value.Value).type, ((BACNET_OBJECT_ID)value.Value).instance);
                    break;
                default:
                    break;
            }

            return apdu_len;
        }

        public static int bacapp_encode_device_obj_property_ref( byte[] buffer, int offset, BACNET_DEVICE_OBJECT_PROPERTY_REFERENCE value)
        {
            int len = 0;

            len += encode_context_object_id(buffer, offset, 0, value.objectIdentifier.type, value.objectIdentifier.instance);
            len += encode_context_enumerated(buffer, offset, 1, value.propertyIdentifier);

            /* Array index is optional so check if needed before inserting */
            if (value.arrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                len += encode_context_unsigned(buffer, offset, 2, value.arrayIndex);
            }

            /* Likewise, device id is optional so see if needed
             * (set type to non device to omit */
            if (value.deviceIndentifier.type == BACNET_OBJECT_TYPE.OBJECT_DEVICE)
            {
                len = encode_context_object_id(buffer, offset, 3, value.deviceIndentifier.type, value.deviceIndentifier.instance);
                
            }
            return len;
        }

        public static int bacapp_encode_context_device_obj_property_ref(byte[] buffer, int offset, byte tag_number, BACNET_DEVICE_OBJECT_PROPERTY_REFERENCE value)
        {
            int len = 0;

            len += encode_opening_tag(buffer, offset, tag_number);
            len += bacapp_encode_device_obj_property_ref(buffer, offset, value);
            len += encode_closing_tag(buffer, offset, tag_number);

            return len;
        }

        public static int bacapp_encode_property_state(byte[] buffer, int offset, BACNET_PROPERTY_STATE value)
        {
            int len = 0;        /* length of each encoding */

                switch (value.tag)
                {
                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.BOOLEAN_VALUE:
                        len = encode_context_boolean(buffer, offset, 0, value.state == 1 ? true : false);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.BINARY_VALUE:
                        len = encode_context_enumerated(buffer, offset, 1, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.EVENT_TYPE:
                        len = encode_context_enumerated(buffer, offset, 2, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.POLARITY:
                        len = encode_context_enumerated(buffer, offset, 3, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.PROGRAM_CHANGE:
                        len = encode_context_enumerated(buffer, offset, 4, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.PROGRAM_STATE:
                        len = encode_context_enumerated(buffer, offset, 5, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.REASON_FOR_HALT:
                        len = encode_context_enumerated(buffer, offset, 6, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.RELIABILITY:
                        len = encode_context_enumerated(buffer, offset, 7, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.STATE:
                        len = encode_context_enumerated(buffer, offset, 8, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.SYSTEM_STATUS:
                        len = encode_context_enumerated(buffer, offset, 9, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.UNITS:
                        len = encode_context_enumerated(buffer, offset, 10, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.UNSIGNED_VALUE:
                        len = encode_context_unsigned(buffer, offset, 11, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.LIFE_SAFETY_MODE:
                        len = encode_context_enumerated(buffer, offset, 12, value.state);
                        break;

                    case BACNET_PROPERTY_STATE.BACNET_PROPERTY_STATE_TYPE.LIFE_SAFETY_STATE:
                        len = encode_context_enumerated(buffer, offset, 13, value.state);
                        break;

                    default:
                        /* FIXME: assert(0); - return a negative len? */
                        break;
                }

            return len;
        }

        public static int encode_context_bitstring(byte[] buffer, int offset, byte tag_number, BACNET_BIT_STRING bit_string)
        {
            int len = 0;
            uint bit_string_encoded_length = 1;     /* 1 for the bits remaining octet */

            /* bit string may use more than 1 octet for the tag, so find out how many */
            bit_string_encoded_length += bitstring_bytes_used(bit_string);
            len = encode_tag(buffer, offset, tag_number, true, bit_string_encoded_length);
            len += encode_bitstring(buffer, offset + len, bit_string);

            return len;
        }

        public static int encode_opening_tag( byte[] buffer, int offset, byte tag_number)
        {
            int len = 1;

            /* set class field to context specific */
            buffer[offset] = 0x8;
            /* additional tag byte after this byte for extended tag byte */
            if (tag_number <= 14)
            {
                buffer[offset] |= (byte)(tag_number << 4);
            }
            else
            {
                buffer[offset] |= 0xF0;
                buffer[offset+1] = tag_number;
                len++;
            }
            /* set type field to opening tag */
            buffer[offset] |= 6;

            return len;
        }

        public static int encode_context_signed(byte[] buffer, int offset, byte tag_number, Int32 value)
        {
            int len = 0;        /* return value */

            /* length of signed int is variable, as per 20.2.11 */
            if ((value >= -128) && (value < 128))
            {
                len = 1;
            }
            else if ((value >= -32768) && (value < 32768))
            {
                len = 2;
            }
            else if ((value > -8388608) && (value < 8388608))
            {
                len = 3;
            }
            else
            {
                len = 4;
            }

            len = encode_tag(buffer, offset, tag_number, true, (uint)len);
            len += encode_bacnet_signed(buffer, offset + len, value);

            return len;
        }

        public static int encode_context_object_id(byte[] buffer, int offset, byte tag_number, BACNET_OBJECT_TYPE object_type, uint instance)
        {
            int len = 0;

            /* length of object id is 4 octets, as per 20.2.14 */

            len = encode_tag(buffer, offset, tag_number, true, 4);
            len += encode_bacnet_object_id(buffer, offset + len, object_type, instance);

            return len;
        }

        public static int encode_closing_tag( byte[] buffer, int offset,byte tag_number)
        {
            int len = 1;

            /* set class field to context specific */
            buffer[offset] = 0x8;
            /* additional tag byte after this byte for extended tag byte */
            if (tag_number <= 14)
            {
                buffer[offset] |= (byte)(tag_number << 4);
            }
            else
            {
                buffer[offset] |= 0xF0;
                buffer[offset+1] = tag_number;
                len++;
            }
            /* set type field to closing tag */
            buffer[offset] |= 7;

            return len;
        }

        public static int encode_bacnet_time(byte[] buffer, int offset, DateTime value)
        {
            buffer[offset + 0] = (byte)value.Hour;
            buffer[offset + 1] = (byte)value.Minute;
            buffer[offset + 2] = (byte)value.Second;
            buffer[offset + 3] = (byte)(value.Millisecond/10);

            return 4;
        }

        public static int encode_context_time(byte[] buffer, int offset, byte tag_number, DateTime value)
        {
            int len = 0;        /* return value */

            /* length of time is 4 octets, as per 20.2.13 */
            len = encode_tag(buffer, offset, tag_number, true, 4);
            len += encode_bacnet_time(buffer, offset + len, value);

            return len;
        }

        public static int encode_bacnet_date(byte[] buffer, int offset, DateTime value)
        {
            /* allow 2 digit years */
            if (value.Year >= 1900)
            {
                buffer[offset] = (byte)(value.Year - 1900);
            }
            else if (value.Year < 0x100)
            {
                buffer[offset] = (byte)value.Year;
            }
            else
                throw new Exception("Date is rubbish");


            buffer[offset + 1] = (byte)value.Month;
            buffer[offset + 2] = (byte)value.Day;
            buffer[offset + 3] = (byte)value.DayOfWeek;

            return 4;
        }

        public static int encode_application_date(byte[] buffer, int offset, DateTime value)
        {
            int len = 0;

            /* assumes that the tag only consumes 1 octet */
            len = encode_bacnet_date(buffer, offset + 1, value);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DATE, false, (uint)len);

            return len;
        }

        public static int encode_application_time(byte[] buffer, int offset, DateTime value)
        {
            int len = 0;

            /* assumes that the tag only consumes 1 octet */
            len = encode_bacnet_time(buffer, offset + 1, value);
            len += encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_TIME, false, (uint)len);

            return len;
        }

        public static int bacapp_encode_datetime(byte[] buffer, int offset, DateTime value)
        {
            int len = 0;

            if (value != new DateTime(1, 1, 1))
            {
                len += encode_application_date(buffer, offset + len, value);
                len += encode_application_time(buffer, offset + len, value);
            }
            return len;
        }

        public static int bacapp_encode_context_datetime(byte[] buffer, int offset, byte tag_number, DateTime value)
        {
            int len = 0;

            if (value != new DateTime(1,1,1))
            {
                len += encode_opening_tag(buffer, offset + len, tag_number);
                len += bacapp_encode_datetime(buffer, offset + len, value);
                len += encode_closing_tag(buffer, offset + len, tag_number);
            }
            return len;
        }

        public static int bacapp_encode_timestamp(byte[] buffer, int offset, BACNET_GENERIC_TIME value)
        {
            int len = 0;        /* length of each encoding */

                switch (value.Tag)
                {
                    case BACNET_GENERIC_TIME.BACNET_TIMESTAMP_TAG.TIME_STAMP_TIME:
                        len = encode_context_time(buffer, offset, 0, value.Time);
                        break;

                    case BACNET_GENERIC_TIME.BACNET_TIMESTAMP_TAG.TIME_STAMP_SEQUENCE:
                        len = encode_context_unsigned(buffer, offset, 1, value.Sequence);
                        break;

                    case BACNET_GENERIC_TIME.BACNET_TIMESTAMP_TAG.TIME_STAMP_DATETIME:
                        len = bacapp_encode_context_datetime(buffer, offset, 2, value.Time);
                        break;
                    case BACNET_GENERIC_TIME.BACNET_TIMESTAMP_TAG.TIME_STAMP_NONE:
                        break;
                    default:
                        throw new NotImplementedException();
                }

            return len;
        }

        public static int bacapp_encode_context_timestamp(byte[] buffer, int offset, byte tag_number, BACNET_GENERIC_TIME value)
        {
            int len = 0;        /* length of each encoding */

            if (value.Tag != BACNET_GENERIC_TIME.BACNET_TIMESTAMP_TAG.TIME_STAMP_NONE)
            {
                len += encode_opening_tag(buffer, offset + len, tag_number);
                len += bacapp_encode_timestamp(buffer, offset + len, value);
                len += encode_closing_tag(buffer, offset + len, tag_number);
            }

            return len;
        }

        public static int encode_application_character_string(byte[] buffer, int offset, int max_length, string value)
        {
            int len = 0;
            int string_len = 0;

            string_len = value.Length + 1 /* for encoding */ ;
            len = encode_tag(buffer, offset, (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CHARACTER_STRING, false, (UInt32)string_len);
            if ((len + string_len) <= max_length)
            {
                len += encode_bacnet_character_string(buffer, offset + len, max_length, value);
            }
            else
            {
                len = 0;
            }

            return len;
        }

        public static int encode_bacnet_character_string_safe(byte[] buffer, int offset, int max_apdu, BACNET_CHARACTER_STRING_ENCODING encoding, string value, int length)
        {
            int apdu_len = 1 /*encoding */ ;
            int i;

            apdu_len += length;
            if (apdu_len <= max_apdu)
            {
                buffer[offset] = (byte)encoding;
                for (i = 0; i < length; i++)
                {
                    buffer[offset + 1 + i] = (byte)value[i];
                }
            }
            else
            {
                apdu_len = 0;
            }

            return apdu_len;
        }

        public static int encode_bacnet_character_string(byte[] buffer, int offset, int max_length, string value)
        {
            return (int)encode_bacnet_character_string_safe(buffer, offset, max_length, BACNET_CHARACTER_STRING_ENCODING.CHARACTER_ANSI_X34, value, value.Length);
        }

        public static int encode_unsigned16(byte[] buffer, int offset, UInt16 value)
        {
            buffer[offset + 0] = (byte)((value & 0xff00) >> 8);
            buffer[offset + 1] = (byte)((value & 0x00ff) >> 0);
            return 2;
        }

        public static int encode_unsigned24(byte[] buffer, int offset, UInt32 value)
        {
            buffer[offset + 0] = (byte)((value & 0xff0000) >> 16);
            buffer[offset + 1] = (byte)((value & 0x00ff00) >> 8);
            buffer[offset + 2] = (byte)((value & 0x0000ff) >> 0);
            return 3;
        }

        public static int encode_unsigned32(byte[] buffer, int offset, UInt32 value)
        {
            buffer[offset + 0] = (byte)((value & 0xff000000) >> 24);
            buffer[offset + 1] = (byte)((value & 0x00ff0000) >> 16);
            buffer[offset + 2] = (byte)((value & 0x0000ff00) >> 8);
            buffer[offset + 3] = (byte)((value & 0x000000ff) >> 0);
            return 4;
        }

        public static int encode_signed16(byte[] buffer, int offset, Int16 value)
        {
            buffer[offset + 0] = (byte)((value & 0xff00) >> 8);
            buffer[offset + 1] = (byte)((value & 0x00ff) >> 0);
            return 2;
        }

        public static int encode_signed24(byte[] buffer, int offset, Int32 value)
        {
            buffer[offset + 0] = (byte)((value & 0xff0000) >> 16);
            buffer[offset + 1] = (byte)((value & 0x00ff00) >> 8);
            buffer[offset + 2] = (byte)((value & 0x0000ff) >> 0);
            return 3;
        }

        public static int encode_signed32(byte[] buffer, int offset, Int32 value)
        {
            buffer[offset + 0] = (byte)((value & 0xff000000) >> 24);
            buffer[offset + 1] = (byte)((value & 0x00ff0000) >> 16);
            buffer[offset + 2] = (byte)((value & 0x0000ff00) >> 8);
            buffer[offset + 3] = (byte)((value & 0x000000ff) >> 0);
            return 4;
        }

        public static int decode_unsigned(byte[] buffer, int offset, uint len_value, out uint value)
        {
            ushort unsigned16_value = 0;

            switch (len_value)
            {
                case 1:
                    value = buffer[offset];
                    break;
                case 2:
                    decode_unsigned16(buffer, offset, out unsigned16_value);
                    value = unsigned16_value;
                    break;
                case 3:
                    decode_unsigned24(buffer, offset, out value);
                    break;
                case 4:
                    decode_unsigned32(buffer, offset, out value);
                    break;
                default:
                    value = 0;
                    break;
            }

            return (int)len_value;
        }

        public static int decode_unsigned32(byte[] buffer, int offset, out uint value)
        {
            value = ((uint)((((uint)buffer[offset+0]) << 24) & 0xff000000));
            value |= ((uint)((((uint)buffer[offset + 1]) << 16) & 0x00ff0000));
            value |= ((uint)((((uint)buffer[offset + 2]) << 8) & 0x0000ff00));
            value |= ((uint)(((uint)buffer[offset + 3]) & 0x000000ff));
            return 4;
        }

        public static int decode_unsigned24(byte[] buffer, int offset, out uint value)
        {
            value = ((uint)((((uint)buffer[offset + 0]) << 16) & 0x00ff0000));
            value |= ((uint)((((uint)buffer[offset + 1]) << 8) & 0x0000ff00));
            value |= ((uint)(((uint)buffer[offset + 2]) & 0x000000ff));
            return 3;
        }

        public static int decode_unsigned16(byte[] buffer, int offset, out ushort value)
        {
            value = ((ushort)((((uint)buffer[offset + 0]) << 8) & 0x0000ff00));
            value |= ((ushort)(((uint)buffer[offset + 1]) & 0x000000ff));
            return 2;
        }

        public static int decode_unsigned8(byte[] buffer, int offset, out byte value)
        {
            value = buffer[offset + 0];
            return 1;
        }

        public static int decode_signed32(byte[] buffer, int offset, out int value)
        {
            value = ((int)((((int)buffer[offset + 0]) << 24) & 0xff000000));
            value |= ((int)((((int)buffer[offset + 1]) << 16) & 0x00ff0000));
            value |= ((int)((((int)buffer[offset + 2]) << 8) & 0x0000ff00));
            value |= ((int)(((int)buffer[offset + 3]) & 0x000000ff));
            return 4;
        }

        public static int decode_signed24(byte[] buffer, int offset, out int value)
        {
            value = ((int)((((int)buffer[offset + 0]) << 16) & 0x00ff0000));
            value |= ((int)((((int)buffer[offset + 1]) << 8) & 0x0000ff00));
            value |= ((int)(((int)buffer[offset + 2]) & 0x000000ff));
            return 3;
        }

        public static int decode_signed16(byte[] buffer, int offset, out short value)
        {
            value = ((short)((((int)buffer[offset + 0]) << 8) & 0x0000ff00));
            value |= ((short)(((int)buffer[offset + 1]) & 0x000000ff));
            return 2;
        }

        public static int decode_signed8(byte[] buffer, int offset, out sbyte value)
        {
            value = (sbyte)buffer[offset + 0];
            return 1;
        }

        public static bool IS_EXTENDED_TAG_NUMBER(byte x) 
        {
            return ((x & 0xF0) == 0xF0);
        }

        public static bool IS_EXTENDED_VALUE(byte x)
        {
            return ((x & 0x07) == 5);
        }

        public static bool IS_CONTEXT_SPECIFIC(byte x)
        {
            return ((x & 0x8) == 0x8);
        }

        public static bool IS_OPENING_TAG(byte x)
        {
            return ((x & 0x07) == 6);
        }

        public static bool IS_CLOSING_TAG(byte x)
        {
            return ((x & 0x07) == 7);
        }

        public static int decode_tag_number(byte[] buffer, int offset, out byte tag_number)
        {
            int len = 1;        /* return value */

            /* decode the tag number first */
            if (IS_EXTENDED_TAG_NUMBER(buffer[offset]))
            {
                /* extended tag */
                tag_number = buffer[offset+1];
                len++;
            }
            else
            {
                tag_number = (byte)(buffer[offset] >> 4);
            }

            return len;
        }

        public static int decode_signed(byte[] buffer, int offset, uint len_value, out int value)
        {
                switch (len_value)
                {
                    case 1:
                        sbyte sbyte_value;
                        decode_signed8(buffer, offset, out sbyte_value);
                        value = sbyte_value;
                        break;
                    case 2:
                        short short_value;
                        decode_signed16(buffer, offset, out short_value);
                        value = short_value;
                        break;
                    case 3:
                        decode_signed24(buffer, offset, out value);
                        break;
                    case 4:
                        decode_signed32(buffer, offset, out value);
                        break;
                    default:
                        value = 0;
                        break;
                }

            return (int)len_value;
        }

        public static int decode_real(byte[] buffer, int offset, out float value)
        {
            byte[] tmp = new byte[] { buffer[offset + 3], buffer[offset + 2], buffer[offset + 1], buffer[offset + 0] };
            value = BitConverter.ToSingle(tmp, 0);
            return 4;
        }

        public static int decode_real_safe(byte[] buffer, int offset, uint len_value, out float value)
        {
            if (len_value != 4)
            {
                value = 0.0f;
                return (int)len_value;
            }
            else
            {
                return decode_real(buffer, offset, out value);
            }
        }

        public static int decode_double(byte[] buffer, int offset, out double value)
        {
            byte[] tmp = new byte[] { buffer[offset + 7], buffer[offset + 6], buffer[offset + 5], buffer[offset + 4], buffer[offset + 3], buffer[offset + 2], buffer[offset + 1], buffer[offset + 0] };
            value = BitConverter.ToDouble(tmp, 0);
            return 8;
        }

        public static int decode_double_safe(byte[] buffer, int offset, uint len_value, out double value)
        {
            if (len_value != 8)
            {
                value = 0.0f;
                return (int)len_value;
            }
            else
            {
                return decode_double(buffer, offset, out value);
            }
        }

        private static bool octetstring_init(byte[] buffer, int offset, int max_length, byte[] octet_string, int octet_string_offset, uint octet_string_length)
        {
            bool status = false;        /* return value */

            if (octet_string_length <= max_length)
            {
                if (octet_string != null) Array.Copy(buffer, offset, octet_string, octet_string_offset, Math.Min(octet_string.Length, buffer.Length - offset));
                status = true;
            }

            return status;
        }

        public static int decode_octet_string(byte[] buffer, int offset, int max_length, byte[] octet_string, int octet_string_offset, uint octet_string_length)
        {
            int len = 0;        /* return value */

            if (octetstring_init(buffer, offset, max_length, octet_string, octet_string_offset, octet_string_length))
            {
                len = (int)octet_string_length;
            }

            return len;
        }

        public static int decode_context_octet_string(byte[] buffer, int offset, int max_length, byte tag_number, byte[] octet_string, int octet_string_offset)
        {
            int len = 0;        /* return value */
            uint len_value = 0;

            octet_string = null;
            if (decode_is_context_tag(buffer, offset, tag_number))
            {
                len +=
                    decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);

                if (octetstring_init(buffer, offset + len, max_length, octet_string, octet_string_offset, len_value))
                {
                    len += (int)len_value;
                }
            }
            else
                len = -1;

            return len;
        }


        /* returns false if the string exceeds capacity
           initialize by using value=NULL */
        private static bool characterstring_init(byte[] buffer, int offset, int max_length, byte encoding, uint length, out string char_string)
        {
            bool status = false;        /* return value */
            int i;

            char_string = "";
            /* save a byte at the end for NULL -
               note: assumes printable characters */
            if (length <= max_length)
            {
                for (i = 0; i < length; i++)
                {
                    char_string += (char)buffer[offset + i];
                }
                status = true;
            }

            return status;
        }

        public static int decode_character_string(byte[] buffer, int offset, int max_length, uint len_value, out string char_string)
        {
            int len = 0;        /* return value */
            bool status = false;

            status = characterstring_init(buffer, offset + 1, max_length, buffer[offset], len_value - 1, out char_string);
            if (status)
            {
                len = (int)len_value;
            }

            return len;
        }

        private static bool bitstring_set_octet(ref BACNET_BIT_STRING bit_string, byte index, byte octet)
        {
            bool status = false;

            if (index < MAX_BITSTRING_BYTES)
            {
                bit_string.value[index] = octet;
                status = true;
            }

            return status;
        }

        private static bool bitstring_set_bits_used(ref BACNET_BIT_STRING bit_string, byte bytes_used, byte unused_bits)
        {
            bool status = false;

            /* FIXME: check that bytes_used is at least one? */
            bit_string.bits_used = (byte)(bytes_used * 8);
            bit_string.bits_used -= unused_bits;
            status = true;

            return status;
        }

        public static int decode_bitstring(byte[] buffer, int offset, uint len_value, out BACNET_BIT_STRING bit_string)
        {
            int len = 0;
            byte unused_bits = 0;
            uint i = 0;
            uint bytes_used = 0;

            bit_string = new BACNET_BIT_STRING();
            bit_string.value = new byte[MAX_BITSTRING_BYTES];
            if (len_value > 0)
            {
                /* the first octet contains the unused bits */
                bytes_used = len_value - 1;
                if (bytes_used <= MAX_BITSTRING_BYTES)
                {
                    len = 1;
                    for (i = 0; i < bytes_used; i++)
                    {
                        bitstring_set_octet(ref bit_string, (byte)i, byte_reverse_bits(buffer[offset+len++]));
                    }
                    unused_bits = (byte)(buffer[offset] & 0x07);
                    bitstring_set_bits_used(ref bit_string, (byte)bytes_used, unused_bits);
                }
            }

            return len;
        }

        public static int decode_context_character_string(byte[] buffer, int offset, int max_length, byte tag_number, out string char_string)
        {
            int len = 0;        /* return value */
            bool status = false;
            uint len_value = 0;

            char_string = null;
            if (decode_is_context_tag(buffer, offset + len, tag_number))
            {
                len +=
                    decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);

                status =
                    characterstring_init(buffer, offset + 1 + len, max_length, buffer[offset + len], len_value - 1, out char_string);
                if (status)
                {
                    len += (int)len_value;
                }
            }
            else
                len = -1;

            return len;
        }

        public static int decode_date(byte[] buffer, int offset, out DateTime bdate)
        {
            int year = (ushort)(buffer[offset] + 1900);
            int month = buffer[offset+1];
            int day = buffer[offset + 2];
            int wday = buffer[offset + 3];

            if(month == 0xFF && day == 0xFF && wday == 0xFF && (year-1900) == 0xFF)
                bdate = new DateTime(1, 1, 1);
            else
                bdate = new DateTime(year, month, day);

            return 4;
        }

        public static int decode_date_safe(byte[] buffer, int offset, uint len_value, out DateTime bdate)
        {
            if (len_value != 4)
            {
                bdate = new DateTime(1, 1, 1);
                return (int)len_value;
            }
            else
            {
                return decode_date(buffer, offset, out bdate);
            }
        }

        public static int decode_bacnet_time(byte[] buffer, int offset, out DateTime btime)
        {
            int hour = buffer[offset+0];
            int min = buffer[offset + 1];
            int sec = buffer[offset + 2];
            int hundredths = buffer[offset + 3];
            if (hour == 0xFF && min == 0xFF && sec == 0xFF && hundredths == 0xFF)
                btime = new DateTime(1, 1, 1);
            else
                btime = new DateTime(1, 1, 1, hour, min, sec, hundredths * 10);

            return 4;
        }

        public static int decode_bacnet_time_safe(byte[] buffer, int offset, uint len_value, out DateTime btime)
        {
            if (len_value != 4)
            {
                btime = new DateTime(1, 1, 1);
                return (int)len_value;
            }
            else
            {
                return decode_bacnet_time(buffer, offset, out btime);
            }
        }

        public static int decode_object_id(byte[] buffer, int offset, out ushort object_type, out uint instance)
        {
            uint value = 0;
            int len = 0;

            len = decode_unsigned32(buffer, offset, out value);
            object_type =
                (ushort)(((value >> BACNET_INSTANCE_BITS) & BACNET_MAX_OBJECT));
            instance = (value & BACNET_MAX_INSTANCE);

            return len;
        }

        public static int decode_object_id_safe(byte[] buffer, int offset, uint len_value, out ushort object_type, out uint instance)
        {
            if (len_value != 4)
            {
                object_type = 0;
                instance = 0;
                return 0;
            }
            else
            {
                return decode_object_id(buffer, offset, out object_type, out instance);
            }
        }

        public static int decode_context_object_id(byte[] buffer, int offset, byte tag_number, out ushort object_type, out uint instance)
        {
            int len = 0;

            if (decode_is_context_tag_with_length(buffer, offset + len, tag_number, out len))
            {
                len += decode_object_id(buffer, offset + len, out object_type, out instance);
            }
            else
            {
                object_type = 0;
                instance = 0;
                len = -1;
            }
            return len;
        }

        public static int decode_application_time(byte[] buffer, int offset, out DateTime btime)
        {
            int len = 0;
            byte tag_number;
            decode_tag_number(buffer, offset + len, out tag_number);

            if (tag_number == (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_TIME)
            {
                len++;
                len += decode_bacnet_time(buffer, offset + len, out btime);
            }
            else
            {
                btime = new DateTime(1, 1, 1);
                len = -1;
            }
            return len;
        }


        public static int decode_context_bacnet_time(byte[] buffer, int offset, byte tag_number, out DateTime btime)
        {
            int len = 0;

            if (decode_is_context_tag_with_length(buffer, offset + len, tag_number, out len))
            {
                len += decode_bacnet_time(buffer, offset + len, out btime);
            }
            else
            {
                btime = new DateTime(1, 1, 1);
                len = -1;
            }
            return len;
        }

        public static int decode_application_date(byte[] buffer, int offset, out DateTime bdate)
        {
            int len = 0;
            byte tag_number;
            decode_tag_number(buffer, offset + len, out tag_number);

            if (tag_number == (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DATE)
            {
                len++;
                len += decode_date(buffer, offset + len, out bdate);
            }
            else
            {
                bdate = new DateTime(1, 1, 1);
                len = -1;
            }
            return len;
        }

        public static bool decode_is_context_tag_with_length(byte[] buffer, int offset, byte tag_number, out int tag_length)
        {
            byte my_tag_number = 0;

            tag_length = decode_tag_number(buffer, offset, out my_tag_number);

            return (bool)(IS_CONTEXT_SPECIFIC(buffer[offset]) &&
                (my_tag_number == tag_number));
        }

        public static int decode_context_date(byte[] buffer, int offset, byte tag_number, out DateTime bdate)
        {
            int len = 0;

            if (decode_is_context_tag_with_length(buffer, offset + len, tag_number, out len))
            {
                len += decode_date(buffer, offset + len, out bdate);
            }
            else
            {
                bdate = new DateTime(1, 1, 1);
                len = -1;
            }
            return len;
        }

        public static int bacapp_decode_data(byte[] buffer, int offset, int max_length, BACNET_APPLICATION_TAG tag_data_type, uint len_value_type, out BACNET_VALUE value)
        {
            int len = 0;
            uint uint_value;
            int int_value;

            value = new BACNET_VALUE();
            value.Tag = tag_data_type;

            switch (tag_data_type)
            {
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL:
                    /* nothing else to do */
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN:
                    value.Value = len_value_type > 0 ? true : false;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    len = decode_unsigned(buffer, offset, len_value_type, out uint_value);
                    value.Value = uint_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_SIGNED_INT:
                    len = decode_signed(buffer, offset, len_value_type, out int_value);
                    value.Value = int_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL:
                    float float_value;
                    len = decode_real_safe(buffer, offset, len_value_type, out float_value);
                    value.Value = float_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DOUBLE:
                    double double_value;
                    len = decode_double_safe(buffer, offset, len_value_type, out double_value);
                    value.Value = double_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING:
                    byte[] octet_string;
                    len = decode_octet_string(buffer, offset, max_length, null, 0, len_value_type);
                    octet_string = new byte[len];
                    len = decode_octet_string(buffer, offset, max_length, octet_string, 0, len_value_type);
                    value.Value = octet_string;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    string string_value;
                    len = decode_character_string(buffer, offset, max_length, len_value_type, out string_value);
                    value.Value = string_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BIT_STRING:
                    BACNET_BIT_STRING bit_value;
                    len = decode_bitstring(buffer, offset, len_value_type, out bit_value);
                    value.Value = bit_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_ENUMERATED:
                    len = decode_enumerated(buffer, offset, len_value_type, out uint_value);
                    value.Value = uint_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DATE:
                    DateTime date_value;
                    len = decode_date_safe(buffer, offset, len_value_type, out date_value);
                    value.Value = date_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_TIME:
                    DateTime time_value;
                    len = decode_bacnet_time_safe(buffer, offset, len_value_type, out time_value);
                    value.Value = time_value;
                    break;
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID:
                    {
                        ushort object_type = 0;
                        uint instance = 0;
                        len = decode_object_id_safe(buffer, offset, len_value_type, out object_type, out instance);
                        value.Value = new BACNET_OBJECT_ID((BACNET_OBJECT_TYPE)object_type, instance);
                    }
                    break;
                default:
                    break;
            }

            //if ((len == 0) && (tag_data_type != BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL) &&
            //(tag_data_type != BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN) &&
            //(tag_data_type != BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING))
            //{
            //    /* indicate that we were not able to decode the value */
            //    value.Tag = BACNET_APPLICATION_TAG.MAX_BACNET_APPLICATION_TAG;
            //}
            return len;
        }

        /* returns the fixed tag type for certain context tagged properties */
        private static BACNET_APPLICATION_TAG bacapp_context_tag_type(BACNET_PROPERTY_ID property, byte tag_number)
        {
            BACNET_APPLICATION_TAG tag = BACNET_APPLICATION_TAG.MAX_BACNET_APPLICATION_TAG;

            switch (property)
            {
                case BACNET_PROPERTY_ID.PROP_ACTUAL_SHED_LEVEL:
                case BACNET_PROPERTY_ID.PROP_REQUESTED_SHED_LEVEL:
                case BACNET_PROPERTY_ID.PROP_EXPECTED_SHED_LEVEL:
                    switch (tag_number)
                    {
                        case 0:
                        case 1:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 2:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL;
                            break;
                        default:
                            break;
                    }
                    break;
                case BACNET_PROPERTY_ID.PROP_ACTION:
                    switch (tag_number)
                    {
                        case 0:
                        case 1:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        case 2:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_ENUMERATED;
                            break;
                        case 3:
                        case 5:
                        case 6:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 7:
                        case 8:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN;
                            break;
                        case 4:        /* propertyValue: abstract syntax */
                        default:
                            break;
                    }
                    break;
                case BACNET_PROPERTY_ID.PROP_LIST_OF_GROUP_MEMBERS:
                    /* Sequence of ReadAccessSpecification */
                    switch (tag_number)
                    {
                        case 0:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        default:
                            break;
                    }
                    break;
                case BACNET_PROPERTY_ID.PROP_EXCEPTION_SCHEDULE:
                    switch (tag_number)
                    {
                        case 1:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        case 3:
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 0:        /* calendarEntry: abstract syntax + context */
                        case 2:        /* list of BACnetTimeValue: abstract syntax */
                        default:
                            break;
                    }
                    break;
                case BACNET_PROPERTY_ID.PROP_LOG_DEVICE_OBJECT_PROPERTY:
                    switch (tag_number)
                    {
                        case 0:        /* Object ID */
                        case 3:        /* Device ID */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        case 1:        /* Property ID */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_ENUMERATED;
                            break;
                        case 2:        /* Array index */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        default:
                            break;
                    }
                    break;
                case BACNET_PROPERTY_ID.PROP_SUBORDINATE_LIST:
                    /* BACnetARRAY[N] of BACnetDeviceObjectReference */
                    switch (tag_number)
                    {
                        case 0:        /* Optional Device ID */
                        case 1:        /* Object ID */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        default:
                            break;
                    }
                    break;

                case BACNET_PROPERTY_ID.PROP_RECIPIENT_LIST:
                    /* List of BACnetDestination */
                    switch (tag_number)
                    {
                        case 0:        /* Device Object ID */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        default:
                            break;
                    }
                    break;
                case BACNET_PROPERTY_ID.PROP_ACTIVE_COV_SUBSCRIPTIONS:
                    /* BACnetCOVSubscription */
                    switch (tag_number)
                    {
                        case 0:        /* BACnetRecipientProcess */
                        case 1:        /* BACnetObjectPropertyReference */
                            break;
                        case 2:        /* issueConfirmedNotifications */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN;
                            break;
                        case 3:        /* timeRemaining */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 4:        /* covIncrement */
                            tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            return tag;
        }

        public static int bacapp_decode_context_data(byte[] buffer, int offset, uint max_apdu_len, BACNET_APPLICATION_TAG property_tag, out BACNET_VALUE value)
        {
            int apdu_len = 0, len = 0;
            int tag_len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            value = new BACNET_VALUE();

            if (IS_CONTEXT_SPECIFIC(buffer[offset]))
            {
                //value->context_specific = true;
                tag_len = decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                apdu_len = tag_len;
                /* Empty construct : (closing tag) => returns NULL value */
                if (tag_len > 0 && (tag_len <= max_apdu_len) && !decode_is_closing_tag_number(buffer, offset + len, tag_number))
                {
                    //value->context_tag = tag_number;
                    if (property_tag < BACNET_APPLICATION_TAG.MAX_BACNET_APPLICATION_TAG)
                    {
                        len = bacapp_decode_data(buffer, offset + apdu_len, (int)max_apdu_len, property_tag, len_value_type, out value);
                        apdu_len += len;
                    }
                    else if (len_value_type > 0)
                    {
                        /* Unknown value : non null size (elementary type) */
                        apdu_len += (int)len_value_type;
                        /* SHOULD NOT HAPPEN, EXCEPTED WHEN READING UNKNOWN CONTEXTUAL PROPERTY */
                    }
                    else
                        apdu_len = -1;
                }
                else if (tag_len == 1)        /* and is a Closing tag */
                    apdu_len = 0;       /* Don't advance over that closing tag. */
            }

            return apdu_len;
        }

        public static int bacapp_decode_application_data(byte[] buffer, int offset, int max_offset, BACNET_PROPERTY_ID property_id, out BACNET_VALUE value)
        {
            int len = 0;
            int tag_len = 0;
            int decode_len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            value = new BACNET_VALUE();

            /* FIXME: use max_apdu_len! */
            if (!IS_CONTEXT_SPECIFIC(buffer[offset]))
            {
                tag_len = decode_tag_number_and_value(buffer, offset, out tag_number, out len_value_type);
                if (tag_len > 0)
                {
                    len += tag_len;
                    decode_len = bacapp_decode_data(buffer, offset + len, max_offset, (BACNET_APPLICATION_TAG)tag_number, len_value_type, out value);
                    if (value.Tag != BACNET_APPLICATION_TAG.MAX_BACNET_APPLICATION_TAG)
                    {
                        len += decode_len;
                    }
                    else
                    {
                        len = -1;
                    }
                }
            }
            else
            {
                return bacapp_decode_context_application_data(buffer, offset, max_offset, property_id, out value);
            }

            return len;
        }

        public static int bacapp_decode_context_application_data(byte[] buffer, int offset, int max_offset, BACNET_PROPERTY_ID property_id, out BACNET_VALUE value)
        {
            int len = 0;
            int tag_len = 0;
            byte tag_number = 0;
            byte sub_tag_number = 0;
            uint len_value_type = 0;

            value = new BACNET_VALUE();

            /* FIXME: use max_apdu_len! */
            if (IS_CONTEXT_SPECIFIC(buffer[offset]))
            {
                value.Tag = BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC;
                List<BACNET_VALUE> list = new List<BACNET_VALUE>();

                decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                while (((len + offset) <= max_offset) && !IS_CLOSING_TAG(buffer[offset + len]))
                {
                    tag_len = decode_tag_number_and_value(buffer, offset + len, out sub_tag_number, out len_value_type);
                    if (tag_len < 0) return -1;

                    if (len_value_type == 0)
                    {
                        BACNET_VALUE sub_value;
                        len += tag_len;
                        tag_len = bacapp_decode_application_data(buffer, offset + len, max_offset, property_id, out sub_value);
                        if (tag_len < 0) return -1;
                        list.Add(sub_value);
                        len += tag_len;
                    }
                    else
                    {
                        BACNET_VALUE sub_value = new BACNET_VALUE();

                        //override tag_number
                        BACNET_APPLICATION_TAG override_tag_number = bacapp_context_tag_type(property_id, sub_tag_number);
                        if (override_tag_number != BACNET_APPLICATION_TAG.MAX_BACNET_APPLICATION_TAG) sub_tag_number = (byte)override_tag_number;

                        //try app decode
                        int sub_tag_len = bacapp_decode_data(buffer, offset + len + tag_len, max_offset, (BACNET_APPLICATION_TAG)sub_tag_number, len_value_type, out sub_value);
                        if (sub_tag_len == (int)len_value_type)
                        {
                            list.Add(sub_value);
                            len += tag_len + (int)len_value_type;
                        }
                        else
                        {
                            //fallback to copy byte array
                            byte[] context_specific = new byte[(int)len_value_type];
                            Array.Copy(buffer, offset + len + tag_len, context_specific, 0, (int)len_value_type);
                            sub_value = new BACNET_VALUE(BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC, context_specific);

                            list.Add(sub_value);
                            len += tag_len + (int)len_value_type;
                        }
                    }
                }
                if ((len + offset) > max_offset) return -1;

                //end tag
                if (decode_is_closing_tag_number(buffer, offset + len, tag_number))
                    len++;

                //context specifique is array of BACNET_VALUE
                value.Value = list.ToArray();
            }
            else
            {
                return -1;
            }

            return len;
        }

        public static int decode_object_id(byte[] buffer, int offset, out BACNET_OBJECT_TYPE object_type, out uint instance)
        {
            uint value = 0;
            int len = 0;

            len = decode_unsigned32(buffer, offset, out value);
            object_type = (BACNET_OBJECT_TYPE)(((value >> BACNET_INSTANCE_BITS) & BACNET_MAX_OBJECT));
            instance = (value & BACNET_MAX_INSTANCE);

            return len;
        }

        public static int decode_enumerated(byte[] buffer, int offset, uint len_value, out uint value)
        {
            int len;
            len = decode_unsigned(buffer, offset, len_value, out value);
            return len;
        }

        public static bool decode_is_context_tag(byte[] buffer, int offset, byte tag_number)
        {
            byte my_tag_number = 0;

            decode_tag_number(buffer, offset, out my_tag_number);
            return (bool)(IS_CONTEXT_SPECIFIC(buffer[offset]) && (my_tag_number == tag_number));
        }

        public static bool decode_is_opening_tag_number(byte[] buffer, int offset, byte tag_number)
        {
            byte my_tag_number = 0;

            decode_tag_number(buffer, offset, out my_tag_number);
            return (bool)(IS_OPENING_TAG(buffer[offset]) && (my_tag_number == tag_number));
        }

        public static bool decode_is_closing_tag_number(byte[] buffer, int offset, byte tag_number)
        {
            byte my_tag_number = 0;

            decode_tag_number(buffer, offset, out my_tag_number);
            return (bool)(IS_CLOSING_TAG(buffer[offset]) && (my_tag_number == tag_number));
        }

        public static bool decode_is_closing_tag(byte[] buffer, int offset)
        {
            return (bool)((buffer[offset] & 0x07) == 7);
        }

        public static bool decode_is_opening_tag(byte[] buffer, int offset)
        {
            return (bool)((buffer[offset] & 0x07) == 6);
        }

        public static int decode_tag_number_and_value(byte[] buffer, int offset, out byte tag_number, out uint value)
        {
            int len = 1;
            ushort value16;
            uint value32;

            len = decode_tag_number(buffer, offset, out tag_number);
            if (IS_EXTENDED_VALUE(buffer[offset]))
            {
                /* tagged as uint32_t */
                if (buffer[offset + len] == 255)
                {
                    len++;
                    len += decode_unsigned32(buffer, offset + len, out value32);
                    value = value32;
                }
                /* tagged as uint16_t */
                else if (buffer[offset + len] == 254)
                {
                    len++;
                    len += decode_unsigned16(buffer, offset + len, out value16);
                    value = value16;
                }
                /* no tag - must be uint8_t */
                else
                {
                    value = buffer[offset + len];
                    len++;
                }
            }
            else if (IS_OPENING_TAG(buffer[offset]))
            {
                value = 0;
            }
            else if (IS_CLOSING_TAG(buffer[offset]))
            {
                /* closing tag */
                value = 0;
            }
            else
            {
                /* small value */
                value = (uint)(buffer[offset] & 0x07);
            }

            return len;
        }
    }

    public class SERVICES
    {
        public static int EncodeIamBroadcast(byte[] buffer, int offset, UInt32 device_id, UInt32 max_apdu, BACNET_SEGMENTATION segmentation, UInt16 vendor_id)
        {
            int org_offset = offset;

            offset += ASN1.encode_application_object_id(buffer, offset, BACNET_OBJECT_TYPE.OBJECT_DEVICE, device_id);
            offset += ASN1.encode_application_unsigned(buffer, offset, max_apdu);
            offset += ASN1.encode_application_enumerated(buffer, offset, (uint)segmentation);
            offset += ASN1.encode_application_unsigned(buffer, offset, vendor_id);

            return offset - org_offset;
        }

        public static int DecodeIamBroadcast(byte[] buffer, int offset, out UInt32 device_id, out UInt32 max_apdu, out BACNET_SEGMENTATION segmentation, out UInt16 vendor_id)
        {
            int len;
            int apdu_len = 0;
            int org_offset = offset;
            uint len_value;
            byte tag_number;
            BACNET_OBJECT_ID object_id;
            uint decoded_value;

            device_id = 0;
            max_apdu = 0;
            segmentation = BACNET_SEGMENTATION.SEGMENTATION_NONE;
            vendor_id = 0;

            /* OBJECT ID - object id */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID)
                return -1;
            len = ASN1.decode_object_id(buffer, offset + apdu_len, out object_id.type, out object_id.instance);
            apdu_len += len;
            if (object_id.type != BACNET_OBJECT_TYPE.OBJECT_DEVICE)
                return -1;
            device_id = object_id.instance;
            /* MAX APDU - unsigned */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len = ASN1.decode_unsigned(buffer, offset + apdu_len, len_value, out decoded_value);
            apdu_len += len;
            max_apdu = decoded_value;
            /* Segmentation - enumerated */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_ENUMERATED)
                return -1;
            len = ASN1.decode_enumerated(buffer, offset + apdu_len, len_value, out decoded_value);
            apdu_len += len;
            if (decoded_value >= (uint)BACNET_SEGMENTATION.MAX_BACNET_SEGMENTATION)
                return -1;
            segmentation = (BACNET_SEGMENTATION)decoded_value;
            /* Vendor ID - unsigned16 */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len = ASN1.decode_unsigned(buffer, offset + apdu_len, len_value, out decoded_value);
            apdu_len += len;
            if (decoded_value > 0xFFFF)
                return -1;
            vendor_id = (ushort)decoded_value;

            return offset - org_offset;
        }

        public static int EncodeIhaveBroadcast(byte[] buffer, int offset, BACNET_OBJECT_ID device_id, BACNET_OBJECT_ID object_id, string object_name)
        {
            int org_offset = offset;

            /* deviceIdentifier */
            offset += ASN1.encode_application_object_id(buffer, offset, device_id.type, device_id.instance);
            /* objectIdentifier */
            offset += ASN1.encode_application_object_id(buffer, offset, object_id.type, object_id.instance);
            /* objectName */
            offset += ASN1.encode_application_character_string(buffer, offset, buffer.Length, object_name);

            return offset - org_offset;
        }

        public static int EncodeWhoHasBroadcast(byte[] buffer, int offset, int low_limit, int high_limit, BACNET_OBJECT_ID object_id, string object_name)
        {
            int org_offset = offset;

            /* optional limits - must be used as a pair */
            if ((low_limit >= 0) && (low_limit <= ASN1.BACNET_MAX_INSTANCE) && (high_limit >= 0) && (high_limit <= ASN1.BACNET_MAX_INSTANCE))
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 0, (uint)low_limit);
                offset += ASN1.encode_context_unsigned(buffer, offset, 1, (uint)high_limit);
            }
            if (!string.IsNullOrEmpty(object_name))
            {
                offset += ASN1.encode_context_character_string(buffer, offset, buffer.Length, 3, object_name);
            }
            else
            {
                offset += ASN1.encode_context_object_id(buffer, offset, 2, object_id.type, object_id.instance);
            }

            return offset - org_offset;
        }

        public static int EncodeWhoIsBroadcast(byte[] buffer, int offset, int low_limit, int high_limit)
        {
            int org_offset = offset;

            /* optional limits - must be used as a pair */
            if ((low_limit >= 0) && (low_limit <= ASN1.BACNET_MAX_INSTANCE) &&
                (high_limit >= 0) && (high_limit <= ASN1.BACNET_MAX_INSTANCE))
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 0, (uint)low_limit);
                offset += ASN1.encode_context_unsigned(buffer, offset, 1, (uint)high_limit);
            }

            return offset - org_offset;
        }

        public static int DecodeWhoIsBroadcast(byte[] buffer, int offset, int apdu_len, out int low_limit, out int high_limit)
        {
            int len = 0;
            byte tag_number;
            uint len_value;
            uint decoded_value;

            low_limit = -1;
            high_limit = -1;

            if (apdu_len <= 0) return len;

            /* optional limits - must be used as a pair */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
            if (tag_number != 0)
                return -1;
            if (apdu_len > len)
            {
                len += ASN1.decode_unsigned(buffer, offset + len, len_value, out decoded_value);
                if (decoded_value <= ASN1.BACNET_MAX_INSTANCE)
                    low_limit = (int)decoded_value;
                if (apdu_len > len)
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                    if (tag_number != 1)
                        return -1;
                    if (apdu_len > len)
                    {
                        len += ASN1.decode_unsigned(buffer, offset + len, len_value, out decoded_value);
                        if (decoded_value <= ASN1.BACNET_MAX_INSTANCE)
                            high_limit = (int)decoded_value;
                    }
                    else
                        return -1;
                }
                else
                    return -1;
            }
            else
                return -1;

            return len;
        }

        public static int EncodeAlarmAcknowledge(byte[] buffer, int offset, uint ackProcessIdentifier, BACNET_OBJECT_ID eventObjectIdentifier, uint eventStateAcked, string ackSource, BACNET_GENERIC_TIME eventTimeStamp, BACNET_GENERIC_TIME ackTimeStamp)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_unsigned(buffer, offset, 0, ackProcessIdentifier);
            offset += ASN1.encode_context_object_id(buffer, offset, 1, eventObjectIdentifier.type, eventObjectIdentifier.instance);
            offset += ASN1.encode_context_enumerated(buffer, offset, 2, eventStateAcked);
            offset += ASN1.bacapp_encode_context_timestamp(buffer, offset, 3, eventTimeStamp);
            offset += ASN1.encode_context_character_string(buffer, offset, buffer.Length, 4, ackSource);
            offset += ASN1.bacapp_encode_context_timestamp(buffer, offset, 5, ackTimeStamp);

            return offset - org_offset;
        }

        public static int EncodeAtomicReadFile(byte[] buffer, int offset, bool is_stream, BACNET_OBJECT_ID object_id, int position, uint count)
        {
            int org_offset = offset;

            offset += ASN1.encode_application_object_id(buffer, offset, object_id.type, object_id.instance);
            switch (is_stream)
            {
                case true:
                    offset += ASN1.encode_opening_tag(buffer, offset, 0);
                    offset += ASN1.encode_application_signed(buffer, offset, position);
                    offset += ASN1.encode_application_unsigned(buffer, offset, count);
                    offset += ASN1.encode_closing_tag(buffer, offset, 0);
                    break;
                case false:
                    offset += ASN1.encode_opening_tag(buffer, offset, 1);
                    offset += ASN1.encode_application_signed(buffer, offset, position);
                    offset += ASN1.encode_application_unsigned(buffer, offset, count);
                    offset += ASN1.encode_closing_tag(buffer, offset, 1);
                    break;
                default:
                    break;
            }

            return offset - org_offset;
        }

        public static int EncodeAtomicReadFileAcknowledge(byte[] buffer, int offset, bool is_stream, bool end_of_file, int position, uint block_count, byte[][] blocks, int[] counts)
        {
            int org_offset = offset;

            offset += ASN1.encode_application_boolean(buffer, offset, end_of_file);
            switch (is_stream)
            {
                case true:
                    offset += ASN1.encode_opening_tag(buffer, offset, 0);
                    offset += ASN1.encode_application_signed(buffer, offset, position);
                    offset += ASN1.encode_application_octet_string(buffer, offset, buffer.Length, blocks[0], 0, counts[0]);
                    offset += ASN1.encode_closing_tag(buffer, offset, 0);
                    break;
                case false:
                    offset += ASN1.encode_opening_tag(buffer, offset, 1);
                    offset += ASN1.encode_application_signed(buffer, offset, position);
                    offset += ASN1.encode_application_unsigned(buffer, offset, block_count);
                    for (int i = 0; i < block_count; i++)
                    {
                        offset += ASN1.encode_application_octet_string(buffer, offset, buffer.Length, blocks[i], 0, counts[i]);
                    }
                    offset += ASN1.encode_closing_tag(buffer, offset, 1);
                    break;
                default:
                    break;
            }

            return offset - org_offset;
        }

        public static int EncodeAtomicWriteFile(byte[] buffer, int offset, bool is_stream, BACNET_OBJECT_ID object_id, int position, uint block_count, byte[][] blocks, int[] counts)
        {
            int org_offset = offset;

            offset += ASN1.encode_application_object_id(buffer, offset, object_id.type, object_id.instance);
            switch (is_stream)
            {
                case true:
                    offset += ASN1.encode_opening_tag(buffer, offset, 0);
                    offset += ASN1.encode_application_signed(buffer, offset, position);
                    offset += ASN1.encode_application_octet_string(buffer, offset, buffer.Length, blocks[0], 0, counts[0]);
                    offset += ASN1.encode_closing_tag(buffer, offset, 0);
                    break;
                case false:
                    offset += ASN1.encode_opening_tag(buffer, offset, 1);
                    offset += ASN1.encode_application_signed(buffer, offset, position);
                    offset += ASN1.encode_application_unsigned(buffer, offset, block_count);
                    for (int i = 0; i < block_count; i++)
                    {
                        offset += ASN1.encode_application_octet_string(buffer, offset, buffer.Length, blocks[i], 0, counts[i]);
                    }
                    offset += ASN1.encode_closing_tag(buffer, offset, 1);
                    break;
                default:
                    break;
            }

            return offset - org_offset;
        }

        public static int EncodeAtomicWriteFileAcknowledge(byte[] buffer, int offset, bool is_stream, int position)
        {
            int org_offset = offset;

            switch (is_stream)
            {
                case true:
                    offset += ASN1.encode_context_signed(buffer, offset, 0, position);
                    break;
                case false:
                    offset += ASN1.encode_context_signed(buffer, offset, 1, position);
                    break;
                default:
                    break;
            }

            return offset - org_offset;
        }

        public static int EncodeCOVNotifyConfirmed(byte[] buffer, int offset, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BACNET_OBJECT_ID monitoredObjectIdentifier, uint timeRemaining, BACNET_PROPERTY_VALUE[] values)
        {
            int org_offset = offset;

            /* tag 0 - subscriberProcessIdentifier */
            offset += ASN1.encode_context_unsigned(buffer, offset, 0, subscriberProcessIdentifier);
            /* tag 1 - initiatingDeviceIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 1, BACNET_OBJECT_TYPE.OBJECT_DEVICE, initiatingDeviceIdentifier);
            /* tag 2 - monitoredObjectIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 2, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            /* tag 3 - timeRemaining */
            offset += ASN1.encode_context_unsigned(buffer, offset, 3, timeRemaining);
            /* tag 4 - listOfValues */
            offset += ASN1.encode_opening_tag(buffer, offset, 4);
            foreach(BACNET_PROPERTY_VALUE value in values)
            {
                /* tag 0 - propertyIdentifier */
                offset += ASN1.encode_context_enumerated(buffer, offset, 0, value.property.propertyIdentifier);
                /* tag 1 - propertyArrayIndex OPTIONAL */
                if (value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                {
                    offset += ASN1.encode_context_unsigned(buffer, offset, 1, value.property.propertyArrayIndex);
                }
                /* tag 2 - value */
                /* abstract syntax gets enclosed in a context tag */
                offset += ASN1.encode_opening_tag(buffer, offset, 2);
                offset += ASN1.bacapp_encode_application_data(buffer, offset, buffer.Length, value.value.GetEnumerator().Current);
                offset += ASN1.encode_closing_tag(buffer, offset, 2);
                /* tag 3 - priority OPTIONAL */
                if (value.priority != ASN1.BACNET_NO_PRIORITY)
                {
                    offset += ASN1.encode_context_unsigned(buffer, offset, 3, value.priority);
                }
                /* is there another one to encode? */
                /* FIXME: check to see if there is room in the APDU */
            }
            offset += ASN1.encode_closing_tag(buffer, offset, 4);

            return offset - org_offset;
        }

        public static int EncodeCOVNotifyUnconfirmed(byte[] buffer, int offset, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BACNET_OBJECT_ID monitoredObjectIdentifier, uint timeRemaining, BACNET_PROPERTY_VALUE[] values)
        {
            int org_offset = offset;

            /* tag 0 - subscriberProcessIdentifier */
            offset += ASN1.encode_context_unsigned(buffer, offset, 0, subscriberProcessIdentifier);
            /* tag 1 - initiatingDeviceIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 1, BACNET_OBJECT_TYPE.OBJECT_DEVICE, initiatingDeviceIdentifier);
            /* tag 2 - monitoredObjectIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 2, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            /* tag 3 - timeRemaining */
            offset += ASN1.encode_context_unsigned(buffer, offset, 3, timeRemaining);
            /* tag 4 - listOfValues */
            offset += ASN1.encode_opening_tag(buffer, offset, 4);
            foreach (BACNET_PROPERTY_VALUE value in values)
            {
                /* tag 0 - propertyIdentifier */
                offset += ASN1.encode_context_enumerated(buffer, offset, 0, value.property.propertyIdentifier);
                /* tag 1 - propertyArrayIndex OPTIONAL */
                if (value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                {
                    offset += ASN1.encode_context_unsigned(buffer, offset, 1, value.property.propertyArrayIndex);
                }
                /* tag 2 - value */
                /* abstract syntax gets enclosed in a context tag */
                offset += ASN1.encode_opening_tag(buffer, offset, 2);
                offset += ASN1.bacapp_encode_application_data(buffer, offset, buffer.Length, value.value.GetEnumerator().Current);
                offset += ASN1.encode_closing_tag(buffer, offset, 2);
                /* tag 3 - priority OPTIONAL */
                if (value.priority != ASN1.BACNET_NO_PRIORITY)
                {
                    offset += ASN1.encode_context_unsigned(buffer, offset, 3, value.priority);
                }
                /* is there another one to encode? */
                /* FIXME: check to see if there is room in the APDU */
            }
            offset += ASN1.encode_closing_tag(buffer, offset, 4);

            return offset - org_offset;
        }

        public static int EncodeSubscribeCOV(byte[] buffer, int offset, uint subscriberProcessIdentifier, BACNET_OBJECT_ID monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime)
        {
            int org_offset = offset;

            /* tag 0 - subscriberProcessIdentifier */
            offset += ASN1.encode_context_unsigned(buffer, offset, 0, subscriberProcessIdentifier);
            /* tag 1 - monitoredObjectIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 1, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            /*
               If both the 'Issue Confirmed Notifications' and
               'Lifetime' parameters are absent, then this shall
               indicate a cancellation request.
             */
            if (!cancellationRequest)
            {
                /* tag 2 - issueConfirmedNotifications */
                offset += ASN1.encode_context_boolean(buffer, offset, 2, issueConfirmedNotifications);
                /* tag 3 - lifetime */
                offset += ASN1.encode_context_unsigned(buffer, offset, 3, lifetime);
            }

            return offset - org_offset;
        }

        public static int EncodeSubscribeProperty(byte[] buffer, int offset, uint subscriberProcessIdentifier, BACNET_OBJECT_ID monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BACNET_PROPERTY_REFERENCE monitoredProperty, bool covIncrementPresent, float covIncrement)
        {
            int org_offset = offset;

            /* tag 0 - subscriberProcessIdentifier */
            offset += ASN1.encode_context_unsigned(buffer, offset, 0, subscriberProcessIdentifier);
            /* tag 1 - monitoredObjectIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 1, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            if (!cancellationRequest)
            {
                /* tag 2 - issueConfirmedNotifications */
                offset += ASN1.encode_context_boolean(buffer, offset, 2, issueConfirmedNotifications);
                /* tag 3 - lifetime */
                offset += ASN1.encode_context_unsigned(buffer, offset, 3, lifetime);
            }
            /* tag 4 - monitoredPropertyIdentifier */
            offset += ASN1.encode_opening_tag(buffer, offset, 4);
            offset += ASN1.encode_context_enumerated(buffer, offset, 0, monitoredProperty.propertyIdentifier);
            if (monitoredProperty.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 1, monitoredProperty.propertyArrayIndex);

            }
            offset += ASN1.encode_closing_tag(buffer, offset, 4);

            /* tag 5 - covIncrement */
            if (covIncrementPresent)
            {
                offset += ASN1.encode_context_real(buffer, offset, 5, covIncrement);
            }

            BVLC.Encode(buffer, org_offset, BACNET_BVLC_FUNCTION.BVLC_ORIGINAL_UNICAST_NPDU, offset - org_offset);


            return offset - org_offset;
        }

        private static int EncodeEventNotifyData(byte[] buffer, int offset, BACNET_EVENT_NOTIFICATION_DATA data)
        {
            int org_offset = offset;

            /* tag 0 - processIdentifier */
            offset += ASN1.encode_context_unsigned(buffer, offset, 0, data.processIdentifier);
            /* tag 1 - initiatingObjectIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 1, data.initiatingObjectIdentifier.type, data.initiatingObjectIdentifier.instance);

            /* tag 2 - eventObjectIdentifier */
            offset += ASN1.encode_context_object_id(buffer, offset, 2, data.eventObjectIdentifier.type, data.eventObjectIdentifier.instance);

            /* tag 3 - timeStamp */
            offset += ASN1.bacapp_encode_context_timestamp(buffer, offset, 3, data.timeStamp);

            /* tag 4 - noticicationClass */
            offset += ASN1.encode_context_unsigned(buffer, offset, 4, data.notificationClass);

            /* tag 5 - priority */
            offset += ASN1.encode_context_unsigned(buffer, offset, 5, data.priority);

            /* tag 6 - eventType */
            offset += ASN1.encode_context_enumerated(buffer, offset, 6, (uint)data.eventType);

            /* tag 7 - messageText */
            if (!string.IsNullOrEmpty(data.messageText))
            {
                offset += ASN1.encode_context_character_string(buffer, offset, buffer.Length, 7, data.messageText);
            }

            /* tag 8 - notifyType */
            offset += ASN1.encode_context_enumerated(buffer, offset, 8, (uint)data.notifyType);

            switch (data.notifyType)
            {
                case BACNET_EVENT_NOTIFICATION_DATA.BACNET_NOTIFY_TYPE.NOTIFY_ALARM:
                case BACNET_EVENT_NOTIFICATION_DATA.BACNET_NOTIFY_TYPE.NOTIFY_EVENT:
                    /* tag 9 - ackRequired */
                    offset += ASN1.encode_context_boolean(buffer, offset, 9, data.ackRequired);

                    /* tag 10 - fromState */
                    offset += ASN1.encode_context_enumerated(buffer, offset, 10, (uint)data.fromState);
                    break;
                default:
                    break;
            }

            /* tag 11 - toState */
            offset += ASN1.encode_context_enumerated(buffer, offset, 11, (uint)data.toState);

            switch (data.notifyType)
            {
                case BACNET_EVENT_NOTIFICATION_DATA.BACNET_NOTIFY_TYPE.NOTIFY_ALARM:
                case BACNET_EVENT_NOTIFICATION_DATA.BACNET_NOTIFY_TYPE.NOTIFY_EVENT:
                    /* tag 12 - event values */
                    offset += ASN1.encode_opening_tag(buffer, offset, 12);

                    switch (data.eventType)
                    {
                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_CHANGE_OF_BITSTRING:
                            offset += ASN1.encode_opening_tag(buffer, offset, 0);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 0, data.changeOfBitstring_referencedBitString);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 1, data.changeOfBitstring_statusFlags);
                            offset += ASN1.encode_closing_tag(buffer, offset, 0);
                            break;

                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_CHANGE_OF_STATE:
                            offset += ASN1.encode_opening_tag(buffer, offset, 1);
                            offset += ASN1.encode_opening_tag(buffer, offset, 0);
                            offset += ASN1.bacapp_encode_property_state(buffer, offset, data.changeOfState_newState);
                            offset += ASN1.encode_closing_tag(buffer, offset, 0);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 1, data.changeOfState_statusFlags);
                            offset += ASN1.encode_closing_tag(buffer, offset, 1);
                            break;

                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_CHANGE_OF_VALUE:
                            offset += ASN1.encode_opening_tag(buffer, offset, 2);
                            offset += ASN1.encode_opening_tag(buffer, offset, 0);

                            switch (data.changeOfValue_tag)
                            {
                                case BACNET_EVENT_NOTIFICATION_DATA.CHANGE_OF_VALUE_TYPE.CHANGE_OF_VALUE_REAL:
                                    offset += ASN1.encode_context_real(buffer, offset, 1, data.changeOfValue_changeValue);
                                    break;
                                case BACNET_EVENT_NOTIFICATION_DATA.CHANGE_OF_VALUE_TYPE.CHANGE_OF_VALUE_BITS:
                                    offset += ASN1.encode_context_bitstring(buffer, offset, 0, data.changeOfValue_changedBits);
                                    break;
                                default:
                                    return 0;
                            }

                            offset += ASN1.encode_closing_tag(buffer, offset, 0);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 1, data.changeOfValue_statusFlags);
                            offset += ASN1.encode_closing_tag(buffer, offset, 2);
                            break;

                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_FLOATING_LIMIT:
                            offset += ASN1.encode_opening_tag(buffer, offset, 4);
                            offset += ASN1.encode_context_real(buffer, offset, 0, data.floatingLimit_referenceValue);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 1, data.floatingLimit_statusFlags);
                            offset += ASN1.encode_context_real(buffer, offset, 2, data.floatingLimit_setPointValue);
                            offset += ASN1.encode_context_real(buffer, offset, 3, data.floatingLimit_errorLimit);
                            offset += ASN1.encode_closing_tag(buffer, offset, 4);
                            break;

                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_OUT_OF_RANGE:
                            offset += ASN1.encode_opening_tag(buffer, offset, 5);
                            offset += ASN1.encode_context_real(buffer, offset, 0, data.outOfRange_exceedingValue);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 1, data.outOfRange_statusFlags);
                            offset += ASN1.encode_context_real(buffer, offset, 2, data.outOfRange_deadband);
                            offset += ASN1.encode_context_real(buffer, offset, 3, data.outOfRange_exceededLimit);
                            offset += ASN1.encode_closing_tag(buffer, offset, 5);
                            break;

                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_CHANGE_OF_LIFE_SAFETY:
                            offset += ASN1.encode_opening_tag(buffer, offset, 8);
                            offset += ASN1.encode_context_enumerated(buffer, offset, 0, (uint)data.changeOfLifeSafety_newState);
                            offset += ASN1.encode_context_enumerated(buffer, offset, 1, (uint)data.changeOfLifeSafety_newMode);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 2, data.changeOfLifeSafety_statusFlags);
                            offset += ASN1.encode_context_enumerated(buffer, offset, 3, (uint)data.changeOfLifeSafety_operationExpected);
                            offset += ASN1.encode_closing_tag(buffer, offset, 8);
                            break;

                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_BUFFER_READY:
                            offset += ASN1.encode_opening_tag(buffer, offset, 10);
                            offset += ASN1.bacapp_encode_context_device_obj_property_ref(buffer, offset, 0, data.bufferReady_bufferProperty);
                            offset += ASN1.encode_context_unsigned(buffer, offset, 1, data.bufferReady_previousNotification);
                            offset += ASN1.encode_context_unsigned(buffer, offset, 2, data.bufferReady_currentNotification);
                            offset += ASN1.encode_closing_tag(buffer, offset, 10);

                            break;
                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_UNSIGNED_RANGE:
                            offset += ASN1.encode_opening_tag(buffer, offset, 11);
                            offset += ASN1.encode_context_unsigned(buffer, offset, 0, data.unsignedRange_exceedingValue);
                            offset += ASN1.encode_context_bitstring(buffer, offset, 1, data.unsignedRange_statusFlags);
                            offset += ASN1.encode_context_unsigned(buffer, offset, 2, data.unsignedRange_exceededLimit);
                            offset += ASN1.encode_closing_tag(buffer, offset, 11);
                            break;
                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_EXTENDED:
                        case BACNET_EVENT_NOTIFICATION_DATA.BACNET_EVENT_TYPE.EVENT_COMMAND_FAILURE:
                        default:
                            throw new NotImplementedException();
                    }
                    offset += ASN1.encode_closing_tag(buffer, offset, 12);
                    break;
                case BACNET_EVENT_NOTIFICATION_DATA.BACNET_NOTIFY_TYPE.NOTIFY_ACK_NOTIFICATION:
                /* FIXME: handle this case */
                default:
                    break;
            }

            return offset - org_offset;
        }

        public static int EncodeEventNotifyConfirmed(byte[] buffer, int offset, BACNET_EVENT_NOTIFICATION_DATA data)
        {
            int org_offset = offset;

            offset += EncodeEventNotifyData(buffer, offset, data);

            return offset - org_offset;
        }

        public static int EncodeEventNotifyUnconfirmed(byte[] buffer, int offset, BACNET_EVENT_NOTIFICATION_DATA data)
        {
            int org_offset = offset;

            offset += EncodeEventNotifyData(buffer, offset, data);

            return offset - org_offset;
        }

        public static int EncodeAlarmSummary(byte[] buffer, int offset, BACNET_OBJECT_ID objectIdentifier, uint alarmState, BACNET_BIT_STRING acknowledgedTransitions)
        {
            int org_offset = offset;

            /* tag 0 - Object Identifier */
            offset += ASN1.encode_application_object_id(buffer, offset, objectIdentifier.type, objectIdentifier.instance);
            /* tag 1 - Alarm State */
            offset += ASN1.encode_application_enumerated(buffer, offset, alarmState);
            /* tag 2 - Acknowledged Transitions */
            offset += ASN1.encode_application_bitstring(buffer, offset, acknowledgedTransitions);

            return offset - org_offset;
        }

        public static int EncodeGetEventInformation(byte[] buffer, int offset, bool send_last, BACNET_OBJECT_ID lastReceivedObjectIdentifier)
        {
            int org_offset = offset;

            /* encode optional parameter */
            if (send_last)
            {
                offset += ASN1.encode_context_object_id(buffer, offset, 0, lastReceivedObjectIdentifier.type, lastReceivedObjectIdentifier.instance);
            }

            return offset - org_offset;
        }

        public static int EncodeGetEventInformationAcknowledge(byte[] buffer, int offset, BACNET_GET_EVENT_INFORMATION_DATA[] events, bool moreEvents)
        {
            int org_offset = offset;

            /* service ack follows */
            /* Tag 0: listOfEventSummaries */
            offset += ASN1.encode_opening_tag(buffer, offset, 0);
            foreach(BACNET_GET_EVENT_INFORMATION_DATA event_data in events)
            {
                /* Tag 0: objectIdentifier */
                offset += ASN1.encode_context_object_id(buffer, offset, 0, event_data.objectIdentifier.type, event_data.objectIdentifier.instance);
                /* Tag 1: eventState */
                offset += ASN1.encode_context_enumerated(buffer, offset, 1, (uint)event_data.eventState);
                /* Tag 2: acknowledgedTransitions */
                offset += ASN1.encode_context_bitstring(buffer, offset, 2, event_data.acknowledgedTransitions);
                /* Tag 3: eventTimeStamps */
                offset += ASN1.encode_opening_tag(buffer, offset, 3);
                for (int i = 0; i < 3; i++)
                {
                    offset += ASN1.bacapp_encode_timestamp(buffer, offset, event_data.eventTimeStamps[i]);
                }
                offset += ASN1.encode_closing_tag(buffer, offset, 3);
                /* Tag 4: notifyType */
                offset += ASN1.encode_context_enumerated(buffer, offset, 4, (uint)event_data.notifyType);
                /* Tag 5: eventEnable */
                offset += ASN1.encode_context_bitstring(buffer, offset, 5, event_data.eventEnable);
                /* Tag 6: eventPriorities */
                offset += ASN1.encode_opening_tag(buffer, offset, 6);
                for (int i = 0; i < 3; i++)
                {
                    offset += ASN1.encode_application_unsigned(buffer, offset, event_data.eventPriorities[i]);
                }
                offset += ASN1.encode_closing_tag(buffer, offset, 6);
            }
            offset += ASN1.encode_closing_tag(buffer, offset, 0);
            offset += ASN1.encode_context_boolean(buffer, offset, 1, moreEvents);

            return offset - org_offset;
        }

        public static int EncodeLifeSafetyOperation(byte[] buffer, int offset, uint processId, string requestingSrc, uint operation, BACNET_OBJECT_ID targetObject)
        {
            int org_offset = offset;

            /* tag 0 - requestingProcessId */
            offset += ASN1.encode_context_unsigned(buffer, offset, 0, processId);
            /* tag 1 - requestingSource */
            offset += ASN1.encode_context_character_string(buffer, offset, buffer.Length, 1, requestingSrc);
            /* Operation */
            offset += ASN1.encode_context_enumerated(buffer, offset, 2, operation);
            /* Object ID */
            offset += ASN1.encode_context_object_id(buffer, offset, 3, targetObject.type, targetObject.instance);

            return offset - org_offset;
        }

        public static int EncodePrivateTransferConfirmed(byte[] buffer, int offset, uint vendorID, uint serviceNumber, byte[] data)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_unsigned(buffer, offset, 0, vendorID);
            offset += ASN1.encode_context_unsigned(buffer, offset, 1, serviceNumber);
            offset += ASN1.encode_opening_tag(buffer, offset, 2);
            Array.Copy(data, 0, buffer, offset, data.Length);
            offset += data.Length;
            offset += ASN1.encode_closing_tag(buffer, offset, 2);

            return offset - org_offset;
        }

        public static int EncodePrivateTransferUnconfirmed(byte[] buffer, int offset, uint vendorID, uint serviceNumber, byte[] data)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_unsigned(buffer, offset, 0, vendorID);
            offset += ASN1.encode_context_unsigned(buffer, offset, 1, serviceNumber);
            offset += ASN1.encode_opening_tag(buffer, offset, 2);
            Array.Copy(data, 0, buffer, offset, data.Length);
            offset += data.Length;
            offset += ASN1.encode_closing_tag(buffer, offset, 2);

            return offset - org_offset;
        }

        public static int EncodePrivateTransferAcknowledge(byte[] buffer, int offset, uint vendorID, uint serviceNumber, byte[] data)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_unsigned(buffer, offset, 0, vendorID);
            offset += ASN1.encode_context_unsigned(buffer, offset, 1, serviceNumber);
            offset += ASN1.encode_opening_tag(buffer, offset, 2);
            Array.Copy(data, 0, buffer, offset, data.Length);
            offset += data.Length;
            offset += ASN1.encode_closing_tag(buffer, offset, 2);

            return offset - org_offset;
        }

        public static int EncodeReinitializeDevice(byte[] buffer, int offset, string password)
        {
            int org_offset = offset;

            /* optional password */
            if (!string.IsNullOrEmpty(password))
            {
                /* FIXME: must be at least 1 character, limited to 20 characters */
                offset += ASN1.encode_context_character_string(buffer, offset, buffer.Length, 1, password);
            }

            return offset - org_offset;
        }

        public enum ReadRangeRequestType
        {
            RR_BY_POSITION = 1,
            RR_BY_SEQUENCE = 2,
            RR_BY_TIME = 4,
            RR_READ_ALL = 8,
        }

        public static int EncodeReadRange(byte[] buffer, int offset, BACNET_OBJECT_ID object_id, uint property_id, uint arrayIndex, ReadRangeRequestType requestType, uint position, DateTime time, int count)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
            offset += ASN1.encode_context_enumerated(buffer, offset, 1, property_id);

            /* optional array index */
            if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 2, arrayIndex);
            }

            /* Build the appropriate (optional) range parameter based on the request type */
            switch (requestType)
            {
                case ReadRangeRequestType.RR_BY_POSITION:
                    offset += ASN1.encode_opening_tag(buffer, offset, 3);
                    offset += ASN1.encode_application_unsigned(buffer, offset, position);
                    offset += ASN1.encode_application_signed(buffer, offset, count);
                    offset += ASN1.encode_closing_tag(buffer, offset, 3);
                    break;

                case ReadRangeRequestType.RR_BY_SEQUENCE:
                    offset += ASN1.encode_opening_tag(buffer, offset, 6);
                    offset += ASN1.encode_application_unsigned(buffer, offset, position);
                    offset += ASN1.encode_application_signed(buffer, offset, count);
                    offset += ASN1.encode_closing_tag(buffer, offset, 6);
                    break;

                case ReadRangeRequestType.RR_BY_TIME:
                    offset += ASN1.encode_opening_tag(buffer, offset, 7);
                    offset += ASN1.encode_application_date(buffer, offset, time);
                    offset += ASN1.encode_application_time(buffer, offset, time);
                    offset += ASN1.encode_application_signed(buffer, offset, count);
                    offset += ASN1.encode_closing_tag(buffer, offset, 7);
                    break;

                case ReadRangeRequestType.RR_READ_ALL:  /* to attempt a read of the whole array or list, omit the range parameter */
                    break;

                default:
                    break;
            }

            return offset - org_offset;
        }

        public static int EncodeReadRangeAcknowledge(byte[] buffer, int offset, BACNET_OBJECT_ID object_id, uint property_id, uint arrayIndex, BACNET_BIT_STRING ResultFlags, uint ItemCount, byte[] application_data, ReadRangeRequestType requestType, uint FirstSequence)
        {
            int org_offset = offset;

            /* service ack follows */
            offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
            offset += ASN1.encode_context_enumerated(buffer, offset, 1, property_id);
            /* context 2 array index is optional */
            if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 2, arrayIndex);
            }
            /* Context 3 BACnet Result Flags */
            offset += ASN1.encode_context_bitstring(buffer, offset, 3, ResultFlags);
            /* Context 4 Item Count */
            offset += ASN1.encode_context_unsigned(buffer, offset, 4, ItemCount);
            /* Context 5 Property list - reading the standard it looks like an empty list still 
             * requires an opening and closing tag as the tagged parameter is not optional
             */
            offset += ASN1.encode_opening_tag(buffer, offset, 5);
            if (ItemCount != 0)
            {
                Array.Copy(application_data, 0, buffer, offset, application_data.Length);
                offset += application_data.Length;
            }
            offset += ASN1.encode_closing_tag(buffer, offset, 5);

            if ((ItemCount != 0) && (requestType != ReadRangeRequestType.RR_BY_POSITION) && (requestType != ReadRangeRequestType.RR_READ_ALL))
            {
                /* Context 6 Sequence number of first item */
                offset += ASN1.encode_context_unsigned(buffer, offset, 6, FirstSequence);
            }

            return offset - org_offset;
        }

        public static int EncodeReadProperty(byte[] buffer, int offset, BACNET_OBJECT_ID object_id, uint property_id, uint array_index = ASN1.BACNET_ARRAY_ALL)
        {
            int org_offset = offset;

            if ((int)object_id.type <= ASN1.BACNET_MAX_OBJECT)
            {
                /* check bounds so that we could create malformed
                   messages for testing */
                offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
            }
            if (property_id <= (uint)BACNET_PROPERTY_ID.MAX_BACNET_PROPERTY_ID)
            {
                /* check bounds so that we could create malformed
                   messages for testing */
                offset += ASN1.encode_context_enumerated(buffer, offset, 1, property_id);
            }
            /* optional array index */
            if (array_index != ASN1.BACNET_ARRAY_ALL)
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 2, array_index);
            }

            return offset - org_offset;
        }

        public static int DecodeAtomicWriteFileAcknowledge(byte[] buffer, int offset, int apdu_len, out bool is_stream, out int position)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            is_stream = false;
            position = 0;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number == 0)
            {
                is_stream = true;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
            }
            else if (tag_number == 1)
            {
                is_stream = false;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
            }
            else
                return -1;

            return len;
        }

        public static int DecodeAtomicReadFileAcknowledge(byte[] buffer, int offset, int apdu_len, out bool end_of_file, out bool is_stream, out int position, out uint count, byte[] target_buffer, int target_offset)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;
            int tag_len = 0;
            int i;

            end_of_file = false;
            is_stream = false;
            position = -1;
            count = 0;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN)
                return -1;
            end_of_file = len_value_type > 0;
            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
            {
                is_stream = true;
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartPosition */
                tag_len =
                    ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                /* fileData */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING)
                    return -1;
                len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
            {
                is_stream = false;
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartRecord */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                /* returnedRecordCount */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                    return -1;
                len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out count);
                for (i = 0; i < count; i++)
                {
                    /* fileData */
                    tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    len += tag_len;
                    if (tag_number != (byte)BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING)
                        return -1;
                    len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
                }
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else
                return -1;

            return len;
        }

        public static int DecodeReadProperty(byte[] buffer, int offset, int apdu_len, out BACNET_OBJECT_ID object_id, out BACNET_PROPERTY_REFERENCE property)
        {
            int len = 0;
            ushort type = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            object_id = new BACNET_OBJECT_ID();
            property = new BACNET_PROPERTY_REFERENCE();

            /* Tag 0: Object ID          */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out type, out object_id.instance);
            object_id.type = (BACNET_OBJECT_TYPE)type;
            /* Tag 1: Property ID */
            len +=
                ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 1)
                return -1;
            len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out property.propertyIdentifier);
            /* Tag 2: Optional Array Index */
            if (len < apdu_len)
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                if ((tag_number == 2) && (len < apdu_len))
                {
                    len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out property.propertyArrayIndex);
                }
                else
                    return -1;
            }
            else
                property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

            return len;
        }

        public static int EncodeReadPropertyAcknowledge(byte[] buffer, int offset, BACNET_OBJECT_ID object_id, uint property_id, uint array_index, IEnumerable<BACNET_VALUE> value_list)
        {
            int org_offset = offset;

            /* service ack follows */
            offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
            offset += ASN1.encode_context_enumerated(buffer, offset, 1, property_id);
            /* context 2 array index is optional */
            if (array_index != ASN1.BACNET_ARRAY_ALL)
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 2, array_index);
            }
            offset += ASN1.encode_opening_tag(buffer, offset, 3);

            /* Value */
            foreach (BACNET_VALUE value in value_list)
            {
                int len = ASN1.bacapp_encode_application_data(buffer, offset, buffer.Length, value);
                if (len < 0) return -1;
                else offset += len;
            }

            offset += ASN1.encode_closing_tag(buffer, offset, 3);

            return offset - org_offset;
        }

        public static int DecodeReadPropertyAcknowledge(byte[] buffer, int offset, int apdu_len, out BACNET_OBJECT_ID object_id, out BACNET_PROPERTY_REFERENCE property, out LinkedList<BACNET_VALUE> value_list)
        {
            byte tag_number = 0;
            uint len_value_type = 0;
            int tag_len = 0;    /* length of tag decode */
            int len = 0;        /* total length of decodes */

            object_id = new BACNET_OBJECT_ID();
            property = new BACNET_PROPERTY_REFERENCE();
            value_list = new LinkedList<BACNET_VALUE>();

            /* FIXME: check apdu_len against the len during decode   */
            /* Tag 0: Object ID */
            if (!ASN1.decode_is_context_tag(buffer, offset, 0))
                return -1;
            len = 1;
            len += ASN1.decode_object_id(buffer, offset + len, out object_id.type, out object_id.instance);
            /* Tag 1: Property ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 1)
                return -1;
            len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out property.propertyIdentifier);
            /* Tag 2: Optional Array Index */
            tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number == 2)
            {
                len += tag_len;
                len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out property.propertyArrayIndex);
            }
            else
                property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

            /* Tag 3: opening context tag */
            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 3))
            {
                /* a tag number of 3 is not extended so only one octet */
                len++;

                BACNET_VALUE value;
                while ((apdu_len - len) > 1)
                {
                    tag_len = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset, (BACNET_PROPERTY_ID)property.propertyIdentifier, out value);
                    if (tag_len < 0) return -1;
                    len += tag_len;
                    value_list.AddLast(value);
                }
            }
            else
                return -1;

            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
                return -1;
            len++;

            return len;
        }

        public static int EncodeReadPropertyMultiple(byte[] buffer, int offset, BACNET_OBJECT_ID object_id, IEnumerable<BACNET_PROPERTY_REFERENCE> property_id_and_array_index)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
            /* Tag 1: sequence of ReadAccessSpecification */
            offset += ASN1.encode_opening_tag(buffer, offset, 1);

            foreach (BACNET_PROPERTY_REFERENCE p in property_id_and_array_index)
            {
                offset += ASN1.encode_context_enumerated(buffer, offset, 0, p.propertyIdentifier);

                /* optional array index */
                if (p.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                    offset += ASN1.encode_context_unsigned(buffer, offset, 1, p.propertyArrayIndex);
            }

            offset += ASN1.encode_closing_tag(buffer, offset, 1);

            return offset - org_offset;
        }

        public static int DecodeReadPropertyMultiple(byte[] buffer, int offset, int apdu_len, out IList<BACNET_OBJECT_ID> object_ids, out IList<IList<BACNET_PROPERTY_REFERENCE>> properties)
        {
            int len = 0;
            ushort type;
            byte tag_number = 0;
            uint len_value_type = 0;
            uint property;
            uint array_index;
            int option_len;

            object_ids = null;
            properties = null;
            List<BACNET_OBJECT_ID> _object_ids = new List<BACNET_OBJECT_ID>();
            List<IList<BACNET_PROPERTY_REFERENCE>> _property_id_and_array_index = new List<IList<BACNET_PROPERTY_REFERENCE>>();

            while ((apdu_len - len) > 0)
            {
                BACNET_OBJECT_ID object_id;
                     
                /* Tag 0: Object ID */
                if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                    return -1;
                len++;
                len += ASN1.decode_object_id(buffer, offset + len, out type, out object_id.instance);
                object_id.type = (BACNET_OBJECT_TYPE)type;

                /* Tag 1: sequence of ReadAccessSpecification */
                if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
                    return -1;
                len++;  /* opening tag is only one octet */
                _object_ids.Add(object_id);

                /* properties */
                List<BACNET_PROPERTY_REFERENCE> __property_id_and_array_index = new List<BACNET_PROPERTY_REFERENCE>();
                while ((apdu_len - len) > 1 && !ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                {
                    /* Tag 0: propertyIdentifier */
                    if (!ASN1.IS_CONTEXT_SPECIFIC(buffer[offset + len]))
                        return -1;

                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    if (tag_number != 0)
                        return -1;

                    /* Should be at least the unsigned value + 1 tag left */
                    if ((len + len_value_type) >= apdu_len)
                        return -1;
                    len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out property);
                    /* Assume most probable outcome */
                    array_index = ASN1.BACNET_ARRAY_ALL;
                    /* Tag 1: Optional propertyArrayIndex */
                    if (ASN1.IS_CONTEXT_SPECIFIC(buffer[offset + len]) && !ASN1.IS_CLOSING_TAG(buffer[offset + len]))
                    {
                        option_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                        if (tag_number == 1)
                        {
                            len += option_len;
                            /* Should be at least the unsigned array index + 1 tag left */
                            if ((len + len_value_type) >= apdu_len)
                                return -1;
                            len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out array_index);
                        }
                    }
                    __property_id_and_array_index.Add(new BACNET_PROPERTY_REFERENCE(property, array_index));
                }

                /* closing tag */
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                    return -1;
                len++;
                _property_id_and_array_index.Add(__property_id_and_array_index);
            }

            properties = _property_id_and_array_index;
            object_ids = _object_ids;

            return len;
        }

        public static int EncodeReadPropertyMultipleAcknowledge(byte[] buffer, int offset, ICollection<BACNET_OBJECT_ID> object_ids, IList<ICollection<BACNET_PROPERTY_VALUE>> value_list)
        {
            int org_offset = offset;

            int count = 0;
            foreach (BACNET_OBJECT_ID object_id in object_ids)
            {
                offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
                /* Tag 1: listOfResults */
                offset += ASN1.encode_opening_tag(buffer, offset, 1);

                ICollection<BACNET_PROPERTY_VALUE> object_property_values = value_list[count];           
                foreach(BACNET_PROPERTY_VALUE p_value in object_property_values)
                {
                    /* Tag 2: propertyIdentifier */
                    offset += ASN1.encode_context_enumerated(buffer, offset, 2, p_value.property.propertyIdentifier);
                    /* Tag 3: optional propertyArrayIndex */
                    if (p_value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                        offset += ASN1.encode_context_unsigned(buffer, offset, 3, p_value.property.propertyArrayIndex);

                    if (p_value.value != null)
                    {
                        /* Tag 4: Value */
                        offset += ASN1.encode_opening_tag(buffer, offset, 4);
                        foreach (BACNET_VALUE v in p_value.value)
                        {
                            int len = ASN1.bacapp_encode_application_data(buffer, offset, buffer.Length, v);
                            if (len < 0) return -1;
                            else offset += len;
                        }
                        offset += ASN1.encode_closing_tag(buffer, offset, 4);
                    }
                    else
                    {
                        /* Tag 5: Error */
                        offset += ASN1.encode_opening_tag(buffer, offset, 5);
                        offset += ASN1.encode_application_enumerated(buffer, offset, 1);      //error_class
                        offset += ASN1.encode_application_enumerated(buffer, offset, 1);       //error_code
                        offset += ASN1.encode_closing_tag(buffer, offset, 5);
                    }
                }

                offset += ASN1.encode_closing_tag(buffer, offset, 1);
                count++;
            }

            return offset - org_offset;
        }

        public static int DecodeReadPropertyMultipleAcknowledge(byte[] buffer, int offset, int apdu_len, out BACNET_OBJECT_ID object_id, out ICollection<BACNET_PROPERTY_VALUE> value_list)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;
            int tag_len;

            object_id = new BACNET_OBJECT_ID();
            value_list = null;

            if (!ASN1.decode_is_context_tag(buffer, offset, 0))
                return -1;
            len = 1;
            len += ASN1.decode_object_id(buffer, offset + len, out object_id.type, out object_id.instance);

            /* Tag 1: listOfResults */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            LinkedList<BACNET_PROPERTY_VALUE> _value_list = new LinkedList<BACNET_PROPERTY_VALUE>();
            while ((apdu_len - len) > 1)
            {
                BACNET_PROPERTY_VALUE new_entry = new BACNET_PROPERTY_VALUE();

                /* end */
                if (ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                {
                    len++;
                    break;
                }

                /* Tag 2: propertyIdentifier */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                if (tag_number != 2)
                    return -1;
                len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out new_entry.property.propertyIdentifier);
                /* Tag 3: Optional Array Index */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                if (tag_number == 3)
                {
                    len += tag_len;
                    len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out new_entry.property.propertyArrayIndex);
                }
                else
                    new_entry.property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

                /* Tag 4: Value */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number == 4)
                {
                    BACNET_VALUE value;
                    List<BACNET_VALUE> local_value_list = new List<BACNET_VALUE>();
                    while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 4))
                    {
                        tag_len = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset - 1, (BACNET_PROPERTY_ID)new_entry.property.propertyIdentifier, out value);
                        if (tag_len < 0) return -1;
                        len += tag_len;
                        local_value_list.Add(value);
                    }
                    new_entry.value = local_value_list;
                    len++;
                }
                else if (tag_number == 5)
                {
                    /* Tag 5: Error */
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    /* FIXME: we could validate that the tag is enumerated... */
                    len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out len_value_type);      //error_class
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    /* FIXME: we could validate that the tag is enumerated... */
                    len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out len_value_type);       //error_code
                    if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 5))
                        return -1;
                    len++;
                }

                _value_list.AddLast(new_entry);
            }
            value_list = _value_list;

            return len;
        }

        public static int EncodeWriteProperty(byte[] buffer, int offset, BACNET_OBJECT_ID object_id, uint property_id, uint array_index, uint priority, IEnumerable<BACNET_VALUE> value_list)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
            offset += ASN1.encode_context_enumerated(buffer, offset, 1, property_id);

            /* optional array index; ALL is -1 which is assumed when missing */
            if (array_index != ASN1.BACNET_ARRAY_ALL)
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 2, array_index);
            }

            /* propertyValue */
            offset += ASN1.encode_opening_tag(buffer, offset, 3);
            foreach (BACNET_VALUE value in value_list)
            {
                int len = ASN1.bacapp_encode_application_data(buffer, offset, buffer.Length, value);
                if (len < 0) return -1;
                else offset += len;
            }
            offset += ASN1.encode_closing_tag(buffer, offset, 3);

            /* optional priority - 0 if not set, 1..16 if set */
            if (priority != ASN1.BACNET_NO_PRIORITY)
            {
                offset += ASN1.encode_context_unsigned(buffer, offset, 4, priority);
            }

            return offset - org_offset;
        }

        public static int DecodeCOVNotifyUnconfirmed(byte[] buffer, int offset, int apdu_len, out uint subscriberProcessIdentifier, out BACNET_OBJECT_ID initiatingDeviceIdentifier, out BACNET_OBJECT_ID monitoredObjectIdentifier, out uint timeRemaining, out ICollection<BACNET_PROPERTY_VALUE> values)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value = 0;
            uint decoded_value;

            subscriberProcessIdentifier = 0;
            initiatingDeviceIdentifier = new BACNET_OBJECT_ID();
            monitoredObjectIdentifier = new BACNET_OBJECT_ID();
            timeRemaining = 0;
            values = null;

            /* tag 0 - subscriberProcessIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_unsigned(buffer, offset + len, len_value, out subscriberProcessIdentifier);
            }
            else
                return -1;

            /* tag 1 - initiatingDeviceIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_object_id(buffer, offset + len, out initiatingDeviceIdentifier.type, out initiatingDeviceIdentifier.instance);
            }
            else
                return -1;

            /* tag 2 - monitoredObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_object_id(buffer, offset + len, out monitoredObjectIdentifier.type, out monitoredObjectIdentifier.instance);
            }
            else
                return -1;

            /* tag 3 - timeRemaining */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_unsigned(buffer, offset + len, len_value, out timeRemaining);
            }
            else
                return -1;
            
            /* tag 4: opening context tag - listOfValues */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 4))
                return -1;
            
            /* a tag number of 4 is not extended so only one octet */
            len++;
            LinkedList<BACNET_PROPERTY_VALUE> _values = new LinkedList<BACNET_PROPERTY_VALUE>();
            while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 4))
            {
                BACNET_PROPERTY_VALUE new_entry = new BACNET_PROPERTY_VALUE();

                /* tag 0 - propertyIdentifier */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                    len += ASN1.decode_enumerated(buffer, offset + len, len_value, out new_entry.property.propertyIdentifier);
                }
                else
                    return -1;

                /* tag 1 - propertyArrayIndex OPTIONAL */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                    len += ASN1.decode_unsigned(buffer, offset + len, len_value, out new_entry.property.propertyArrayIndex);
                }
                else
                    new_entry.property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;

                /* tag 2: opening context tag - value */
                if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 2))
                    return -1;

                /* a tag number of 2 is not extended so only one octet */
                len++;
                List<BACNET_VALUE> b_values = new List<BACNET_VALUE>();
                while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
                {
                    BACNET_VALUE b_value;
                    int tmp = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset, (BACNET_PROPERTY_ID)new_entry.property.propertyIdentifier, out b_value);
                    if (tmp < 0) return -1;
                    len += tmp;
                    b_values.Add(b_value);
                }
                new_entry.value = b_values;

                /* a tag number of 2 is not extended so only one octet */
                len++;
                /* tag 3 - priority OPTIONAL */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                    len += ASN1.decode_unsigned(buffer, offset + len, len_value, out decoded_value);
                    new_entry.priority = (byte)decoded_value;
                }
                else
                    new_entry.priority = (byte)ASN1.BACNET_NO_PRIORITY;

                _values.AddLast(new_entry);
            }

            values = _values;

            return len;
        }

        public static int DecodeWriteProperty(byte[] buffer, int offset, int apdu_len, out BACNET_OBJECT_ID object_id, out BACNET_PROPERTY_VALUE value)
        {
            int len = 0;
            int tag_len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;
            ushort type = 0;  /* for decoding */
            uint unsigned_value = 0;

            object_id = new BACNET_OBJECT_ID();
            value = new BACNET_PROPERTY_VALUE();

            /* Tag 0: Object ID          */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out type, out object_id.instance);
            object_id.type = (BACNET_OBJECT_TYPE)type;
            /* Tag 1: Property ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 1)
                return -1;
            len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out value.property.propertyIdentifier);
            /* Tag 2: Optional Array Index */
            /* note: decode without incrementing len so we can check for opening tag */
            tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number == 2)
            {
                len += tag_len;
                len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out value.property.propertyArrayIndex);
            }
            else
                value.property.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;
            /* Tag 3: opening context tag */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 3))
                return -1;
            offset++;
            
            //data
            List<BACNET_VALUE> _value_list = new List<BACNET_VALUE>();
            while ((apdu_len - len) > 1 && !ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
            {
                BACNET_VALUE b_value;
                int l = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset, (BACNET_PROPERTY_ID)value.property.propertyIdentifier, out b_value);
                if (l <= 0) return -1;
                len += l;
                _value_list.Add(b_value);
            }
            value.value = _value_list;

            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
                return -2;
            /* a tag number of 3 is not extended so only one octet */
            len++;
            /* Tag 4: optional Priority - assumed MAX if not explicitly set */
            value.priority = (byte)ASN1.BACNET_MAX_PRIORITY;
            if (len < apdu_len)
            {
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                if (tag_number == 4)
                {
                    len += tag_len;
                    len = ASN1.decode_unsigned(buffer, offset + len, len_value_type, out unsigned_value);
                    if ((unsigned_value >= ASN1.BACNET_MIN_PRIORITY) && (unsigned_value <= ASN1.BACNET_MAX_PRIORITY))
                        value.priority = (byte)unsigned_value;
                    else
                        return -1;
                }
            }

            return len;
        }

        public static int EncodeWritePropertyMultiple(byte[] buffer, int offset, BACNET_OBJECT_ID object_id, ICollection<BACNET_PROPERTY_VALUE> value_list)
        {
            int org_offset = offset;

            offset += ASN1.encode_context_object_id(buffer, offset, 0, object_id.type, object_id.instance);
            /* Tag 1: sequence of WriteAccessSpecification */
            offset += ASN1.encode_opening_tag(buffer, offset, 1);

            foreach(BACNET_PROPERTY_VALUE p_value in value_list)
            {
                /* Tag 0: Property */
                offset += ASN1.encode_context_enumerated(buffer, offset, 0, p_value.property.propertyIdentifier);

                /* Tag 1: array index */
                if (p_value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                    offset += ASN1.encode_context_unsigned(buffer, offset, 1, p_value.property.propertyArrayIndex);

                /* Tag 2: Value */
                offset += ASN1.encode_opening_tag(buffer, offset, 2);
                foreach (BACNET_VALUE value in p_value.value)
                {
                    int len = ASN1.bacapp_encode_application_data(buffer, offset, buffer.Length, value);
                    if (len < 0) return -1;
                    else offset += len;
                }
                offset += ASN1.encode_closing_tag(buffer, offset, 2);

                /* Tag 3: Priority */
                if (p_value.priority != ASN1.BACNET_NO_PRIORITY)
                    ASN1.encode_context_unsigned(buffer, offset, 3, p_value.priority);
            }

            offset += ASN1.encode_closing_tag(buffer, offset, 1);

            return offset - org_offset;
        }

        public static int DecodeWritePropertyMultiple(byte[] buffer, int offset, int apdu_len, out BACNET_OBJECT_ID object_id, out ICollection<BACNET_PROPERTY_VALUE> values_refs)
        {
            int len = 0;
            byte tag_number;
            uint len_value;
            uint ulVal;
            ushort type;
            uint property_id;

            object_id = new BACNET_OBJECT_ID();
            values_refs = null;

            /* Context tag 0 - Object ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
            if ((tag_number == 0) && (apdu_len > len))
            {
                apdu_len -= len;
                if (apdu_len >= 4)
                {
                    len += ASN1.decode_object_id(buffer, offset + len, out type, out object_id.instance);
                    object_id.type = (BACNET_OBJECT_TYPE)type;
                }
                else
                    return -1;
            }
            else
                return -1;

            /* Tag 1: sequence of WriteAccessSpecification */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            LinkedList<BACNET_PROPERTY_VALUE> _values = new LinkedList<BACNET_PROPERTY_VALUE>();
            while ((apdu_len - len) > 1)
            {
                BACNET_PROPERTY_VALUE new_entry = new BACNET_PROPERTY_VALUE();

                /* tag 0 - Property Identifier */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                if (tag_number == 0)
                    len += ASN1.decode_enumerated(buffer, offset + len, len_value, out property_id);
                else
                    return -1;

                /* tag 1 - Property Array Index - optional */
                ulVal = ASN1.BACNET_ARRAY_ALL;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                if (tag_number == 1)
                {
                    len += ASN1.decode_unsigned(buffer, offset + len, len_value, out ulVal);
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                }
                new_entry.property = new BACNET_PROPERTY_REFERENCE(property_id, ulVal);

                /* tag 2 - Property Value */
                if ((tag_number == 2) && (ASN1.decode_is_opening_tag(buffer, offset + len - 1)))
                {
                    List<BACNET_VALUE> values = new List<BACNET_VALUE>();
                    while(!ASN1.decode_is_closing_tag(buffer, offset + len))
                    {
                        BACNET_VALUE value;
                        int l = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset, (BACNET_PROPERTY_ID)property_id, out value);
                        if (l <= 0) return -1;
                        len += l;
                        values.Add(value);
                    }
                    len++;
                    new_entry.value = values;
                }
                else
                    return -1;

                /* tag 3 - Priority - optional */
                ulVal = ASN1.BACNET_NO_PRIORITY;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                if (tag_number == 3)
                    len += ASN1.decode_unsigned(buffer, offset + len, len_value, out ulVal);
                else
                    len--;
                new_entry.priority = (byte)ulVal;

                _values.AddLast(new_entry);
            }

            /* Closing tag 1 - List of Properties */
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            values_refs = _values;

            return len;
        }

        public static int EncodeTimeSync(byte[] buffer, int offset, DateTime time)
        {
            int org_offset = offset;

            offset += ASN1.encode_application_date(buffer, offset, time);
            offset += ASN1.encode_application_time(buffer, offset, time);

            return offset - org_offset;
        }

        public static int EncodeUtcTimeSync(byte[] buffer, int offset, DateTime time)
        {
            int org_offset = offset;

            offset += ASN1.encode_application_date(buffer, offset, time);
            offset += ASN1.encode_application_time(buffer, offset, time);

            return offset - org_offset;
        }

        public static int EncodeError(byte[] buffer, int offset, uint error_class, uint error_code)
        {
            int org_offset = offset;

            offset += ASN1.encode_application_enumerated(buffer, offset, error_class);
            offset += ASN1.encode_application_enumerated(buffer, offset, error_code);

            return offset - org_offset;
        }

        public static int EncodeSimpleAck(byte[] buffer, int offset)
        {
            int org_offset = offset;

            //no args

            return offset - org_offset;
        }

        public static int DecodeError(byte[] buffer, int offset, int length, out uint error_class, out uint error_code)
        {
            int org_offset = offset;

            byte tag_number;
            uint len_value_type;
            offset += ASN1.decode_tag_number_and_value(buffer, offset, out tag_number, out len_value_type);
            /* FIXME: we could validate that the tag is enumerated... */
            offset += ASN1.decode_enumerated(buffer, offset, len_value_type, out error_class);
            offset += ASN1.decode_tag_number_and_value(buffer, offset, out tag_number, out len_value_type);
            /* FIXME: we could validate that the tag is enumerated... */
            offset += ASN1.decode_enumerated(buffer, offset, len_value_type, out error_code);

            return offset - org_offset;
        }
    }
}
