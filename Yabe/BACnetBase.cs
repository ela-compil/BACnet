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
    public enum BacnetPduTypes : byte
    {
        PDU_TYPE_CONFIRMED_SERVICE_REQUEST = 0,
        SERVER = 1,
        NEGATIVE_ACK = 2,
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

    public enum BacnetSegmentations
    {
        SEGMENTATION_BOTH = 0,
        SEGMENTATION_TRANSMIT = 1,
        SEGMENTATION_RECEIVE = 2,
        SEGMENTATION_NONE = 3,
        MAX_BACNET_SEGMENTATION = 4
    };

    public enum BacnetDeviceStatus
    {
        STATUS_OPERATIONAL = 0,
        STATUS_OPERATIONAL_READ_ONLY = 1,
        STATUS_DOWNLOAD_REQUIRED = 2,
        STATUS_DOWNLOAD_IN_PROGRESS = 3,
        STATUS_NON_OPERATIONAL = 4,
        STATUS_BACKUP_IN_PROGRESS = 5,
        MAX_DEVICE_STATUS = 6
    };

    public enum BacnetRejectReasons
    {
        REJECT_REASON_OTHER = 0,
        REJECT_REASON_BUFFER_OVERFLOW = 1,
        REJECT_REASON_INCONSISTENT_PARAMETERS = 2,
        REJECT_REASON_INVALID_PARAMETER_DATA_TYPE = 3,
        REJECT_REASON_INVALID_TAG = 4,
        REJECT_REASON_MISSING_REQUIRED_PARAMETER = 5,
        REJECT_REASON_PARAMETER_OUT_OF_RANGE = 6,
        REJECT_REASON_TOO_MANY_ARGUMENTS = 7,
        REJECT_REASON_UNDEFINED_ENUMERATION = 8,
        REJECT_REASON_UNRECOGNIZED_SERVICE = 9,
        /* Enumerated values 0-63 are reserved for definition by ASHRAE. */
        /* Enumerated values 64-65535 may be used by others subject to */
        /* the procedures and constraints described in Clause 23. */
        MAX_BACNET_REJECT_REASON = 10,
        /* do the MAX here instead of outside of enum so that
           compilers will allocate adequate sized datatype for enum */
        REJECT_REASON_PROPRIETARY_FIRST = 64,
        REJECT_REASON_PROPRIETARY_LAST = 65535
    };

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
    };

    /* These are sorted in the order given in
       Clause 18. ERROR, REJECT AND ABORT CODES
       The Class and Code pairings are required
       to be used in accordance with Clause 18. */
    public enum BacnetErrorCodes
    {
        /* valid for all classes */
        ERROR_CODE_OTHER = 0,

        /* Error Class - Device */
        ERROR_CODE_DEVICE_BUSY = 3,
        ERROR_CODE_CONFIGURATION_IN_PROGRESS = 2,
        ERROR_CODE_OPERATIONAL_PROBLEM = 25,

        /* Error Class - Object */
        ERROR_CODE_DYNAMIC_CREATION_NOT_SUPPORTED = 4,
        ERROR_CODE_NO_OBJECTS_OF_SPECIFIED_TYPE = 17,
        ERROR_CODE_OBJECT_DELETION_NOT_PERMITTED = 23,
        ERROR_CODE_OBJECT_IDENTIFIER_ALREADY_EXISTS = 24,
        ERROR_CODE_READ_ACCESS_DENIED = 27,
        ERROR_CODE_UNKNOWN_OBJECT = 31,
        ERROR_CODE_UNSUPPORTED_OBJECT_TYPE = 36,

        /* Error Class - Property */
        ERROR_CODE_CHARACTER_SET_NOT_SUPPORTED = 41,
        ERROR_CODE_DATATYPE_NOT_SUPPORTED = 47,
        ERROR_CODE_INCONSISTENT_SELECTION_CRITERION = 8,
        ERROR_CODE_INVALID_ARRAY_INDEX = 42,
        ERROR_CODE_INVALID_DATA_TYPE = 9,
        ERROR_CODE_NOT_COV_PROPERTY = 44,
        ERROR_CODE_OPTIONAL_FUNCTIONALITY_NOT_SUPPORTED = 45,
        ERROR_CODE_PROPERTY_IS_NOT_AN_ARRAY = 50,
        /* ERROR_CODE_READ_ACCESS_DENIED = 27, */
        ERROR_CODE_UNKNOWN_PROPERTY = 32,
        ERROR_CODE_VALUE_OUT_OF_RANGE = 37,
        ERROR_CODE_WRITE_ACCESS_DENIED = 40,

        /* Error Class - Resources */
        ERROR_CODE_NO_SPACE_FOR_OBJECT = 18,
        ERROR_CODE_NO_SPACE_TO_ADD_LIST_ELEMENT = 19,
        ERROR_CODE_NO_SPACE_TO_WRITE_PROPERTY = 20,

        /* Error Class - Security */
        ERROR_CODE_AUTHENTICATION_FAILED = 1,
        /* ERROR_CODE_CHARACTER_SET_NOT_SUPPORTED = 41, */
        ERROR_CODE_INCOMPATIBLE_SECURITY_LEVELS = 6,
        ERROR_CODE_INVALID_OPERATOR_NAME = 12,
        ERROR_CODE_KEY_GENERATION_ERROR = 15,
        ERROR_CODE_PASSWORD_FAILURE = 26,
        ERROR_CODE_SECURITY_NOT_SUPPORTED = 28,
        ERROR_CODE_TIMEOUT = 30,

        /* Error Class - Services */
        /* ERROR_CODE_CHARACTER_SET_NOT_SUPPORTED = 41, */
        ERROR_CODE_COV_SUBSCRIPTION_FAILED = 43,
        ERROR_CODE_DUPLICATE_NAME = 48,
        ERROR_CODE_DUPLICATE_OBJECT_ID = 49,
        ERROR_CODE_FILE_ACCESS_DENIED = 5,
        ERROR_CODE_INCONSISTENT_PARAMETERS = 7,
        ERROR_CODE_INVALID_CONFIGURATION_DATA = 46,
        ERROR_CODE_INVALID_FILE_ACCESS_METHOD = 10,
        ERROR_CODE_INVALID_FILE_START_POSITION = 11,
        ERROR_CODE_INVALID_PARAMETER_DATA_TYPE = 13,
        ERROR_CODE_INVALID_TIME_STAMP = 14,
        ERROR_CODE_MISSING_REQUIRED_PARAMETER = 16,
        /* ERROR_CODE_OPTIONAL_FUNCTIONALITY_NOT_SUPPORTED = 45, */
        ERROR_CODE_PROPERTY_IS_NOT_A_LIST = 22,
        ERROR_CODE_SERVICE_REQUEST_DENIED = 29,

        /* Error Class - VT */
        ERROR_CODE_UNKNOWN_VT_CLASS = 34,
        ERROR_CODE_UNKNOWN_VT_SESSION = 35,
        ERROR_CODE_NO_VT_SESSIONS_AVAILABLE = 21,
        ERROR_CODE_VT_SESSION_ALREADY_CLOSED = 38,
        ERROR_CODE_VT_SESSION_TERMINATION_FAILURE = 39,

        /* unused */
        ERROR_CODE_RESERVED1 = 33,
        /* new error codes from new addenda */
        ERROR_CODE_ABORT_BUFFER_OVERFLOW = 51,
        ERROR_CODE_ABORT_INVALID_APDU_IN_THIS_STATE = 52,
        ERROR_CODE_ABORT_PREEMPTED_BY_HIGHER_PRIORITY_TASK = 53,
        ERROR_CODE_ABORT_SEGMENTATION_NOT_SUPPORTED = 54,
        ERROR_CODE_ABORT_PROPRIETARY = 55,
        ERROR_CODE_ABORT_OTHER = 56,
        ERROR_CODE_INVALID_TAG = 57,
        ERROR_CODE_NETWORK_DOWN = 58,
        ERROR_CODE_REJECT_BUFFER_OVERFLOW = 59,
        ERROR_CODE_REJECT_INCONSISTENT_PARAMETERS = 60,
        ERROR_CODE_REJECT_INVALID_PARAMETER_DATA_TYPE = 61,
        ERROR_CODE_REJECT_INVALID_TAG = 62,
        ERROR_CODE_REJECT_MISSING_REQUIRED_PARAMETER = 63,
        ERROR_CODE_REJECT_PARAMETER_OUT_OF_RANGE = 64,
        ERROR_CODE_REJECT_TOO_MANY_ARGUMENTS = 65,
        ERROR_CODE_REJECT_UNDEFINED_ENUMERATION = 66,
        ERROR_CODE_REJECT_UNRECOGNIZED_SERVICE = 67,
        ERROR_CODE_REJECT_PROPRIETARY = 68,
        ERROR_CODE_REJECT_OTHER = 69,
        ERROR_CODE_UNKNOWN_DEVICE = 70,
        ERROR_CODE_UNKNOWN_ROUTE = 71,
        ERROR_CODE_VALUE_NOT_INITIALIZED = 72,
        ERROR_CODE_INVALID_EVENT_STATE = 73,
        ERROR_CODE_NO_ALARM_CONFIGURED = 74,
        ERROR_CODE_LOG_BUFFER_FULL = 75,
        ERROR_CODE_LOGGED_VALUE_PURGED = 76,
        ERROR_CODE_NO_PROPERTY_SPECIFIED = 77,
        ERROR_CODE_NOT_CONFIGURED_FOR_TRIGGERED_LOGGING = 78,
        ERROR_CODE_UNKNOWN_SUBSCRIPTION = 79,
        ERROR_CODE_PARAMETER_OUT_OF_RANGE = 80,
        ERROR_CODE_LIST_ELEMENT_NOT_FOUND = 81,
        ERROR_CODE_BUSY = 82,
        ERROR_CODE_COMMUNICATION_DISABLED = 83,
        ERROR_CODE_SUCCESS = 84,
        ERROR_CODE_ACCESS_DENIED = 85,
        ERROR_CODE_BAD_DESTINATION_ADDRESS = 86,
        ERROR_CODE_BAD_DESTINATION_DEVICE_ID = 87,
        ERROR_CODE_BAD_SIGNATURE = 88,
        ERROR_CODE_BAD_SOURCE_ADDRESS = 89,
        ERROR_CODE_BAD_TIMESTAMP = 90,
        ERROR_CODE_CANNOT_USE_KEY = 91,
        ERROR_CODE_CANNOT_VERIFY_MESSAGE_ID = 92,
        ERROR_CODE_CORRECT_KEY_REVISION = 93,
        ERROR_CODE_DESTINATION_DEVICE_ID_REQUIRED = 94,
        ERROR_CODE_DUPLICATE_MESSAGE = 95,
        ERROR_CODE_ENCRYPTION_NOT_CONFIGURED = 96,
        ERROR_CODE_ENCRYPTION_REQUIRED = 97,
        ERROR_CODE_INCORRECT_KEY = 98,
        ERROR_CODE_INVALID_KEY_DATA = 99,
        ERROR_CODE_KEY_UPDATE_IN_PROGRESS = 100,
        ERROR_CODE_MALFORMED_MESSAGE = 101,
        ERROR_CODE_NOT_KEY_SERVER = 102,
        ERROR_CODE_SECURITY_NOT_CONFIGURED = 103,
        ERROR_CODE_SOURCE_SECURITY_REQUIRED = 104,
        ERROR_CODE_TOO_MANY_KEYS = 105,
        ERROR_CODE_UNKNOWN_AUTHENTICATION_TYPE = 106,
        ERROR_CODE_UNKNOWN_KEY = 107,
        ERROR_CODE_UNKNOWN_KEY_REVISION = 108,
        ERROR_CODE_UNKNOWN_SOURCE_MESSAGE = 109,
        ERROR_CODE_NOT_ROUTER_TO_DNET = 110,
        ERROR_CODE_ROUTER_BUSY = 111,
        ERROR_CODE_UNKNOWN_NETWORK_MESSAGE = 112,
        ERROR_CODE_MESSAGE_TOO_LONG = 113,
        ERROR_CODE_SECURITY_ERROR = 114,
        ERROR_CODE_ADDRESSING_ERROR = 115,
        ERROR_CODE_WRITE_BDT_FAILED = 116,
        ERROR_CODE_READ_BDT_FAILED = 117,
        ERROR_CODE_REGISTER_FOREIGN_DEVICE_FAILED = 118,
        ERROR_CODE_READ_FDT_FAILED = 119,
        ERROR_CODE_DELETE_FDT_ENTRY_FAILED = 120,
        ERROR_CODE_DISTRIBUTE_BROADCAST_FAILED = 121,
        ERROR_CODE_UNKNOWN_FILE_SIZE = 122,
        ERROR_CODE_ABORT_APDU_TOO_LONG = 123,
        ERROR_CODE_ABORT_APPLICATION_EXCEEDED_REPLY_TIME = 124,
        ERROR_CODE_ABORT_OUT_OF_RESOURCES = 125,
        ERROR_CODE_ABORT_TSM_TIMEOUT = 126,
        ERROR_CODE_ABORT_WINDOW_SIZE_OUT_OF_RANGE = 127,
        ERROR_CODE_FILE_FULL = 128,
        ERROR_CODE_INCONSISTENT_CONFIGURATION = 129,
        ERROR_CODE_INCONSISTENT_OBJECT_TYPE = 130,
        ERROR_CODE_INTERNAL_ERROR = 131,
        ERROR_CODE_NOT_CONFIGURED = 132,
        ERROR_CODE_OUT_OF_MEMORY = 133,
        ERROR_CODE_VALUE_TOO_LONG = 134,
        ERROR_CODE_ABORT_INSUFFICIENT_SECURITY = 135,
        ERROR_CODE_ABORT_SECURITY_ERROR = 136,

        MAX_BACNET_ERROR_CODE = 137,

        /* Enumerated values 0-255 are reserved for definition by ASHRAE. */
        /* Enumerated values 256-65535 may be used by others subject to */
        /* the procedures and constraints described in Clause 23. */
        /* do the max range inside of enum so that
           compilers will allocate adequate sized datatype for enum
           which is used to store decoding */
        ERROR_CODE_PROPRIETARY_FIRST = 256,
        ERROR_CODE_PROPRIETARY_LAST = 65535
    };

    [Flags]
    public enum BacnetStatusFlags
    {
        STATUS_FLAG_IN_ALARM = 1,
        STATUS_FLAG_FAULT = 2,
        STATUS_FLAG_OVERRIDDEN = 4,
        STATUS_FLAG_OUT_OF_SERVICE = 8,
    };

    public enum BacnetServicesSupported
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

    public enum BacnetUnconfirmedServices : byte
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

    public enum BacnetConfirmedServices : byte
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

    // Add FC : from Karg's Stack
    public enum BacnetUnitsId
    {
        UNITS_METERS_PER_SECOND_PER_SECOND = 166,
        /* Area */
        UNITS_SQUARE_METERS = 0,
        UNITS_SQUARE_CENTIMETERS = 116,
        UNITS_SQUARE_FEET = 1,
        UNITS_SQUARE_INCHES = 115,
        /* Currency */
        UNITS_CURRENCY1 = 105,
        UNITS_CURRENCY2 = 106,
        UNITS_CURRENCY3 = 107,
        UNITS_CURRENCY4 = 108,
        UNITS_CURRENCY5 = 109,
        UNITS_CURRENCY6 = 110,
        UNITS_CURRENCY7 = 111,
        UNITS_CURRENCY8 = 112,
        UNITS_CURRENCY9 = 113,
        UNITS_CURRENCY10 = 114,
        /* Electrical */
        UNITS_MILLIAMPERES = 2,
        UNITS_AMPERES = 3,
        UNITS_AMPERES_PER_METER = 167,
        UNITS_AMPERES_PER_SQUARE_METER = 168,
        UNITS_AMPERE_SQUARE_METERS = 169,
        UNITS_DECIBELS = 199,
        UNITS_DECIBELS_MILLIVOLT = 200,
        UNITS_DECIBELS_VOLT = 201,
        UNITS_FARADS = 170,
        UNITS_HENRYS = 171,
        UNITS_OHMS = 4,
        UNITS_OHM_METERS = 172,
        UNITS_MILLIOHMS = 145,
        UNITS_KILOHMS = 122,
        UNITS_MEGOHMS = 123,
        UNITS_MICROSIEMENS = 190,
        UNITS_MILLISIEMENS = 202,
        UNITS_SIEMENS = 173,        /* 1 mho equals 1 siemens */
        UNITS_SIEMENS_PER_METER = 174,
        UNITS_TESLAS = 175,
        UNITS_VOLTS = 5,
        UNITS_MILLIVOLTS = 124,
        UNITS_KILOVOLTS = 6,
        UNITS_MEGAVOLTS = 7,
        UNITS_VOLT_AMPERES = 8,
        UNITS_KILOVOLT_AMPERES = 9,
        UNITS_MEGAVOLT_AMPERES = 10,
        UNITS_VOLT_AMPERES_REACTIVE = 11,
        UNITS_KILOVOLT_AMPERES_REACTIVE = 12,
        UNITS_MEGAVOLT_AMPERES_REACTIVE = 13,
        UNITS_VOLTS_PER_DEGREE_KELVIN = 176,
        UNITS_VOLTS_PER_METER = 177,
        UNITS_DEGREES_PHASE = 14,
        UNITS_POWER_FACTOR = 15,
        UNITS_WEBERS = 178,
        /* Energy */
        UNITS_JOULES = 16,
        UNITS_KILOJOULES = 17,
        UNITS_KILOJOULES_PER_KILOGRAM = 125,
        UNITS_MEGAJOULES = 126,
        UNITS_WATT_HOURS = 18,
        UNITS_KILOWATT_HOURS = 19,
        UNITS_MEGAWATT_HOURS = 146,
        UNITS_WATT_HOURS_REACTIVE = 203,
        UNITS_KILOWATT_HOURS_REACTIVE = 204,
        UNITS_MEGAWATT_HOURS_REACTIVE = 205,
        UNITS_BTUS = 20,
        UNITS_KILO_BTUS = 147,
        UNITS_MEGA_BTUS = 148,
        UNITS_THERMS = 21,
        UNITS_TON_HOURS = 22,
        /* Enthalpy */
        UNITS_JOULES_PER_KILOGRAM_DRY_AIR = 23,
        UNITS_KILOJOULES_PER_KILOGRAM_DRY_AIR = 149,
        UNITS_MEGAJOULES_PER_KILOGRAM_DRY_AIR = 150,
        UNITS_BTUS_PER_POUND_DRY_AIR = 24,
        UNITS_BTUS_PER_POUND = 117,
        /* Entropy */
        UNITS_JOULES_PER_DEGREE_KELVIN = 127,
        UNITS_KILOJOULES_PER_DEGREE_KELVIN = 151,
        UNITS_MEGAJOULES_PER_DEGREE_KELVIN = 152,
        UNITS_JOULES_PER_KILOGRAM_DEGREE_KELVIN = 128,
        /* Force */
        UNITS_NEWTON = 153,
        /* Frequency */
        UNITS_CYCLES_PER_HOUR = 25,
        UNITS_CYCLES_PER_MINUTE = 26,
        UNITS_HERTZ = 27,
        UNITS_KILOHERTZ = 129,
        UNITS_MEGAHERTZ = 130,
        UNITS_PER_HOUR = 131,
        /* Humidity */
        UNITS_GRAMS_OF_WATER_PER_KILOGRAM_DRY_AIR = 28,
        UNITS_PERCENT_RELATIVE_HUMIDITY = 29,
        /* Length */
        UNITS_MICROMETERS = 194,
        UNITS_MILLIMETERS = 30,
        UNITS_CENTIMETERS = 118,
        UNITS_KILOMETERS = 193,
        UNITS_METERS = 31,
        UNITS_INCHES = 32,
        UNITS_FEET = 33,
        /* Light */
        UNITS_CANDELAS = 179,
        UNITS_CANDELAS_PER_SQUARE_METER = 180,
        UNITS_WATTS_PER_SQUARE_FOOT = 34,
        UNITS_WATTS_PER_SQUARE_METER = 35,
        UNITS_LUMENS = 36,
        UNITS_LUXES = 37,
        UNITS_FOOT_CANDLES = 38,
        /* Mass */
        UNITS_MILLIGRAMS = 196,
        UNITS_GRAMS = 195,
        UNITS_KILOGRAMS = 39,
        UNITS_POUNDS_MASS = 40,
        UNITS_TONS = 41,
        /* Mass Flow */
        UNITS_GRAMS_PER_SECOND = 154,
        UNITS_GRAMS_PER_MINUTE = 155,
        UNITS_KILOGRAMS_PER_SECOND = 42,
        UNITS_KILOGRAMS_PER_MINUTE = 43,
        UNITS_KILOGRAMS_PER_HOUR = 44,
        UNITS_POUNDS_MASS_PER_SECOND = 119,
        UNITS_POUNDS_MASS_PER_MINUTE = 45,
        UNITS_POUNDS_MASS_PER_HOUR = 46,
        UNITS_TONS_PER_HOUR = 156,
        /* Power */
        UNITS_MILLIWATTS = 132,
        UNITS_WATTS = 47,
        UNITS_KILOWATTS = 48,
        UNITS_MEGAWATTS = 49,
        UNITS_BTUS_PER_HOUR = 50,
        UNITS_KILO_BTUS_PER_HOUR = 157,
        UNITS_HORSEPOWER = 51,
        UNITS_TONS_REFRIGERATION = 52,
        /* Pressure */
        UNITS_PASCALS = 53,
        UNITS_HECTOPASCALS = 133,
        UNITS_KILOPASCALS = 54,
        UNITS_MILLIBARS = 134,
        UNITS_BARS = 55,
        UNITS_POUNDS_FORCE_PER_SQUARE_INCH = 56,
        UNITS_MILLIMETERS_OF_WATER = 206,
        UNITS_CENTIMETERS_OF_WATER = 57,
        UNITS_INCHES_OF_WATER = 58,
        UNITS_MILLIMETERS_OF_MERCURY = 59,
        UNITS_CENTIMETERS_OF_MERCURY = 60,
        UNITS_INCHES_OF_MERCURY = 61,
        /* Temperature */
        UNITS_DEGREES_CELSIUS = 62,
        UNITS_DEGREES_KELVIN = 63,
        UNITS_DEGREES_KELVIN_PER_HOUR = 181,
        UNITS_DEGREES_KELVIN_PER_MINUTE = 182,
        UNITS_DEGREES_FAHRENHEIT = 64,
        UNITS_DEGREE_DAYS_CELSIUS = 65,
        UNITS_DEGREE_DAYS_FAHRENHEIT = 66,
        UNITS_DELTA_DEGREES_FAHRENHEIT = 120,
        UNITS_DELTA_DEGREES_KELVIN = 121,
        /* Time */
        UNITS_YEARS = 67,
        UNITS_MONTHS = 68,
        UNITS_WEEKS = 69,
        UNITS_DAYS = 70,
        UNITS_HOURS = 71,
        UNITS_MINUTES = 72,
        UNITS_SECONDS = 73,
        UNITS_HUNDREDTHS_SECONDS = 158,
        UNITS_MILLISECONDS = 159,
        /* Torque */
        UNITS_NEWTON_METERS = 160,
        /* Velocity */
        UNITS_MILLIMETERS_PER_SECOND = 161,
        UNITS_MILLIMETERS_PER_MINUTE = 162,
        UNITS_METERS_PER_SECOND = 74,
        UNITS_METERS_PER_MINUTE = 163,
        UNITS_METERS_PER_HOUR = 164,
        UNITS_KILOMETERS_PER_HOUR = 75,
        UNITS_FEET_PER_SECOND = 76,
        UNITS_FEET_PER_MINUTE = 77,
        UNITS_MILES_PER_HOUR = 78,
        /* Volume */
        UNITS_CUBIC_FEET = 79,
        UNITS_CUBIC_METERS = 80,
        UNITS_IMPERIAL_GALLONS = 81,
        UNITS_MILLILITERS = 197,
        UNITS_LITERS = 82,
        UNITS_US_GALLONS = 83,
        /* Volumetric Flow */
        UNITS_CUBIC_FEET_PER_SECOND = 142,
        UNITS_CUBIC_FEET_PER_MINUTE = 84,
        UNITS_CUBIC_FEET_PER_HOUR = 191,
        UNITS_CUBIC_METERS_PER_SECOND = 85,
        UNITS_CUBIC_METERS_PER_MINUTE = 165,
        UNITS_CUBIC_METERS_PER_HOUR = 135,
        UNITS_IMPERIAL_GALLONS_PER_MINUTE = 86,
        UNITS_MILLILITERS_PER_SECOND = 198,
        UNITS_LITERS_PER_SECOND = 87,
        UNITS_LITERS_PER_MINUTE = 88,
        UNITS_LITERS_PER_HOUR = 136,
        UNITS_US_GALLONS_PER_MINUTE = 89,
        UNITS_US_GALLONS_PER_HOUR = 192,
        /* Other */
        UNITS_DEGREES_ANGULAR = 90,
        UNITS_DEGREES_CELSIUS_PER_HOUR = 91,
        UNITS_DEGREES_CELSIUS_PER_MINUTE = 92,
        UNITS_DEGREES_FAHRENHEIT_PER_HOUR = 93,
        UNITS_DEGREES_FAHRENHEIT_PER_MINUTE = 94,
        UNITS_JOULE_SECONDS = 183,
        UNITS_KILOGRAMS_PER_CUBIC_METER = 186,
        UNITS_KW_HOURS_PER_SQUARE_METER = 137,
        UNITS_KW_HOURS_PER_SQUARE_FOOT = 138,
        UNITS_MEGAJOULES_PER_SQUARE_METER = 139,
        UNITS_MEGAJOULES_PER_SQUARE_FOOT = 140,
        UNITS_NO_UNITS = 95,
        UNITS_NEWTON_SECONDS = 187,
        UNITS_NEWTONS_PER_METER = 188,
        UNITS_PARTS_PER_MILLION = 96,
        UNITS_PARTS_PER_BILLION = 97,
        UNITS_PERCENT = 98,
        UNITS_PERCENT_OBSCURATION_PER_FOOT = 143,
        UNITS_PERCENT_OBSCURATION_PER_METER = 144,
        UNITS_PERCENT_PER_SECOND = 99,
        UNITS_PER_MINUTE = 100,
        UNITS_PER_SECOND = 101,
        UNITS_PSI_PER_DEGREE_FAHRENHEIT = 102,
        UNITS_RADIANS = 103,
        UNITS_RADIANS_PER_SECOND = 184,
        UNITS_REVOLUTIONS_PER_MINUTE = 104,
        UNITS_SQUARE_METERS_PER_NEWTON = 185,
        UNITS_WATTS_PER_METER_PER_DEGREE_KELVIN = 189,
        UNITS_WATTS_PER_SQUARE_METER_DEGREE_KELVIN = 141,
        UNITS_PER_MILLE = 207,
        UNITS_GRAMS_PER_GRAM = 208,
        UNITS_KILOGRAMS_PER_KILOGRAM = 209,
        UNITS_GRAMS_PER_KILOGRAM = 210,
        UNITS_MILLIGRAMS_PER_GRAM = 211,
        UNITS_MILLIGRAMS_PER_KILOGRAM = 212,
        UNITS_GRAMS_PER_MILLILITER = 213,
        UNITS_GRAMS_PER_LITER = 214,
        UNITS_MILLIGRAMS_PER_LITER = 215,
        UNITS_MICROGRAMS_PER_LITER = 216,
        UNITS_GRAMS_PER_CUBIC_METER = 217,
        UNITS_MILLIGRAMS_PER_CUBIC_METER = 218,
        UNITS_MICROGRAMS_PER_CUBIC_METER = 219,
        UNITS_NANOGRAMS_PER_CUBIC_METER = 220,
        UNITS_GRAMS_PER_CUBIC_CENTIMETER = 221,
        UNITS_BECQUERELS = 222,
        UNITS_MEGABECQUERELS = 224,
        UNITS_GRAY = 225,
        UNITS_MILLIGRAY = 226,
        UNITS_MICROGRAY = 227,
        UNITS_SIEVERTS = 228,
        UNITS_MILLISIEVERTS = 229,
        UNITS_MICROSIEVERTS = 230,
        UNITS_MICROSIEVERTS_PER_HOUR = 231,
        UNITS_DECIBELS_A = 232,
        UNITS_NEPHELOMETRIC_TURBIDITY_UNIT = 233,
        UNITS_PH = 234,
        UNITS_GRAMS_PER_SQUARE_METER = 235,
        UNITS_MINUTES_PER_DEGREE_KELVIN = 236,
    }


    public enum BacnetMaxSegments : byte
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

    public enum BacnetMaxAdpu : byte
    {
        MAX_APDU50 = 0,
        MAX_APDU128 = 1,
        MAX_APDU206 = 2,
        MAX_APDU480 = 3,
        MAX_APDU1024 = 4,
        MAX_APDU1476 = 5,
    };

    public enum BacnetObjectTypes
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

    public enum BacnetApplicationTags
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
        /* BACnetReadAccessResult */
        BACNET_APPLICATION_TAG_READ_ACCESS_RESULT,
        /* BACnetLightingCommand */
        BACNET_APPLICATION_TAG_LIGHTING_COMMAND,
        BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED,
        BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_ENCODED,
    };

    public enum BacnetCharacterStringEncodings
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

    public struct BacnetPropetyState
    {
        public enum BacnetPropertyStateTypes
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

        public BacnetPropertyStateTypes tag;
        public uint state;
    } ;

    public struct BacnetDeviceObjectPropertyReference
    {
        public BacnetObjectId objectIdentifier;
        public uint propertyIdentifier;
        public UInt32 arrayIndex;
        public BacnetObjectId deviceIndentifier;
    } ;

    public struct BacnetEventNotificationData
    {
        public UInt32 processIdentifier;
        public BacnetObjectId initiatingObjectIdentifier;
        public BacnetObjectId eventObjectIdentifier;
        public BacnetGenericTime timeStamp;
        public UInt32 notificationClass;
        public byte priority;
        public BacnetEventTypes eventType;
        public string messageText;       /* OPTIONAL - Set to NULL if not being used */
        public BacnetNotifyTypes notifyType;
        public bool ackRequired;
        public BacnetEventStates fromState;
        public BacnetEventStates toState;

        public enum BacnetEventStates
        {
            EVENT_STATE_NORMAL = 0,
            EVENT_STATE_FAULT = 1,
            EVENT_STATE_OFFNORMAL = 2,
            EVENT_STATE_HIGH_LIMIT = 3,
            EVENT_STATE_LOW_LIMIT = 4
        } ;

        public enum BacnetNotifyTypes
        {
            NOTIFY_ALARM = 0,
            NOTIFY_EVENT = 1,
            NOTIFY_ACK_NOTIFICATION = 2
        } ;

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

        public enum BacnetCOVTypes
        {
            CHANGE_OF_VALUE_BITS,
            CHANGE_OF_VALUE_REAL
        } ;

        public enum BacnetLifeSafetyStates
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
        } ;

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
        } ;

        /*
         ** Each of these structures in the union maps to a particular eventtype
         ** Based on BACnetNotificationParameters
         */

        /*
         ** EVENT_CHANGE_OF_BITSTRING
         */
        public BacnetBitString changeOfBitstring_referencedBitString;
        public BacnetBitString changeOfBitstring_statusFlags;
        /*
         ** EVENT_CHANGE_OF_STATE
         */
        public BacnetPropetyState changeOfState_newState;
        public BacnetBitString changeOfState_statusFlags;
        /*
         ** EVENT_CHANGE_OF_VALUE
         */
        public BacnetBitString changeOfValue_changedBits;
        public float changeOfValue_changeValue;
        public BacnetCOVTypes changeOfValue_tag;
        public BacnetBitString changeOfValue_statusFlags;
        /*
         ** EVENT_COMMAND_FAILURE
         **
         ** Not Supported!
         */
        /*
         ** EVENT_FLOATING_LIMIT
         */
        public float floatingLimit_referenceValue;
        public BacnetBitString floatingLimit_statusFlags;
        public float floatingLimit_setPointValue;
        public float floatingLimit_errorLimit;
        /*
         ** EVENT_OUT_OF_RANGE
         */
        public float outOfRange_exceedingValue;
        public BacnetBitString outOfRange_statusFlags;
        public float outOfRange_deadband;
        public float outOfRange_exceededLimit;
        /*
         ** EVENT_CHANGE_OF_LIFE_SAFETY
         */
        public BacnetLifeSafetyStates changeOfLifeSafety_newState;
        public BacnetLifeSafetyModes changeOfLifeSafety_newMode;
        public BacnetBitString changeOfLifeSafety_statusFlags;
        public BacnetLifeSafetyOperations changeOfLifeSafety_operationExpected;
        /*
         ** EVENT_EXTENDED
         **
         ** Not Supported!
         */
        /*
         ** EVENT_BUFFER_READY
         */
        public BacnetDeviceObjectPropertyReference bufferReady_bufferProperty;
        public uint bufferReady_previousNotification;
        public uint bufferReady_currentNotification;
        /*
         ** EVENT_UNSIGNED_RANGE
         */
        public uint unsignedRange_exceedingValue;
        public BacnetBitString unsignedRange_statusFlags;
        public uint unsignedRange_exceededLimit;
    };

    public struct BacnetValue
    {
        public BacnetApplicationTags Tag;
        public object Value;
        public BacnetValue(BacnetApplicationTags tag, object value)
        {
            this.Tag = tag;
            this.Value = value;
        }
        public BacnetValue(object value)
        {
            this.Value = value;
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL;

            //guess at the tag
            if (value != null)
            {
                Type t = value.GetType();
                if (t == typeof(string))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING;
                else if (t == typeof(int) || t == typeof(short) || t == typeof(byte))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT;
                else if (t == typeof(uint) || t == typeof(ushort) || t == typeof(sbyte))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                else if (t == typeof(bool))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
                else if (t == typeof(float))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;
                else if (t == typeof(double))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE;
                else if (t == typeof(BacnetBitString))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING;
                else if (t == typeof(BacnetObjectId))
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
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

    public struct BacnetObjectId
    {
        public BacnetObjectTypes type;
        public UInt32 instance;
        public BacnetObjectId(BacnetObjectTypes type, UInt32 instance)
        {
            this.type = type;
            this.instance = instance;
        }
        public override string ToString()
        {
            return type.ToString() + ":" + instance;
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else return obj.ToString().Equals(this.ToString());
        }

        public static BacnetObjectId Parse(string value)
        {
            BacnetObjectId ret = new BacnetObjectId();
            if (string.IsNullOrEmpty(value)) return ret;
            int p = value.IndexOf(":");
            if (p < 0) return ret;
            string str_type = value.Substring(0, p);
            string str_instance = value.Substring(p + 1);
            ret.type = (BacnetObjectTypes)Enum.Parse(typeof(BacnetObjectTypes), str_type);
            ret.instance = uint.Parse(str_instance);
            return ret;
        }
    };

    public struct BacnetPropertyReference
    {
        public UInt32 propertyIdentifier;
        public UInt32 propertyArrayIndex;        /* optional */
        public BacnetPropertyReference(uint id, uint array_index)
        {
            propertyIdentifier = id;
            propertyArrayIndex = array_index;
        }
        public override string ToString()
        {
            return ((BacnetPropertyIds)propertyIdentifier).ToString();
        }
    };

    public struct BacnetPropertyValue
    {
        public BacnetPropertyReference property;
        public IList<BacnetValue> value;
        public byte priority;

        public override string ToString()
        {
            return property.ToString();
        }
    }

    public struct BacnetGenericTime
    {
        public BacnetTimestampTags Tag;
        public DateTime Time;
        public UInt16 Sequence;
    }

    public enum BacnetTimestampTags
    {
        TIME_STAMP_NONE = -1,
        TIME_STAMP_TIME = 0,
        TIME_STAMP_SEQUENCE = 1,
        TIME_STAMP_DATETIME = 2
    };

    public struct BacnetGetEventInformationData
    {
        public BacnetObjectId objectIdentifier;
        public BacnetEventNotificationData.BacnetEventStates eventState;
        public BacnetBitString acknowledgedTransitions;
        public BacnetGenericTime[] eventTimeStamps;    //3
        public BacnetEventNotificationData.BacnetNotifyTypes notifyType;
        public BacnetBitString eventEnable;
        public uint[] eventPriorities;     //3
    };

    public struct BacnetReadAccessSpecification
    {
        public BacnetObjectId objectIdentifier;
        public IList<BacnetPropertyReference> propertyReferences;
        public BacnetReadAccessSpecification(BacnetObjectId objectIdentifier, IList<BacnetPropertyReference> propertyReferences)
        {
            this.objectIdentifier = objectIdentifier;
            this.propertyReferences = propertyReferences;
        }
        public static object Parse(string value)
        {
            BacnetReadAccessSpecification ret = new BacnetReadAccessSpecification();
            if (string.IsNullOrEmpty(value)) return ret;
            string[] tmp = value.Split(':');
            if (tmp == null || tmp.Length < 2) return ret;
            ret.objectIdentifier.type = (BacnetObjectTypes)Enum.Parse(typeof(BacnetObjectTypes), tmp[0]);
            ret.objectIdentifier.instance = uint.Parse(tmp[1]);
            List<BacnetPropertyReference> refs = new List<BacnetPropertyReference>();
            for (int i = 2; i < tmp.Length; i++)
            {
                BacnetPropertyReference n = new BacnetPropertyReference();
                n.propertyArrayIndex = System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL;
                n.propertyIdentifier = (uint)(BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), tmp[i]);
                refs.Add(n);
            }
            ret.propertyReferences = refs;
            return ret;
        }
        public override string ToString()
        {
            string ret = objectIdentifier.ToString();
            foreach (BacnetPropertyReference r in propertyReferences)
            {
                ret += ":" + ((BacnetPropertyIds)r.propertyIdentifier).ToString();
            }
            return ret;
        }
    }

    public struct BacnetReadAccessResult
    {
        public BacnetObjectId objectIdentifier;
        public IList<BacnetPropertyValue> values;
        public BacnetReadAccessResult(BacnetObjectId objectIdentifier, IList<BacnetPropertyValue> values)
        {
            this.objectIdentifier = objectIdentifier;
            this.values = values;
        }
    }

    public struct BacnetCOVSubscription
    {
        /* BACnetRecipientProcess */
        public BacnetAddress Recipient;
        public uint subscriptionProcessIdentifier;
        /* BACnetObjectPropertyReference */
        public BacnetObjectId monitoredObjectIdentifier;
        public BacnetPropertyReference monitoredProperty;
        /* BACnetCOVSubscription */
        public bool IssueConfirmedNotifications;
        public uint TimeRemaining; 
        public float COVIncrement;
    }

    public struct BacnetError
    {
        public BacnetErrorClasses error_class;
        public BacnetErrorCodes error_code;
        public BacnetError(BacnetErrorClasses error_class, BacnetErrorCodes error_code)
        {
            this.error_class = error_class;
            this.error_code = error_code;
        }
    }

    public struct BacnetBitString
    {
        public byte bits_used;
        public byte[] value;

        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < bits_used; i++)
            {
                ret = ret + ((value[i / 8] & (1 << (i % 8))) > 0 ? "1" : "0");
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

        public static BacnetBitString Parse(string str)
        {
            BacnetBitString ret = new BacnetBitString();
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

    public enum BacnetAddressTypes
    {
        None,
        IP,
        MSTP,
        Ethernet,
        ArcNet,
        LonTalk,
        PTP,
    }

    public class BacnetAddress
    {
        public UInt16 net;
        public byte[] adr;
        public BacnetAddressTypes type;

        // Modif FC
        public BacnetAddress RoutedSource=null;

        public BacnetAddress(BacnetAddressTypes type, UInt16 net, byte[] adr)
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
                case BacnetAddressTypes.IP:
                    if(adr == null || adr.Length < 6) return "0.0.0.0";
                    ushort port = (ushort)((adr[4] << 8) | (adr[5] << 0));
                    return adr[0] + "." + adr[1] + "." + adr[2] + "." + adr[3] + ":" + port;
                case BacnetAddressTypes.MSTP:
                    if(adr == null || adr.Length < 1) return "-1";
                    return adr[0].ToString();
                case BacnetAddressTypes.PTP:
                    return "x";
                default:
                    return base.ToString();
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BacnetAddress)) return false;
            BacnetAddress d = (BacnetAddress)obj;
            if (adr == null && d.adr == null) return true;
            else if (adr == null || d.adr == null) return false;
            else if (adr.Length != d.adr.Length) return false;
            else
            {
                for (int i = 0; i < adr.Length; i++)
                    if (adr[i] != d.adr[i]) return false;

                // Modif FC
                if ((RoutedSource == null) && (d.RoutedSource != null))
                    return false;                  
                if ((d.RoutedSource==null)&&(RoutedSource == null)) return true;
                return RoutedSource.Equals(d.RoutedSource);

            }
        }
    }

    public enum BacnetPtpFrameTypes : byte
    {
        FRAME_TYPE_HEARTBEAT_XOFF = 0,
        FRAME_TYPE_HEARTBEAT_XON = 1,
        FRAME_TYPE_DATA0 = 2,
        FRAME_TYPE_DATA1 = 3,
        FRAME_TYPE_DATA_ACK0_XOFF = 4,
        FRAME_TYPE_DATA_ACK1_XOFF = 5,
        FRAME_TYPE_DATA_ACK0_XON = 6,
        FRAME_TYPE_DATA_ACK1_XON = 7,
        FRAME_TYPE_DATA_NAK0_XOFF = 8,
        FRAME_TYPE_DATA_NAK1_XOFF = 9,
        FRAME_TYPE_DATA_NAK0_XON = 0x0A,
        FRAME_TYPE_DATA_NAK1_XON = 0x0B,
        FRAME_TYPE_CONNECT_REQUEST = 0x0C,
        FRAME_TYPE_CONNECT_RESPONSE = 0x0D,
        FRAME_TYPE_DISCONNECT_REQUEST = 0x0E,
        FRAME_TYPE_DISCONNECT_RESPONSE = 0x0F,
        FRAME_TYPE_TEST_REQUEST = 0x14,
        FRAME_TYPE_TEST_RESPONSE = 0x15,
        FRAME_TYPE_GREETING = 0xFF,     //special invention
    }

    public enum BacnetPtpDisconnectReasons : byte
    {
        PTP_DISCONNECT_NO_MORE_DATA = 0,
        PTP_DISCONNECT_PREEMPTED = 1,
        PTP_DISCONNECT_INVALID_PASSWORD = 2,
        PTP_DISCONNECT_OTHER = 3,
    }

    /* MS/TP Frame Type */
    public enum BacnetMstpFrameTypes : byte
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

    public enum BacnetNodeTypes
    {
        NT_UNKNOWN,
        NT_SYSTEM,
        NT_NETWORK,
        NT_DEVICE,
        NT_ORGANIZATIONAL,
        NT_AREA,
        NT_EQUIPMENT,
        NT_POINT,
        NT_COLLECTION,
        NT_PROPERTY,
        NT_FUNCTIONAL,
        NT_OTHER,
    }

    public enum BacnetBvlcFunctions : byte
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
    public enum BacnetNpduControls : byte
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
    public enum BacnetNetworkMessageTypes : byte
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

    public enum BacnetReinitializedStates
    {
        BACNET_REINIT_COLDSTART = 0,
        BACNET_REINIT_WARMSTART = 1,
        BACNET_REINIT_STARTBACKUP = 2,
        BACNET_REINIT_ENDBACKUP = 3,
        BACNET_REINIT_STARTRESTORE = 4,
        BACNET_REINIT_ENDRESTORE = 5,
        BACNET_REINIT_ABORTRESTORE = 6,
        BACNET_REINIT_IDLE = 255
    };
}

namespace System.IO.BACnet.Serialize
{
    public class BVLC
    {
        public const byte BVLL_TYPE_BACNET_IP = 0x81;
        public const byte BVLC_HEADER_LENGTH = 4;
        public const BacnetMaxAdpu BVLC_MAX_APDU = BacnetMaxAdpu.MAX_APDU1476;

        public static int Encode(byte[] buffer, int offset, BacnetBvlcFunctions function, int msg_length)
        {
            buffer[offset + 0] = BVLL_TYPE_BACNET_IP;
            buffer[offset + 1] = (byte)function;
            buffer[offset + 2] = (byte)((msg_length & 0xFF00) >> 8);
            buffer[offset + 3] = (byte)((msg_length & 0x00FF) >> 0);
            return 4;
        }

        public static int Decode(byte[] buffer, int offset, out BacnetBvlcFunctions function, out int msg_length)
        {
            function = (BacnetBvlcFunctions)buffer[offset + 1];
            msg_length = (buffer[offset + 2] << 8) | (buffer[offset + 3] << 0);
            if (buffer[offset + 0] != BVLL_TYPE_BACNET_IP) return -1;
            return 4;
        }
    }

    public class MSTP
    {
        public const byte MSTP_PREAMBLE1 = 0x55;
        public const byte MSTP_PREAMBLE2 = 0xFF;
        public const BacnetMaxAdpu MSTP_MAX_APDU = BacnetMaxAdpu.MAX_APDU480;
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

        public static int Encode(byte[] buffer, int offset, BacnetMstpFrameTypes frame_type, byte destination_address, byte source_address, int msg_length)
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

        public static int Decode(byte[] buffer, int offset, int max_length, out BacnetMstpFrameTypes frame_type, out byte destination_address, out byte source_address, out int msg_length)
        {
            frame_type = (BacnetMstpFrameTypes)buffer[offset + 2];
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

    public class PTP
    {
        public const byte PTP_PREAMBLE1 = 0x55;
        public const byte PTP_PREAMBLE2 = 0xFF;
        public const byte PTP_GREETING_PREAMBLE1 = 0x42;
        public const byte PTP_GREETING_PREAMBLE2 = 0x41;
        public const BacnetMaxAdpu PTP_MAX_APDU = BacnetMaxAdpu.MAX_APDU480;
        public const byte PTP_HEADER_LENGTH = 6;

        public static int Encode(byte[] buffer, int offset, BacnetPtpFrameTypes frame_type, int msg_length)
        {
            buffer[offset + 0] = PTP_PREAMBLE1;
            buffer[offset + 1] = PTP_PREAMBLE2;
            buffer[offset + 2] = (byte)frame_type;
            buffer[offset + 3] = (byte)((msg_length & 0xFF00) >> 8);
            buffer[offset + 4] = (byte)((msg_length & 0x00FF) >> 0);
            buffer[offset + 5] = MSTP.CRC_Calc_Header(buffer, offset + 2, 3);
            if (msg_length > 0)
            {
                //calculate data crc
                ushort data_crc = MSTP.CRC_Calc_Data(buffer, offset + 6, msg_length);
                buffer[offset + 6 + msg_length + 0] = (byte)(data_crc & 0xFF);  //LSB first!
                buffer[offset + 6 + msg_length + 1] = (byte)(data_crc >> 8);
            }
            return PTP_HEADER_LENGTH + (msg_length) + (msg_length > 0 ? 2 : 0);
        }

        public static int Decode(byte[] buffer, int offset, int max_length, out BacnetPtpFrameTypes frame_type, out int msg_length)
        {
            frame_type = (BacnetPtpFrameTypes)buffer[offset + 2];
            msg_length = (buffer[offset + 3] << 8) | (buffer[offset + 4] << 0);
            byte crc_header = buffer[offset + 5];
            ushort crc_data = 0;
            if (max_length < PTP_HEADER_LENGTH) return -1;     //not enough data
            if (msg_length > 0) crc_data = (ushort)((buffer[offset + 6 + msg_length + 1] << 8) | (buffer[offset + 6 + msg_length + 0] << 0));
            if (buffer[offset + 0] != PTP_PREAMBLE1) return -1;
            if (buffer[offset + 1] != PTP_PREAMBLE2) return -1;
            if (MSTP.CRC_Calc_Header(buffer, offset + 2, 3) != crc_header) return -1;
            if (msg_length > 0 && max_length >= (PTP_HEADER_LENGTH + msg_length + 2) && MSTP.CRC_Calc_Data(buffer, offset + 6, msg_length) != crc_data) return -1;
            return 8 + (msg_length) + (msg_length > 0 ? 2 : 0);
        }

    }

    public class NPDU
    {
        public const byte BACNET_PROTOCOL_VERSION = 1;

        public static BacnetNpduControls DecodeFunction(byte[] buffer, int offset)
        {
            if (buffer[offset + 0] != BACNET_PROTOCOL_VERSION) return 0;
            return (BacnetNpduControls)buffer[offset + 1];
        }

        public static int Decode(byte[] buffer, int offset, out BacnetNpduControls function, out BacnetAddress destination, out BacnetAddress source, out byte hop_count, out BacnetNetworkMessageTypes network_msg_type, out ushort vendor_id)
        {
            int org_offset = offset;

            offset++;
            function = (BacnetNpduControls)buffer[offset++];

            destination = null;
            if ((function & BacnetNpduControls.DestinationSpecified) == BacnetNpduControls.DestinationSpecified)
            {
                destination = new BacnetAddress(BacnetAddressTypes.None, (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0)), null);
                int adr_len = buffer[offset++];
                if (adr_len > 0)
                {
                    destination.adr = new byte[adr_len];
                    for (int i = 0; i < destination.adr.Length; i++)
                        destination.adr[i] = buffer[offset++];
                }
            }

            source = null;
            if ((function & BacnetNpduControls.SourceSpecified) == BacnetNpduControls.SourceSpecified)
            {
                source = new BacnetAddress(BacnetAddressTypes.None, (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0)), null);
                int adr_len = buffer[offset++];
                if (adr_len > 0)
                {
                    source.adr = new byte[adr_len];
                    for (int i = 0; i < source.adr.Length; i++)
                        source.adr[i] = buffer[offset++];
                }
            }

            hop_count = 0;
            if ((function & BacnetNpduControls.DestinationSpecified) == BacnetNpduControls.DestinationSpecified)
            {
                hop_count = buffer[offset++];
            }

            network_msg_type = BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK;
            vendor_id = 0;
            if ((function & BacnetNpduControls.NetworkLayerMessage) == BacnetNpduControls.NetworkLayerMessage)
            {
                network_msg_type = (BacnetNetworkMessageTypes)buffer[offset++];
                if (((byte)network_msg_type) >= 0x80)
                {
                    vendor_id = (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0));
                }
            }

            if (buffer[org_offset + 0] != BACNET_PROTOCOL_VERSION) return -1;
            return offset - org_offset;
        }

        public static void Encode(EncodeBuffer buffer, BacnetNpduControls function, BacnetAddress destination, BacnetAddress source, byte hop_count, BacnetNetworkMessageTypes network_msg_type, ushort vendor_id)
        {
            // Modif FC
            bool has_destination = destination != null && destination.net > 0; // && destination.net != 0xFFFF;
            bool has_source = source != null && source.net > 0 && source.net != 0xFFFF;

            buffer.buffer[buffer.offset++] = BACNET_PROTOCOL_VERSION;
            buffer.buffer[buffer.offset++] = (byte)(function | (has_destination ? BacnetNpduControls.DestinationSpecified : 0) | (has_source ? BacnetNpduControls.SourceSpecified : 0));

            if (has_destination)
            {
                buffer.buffer[buffer.offset++] =(byte)((destination.net & 0xFF00) >> 8);
                buffer.buffer[buffer.offset++] =(byte)((destination.net & 0x00FF) >> 0);

                if (destination.net == 0xFFFF)                  //patch by F. Chaxel
                    buffer.buffer[buffer.offset++] = 0;
                else
                {
                    buffer.buffer[buffer.offset++] = (byte)destination.adr.Length;
                    if (destination.adr.Length > 0)
                    {
                        for (int i = 0; i < destination.adr.Length; i++)
                            buffer.buffer[buffer.offset++] = destination.adr[i];
                    }
                }
            }

            if (has_source)
            {
                buffer.buffer[buffer.offset++] =(byte)((source.net & 0xFF00) >> 8);
                buffer.buffer[buffer.offset++] = (byte)((source.net & 0x00FF) >> 0);
                // Modif FC
                if (destination.net == 0xFFFF)
                    buffer.buffer[buffer.offset++] = 0;
                else
                {
                    buffer.buffer[buffer.offset++] = (byte)destination.adr.Length;
                    if (destination.adr.Length > 0)
                    {
                        for (int i = 0; i < destination.adr.Length; i++)
                            buffer.buffer[buffer.offset++] = destination.adr[i];
                    }
                }
            }

            if (has_destination)
            {
                buffer.buffer[buffer.offset++] = hop_count;
            }

            /*
            //display warning
            if (has_destination || has_source)
            {
                System.Diagnostics.Trace.TraceWarning("NPDU size is more than 4. This will give an error in the current max_apdu calculation");
            }
            */

            if ((function & BacnetNpduControls.NetworkLayerMessage) > 0)
            {
                buffer.buffer[buffer.offset++] =(byte)network_msg_type;
                if (((byte)network_msg_type) >= 0x80)
                {
                    buffer.buffer[buffer.offset++] =(byte)((vendor_id & 0xFF00) >> 8);
                    buffer.buffer[buffer.offset++] =(byte)((vendor_id & 0x00FF) >> 0);
                }
            }
        }
    }

    public class APDU
    {
        public static BacnetPduTypes GetDecodedType(byte[] buffer, int offset)
        {
            return (BacnetPduTypes)buffer[offset];
        }

        public static void SetDecodedType(byte[] buffer, int offset, BacnetPduTypes type)
        {
            buffer[offset] = (byte)type;
        }

        public static int GetDecodedInvokeId(byte[] buffer, int offset)
        {
            BacnetPduTypes type = GetDecodedType(buffer, offset);
            switch (type & BacnetPduTypes.PDU_TYPE_MASK)
            {
                case BacnetPduTypes.PDU_TYPE_SIMPLE_ACK:
                case BacnetPduTypes.PDU_TYPE_COMPLEX_ACK:
                case BacnetPduTypes.PDU_TYPE_ERROR:
                case BacnetPduTypes.PDU_TYPE_REJECT:
                case BacnetPduTypes.PDU_TYPE_ABORT:
                    return buffer[offset + 1];
                case BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                    return buffer[offset + 2];
                default:
                    return -1;
            }
        }

        public static void EncodeConfirmedServiceRequest(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte invoke_id, byte sequence_number, byte proposed_window_size)
        {
            buffer.buffer[buffer.offset++] =(byte)type;
            buffer.buffer[buffer.offset++] =(byte)((byte)max_segments | (byte)max_adpu);
            buffer.buffer[buffer.offset++] =invoke_id;

            if((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
            {
                buffer.buffer[buffer.offset++] =sequence_number;
                buffer.buffer[buffer.offset++] =proposed_window_size;
            }
            buffer.buffer[buffer.offset++] =(byte)service;
        }

        public static int DecodeConfirmedServiceRequest(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service, out BacnetMaxSegments max_segments, out BacnetMaxAdpu max_adpu, out byte invoke_id, out byte sequence_number, out byte proposed_window_number)
        {
            int org_offset = offset;

            type = (BacnetPduTypes)buffer[offset++];
            max_segments = (BacnetMaxSegments)(buffer[offset] & 0xF0);
            max_adpu = (BacnetMaxAdpu)(buffer[offset++] & 0x0F);
            invoke_id = buffer[offset++];

            sequence_number = 0;
            proposed_window_number = 0;
            if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
            {
                sequence_number = buffer[offset++];
                proposed_window_number = buffer[offset++];
            }
            service = (BacnetConfirmedServices)buffer[offset++];

            return offset - org_offset;
        }

        public static void EncodeUnconfirmedServiceRequest(EncodeBuffer buffer, BacnetPduTypes type, BacnetUnconfirmedServices service)
        {
            buffer.buffer[buffer.offset++] =(byte)type;
            buffer.buffer[buffer.offset++] =(byte)service;
        }

        public static int DecodeUnconfirmedServiceRequest(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetUnconfirmedServices service)
        {
            int org_offset = offset;

            type = (BacnetPduTypes)buffer[offset++];
            service = (BacnetUnconfirmedServices)buffer[offset++];

            return offset - org_offset;
        }

        public static void EncodeSimpleAck(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id)
        {
            buffer.buffer[buffer.offset++] =(byte)type;
            buffer.buffer[buffer.offset++] =invoke_id;
            buffer.buffer[buffer.offset++] =(byte)service;
        }

        public static int DecodeSimpleAck(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service, out byte invoke_id)
        {
            int org_offset = offset;

            type = (BacnetPduTypes)buffer[offset++];
            invoke_id = buffer[offset++];
            service = (BacnetConfirmedServices)buffer[offset++];

            return offset - org_offset;
        }

        public static int EncodeComplexAck(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte sequence_number, byte proposed_window_number)
        {
            int len = 3;
            buffer.buffer[buffer.offset++] =(byte)type;
            buffer.buffer[buffer.offset++] =invoke_id;
            if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
            {
                buffer.buffer[buffer.offset++] =sequence_number;
                buffer.buffer[buffer.offset++] =proposed_window_number;
                len += 2;
            }
            buffer.buffer[buffer.offset++] =(byte)service;
            return len;
        }

        public static int DecodeComplexAck(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service, out byte invoke_id, out byte sequence_number, out byte proposed_window_number)
        {
            int org_offset = offset;

            type = (BacnetPduTypes)buffer[offset++];
            invoke_id = buffer[offset++];

            sequence_number = 0;
            proposed_window_number = 0;
            if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
            {
                sequence_number = buffer[offset++];
                proposed_window_number = buffer[offset++];
            }
            service = (BacnetConfirmedServices)buffer[offset++];

            return offset - org_offset;
        }

        public static void EncodeSegmentAck(EncodeBuffer buffer, BacnetPduTypes type, byte original_invoke_id, byte sequence_number, byte actual_window_size)
        {
            buffer.buffer[buffer.offset++] =(byte)type;
            buffer.buffer[buffer.offset++] =original_invoke_id;
            buffer.buffer[buffer.offset++] =sequence_number;
            buffer.buffer[buffer.offset++] =actual_window_size;
        }

        public static int DecodeSegmentAck(byte[] buffer, int offset, out BacnetPduTypes type, out byte original_invoke_id, out byte sequence_number, out byte actual_window_size)
        {
            int org_offset = offset;

            type = (BacnetPduTypes)buffer[offset++];
            original_invoke_id = buffer[offset++];
            sequence_number = buffer[offset++];
            actual_window_size = buffer[offset++];

            return offset - org_offset;
        }

        public static void EncodeError(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id)
        {
            buffer.buffer[buffer.offset++] =(byte)type;
            buffer.buffer[buffer.offset++] =invoke_id;
            buffer.buffer[buffer.offset++] =(byte)service;
        }

        public static int DecodeError(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service, out byte invoke_id)
        {
            int org_offset = offset;

            type = (BacnetPduTypes)buffer[offset++];
            invoke_id = buffer[offset++];
            service = (BacnetConfirmedServices)buffer[offset++];

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
        public static void EncodeAbort(EncodeBuffer buffer, BacnetPduTypes type, byte invoke_id, byte reason)
        {
            buffer.buffer[buffer.offset++] =(byte)type;
            buffer.buffer[buffer.offset++] =invoke_id;
            buffer.buffer[buffer.offset++] =reason;
        }

        public static int DecodeAbort(byte[] buffer, int offset, out BacnetPduTypes type, out byte invoke_id, out byte reason)
        {
            int org_offset = offset;

            type = (BacnetPduTypes)buffer[offset++];
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

        public static void encode_bacnet_object_id(EncodeBuffer buffer, BacnetObjectTypes object_type, UInt32 instance)
        {
            UInt32 value = 0;
            UInt32 type = 0;

            type = (UInt32)object_type;
            value = ((type & BACNET_MAX_OBJECT) << BACNET_INSTANCE_BITS) | (instance & BACNET_MAX_INSTANCE);
            encode_unsigned32(buffer, value);
        }

        public static void encode_tag(EncodeBuffer buffer, byte tag_number, bool context_specific, UInt32 len_value_type)
        {
            int len = 1;
            byte[] tmp = new byte[3];

            tmp[0] = 0;
            if (context_specific) tmp[0] |= 0x8;

            /* additional tag byte after this byte */
            /* for extended tag byte */
            if (tag_number <= 14)
            {
                tmp[0] |= (byte)(tag_number << 4);
            }
            else
            {
                tmp[0] |= 0xF0;
                tmp[1] = tag_number;
                len++;
            }

            /* NOTE: additional len byte(s) after extended tag byte */
            /* if larger than 4 */
            if (len_value_type <= 4)
            {
                tmp[0] |= (byte)len_value_type;
                buffer.Add(tmp, len);
            }
            else
            {
                tmp[0] |= 5;
                if (len_value_type <= 253)
                {
                    tmp[len++] = (byte)len_value_type;
                    buffer.Add(tmp, len);
                }
                else if (len_value_type <= 65535)
                {
                    tmp[len++] = 254;
                    buffer.Add(tmp, len);
                    encode_unsigned16(buffer, (UInt16)len_value_type);
                }
                else
                {
                    tmp[len++] = 255;
                    buffer.Add(tmp, len);
                    encode_unsigned32(buffer, len_value_type);
                }
            }
        }

        public static void encode_bacnet_enumerated(EncodeBuffer buffer, UInt32 value)
        {
            encode_bacnet_unsigned(buffer, value);
        }

        public static void encode_application_object_id(EncodeBuffer buffer, BacnetObjectTypes object_type, UInt32 instance)
        {
            EncodeBuffer tmp1 = buffer.Copy();
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, false, 0);  //dummy
            EncodeBuffer tmp2 = buffer.Copy();
            encode_bacnet_object_id(buffer, object_type, instance);
            encode_tag(tmp1, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, false, (uint)tmp2.GetDiff(buffer));
        }

        public static void encode_application_unsigned(EncodeBuffer buffer, UInt32 value)
        {
            EncodeBuffer tmp1 = buffer.Copy();
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, false, 0);  //dummy
            EncodeBuffer tmp2 = buffer.Copy();
            encode_bacnet_unsigned(buffer, value);
            encode_tag(tmp1, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, false, (uint)tmp2.GetDiff(buffer));
        }

        public static void encode_application_enumerated(EncodeBuffer buffer, UInt32 value)
        {
            EncodeBuffer tmp1 = buffer.Copy();
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, false, 0);  //dummy
            EncodeBuffer tmp2 = buffer.Copy();
            encode_bacnet_enumerated(buffer, value);
            encode_tag(tmp1, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, false, (uint)tmp2.GetDiff(buffer));
        }

        public static void encode_application_signed(EncodeBuffer buffer, Int32 value)
        {
            EncodeBuffer tmp1 = buffer.Copy();
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT, false, 0);  //dummy
            EncodeBuffer tmp2 = buffer.Copy();
            encode_bacnet_signed(buffer, value);
            encode_tag(tmp1, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT, false, (uint)tmp2.GetDiff(buffer));
        }

        public static void encode_bacnet_unsigned(EncodeBuffer buffer, UInt32 value)
        {
            if (value < 0x100)
            {
                buffer.Add((byte)value);
            }
            else if (value < 0x10000)
            {
                encode_unsigned16(buffer, (UInt16)value);
            }
            else if (value < 0x1000000)
            {
                encode_unsigned24(buffer, value);
            }
            else
            {
                encode_unsigned32(buffer, value);
            }
        }

        public static void encode_context_boolean(EncodeBuffer buffer, byte tag_number, bool boolean_value)
        {
            encode_tag(buffer, (byte)tag_number, true, 1);
            buffer.Add((boolean_value ? (byte)1 : (byte)0));
        }

        public static void encode_context_real(EncodeBuffer buffer, byte tag_number, float value)
        {
            encode_tag(buffer, tag_number, true, 4);
            encode_bacnet_real(buffer, value);
        }

        public static void encode_context_unsigned(EncodeBuffer buffer, byte tag_number, UInt32 value)
        {
            int len;

            /* length of unsigned is variable, as per 20.2.4 */
            if (value < 0x100)
                len = 1;
            else if (value < 0x10000)
                len = 2;
            else if (value < 0x1000000)
                len = 3;
            else
                len = 4;

            encode_tag(buffer, tag_number, true, (UInt32)len);
            encode_bacnet_unsigned(buffer, value);
        }

        public static void encode_context_character_string(EncodeBuffer buffer, byte tag_number, string value)
        {
            int string_len = 0;

            string_len = value.Length + 1 /* for encoding */ ;
            encode_tag(buffer, tag_number, true, (UInt32)string_len);
            encode_bacnet_character_string(buffer, value);
        }

        public static void encode_context_enumerated(EncodeBuffer buffer, byte tag_number, UInt32 value)
        {
            int len = 0;        /* return value */

            if (value < 0x100)
                len = 1;
            else if (value < 0x10000)
                len = 2;
            else if (value < 0x1000000)
                len = 3;
            else
                len = 4;

            encode_tag(buffer, tag_number, true, (uint)len);
            encode_bacnet_enumerated(buffer, value);
        }

        public static void encode_bacnet_signed(EncodeBuffer buffer, Int32 value)
        {
            /* don't encode the leading X'FF' or X'00' of the two's compliment.
               That is, the first octet of any multi-octet encoded value shall
               not be X'00' if the most significant bit (bit 7) of the second
               octet is 0, and the first octet shall not be X'FF' if the most
               significant bit of the second octet is 1. */
            if ((value >= -128) && (value < 128))
                buffer.Add((byte)(sbyte)value);
            else if ((value >= -32768) && (value < 32768))
                encode_signed16(buffer, (Int16)value);
            else if ((value > -8388608) && (value < 8388608))
                encode_signed24(buffer, value);
            else
                encode_signed32(buffer, value);

        }

        public static void encode_octet_string(EncodeBuffer buffer, byte[] octet_string, int octet_offset, int octet_count)
        {
            if (octet_string != null)
            {
                for (int i = octet_offset; i < (octet_offset+octet_count); i++)
                    buffer.Add(octet_string[i]);
            }
        }

        public static void encode_application_octet_string(EncodeBuffer buffer, byte[] octet_string, int octet_offset, int octet_count)
        {
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING, false, (uint)octet_count);
            encode_octet_string(buffer, octet_string, octet_offset, octet_count);
        }

        public static void encode_application_boolean(EncodeBuffer buffer, bool boolean_value)
        {
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN, false, boolean_value ? (uint)1 : (uint)0);
        }

        public static void encode_bacnet_real(EncodeBuffer buffer, float value)
        {
            byte[] data = BitConverter.GetBytes(value);
            buffer.Add(data[3]);
            buffer.Add(data[2]);
            buffer.Add(data[1]);
            buffer.Add(data[0]);
        }

        public static void encode_bacnet_double(EncodeBuffer buffer, double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            buffer.Add(data[7]);
            buffer.Add(data[6]);
            buffer.Add(data[5]);
            buffer.Add(data[4]);
            buffer.Add(data[3]);
            buffer.Add(data[2]);
            buffer.Add(data[1]);
            buffer.Add(data[0]);
        }

        public static void encode_application_real(EncodeBuffer buffer, float value)
        {
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, false, 4);
            encode_bacnet_real(buffer, value);
        }

        public static void encode_application_double(EncodeBuffer buffer, double value)
        {
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE, false, 8);
            encode_bacnet_double(buffer, value);
        }

        private static byte bitstring_bytes_used(BacnetBitString bit_string)
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

        private static byte bitstring_octet(BacnetBitString bit_string, byte octet_index)
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

        public static void encode_bitstring(EncodeBuffer buffer, BacnetBitString bit_string)
        {
            byte remaining_used_bits = 0;
            byte used_bytes = 0;
            byte i = 0;

            /* if the bit string is empty, then the first octet shall be zero */
            if (bit_string.bits_used == 0)
            {
                buffer.Add(0);
            }
            else
            {
                used_bytes = bitstring_bytes_used(bit_string);
                remaining_used_bits = (byte)(bit_string.bits_used - ((used_bytes - 1) * 8));
                /* number of unused bits in the subsequent final octet */
                buffer.Add((byte)(8 - remaining_used_bits));
                for (i = 0; i < used_bytes; i++)
                    buffer.Add(byte_reverse_bits(bitstring_octet(bit_string, i)));
            }
        }

        public static void encode_application_bitstring(EncodeBuffer buffer, BacnetBitString bit_string)
        {
            uint bit_string_encoded_length = 1;     /* 1 for the bits remaining octet */

            /* bit string may use more than 1 octet for the tag, so find out how many */
            bit_string_encoded_length += bitstring_bytes_used(bit_string);
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING, false, bit_string_encoded_length);
            encode_bitstring(buffer, bit_string);
        }

        public static void bacapp_encode_application_data(EncodeBuffer buffer, BacnetValue value)
        {
            if (value.Value == null)
            {
                // Modif FC
                buffer.Add((byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL);  
                return;
            }

            switch (value.Tag)
            {
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                    /* don't encode anything */
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                    encode_application_boolean(buffer, (bool)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    encode_application_unsigned(buffer, (uint)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                    encode_application_signed(buffer, (int)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                    encode_application_real(buffer, (float)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                    encode_application_double(buffer, (double)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                    encode_application_octet_string(buffer, (byte[])value.Value, 0, ((byte[])value.Value).Length);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    encode_application_character_string(buffer, (string)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                    encode_application_bitstring(buffer, (BacnetBitString)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                    encode_application_enumerated(buffer, (uint)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                    encode_application_date(buffer, (DateTime)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                    encode_application_time(buffer, (DateTime)value.Value);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                    encode_application_object_id(buffer, ((BacnetObjectId)value.Value).type, ((BacnetObjectId)value.Value).instance);
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_COV_SUBSCRIPTION:
                    encode_cov_subscription(buffer, ((BacnetCOVSubscription)value.Value));       //is this the right way to do it, I wonder?
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_RESULT:
                    encode_read_access_result(buffer, ((BacnetReadAccessResult)value.Value));       //is this the right way to do it, I wonder?
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION:
                    encode_read_access_specification(buffer, ((BacnetReadAccessSpecification)value.Value));     //is this the right way to do it, I wonder?
                    break;
                default:
                    //context specific
                    if (value.Value is byte[])
                    {
                        byte[] arr = (byte[])value.Value;
                        if (buffer != null) buffer.Add(arr, arr.Length);
                    }
                    else
                        throw new Exception("I cannot encode this");
                    break;
            }
        }

        public static void bacapp_encode_device_obj_property_ref(EncodeBuffer buffer, BacnetDeviceObjectPropertyReference value)
        {
            encode_context_object_id(buffer, 0, value.objectIdentifier.type, value.objectIdentifier.instance);
            encode_context_enumerated(buffer, 1, value.propertyIdentifier);

            /* Array index is optional so check if needed before inserting */
            if (value.arrayIndex != ASN1.BACNET_ARRAY_ALL)
                encode_context_unsigned(buffer, 2, value.arrayIndex);

            /* Likewise, device id is optional so see if needed
             * (set type to non device to omit */
            if (value.deviceIndentifier.type == BacnetObjectTypes.OBJECT_DEVICE)
                encode_context_object_id(buffer, 3, value.deviceIndentifier.type, value.deviceIndentifier.instance);
        }

        public static void bacapp_encode_context_device_obj_property_ref(EncodeBuffer buffer, byte tag_number, BacnetDeviceObjectPropertyReference value)
        {
            encode_opening_tag(buffer, tag_number);
            bacapp_encode_device_obj_property_ref(buffer, value);
            encode_closing_tag(buffer, tag_number);
        }

        public static void bacapp_encode_property_state(EncodeBuffer buffer, BacnetPropetyState value)
        {
            switch (value.tag)
            {
                case BacnetPropetyState.BacnetPropertyStateTypes.BOOLEAN_VALUE:
                    encode_context_boolean(buffer, 0, value.state == 1 ? true : false);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.BINARY_VALUE:
                    encode_context_enumerated(buffer, 1, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.EVENT_TYPE:
                    encode_context_enumerated(buffer, 2, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.POLARITY:
                    encode_context_enumerated(buffer, 3, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.PROGRAM_CHANGE:
                    encode_context_enumerated(buffer, 4, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.PROGRAM_STATE:
                    encode_context_enumerated(buffer, 5, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.REASON_FOR_HALT:
                    encode_context_enumerated(buffer, 6, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.RELIABILITY:
                    encode_context_enumerated(buffer, 7, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.STATE:
                    encode_context_enumerated(buffer, 8, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.SYSTEM_STATUS:
                    encode_context_enumerated(buffer, 9, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.UNITS:
                    encode_context_enumerated(buffer, 10, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.UNSIGNED_VALUE:
                    encode_context_unsigned(buffer, 11, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.LIFE_SAFETY_MODE:
                    encode_context_enumerated(buffer, 12, value.state);
                    break;
                case BacnetPropetyState.BacnetPropertyStateTypes.LIFE_SAFETY_STATE:
                    encode_context_enumerated(buffer, 13, value.state);
                    break;
                default:
                    /* FIXME: assert(0); - return a negative len? */
                    break;
            }
        }

        public static void encode_context_bitstring(EncodeBuffer buffer, byte tag_number, BacnetBitString bit_string)
        {
            uint bit_string_encoded_length = 1;     /* 1 for the bits remaining octet */

            /* bit string may use more than 1 octet for the tag, so find out how many */
            bit_string_encoded_length += bitstring_bytes_used(bit_string);
            encode_tag(buffer, tag_number, true, bit_string_encoded_length);
            encode_bitstring(buffer, bit_string);
        }

        public static void encode_opening_tag(EncodeBuffer buffer, byte tag_number)
        {
            int len = 1;
            byte[] tmp = new byte[2];

            /* set class field to context specific */
            tmp[0] = 0x8;
            /* additional tag byte after this byte for extended tag byte */
            if (tag_number <= 14)
            {
                tmp[0] |= (byte)(tag_number << 4);
            }
            else
            {
                tmp[0] |= 0xF0;
                tmp[1] = tag_number;
                len++;
            }
            /* set type field to opening tag */
            tmp[0] |= 6;

            buffer.Add(tmp, len);
        }

        public static void encode_context_signed(EncodeBuffer buffer, byte tag_number, Int32 value)
        {
            int len = 0;        /* return value */

            /* length of signed int is variable, as per 20.2.11 */
            if ((value >= -128) && (value < 128))
                len = 1;
            else if ((value >= -32768) && (value < 32768))
                len = 2;
            else if ((value > -8388608) && (value < 8388608))
                len = 3;
            else
                len = 4;

            encode_tag(buffer, tag_number, true, (uint)len);
            encode_bacnet_signed(buffer, value);
        }

        public static void encode_context_object_id(EncodeBuffer buffer, byte tag_number, BacnetObjectTypes object_type, uint instance)
        {
            encode_tag(buffer, tag_number, true, 4);
            encode_bacnet_object_id(buffer, object_type, instance);
        }

        public static void encode_closing_tag(EncodeBuffer buffer,byte tag_number)
        {
            int len = 1;
            byte[] tmp = new byte[2];

            /* set class field to context specific */
            tmp[0] = 0x8;
            /* additional tag byte after this byte for extended tag byte */
            if (tag_number <= 14)
            {
                tmp[0] |= (byte)(tag_number << 4);
            }
            else
            {
                tmp[0] |= 0xF0;
                tmp[1] = tag_number;
                len++;
            }
            /* set type field to closing tag */
            tmp[0] |= 7;

            buffer.Add(tmp, len);
        }

        public static void encode_bacnet_time(EncodeBuffer buffer, DateTime value)
        {
            buffer.Add((byte)value.Hour);
            buffer.Add((byte)value.Minute);
            buffer.Add((byte)value.Second);
            buffer.Add((byte)(value.Millisecond / 10));
        }

        public static void encode_context_time(EncodeBuffer buffer, byte tag_number, DateTime value)
        {
            encode_tag(buffer, tag_number, true, 4);
            encode_bacnet_time(buffer, value);
        }

        public static void encode_bacnet_date(EncodeBuffer buffer, DateTime value)
        {
            /* allow 2 digit years */
            if (value.Year >= 1900)
                buffer.Add((byte)(value.Year - 1900));
            else if (value.Year < 0x100)
                buffer.Add((byte)value.Year);
            else
                throw new Exception("Date is rubbish");

            buffer.Add((byte)value.Month);
            buffer.Add((byte)value.Day);
            buffer.Add((byte)value.DayOfWeek);
        }

        public static void encode_application_date(EncodeBuffer buffer, DateTime value)
        {
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, false, 4);
            encode_bacnet_date(buffer, value);
        }

        public static void encode_application_time(EncodeBuffer buffer, DateTime value)
        {
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, false, 4);
            encode_bacnet_time(buffer, value);
        }

        public static void bacapp_encode_datetime(EncodeBuffer buffer, DateTime value)
        {
            if (value != new DateTime(1, 1, 1))
            {
                encode_application_date(buffer, value);
                encode_application_time(buffer, value);
            }
        }

        public static void bacapp_encode_context_datetime(EncodeBuffer buffer, byte tag_number, DateTime value)
        {
            if (value != new DateTime(1,1,1))
            {
                encode_opening_tag(buffer, tag_number);
                bacapp_encode_datetime(buffer, value);
                encode_closing_tag(buffer, tag_number);
            }
        }

        public static void bacapp_encode_timestamp(EncodeBuffer buffer, BacnetGenericTime value)
        {
            switch (value.Tag)
            {
                case BacnetTimestampTags.TIME_STAMP_TIME:
                    encode_context_time(buffer, 0, value.Time);
                    break;
                case BacnetTimestampTags.TIME_STAMP_SEQUENCE:
                    encode_context_unsigned(buffer, 1, value.Sequence);
                    break;
                case BacnetTimestampTags.TIME_STAMP_DATETIME:
                    bacapp_encode_context_datetime(buffer, 2, value.Time);
                    break;
                case BacnetTimestampTags.TIME_STAMP_NONE:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void bacapp_encode_context_timestamp(EncodeBuffer buffer, byte tag_number, BacnetGenericTime value)
        {
            if (value.Tag != BacnetTimestampTags.TIME_STAMP_NONE)
            {
                encode_opening_tag(buffer, tag_number);
                bacapp_encode_timestamp(buffer, value);
                encode_closing_tag(buffer, tag_number);
            }
        }

        public static void encode_application_character_string(EncodeBuffer buffer, string value)
        {
            int string_len = 0;

            string_len = value.Length + 1 /* for encoding */ ;
            encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING, false, (UInt32)string_len);
            encode_bacnet_character_string(buffer, value);
        }

        public static void encode_bacnet_character_string_safe(EncodeBuffer buffer, BacnetCharacterStringEncodings encoding, string value, int length)
        {
            buffer.Add((byte)encoding);
            for (int i = 0; i < length; i++)
                buffer.Add((byte)value[i]);
        }

        public static void encode_bacnet_character_string(EncodeBuffer buffer, string value)
        {
            encode_bacnet_character_string_safe(buffer, BacnetCharacterStringEncodings.CHARACTER_ANSI_X34, value, value.Length);
        }

        public static void encode_unsigned16(EncodeBuffer buffer, UInt16 value)
        {
            buffer.Add((byte)((value & 0xff00) >> 8));
            buffer.Add((byte)((value & 0x00ff) >> 0));
        }

        public static void encode_unsigned24(EncodeBuffer buffer, UInt32 value)
        {
            buffer.Add((byte)((value & 0xff0000) >> 16));
            buffer.Add((byte)((value & 0x00ff00) >> 8));
            buffer.Add((byte)((value & 0x0000ff) >> 0));
        }

        public static void encode_unsigned32(EncodeBuffer buffer, UInt32 value)
        {
            buffer.Add((byte)((value & 0xff000000) >> 24));
            buffer.Add((byte)((value & 0x00ff0000) >> 16));
            buffer.Add((byte)((value & 0x0000ff00) >> 8));
            buffer.Add((byte)((value & 0x000000ff) >> 0));
        }

        public static void encode_signed16(EncodeBuffer buffer, Int16 value)
        {
            buffer.Add((byte)((value & 0xff00) >> 8));
            buffer.Add((byte)((value & 0x00ff) >> 0));
        }

        public static void encode_signed24(EncodeBuffer buffer, Int32 value)
        {
            buffer.Add((byte)((value & 0xff0000) >> 16));
            buffer.Add((byte)((value & 0x00ff00) >> 8));
            buffer.Add((byte)((value & 0x0000ff) >> 0));
        }

        public static void encode_signed32(EncodeBuffer buffer, Int32 value)
        {
            buffer.Add((byte)((value & 0xff000000) >> 24));
            buffer.Add((byte)((value & 0x00ff0000) >> 16));
            buffer.Add((byte)((value & 0x0000ff00) >> 8));
            buffer.Add((byte)((value & 0x000000ff) >> 0));
        }

        public static void encode_read_access_specification(EncodeBuffer buffer, BacnetReadAccessSpecification value)
        {
            /* Tag 0: BACnetObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 0, value.objectIdentifier.type, value.objectIdentifier.instance);

            /* Tag 1: sequence of BACnetPropertyReference */
            ASN1.encode_opening_tag(buffer, 1);
            foreach (BacnetPropertyReference p in value.propertyReferences)
            {
                ASN1.encode_context_enumerated(buffer, 0, p.propertyIdentifier);

                /* optional array index */
                if (p.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                    ASN1.encode_context_unsigned(buffer, 1, p.propertyArrayIndex);
            }
            ASN1.encode_closing_tag(buffer, 1);
        }

        public static void encode_read_access_result(EncodeBuffer buffer, BacnetReadAccessResult value)
        {
            /* Tag 0: BACnetObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 0, value.objectIdentifier.type, value.objectIdentifier.instance);

            /* Tag 1: listOfResults */
            ASN1.encode_opening_tag(buffer, 1);
            foreach (BacnetPropertyValue p_value in value.values)
            {
                /* Tag 2: propertyIdentifier */
                ASN1.encode_context_enumerated(buffer, 2, p_value.property.propertyIdentifier);
                /* Tag 3: optional propertyArrayIndex */
                if (p_value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                    ASN1.encode_context_unsigned(buffer, 3, p_value.property.propertyArrayIndex);

                if (p_value.value != null && p_value.value is IList<BacnetError>)
                {
                    /* Tag 5: Error */
                    ASN1.encode_opening_tag(buffer, 5);
                    ASN1.encode_application_enumerated(buffer, (uint)((IList<BacnetError>)p_value.value)[0].error_class);
                    ASN1.encode_application_enumerated(buffer, (uint)((IList<BacnetError>)p_value.value)[0].error_code);
                    ASN1.encode_closing_tag(buffer, 5);
                }
                else
                {
                    /* Tag 4: Value */
                    ASN1.encode_opening_tag(buffer, 4);
                    foreach (BacnetValue v in p_value.value)
                    {
                        ASN1.bacapp_encode_application_data(buffer, v);
                    }
                    ASN1.encode_closing_tag(buffer, 4);
                }
            }
            ASN1.encode_closing_tag(buffer, 1);
        }

        public static int decode_read_access_result(byte[] buffer, int offset, int apdu_len, out BacnetReadAccessResult value)
        {
            int len = 0;
            byte tag_number;
            uint len_value_type;
            int tag_len;

            value = new BacnetReadAccessResult();

            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len = 1;
            len += ASN1.decode_object_id(buffer, offset + len, out value.objectIdentifier.type, out value.objectIdentifier.instance);

            /* Tag 1: listOfResults */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            List<BacnetPropertyValue> _value_list = new List<BacnetPropertyValue>();
            while ((apdu_len - len) > 0)
            {
                BacnetPropertyValue new_entry = new BacnetPropertyValue();

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
                    BacnetValue v;
                    List<BacnetValue> local_value_list = new List<BacnetValue>();
                    while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 4))
                    {
                        tag_len = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset - 1, value.objectIdentifier.type, (BacnetPropertyIds)new_entry.property.propertyIdentifier, out v);
                        if (tag_len < 0) return -1;
                        len += tag_len;
                        local_value_list.Add(v);
                    }
                    new_entry.value = local_value_list;
                    len++;
                }
                else if (tag_number == 5)
                {
                    /* Tag 5: Error */
                    BacnetError err = new BacnetError();
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out len_value_type);      //error_class
                    err.error_class = (BacnetErrorClasses)len_value_type;
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out len_value_type);       //error_code
                    err.error_code = (BacnetErrorCodes)len_value_type;
                    if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 5))
                        return -1;
                    len++;

                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ERROR, err) };
                }

                _value_list.Add(new_entry);
            }
            value.values = _value_list;

            return len;
        }

        public static int decode_read_access_specification(byte[] buffer, int offset, int apdu_len, out BacnetReadAccessSpecification value)
        {
            int len = 0;
            byte tag_number;
            uint len_value_type;
            int tmp;

            value = new BacnetReadAccessSpecification();

            /* Tag 0: Object ID */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out value.objectIdentifier.type, out value.objectIdentifier.instance);

            /* Tag 1: sequence of ReadAccessSpecification */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
                return -1;
            len++;  /* opening tag is only one octet */

            /* properties */
            List<BacnetPropertyReference> __property_id_and_array_index = new List<BacnetPropertyReference>();
            while ((apdu_len - len) > 1 && !ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
            {
                BacnetPropertyReference p_ref = new BacnetPropertyReference();

                /* Tag 0: propertyIdentifier */
                if (!ASN1.IS_CONTEXT_SPECIFIC(buffer[offset + len]))
                    return -1;

                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                if (tag_number != 0)
                    return -1;

                /* Should be at least the unsigned value + 1 tag left */
                if ((len + len_value_type) >= apdu_len)
                    return -1;
                len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out p_ref.propertyIdentifier);
                /* Assume most probable outcome */
                p_ref.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;
                /* Tag 1: Optional propertyArrayIndex */
                if (ASN1.IS_CONTEXT_SPECIFIC(buffer[offset + len]) && !ASN1.IS_CLOSING_TAG(buffer[offset + len]))
                {
                    tmp = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    if (tag_number == 1)
                    {
                        len += tmp;
                        /* Should be at least the unsigned array index + 1 tag left */
                        if ((len + len_value_type) >= apdu_len)
                            return -1;
                        len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out p_ref.propertyArrayIndex);
                    }
                }
                __property_id_and_array_index.Add(p_ref);
            }

            /* closing tag */
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            value.propertyReferences = __property_id_and_array_index;
            return len;
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

        private static bool octetstring_copy(byte[] buffer, int offset, int max_offset, byte[] octet_string, int octet_string_offset, uint octet_string_length)
        {
            bool status = false;        /* return value */

            if (octet_string_length <= (max_offset + offset))
            {
                if (octet_string != null) Array.Copy(buffer, offset, octet_string, octet_string_offset, Math.Min(octet_string.Length, buffer.Length - offset));
                status = true;
            }

            return status;
        }

        public static int decode_octet_string(byte[] buffer, int offset, int max_length, byte[] octet_string, int octet_string_offset, uint octet_string_length)
        {
            int len = 0;        /* return value */

            octetstring_copy(buffer, offset, max_length, octet_string, octet_string_offset, octet_string_length);
            len = (int)octet_string_length;

            return len;
        }

        public static int decode_context_octet_string(byte[] buffer, int offset, int max_length, byte tag_number, byte[] octet_string, int octet_string_offset)
        {
            int len = 0;        /* return value */
            uint len_value = 0;

            octet_string = null;
            if (decode_is_context_tag(buffer, offset, tag_number))
            {
                len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);

                if (octetstring_copy(buffer, offset + len, max_length, octet_string, octet_string_offset, len_value))
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

        private static bool bitstring_set_octet(ref BacnetBitString bit_string, byte index, byte octet)
        {
            bool status = false;

            if (index < MAX_BITSTRING_BYTES)
            {
                bit_string.value[index] = octet;
                status = true;
            }

            return status;
        }

        private static bool bitstring_set_bits_used(ref BacnetBitString bit_string, byte bytes_used, byte unused_bits)
        {
            bool status = false;

            /* FIXME: check that bytes_used is at least one? */
            bit_string.bits_used = (byte)(bytes_used * 8);
            bit_string.bits_used -= unused_bits;
            status = true;

            return status;
        }

        public static int decode_bitstring(byte[] buffer, int offset, uint len_value, out BacnetBitString bit_string)
        {
            int len = 0;
            byte unused_bits = 0;
            uint i = 0;
            uint bytes_used = 0;

            bit_string = new BacnetBitString();
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
            int month = buffer[offset + 1];
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
            int hour = buffer[offset + 0];
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

            if (tag_number == (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME)
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

            if (tag_number == (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE)
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

        public static int bacapp_decode_data(byte[] buffer, int offset, int max_length, BacnetApplicationTags tag_data_type, uint len_value_type, out BacnetValue value)
        {
            int len = 0;
            uint uint_value;
            int int_value;

            value = new BacnetValue();
            value.Tag = tag_data_type;

            switch (tag_data_type)
            {
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                    /* nothing else to do */
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                    value.Value = len_value_type > 0 ? true : false;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    len = decode_unsigned(buffer, offset, len_value_type, out uint_value);
                    value.Value = uint_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                    len = decode_signed(buffer, offset, len_value_type, out int_value);
                    value.Value = int_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                    float float_value;
                    len = decode_real_safe(buffer, offset, len_value_type, out float_value);
                    value.Value = float_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                    double double_value;
                    len = decode_double_safe(buffer, offset, len_value_type, out double_value);
                    value.Value = double_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                    byte[] octet_string = new byte[len_value_type];
                    len = decode_octet_string(buffer, offset, max_length, octet_string, 0, len_value_type);
                    value.Value = octet_string;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    string string_value;
                    len = decode_character_string(buffer, offset, max_length, len_value_type, out string_value);
                    value.Value = string_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                    BacnetBitString bit_value;
                    len = decode_bitstring(buffer, offset, len_value_type, out bit_value);
                    value.Value = bit_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                    len = decode_enumerated(buffer, offset, len_value_type, out uint_value);
                    value.Value = uint_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                    DateTime date_value;
                    len = decode_date_safe(buffer, offset, len_value_type, out date_value);
                    value.Value = date_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                    DateTime time_value;
                    len = decode_bacnet_time_safe(buffer, offset, len_value_type, out time_value);
                    value.Value = time_value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                    {
                        ushort object_type = 0;
                        uint instance = 0;
                        len = decode_object_id_safe(buffer, offset, len_value_type, out object_type, out instance);
                        value.Value = new BacnetObjectId((BacnetObjectTypes)object_type, instance);
                    }
                    break;
                default:
                    break;
            }

            return len;
        }

        /* returns the fixed tag type for certain context tagged properties */
        private static BacnetApplicationTags bacapp_context_tag_type(BacnetPropertyIds property, byte tag_number)
        {
            BacnetApplicationTags tag = BacnetApplicationTags.MAX_BACNET_APPLICATION_TAG;

            switch (property)
            {
                case BacnetPropertyIds.PROP_ACTUAL_SHED_LEVEL:
                case BacnetPropertyIds.PROP_REQUESTED_SHED_LEVEL:
                case BacnetPropertyIds.PROP_EXPECTED_SHED_LEVEL:
                    switch (tag_number)
                    {
                        case 0:
                        case 1:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 2:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;
                            break;
                        default:
                            break;
                    }
                    break;
                case BacnetPropertyIds.PROP_ACTION:
                    switch (tag_number)
                    {
                        case 0:
                        case 1:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        case 2:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;
                            break;
                        case 3:
                        case 5:
                        case 6:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 7:
                        case 8:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
                            break;
                        case 4:        /* propertyValue: abstract syntax */
                        default:
                            break;
                    }
                    break;
                case BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS:
                    /* Sequence of ReadAccessSpecification */
                    switch (tag_number)
                    {
                        case 0:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        default:
                            break;
                    }
                    break;
                case BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE:
                    switch (tag_number)
                    {
                        case 1:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        case 3:
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 0:        /* calendarEntry: abstract syntax + context */
                        case 2:        /* list of BACnetTimeValue: abstract syntax */
                        default:
                            break;
                    }
                    break;
                case BacnetPropertyIds.PROP_LOG_DEVICE_OBJECT_PROPERTY:
                    switch (tag_number)
                    {
                        case 0:        /* Object ID */
                        case 3:        /* Device ID */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        case 1:        /* Property ID */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;
                            break;
                        case 2:        /* Array index */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        default:
                            break;
                    }
                    break;
                case BacnetPropertyIds.PROP_SUBORDINATE_LIST:
                    /* BACnetARRAY[N] of BACnetDeviceObjectReference */
                    switch (tag_number)
                    {
                        case 0:        /* Optional Device ID */
                        case 1:        /* Object ID */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        default:
                            break;
                    }
                    break;

                case BacnetPropertyIds.PROP_RECIPIENT_LIST:
                    /* List of BACnetDestination */
                    switch (tag_number)
                    {
                        case 0:        /* Device Object ID */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                            break;
                        default:
                            break;
                    }
                    break;
                case BacnetPropertyIds.PROP_ACTIVE_COV_SUBSCRIPTIONS:
                    /* BACnetCOVSubscription */
                    switch (tag_number)
                    {
                        case 0:        /* BACnetRecipientProcess */
                        case 1:        /* BACnetObjectPropertyReference */
                            break;
                        case 2:        /* issueConfirmedNotifications */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
                            break;
                        case 3:        /* timeRemaining */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
                            break;
                        case 4:        /* covIncrement */
                            tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;
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

        public static int bacapp_decode_context_data(byte[] buffer, int offset, uint max_apdu_len, BacnetApplicationTags property_tag, out BacnetValue value)
        {
            int apdu_len = 0, len = 0;
            int tag_len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            value = new BacnetValue();

            if (IS_CONTEXT_SPECIFIC(buffer[offset]))
            {
                //value->context_specific = true;
                tag_len = decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                apdu_len = tag_len;
                /* Empty construct : (closing tag) => returns NULL value */
                if (tag_len > 0 && (tag_len <= max_apdu_len) && !decode_is_closing_tag_number(buffer, offset + len, tag_number))
                {
                    //value->context_tag = tag_number;
                    if (property_tag < BacnetApplicationTags.MAX_BACNET_APPLICATION_TAG)
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

        public static int bacapp_decode_application_data(byte[] buffer, int offset, int max_offset, BacnetObjectTypes object_type, BacnetPropertyIds property_id, out BacnetValue value)
        {
            int len = 0;
            int tag_len = 0;
            int decode_len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            value = new BacnetValue();

            /* FIXME: use max_apdu_len! */
            if (!IS_CONTEXT_SPECIFIC(buffer[offset]))
            {
                tag_len = decode_tag_number_and_value(buffer, offset, out tag_number, out len_value_type);
                if (tag_len > 0)
                {
                    len += tag_len;
                    decode_len = bacapp_decode_data(buffer, offset + len, max_offset, (BacnetApplicationTags)tag_number, len_value_type, out value);
                    if (decode_len < 0) return decode_len;
                    len += decode_len;
                }
            }
            else
            {
                return bacapp_decode_context_application_data(buffer, offset, max_offset, object_type, property_id, out value);
            }

            return len;
        }

        public static int bacapp_decode_context_application_data(byte[] buffer, int offset, int max_offset, BacnetObjectTypes object_type, BacnetPropertyIds property_id, out BacnetValue value)
        {
            int len = 0;
            int tag_len = 0;
            byte tag_number = 0;
            byte sub_tag_number = 0;
            uint len_value_type = 0;

            value = new BacnetValue();

            if (IS_CONTEXT_SPECIFIC(buffer[offset]))
            {
                //this seems to be a strange way to determine object encodings
                if (property_id == BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS)
                {
                    BacnetReadAccessSpecification v;
                    tag_len = ASN1.decode_read_access_specification(buffer, offset, max_offset, out v);
                    if (tag_len < 0) return -1;
                    value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION;
                    value.Value = v;
                    return tag_len;
                }
                else if (property_id == BacnetPropertyIds.PROP_ACTIVE_COV_SUBSCRIPTIONS)
                {
                    BacnetCOVSubscription v;
                    tag_len = ASN1.decode_cov_subscription(buffer, offset, max_offset, out v);
                    if (tag_len < 0) return -1;
                    value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_COV_SUBSCRIPTION;
                    value.Value = v;
                    return tag_len;
                }
                else if (object_type == BacnetObjectTypes.OBJECT_GROUP && property_id == BacnetPropertyIds.PROP_PRESENT_VALUE)
                {
                    BacnetReadAccessResult v;
                    tag_len = ASN1.decode_read_access_result(buffer, offset, max_offset, out v);
                    if (tag_len < 0) return -1;
                    value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_RESULT;
                    value.Value = v;
                    return tag_len;
                }

                value.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED;
                List<BacnetValue> list = new List<BacnetValue>();

                decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                while (((len + offset) <= max_offset) && !IS_CLOSING_TAG(buffer[offset + len]))
                {
                    tag_len = decode_tag_number_and_value(buffer, offset + len, out sub_tag_number, out len_value_type);
                    if (tag_len < 0) return -1;

                    if (len_value_type == 0)
                    {
                        BacnetValue sub_value;
                        len += tag_len;
                        tag_len = bacapp_decode_application_data(buffer, offset + len, max_offset, BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE, BacnetPropertyIds.MAX_BACNET_PROPERTY_ID, out sub_value);
                        if (tag_len < 0) return -1;
                        list.Add(sub_value);
                        len += tag_len;
                    }
                    else
                    {
                        BacnetValue sub_value = new BacnetValue();

                        //override tag_number
                        BacnetApplicationTags override_tag_number = bacapp_context_tag_type(property_id, sub_tag_number);
                        if (override_tag_number != BacnetApplicationTags.MAX_BACNET_APPLICATION_TAG) sub_tag_number = (byte)override_tag_number;

                        //try app decode
                        int sub_tag_len = bacapp_decode_data(buffer, offset + len + tag_len, max_offset, (BacnetApplicationTags)sub_tag_number, len_value_type, out sub_value);
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
                            sub_value = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_ENCODED, context_specific);

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

        public static int decode_object_id(byte[] buffer, int offset, out BacnetObjectTypes object_type, out uint instance)
        {
            uint value = 0;
            int len = 0;

            len = decode_unsigned32(buffer, offset, out value);
            object_type = (BacnetObjectTypes)(((value >> BACNET_INSTANCE_BITS) & BACNET_MAX_OBJECT));
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

        /// <summary>
        /// This is used by the Active_COV_Subscriptions property in DEVICE
        /// </summary>
        public static void encode_cov_subscription(EncodeBuffer buffer, BacnetCOVSubscription value)
        {
            /* Recipient [0] BACnetRecipientProcess - opening */
            ASN1.encode_opening_tag(buffer, 0);

            /*  recipient [0] BACnetRecipient - opening */
            ASN1.encode_opening_tag(buffer, 0);
            /* CHOICE - device [0] BACnetObjectIdentifier - opening */
            /* CHOICE - address [1] BACnetAddress - opening */
            ASN1.encode_opening_tag(buffer, 1);
            /* network-number Unsigned16, */
            /* -- A value of 0 indicates the local network */
            ASN1.encode_application_unsigned(buffer, value.Recipient.net);
            /* mac-address OCTET STRING */
            /* -- A string of length 0 indicates a broadcast */
            if (value.Recipient.net == 0xFFFF)
                ASN1.encode_application_octet_string(buffer, new byte[0], 0, 0);
            else
                ASN1.encode_application_octet_string(buffer, value.Recipient.adr, 0, value.Recipient.adr.Length);
            /* CHOICE - address [1] BACnetAddress - closing */
            ASN1.encode_closing_tag(buffer, 1);
            /*  recipient [0] BACnetRecipient - closing */
            ASN1.encode_closing_tag(buffer, 0);

            /* processIdentifier [1] Unsigned32 */
            ASN1.encode_context_unsigned(buffer, 1, value.subscriptionProcessIdentifier);
            /* Recipient [0] BACnetRecipientProcess - closing */
            ASN1.encode_closing_tag(buffer, 0);

            /*  MonitoredPropertyReference [1] BACnetObjectPropertyReference, */
            ASN1.encode_opening_tag(buffer, 1);
            /* objectIdentifier [0] */
            ASN1.encode_context_object_id(buffer, 0, value.monitoredObjectIdentifier.type, value.monitoredObjectIdentifier.instance);
            /* propertyIdentifier [1] */
            /* FIXME: we are monitoring 2 properties! How to encode? */
            ASN1.encode_context_enumerated(buffer, 1, value.monitoredProperty.propertyIdentifier);
            if (value.monitoredProperty.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                ASN1.encode_context_unsigned(buffer, 2, value.monitoredProperty.propertyArrayIndex);
            /* MonitoredPropertyReference [1] - closing */
            ASN1.encode_closing_tag(buffer, 1);

            /* IssueConfirmedNotifications [2] BOOLEAN, */
            ASN1.encode_context_boolean(buffer, 2, value.IssueConfirmedNotifications);
            /* TimeRemaining [3] Unsigned, */
            ASN1.encode_context_unsigned(buffer, 3, value.TimeRemaining);
            /* COVIncrement [4] REAL OPTIONAL, */
            if (value.COVIncrement > 0)
                ASN1.encode_context_real(buffer, 4, value.COVIncrement);
        }

        public static int decode_cov_subscription(byte[] buffer, int offset, int apdu_len, out BacnetCOVSubscription value)
        {
            int len = 0;
            int tag_len;
            byte tag_number;
            uint len_value_type;
            uint tmp;

            value = new BacnetCOVSubscription();
            value.Recipient = new BacnetAddress(BacnetAddressTypes.None, 0, null);

            if (!decode_is_opening_tag_number(buffer, offset + len, 0))
                return -1;
            len++;
            if (!decode_is_opening_tag_number(buffer, offset + len, 0))
                return -1;
            len++;
            if (!decode_is_opening_tag_number(buffer, offset + len, 1))
                return -1;
            len++;
            len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len += decode_unsigned(buffer, offset + len, len_value_type, out tmp);
            value.Recipient.net = (ushort)tmp;
            len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                return -1;
            value.Recipient.adr = new byte[len_value_type];
            len += decode_octet_string(buffer, offset + len, apdu_len, value.Recipient.adr, 0, len_value_type);
            if (!decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            len++;
            if (!decode_is_closing_tag_number(buffer, offset + len, 0))
                return -1;
            len++;

            len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 1)
                return -1;
            len += decode_unsigned(buffer, offset + len, len_value_type, out value.subscriptionProcessIdentifier);
            if (!decode_is_closing_tag_number(buffer, offset + len, 0))
                return -1;
            len++;

            if (!decode_is_opening_tag_number(buffer, offset + len, 1))
                return -1;
            len++;
            len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 0)
                return -1;
            len += decode_object_id(buffer, offset + len, out value.monitoredObjectIdentifier.type, out value.monitoredObjectIdentifier.instance);
            len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 1)
                return -1;
            len += decode_enumerated(buffer, offset + len, len_value_type, out value.monitoredProperty.propertyIdentifier);
            tag_len = decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number == 2)
            {
                len += tag_len;
                len += decode_unsigned(buffer, offset + len, len_value_type, out value.monitoredProperty.propertyArrayIndex);
            }
            else
                value.monitoredProperty.propertyArrayIndex = BACNET_ARRAY_ALL;
            if (!decode_is_closing_tag_number(buffer, offset + len, 1))
                return -1;
            len++;

            len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 2)
                return -1;
            value.IssueConfirmedNotifications = buffer[offset + len] > 0 ? true : false;
            len++;

            len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != 3)
                return -1;
            len += decode_unsigned(buffer, offset + len, len_value_type, out value.TimeRemaining);

            if (len < apdu_len && !IS_CLOSING_TAG(buffer[offset+len]))
            {
                len += decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                if (tag_number != 4)
                    return -1;
                len += decode_real(buffer, offset + len, out value.COVIncrement);
            }

            return len;
        }
    }

    public class EncodeBuffer
    {
        public byte[] buffer;           //buffer to serialize into
        public int offset;              //offset in buffer ... will go beyond max_offset (so that you may count what's needed)
        public int max_offset;          //don't write beyond this offset 
        public int serialize_counter;   //used with 'min_limit'
        public int min_limit;           //don't write before this limit (used for segmentation)
        public EncodeResult result;
        public bool expandable;
        public EncodeBuffer()
        {
            expandable = true;
            buffer = new byte[128];
            max_offset = buffer.Length - 1;
        }
        public EncodeBuffer(byte[] buffer, int offset)
        {
            if (buffer == null) buffer = new byte[0];
            this.expandable = false;
            this.buffer = buffer;
            this.offset = offset;
            this.max_offset = buffer.Length;
        }
        public void Increment()
        {
            if (offset < max_offset)
            {
                if (serialize_counter >= min_limit)
                    offset++;
                serialize_counter++;
            }
            else
            {
                if (serialize_counter >= min_limit)
                    offset++;
            }
        }
        public void Add(byte b)
        {
            if (offset < max_offset)
            {
                if (serialize_counter >= min_limit)
                    buffer[offset] = b;
            }
            else
            {
                if (expandable)
                {
                    Array.Resize<byte>(ref buffer, buffer.Length * 2);
                    max_offset = buffer.Length - 1;
                    if (serialize_counter >= min_limit)
                        buffer[offset] = b;
                }
                else
                    result |= EncodeResult.NotEnoughBuffer;
            }
            Increment();
        }
        public void Add(byte[] buffer, int count)
        {
            for (int i = 0; i < count; i++)
                Add(buffer[i]);
        }
        public int GetDiff(EncodeBuffer buffer)
        {
            int diff = Math.Abs(buffer.offset - offset);
            diff = Math.Max(Math.Abs(buffer.serialize_counter - serialize_counter), diff);
            return diff;
        }
        public EncodeBuffer Copy()
        {
            EncodeBuffer ret = new EncodeBuffer();
            ret.buffer = buffer;
            ret.max_offset = max_offset;
            ret.min_limit = min_limit;
            ret.offset = offset;
            ret.result = result;
            ret.serialize_counter = serialize_counter;
            ret.expandable = expandable;
            return ret;
        }

        public byte[] ToArray()
        {
            byte[] ret = new byte[offset];
            Array.Copy(buffer, 0, ret, 0, ret.Length);
            return ret;
        }
        public void Reset(int offset)
        {
            this.offset = offset;
            serialize_counter = 0;
            result = EncodeResult.Good;
        }
        public override string ToString()
        {
            return offset + " - " + serialize_counter;
        }
        public int GetLength()
        {
            return Math.Min(offset, max_offset);
        }
    }

    [Flags]
    public enum EncodeResult
    {
        Good = 0,
        NotEnoughBuffer = 1,
    }

    public class Services
    {
        public static void EncodeIamBroadcast(EncodeBuffer buffer, UInt32 device_id, uint max_apdu, BacnetSegmentations segmentation, UInt16 vendor_id)
        {
            ASN1.encode_application_object_id(buffer, BacnetObjectTypes.OBJECT_DEVICE, device_id);
            ASN1.encode_application_unsigned(buffer, max_apdu);
            ASN1.encode_application_enumerated(buffer, (uint)segmentation);
            ASN1.encode_application_unsigned(buffer, vendor_id);
        }

        public static int DecodeIamBroadcast(byte[] buffer, int offset, out UInt32 device_id, out UInt32 max_apdu, out BacnetSegmentations segmentation, out UInt16 vendor_id)
        {
            int len;
            int apdu_len = 0;
            int org_offset = offset;
            uint len_value;
            byte tag_number;
            BacnetObjectId object_id;
            uint decoded_value;

            device_id = 0;
            max_apdu = 0;
            segmentation = BacnetSegmentations.SEGMENTATION_NONE;
            vendor_id = 0;

            /* OBJECT ID - object id */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
                return -1;
            len = ASN1.decode_object_id(buffer, offset + apdu_len, out object_id.type, out object_id.instance);
            apdu_len += len;
            if (object_id.type != BacnetObjectTypes.OBJECT_DEVICE)
                return -1;
            device_id = object_id.instance;
            /* MAX APDU - unsigned */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len = ASN1.decode_unsigned(buffer, offset + apdu_len, len_value, out decoded_value);
            apdu_len += len;
            max_apdu = decoded_value;
            /* Segmentation - enumerated */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)
                return -1;
            len = ASN1.decode_enumerated(buffer, offset + apdu_len, len_value, out decoded_value);
            apdu_len += len;
            if (decoded_value >= (uint)BacnetSegmentations.MAX_BACNET_SEGMENTATION)
                return -1;
            segmentation = (BacnetSegmentations)decoded_value;
            /* Vendor ID - unsigned16 */
            len =
                ASN1.decode_tag_number_and_value(buffer, offset + apdu_len, out tag_number, out len_value);
            apdu_len += len;
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                return -1;
            len = ASN1.decode_unsigned(buffer, offset + apdu_len, len_value, out decoded_value);
            apdu_len += len;
            if (decoded_value > 0xFFFF)
                return -1;
            vendor_id = (ushort)decoded_value;

            return offset - org_offset;
        }

        public static void EncodeIhaveBroadcast(EncodeBuffer buffer, BacnetObjectId device_id, BacnetObjectId object_id, string object_name)
        {
            /* deviceIdentifier */
            ASN1.encode_application_object_id(buffer, device_id.type, device_id.instance);
            /* objectIdentifier */
            ASN1.encode_application_object_id(buffer, object_id.type, object_id.instance);
            /* objectName */
            ASN1.encode_application_character_string(buffer, object_name);
        }

        public static void EncodeWhoHasBroadcast(EncodeBuffer buffer, int low_limit, int high_limit, BacnetObjectId object_id, string object_name)
        {
            /* optional limits - must be used as a pair */
            if ((low_limit >= 0) && (low_limit <= ASN1.BACNET_MAX_INSTANCE) && (high_limit >= 0) && (high_limit <= ASN1.BACNET_MAX_INSTANCE))
            {
                ASN1.encode_context_unsigned(buffer, 0, (uint)low_limit);
                ASN1.encode_context_unsigned(buffer, 1, (uint)high_limit);
            }
            if (!string.IsNullOrEmpty(object_name))
            {
                ASN1.encode_context_character_string(buffer, 3, object_name);
            }
            else
            {
                ASN1.encode_context_object_id(buffer, 2, object_id.type, object_id.instance);
            }
        }

        public static void EncodeWhoIsBroadcast(EncodeBuffer buffer, int low_limit, int high_limit)
        {
            /* optional limits - must be used as a pair */
            if ((low_limit >= 0) && (low_limit <= ASN1.BACNET_MAX_INSTANCE) &&
                (high_limit >= 0) && (high_limit <= ASN1.BACNET_MAX_INSTANCE))
            {
                ASN1.encode_context_unsigned(buffer, 0, (uint)low_limit);
                ASN1.encode_context_unsigned(buffer, 1, (uint)high_limit);
            }
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

        public static void EncodeAlarmAcknowledge(EncodeBuffer buffer, uint ackProcessIdentifier, BacnetObjectId eventObjectIdentifier, uint eventStateAcked, string ackSource, BacnetGenericTime eventTimeStamp, BacnetGenericTime ackTimeStamp)
        {
            ASN1.encode_context_unsigned(buffer, 0, ackProcessIdentifier);
            ASN1.encode_context_object_id(buffer, 1, eventObjectIdentifier.type, eventObjectIdentifier.instance);
            ASN1.encode_context_enumerated(buffer, 2, eventStateAcked);
            ASN1.bacapp_encode_context_timestamp(buffer, 3, eventTimeStamp);
            ASN1.encode_context_character_string(buffer, 4, ackSource);
            ASN1.bacapp_encode_context_timestamp(buffer, 5, ackTimeStamp);
        }

        public static void EncodeAtomicReadFile(EncodeBuffer buffer, bool is_stream, BacnetObjectId object_id, int position, uint count)
        {
            ASN1.encode_application_object_id(buffer, object_id.type, object_id.instance);
            switch (is_stream)
            {
                case true:
                    ASN1.encode_opening_tag(buffer, 0);
                    ASN1.encode_application_signed(buffer, position);
                    ASN1.encode_application_unsigned(buffer, count);
                    ASN1.encode_closing_tag(buffer, 0);
                    break;
                case false:
                    ASN1.encode_opening_tag(buffer, 1);
                    ASN1.encode_application_signed(buffer, position);
                    ASN1.encode_application_unsigned(buffer, count);
                    ASN1.encode_closing_tag(buffer, 1);
                    break;
                default:
                    break;
            }
        }

        public static int DecodeAtomicReadFile(byte[] buffer, int offset, int apdu_len, out bool is_stream, out BacnetObjectId object_id, out int position, out uint count)
        {
            int len = 0;
            byte tag_number;
            uint len_value_type;
            int tag_len;

            is_stream = true;
            object_id = new BacnetObjectId();
            position = -1;
            count = 0;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
                return -1;
            len += ASN1.decode_object_id(buffer, offset + len, out object_id.type, out object_id.instance);
            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
            {
                is_stream = true;
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartPosition */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                /* requestedOctetCount */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                    return -1;
                len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out count);
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
                tag_len =
                    ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number,
                    out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                /* RecordCount */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                    return -1;
                len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out count);
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else
                return -1;

            return len;
        }

        public static void EncodeAtomicReadFileAcknowledge(EncodeBuffer buffer, bool is_stream, bool end_of_file, int position, uint block_count, byte[][] blocks, int[] counts)
        {
            ASN1.encode_application_boolean(buffer, end_of_file);
            switch (is_stream)
            {
                case true:
                    ASN1.encode_opening_tag(buffer, 0);
                    ASN1.encode_application_signed(buffer, position);
                    ASN1.encode_application_octet_string(buffer, blocks[0], 0, counts[0]);
                    ASN1.encode_closing_tag(buffer, 0);
                    break;
                case false:
                    ASN1.encode_opening_tag(buffer, 1);
                    ASN1.encode_application_signed(buffer, position);
                    ASN1.encode_application_unsigned(buffer, block_count);
                    for (int i = 0; i < block_count; i++)
                        ASN1.encode_application_octet_string(buffer, blocks[i], 0, counts[i]);
                    ASN1.encode_closing_tag(buffer, 1);
                    break;
                default:
                    break;
            }
        }

        public static void EncodeAtomicWriteFile(EncodeBuffer buffer, bool is_stream, BacnetObjectId object_id, int position, uint block_count, byte[][] blocks, int[] counts)
        {
            ASN1.encode_application_object_id(buffer, object_id.type, object_id.instance);
            switch (is_stream)
            {
                case true:
                    ASN1.encode_opening_tag(buffer, 0);
                    ASN1.encode_application_signed(buffer, position);
                    ASN1.encode_application_octet_string(buffer, blocks[0], 0, counts[0]);
                    ASN1.encode_closing_tag(buffer, 0);
                    break;
                case false:
                    ASN1.encode_opening_tag(buffer, 1);
                    ASN1.encode_application_signed(buffer, position);
                    ASN1.encode_application_unsigned(buffer, block_count);
                    for (int i = 0; i < block_count; i++)
                        ASN1.encode_application_octet_string(buffer, blocks[i], 0, counts[i]);
                    ASN1.encode_closing_tag(buffer, 1);
                    break;
                default:
                    break;
            }
        }

        public static int DecodeAtomicWriteFile(byte[] buffer, int offset, int apdu_len, out bool is_stream, out BacnetObjectId object_id, out int position, out uint block_count, out byte[][] blocks, out int[] counts)
        {
            int len = 0;
            byte tag_number;
            uint len_value_type;
            int i;
            int tag_len;

            object_id = new BacnetObjectId();
            is_stream = true;
            position = -1;
            block_count = 0;
            blocks = null;
            counts = null;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)
                return -1;
            len += ASN1.decode_object_id(buffer, offset + len, out object_id.type, out object_id.instance);
            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
            {
                is_stream = true;
                /* a tag number of 2 is not extended so only one octet */
                len++;
                /* fileStartPosition */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                /* fileData */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                    return -1;
                block_count = 1;
                blocks = new byte[1][];
                blocks[0] = new byte[len_value_type];
                counts = new int[] { (int)len_value_type };
                len += ASN1.decode_octet_string(buffer, offset + len, apdu_len, blocks[0], 0, len_value_type);
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
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                /* returnedRecordCount */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                    return -1;
                len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out block_count);
                /* fileData */
                blocks = new byte[block_count][];
                counts = new int[block_count];
                for (i = 0; i < block_count; i++)
                {
                    tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                    len += tag_len;
                    blocks[i] = new byte[len_value_type];
                    counts[i] = (int)len_value_type;
                    if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                        return -1;
                    len += ASN1.decode_octet_string(buffer, offset + len, apdu_len, blocks[i], 0, len_value_type);
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

        public static void EncodeAtomicWriteFileAcknowledge(EncodeBuffer buffer, bool is_stream, int position)
        {
            switch (is_stream)
            {
                case true:
                    ASN1.encode_context_signed(buffer, 0, position);
                    break;
                case false:
                    ASN1.encode_context_signed(buffer, 1, position);
                    break;
                default:
                    break;
            }
        }

        public static void EncodeCOVNotifyConfirmed(EncodeBuffer buffer, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IEnumerable<BacnetPropertyValue> values)
        {
            /* tag 0 - subscriberProcessIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
            /* tag 1 - initiatingDeviceIdentifier */
            ASN1.encode_context_object_id(buffer, 1, BacnetObjectTypes.OBJECT_DEVICE, initiatingDeviceIdentifier);
            /* tag 2 - monitoredObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 2, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            /* tag 3 - timeRemaining */
            ASN1.encode_context_unsigned(buffer, 3, timeRemaining);
            /* tag 4 - listOfValues */
            ASN1.encode_opening_tag(buffer, 4);
            foreach(BacnetPropertyValue value in values)
            {
                /* tag 0 - propertyIdentifier */
                ASN1.encode_context_enumerated(buffer, 0, value.property.propertyIdentifier);
                /* tag 1 - propertyArrayIndex OPTIONAL */
                if (value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                {
                    ASN1.encode_context_unsigned(buffer, 1, value.property.propertyArrayIndex);
                }
                /* tag 2 - value */
                /* abstract syntax gets enclosed in a context tag */
                ASN1.encode_opening_tag(buffer, 2);
                foreach (BacnetValue v in value.value)
                {
                    ASN1.bacapp_encode_application_data(buffer, v);
                }
                ASN1.encode_closing_tag(buffer, 2);
                /* tag 3 - priority OPTIONAL */
                if (value.priority != ASN1.BACNET_NO_PRIORITY)
                {
                    ASN1.encode_context_unsigned(buffer, 3, value.priority);
                }
                /* is there another one to encode? */
                /* FIXME: check to see if there is room in the APDU */
            }
            ASN1.encode_closing_tag(buffer, 4);
        }

        public static void EncodeCOVNotifyUnconfirmed(EncodeBuffer buffer, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IEnumerable<BacnetPropertyValue> values)
        {
            /* tag 0 - subscriberProcessIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
            /* tag 1 - initiatingDeviceIdentifier */
            ASN1.encode_context_object_id(buffer, 1, BacnetObjectTypes.OBJECT_DEVICE, initiatingDeviceIdentifier);
            /* tag 2 - monitoredObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 2, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            /* tag 3 - timeRemaining */
            ASN1.encode_context_unsigned(buffer, 3, timeRemaining);
            /* tag 4 - listOfValues */
            ASN1.encode_opening_tag(buffer, 4);
            foreach (BacnetPropertyValue value in values)
            {
                /* tag 0 - propertyIdentifier */
                ASN1.encode_context_enumerated(buffer, 0, value.property.propertyIdentifier);
                /* tag 1 - propertyArrayIndex OPTIONAL */
                if (value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                {
                    ASN1.encode_context_unsigned(buffer, 1, value.property.propertyArrayIndex);
                }
                /* tag 2 - value */
                /* abstract syntax gets enclosed in a context tag */
                ASN1.encode_opening_tag(buffer, 2);
                foreach (BacnetValue v in value.value)
                {
                    ASN1.bacapp_encode_application_data(buffer, v);
                }
                ASN1.encode_closing_tag(buffer, 2);
                /* tag 3 - priority OPTIONAL */
                if (value.priority != ASN1.BACNET_NO_PRIORITY)
                {
                    ASN1.encode_context_unsigned(buffer, 3, value.priority);
                }
                /* is there another one to encode? */
                /* FIXME: check to see if there is room in the APDU */
            }
            ASN1.encode_closing_tag(buffer, 4);
        }

        public static void EncodeSubscribeCOV(EncodeBuffer buffer, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime)
        {
            /* tag 0 - subscriberProcessIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
            /* tag 1 - monitoredObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 1, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            /*
               If both the 'Issue Confirmed Notifications' and
               'Lifetime' parameters are absent, then this shall
               indicate a cancellation request.
             */
            if (!cancellationRequest)
            {
                /* tag 2 - issueConfirmedNotifications */
                ASN1.encode_context_boolean(buffer, 2, issueConfirmedNotifications);
                /* tag 3 - lifetime */
                ASN1.encode_context_unsigned(buffer, 3, lifetime);
            }
        }

        public static int DecodeSubscribeCOV(byte[] buffer, int offset, int apdu_len, out uint subscriberProcessIdentifier, out BacnetObjectId monitoredObjectIdentifier, out bool cancellationRequest, out bool issueConfirmedNotifications, out uint lifetime)
        {
            int len = 0;
            byte tag_number;
            uint len_value;

            subscriberProcessIdentifier = 0;
            monitoredObjectIdentifier = new BacnetObjectId();
            cancellationRequest = false;
            issueConfirmedNotifications = false;
            lifetime = 0;

            /* tag 0 - subscriberProcessIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_unsigned(buffer, offset + len, len_value, out subscriberProcessIdentifier);
            }
            else 
                return -1;
            /* tag 1 - monitoredObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_object_id(buffer, offset + len, out monitoredObjectIdentifier.type, out monitoredObjectIdentifier.instance);
            }
            else
                return -1;
            /* optional parameters - if missing, means cancellation */
            if (len < apdu_len)
            {
                /* tag 2 - issueConfirmedNotifications - optional */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
                {
                    cancellationRequest = false;
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                    issueConfirmedNotifications = buffer[offset + len] > 0;
                    len += (int)len_value;
                }
                else
                {
                    cancellationRequest = true;
                }
                /* tag 3 - lifetime - optional */
                if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
                {
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                    len += ASN1.decode_unsigned(buffer, offset + len, len_value, out lifetime);
                }
                else
                {
                    lifetime = 0;
                }
            }
            else
            {
                cancellationRequest = true;
            }

            return len;
        }

        public static void EncodeSubscribeProperty(EncodeBuffer buffer, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetPropertyReference monitoredProperty, bool covIncrementPresent, float covIncrement)
        {
            /* tag 0 - subscriberProcessIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, subscriberProcessIdentifier);
            /* tag 1 - monitoredObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 1, monitoredObjectIdentifier.type, monitoredObjectIdentifier.instance);
            if (!cancellationRequest)
            {
                /* tag 2 - issueConfirmedNotifications */
                ASN1.encode_context_boolean(buffer, 2, issueConfirmedNotifications);
                /* tag 3 - lifetime */
                ASN1.encode_context_unsigned(buffer, 3, lifetime);
            }
            /* tag 4 - monitoredPropertyIdentifier */
            ASN1.encode_opening_tag(buffer, 4);
            ASN1.encode_context_enumerated(buffer, 0, monitoredProperty.propertyIdentifier);
            if (monitoredProperty.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 1, monitoredProperty.propertyArrayIndex);

            }
            ASN1.encode_closing_tag(buffer, 4);

            /* tag 5 - covIncrement */
            if (covIncrementPresent)
            {
                ASN1.encode_context_real(buffer, 5, covIncrement);
            }
        }

        public static int DecodeSubscribeProperty(byte[] buffer, int offset, int apdu_len, out uint subscriberProcessIdentifier, out BacnetObjectId monitoredObjectIdentifier, out BacnetPropertyReference monitoredProperty, out bool cancellationRequest, out bool issueConfirmedNotifications, out uint lifetime, out float covIncrement)
        {
            int len = 0;
            byte tag_number;
            uint len_value;
            uint decoded_value;

            subscriberProcessIdentifier = 0;
            monitoredObjectIdentifier = new BacnetObjectId();
            cancellationRequest = false;
            issueConfirmedNotifications = false;
            lifetime = 0;
            covIncrement = 0;
            monitoredProperty = new BacnetPropertyReference();

            /* tag 0 - subscriberProcessIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_unsigned(buffer, offset + len, len_value, out subscriberProcessIdentifier);
            }
            else
                return -1;

            /* tag 1 - monitoredObjectIdentifier */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_object_id(buffer, offset + len, out monitoredObjectIdentifier.type, out monitoredObjectIdentifier.instance);
            }
            else
                return -1;

            /* tag 2 - issueConfirmedNotifications - optional */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 2))
            {
                cancellationRequest = false;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                issueConfirmedNotifications = buffer[offset + len] > 0;
                len++;
            }
            else
            {
                cancellationRequest = true;
            }

            /* tag 3 - lifetime - optional */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 3))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_unsigned(buffer, offset + len, len_value, out lifetime);
            }
            else
            {
                lifetime = 0;
            }

            /* tag 4 - monitoredPropertyIdentifier */
            if (!ASN1.decode_is_opening_tag_number(buffer, offset + len, 4))
                return -1;

            /* a tag number of 4 is not extended so only one octet */
            len++;
            /* the propertyIdentifier is tag 0 */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_enumerated(buffer, offset + len, len_value, out decoded_value);
                monitoredProperty.propertyIdentifier = decoded_value;
            }
            else
                return -1;

            /* the optional array index is tag 1 */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 1))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number,out len_value);
                len += ASN1.decode_unsigned(buffer, offset + len, len_value, out decoded_value);
                monitoredProperty.propertyArrayIndex = decoded_value;
            }
            else
            {
                monitoredProperty.propertyArrayIndex = ASN1.BACNET_ARRAY_ALL;
            }

            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 4))
                return -1;

            /* a tag number of 4 is not extended so only one octet */
            len++;
            /* tag 5 - covIncrement - optional */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 5))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
                len += ASN1.decode_real(buffer, offset + len, out covIncrement);
            }
            else
            {
                covIncrement = 0;
            }

            return len;
        }

        private static void EncodeEventNotifyData(EncodeBuffer buffer, BacnetEventNotificationData data)
        {
            /* tag 0 - processIdentifier */
            ASN1.encode_context_unsigned(buffer, 0, data.processIdentifier);
            /* tag 1 - initiatingObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 1, data.initiatingObjectIdentifier.type, data.initiatingObjectIdentifier.instance);

            /* tag 2 - eventObjectIdentifier */
            ASN1.encode_context_object_id(buffer, 2, data.eventObjectIdentifier.type, data.eventObjectIdentifier.instance);

            /* tag 3 - timeStamp */
            ASN1.bacapp_encode_context_timestamp(buffer, 3, data.timeStamp);

            /* tag 4 - noticicationClass */
            ASN1.encode_context_unsigned(buffer, 4, data.notificationClass);

            /* tag 5 - priority */
            ASN1.encode_context_unsigned(buffer, 5, data.priority);

            /* tag 6 - eventType */
            ASN1.encode_context_enumerated(buffer, 6, (uint)data.eventType);

            /* tag 7 - messageText */
            if (!string.IsNullOrEmpty(data.messageText))
                ASN1.encode_context_character_string(buffer, 7, data.messageText);

            /* tag 8 - notifyType */
            ASN1.encode_context_enumerated(buffer, 8, (uint)data.notifyType);

            switch (data.notifyType)
            {
                case BacnetEventNotificationData.BacnetNotifyTypes.NOTIFY_ALARM:
                case BacnetEventNotificationData.BacnetNotifyTypes.NOTIFY_EVENT:
                    /* tag 9 - ackRequired */
                    ASN1.encode_context_boolean(buffer, 9, data.ackRequired);

                    /* tag 10 - fromState */
                    ASN1.encode_context_enumerated(buffer, 10, (uint)data.fromState);
                    break;
                default:
                    break;
            }

            /* tag 11 - toState */
            ASN1.encode_context_enumerated(buffer, 11, (uint)data.toState);

            switch (data.notifyType)
            {
                case BacnetEventNotificationData.BacnetNotifyTypes.NOTIFY_ALARM:
                case BacnetEventNotificationData.BacnetNotifyTypes.NOTIFY_EVENT:
                    /* tag 12 - event values */
                    ASN1.encode_opening_tag(buffer, 12);

                    switch (data.eventType)
                    {
                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_CHANGE_OF_BITSTRING:
                            ASN1.encode_opening_tag(buffer, 0);
                            ASN1.encode_context_bitstring(buffer, 0, data.changeOfBitstring_referencedBitString);
                            ASN1.encode_context_bitstring(buffer, 1, data.changeOfBitstring_statusFlags);
                            ASN1.encode_closing_tag(buffer, 0);
                            break;

                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_CHANGE_OF_STATE:
                            ASN1.encode_opening_tag(buffer, 1);
                            ASN1.encode_opening_tag(buffer, 0);
                            ASN1.bacapp_encode_property_state(buffer, data.changeOfState_newState);
                            ASN1.encode_closing_tag(buffer, 0);
                            ASN1.encode_context_bitstring(buffer, 1, data.changeOfState_statusFlags);
                            ASN1.encode_closing_tag(buffer, 1);
                            break;

                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_CHANGE_OF_VALUE:
                            ASN1.encode_opening_tag(buffer, 2);
                            ASN1.encode_opening_tag(buffer, 0);

                            switch (data.changeOfValue_tag)
                            {
                                case BacnetEventNotificationData.BacnetCOVTypes.CHANGE_OF_VALUE_REAL:
                                    ASN1.encode_context_real(buffer, 1, data.changeOfValue_changeValue);
                                    break;
                                case BacnetEventNotificationData.BacnetCOVTypes.CHANGE_OF_VALUE_BITS:
                                    ASN1.encode_context_bitstring(buffer, 0, data.changeOfValue_changedBits);
                                    break;
                                default:
                                    throw new Exception("Hmm?");
                            }

                            ASN1.encode_closing_tag(buffer, 0);
                            ASN1.encode_context_bitstring(buffer, 1, data.changeOfValue_statusFlags);
                            ASN1.encode_closing_tag(buffer, 2);
                            break;

                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_FLOATING_LIMIT:
                            ASN1.encode_opening_tag(buffer, 4);
                            ASN1.encode_context_real(buffer, 0, data.floatingLimit_referenceValue);
                            ASN1.encode_context_bitstring(buffer, 1, data.floatingLimit_statusFlags);
                            ASN1.encode_context_real(buffer, 2, data.floatingLimit_setPointValue);
                            ASN1.encode_context_real(buffer, 3, data.floatingLimit_errorLimit);
                            ASN1.encode_closing_tag(buffer, 4);
                            break;

                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_OUT_OF_RANGE:
                            ASN1.encode_opening_tag(buffer, 5);
                            ASN1.encode_context_real(buffer, 0, data.outOfRange_exceedingValue);
                            ASN1.encode_context_bitstring(buffer, 1, data.outOfRange_statusFlags);
                            ASN1.encode_context_real(buffer, 2, data.outOfRange_deadband);
                            ASN1.encode_context_real(buffer, 3, data.outOfRange_exceededLimit);
                            ASN1.encode_closing_tag(buffer, 5);
                            break;

                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY:
                            ASN1.encode_opening_tag(buffer, 8);
                            ASN1.encode_context_enumerated(buffer, 0, (uint)data.changeOfLifeSafety_newState);
                            ASN1.encode_context_enumerated(buffer, 1, (uint)data.changeOfLifeSafety_newMode);
                            ASN1.encode_context_bitstring(buffer, 2, data.changeOfLifeSafety_statusFlags);
                            ASN1.encode_context_enumerated(buffer, 3, (uint)data.changeOfLifeSafety_operationExpected);
                            ASN1.encode_closing_tag(buffer, 8);
                            break;

                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_BUFFER_READY:
                            ASN1.encode_opening_tag(buffer, 10);
                            ASN1.bacapp_encode_context_device_obj_property_ref(buffer, 0, data.bufferReady_bufferProperty);
                            ASN1.encode_context_unsigned(buffer, 1, data.bufferReady_previousNotification);
                            ASN1.encode_context_unsigned(buffer, 2, data.bufferReady_currentNotification);
                            ASN1.encode_closing_tag(buffer, 10);

                            break;
                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_UNSIGNED_RANGE:
                            ASN1.encode_opening_tag(buffer, 11);
                            ASN1.encode_context_unsigned(buffer, 0, data.unsignedRange_exceedingValue);
                            ASN1.encode_context_bitstring(buffer, 1, data.unsignedRange_statusFlags);
                            ASN1.encode_context_unsigned(buffer, 2, data.unsignedRange_exceededLimit);
                            ASN1.encode_closing_tag(buffer, 11);
                            break;
                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_EXTENDED:
                        case BacnetEventNotificationData.BacnetEventTypes.EVENT_COMMAND_FAILURE:
                        default:
                            throw new NotImplementedException();
                    }
                    ASN1.encode_closing_tag(buffer, 12);
                    break;
                case BacnetEventNotificationData.BacnetNotifyTypes.NOTIFY_ACK_NOTIFICATION:
                /* FIXME: handle this case */
                default:
                    break;
            }
        }

        public static void EncodeEventNotifyConfirmed(EncodeBuffer buffer, BacnetEventNotificationData data)
        {
            EncodeEventNotifyData(buffer, data);
        }

        public static void EncodeEventNotifyUnconfirmed(EncodeBuffer buffer, BacnetEventNotificationData data)
        {
            EncodeEventNotifyData(buffer, data);
        }

        public static void EncodeAlarmSummary(EncodeBuffer buffer, BacnetObjectId objectIdentifier, uint alarmState, BacnetBitString acknowledgedTransitions)
        {
            /* tag 0 - Object Identifier */
            ASN1.encode_application_object_id(buffer, objectIdentifier.type, objectIdentifier.instance);
            /* tag 1 - Alarm State */
            ASN1.encode_application_enumerated(buffer, alarmState);
            /* tag 2 - Acknowledged Transitions */
            ASN1.encode_application_bitstring(buffer, acknowledgedTransitions);
        }

        public static void EncodeGetEventInformation(EncodeBuffer buffer, bool send_last, BacnetObjectId lastReceivedObjectIdentifier)
        {
            /* encode optional parameter */
            if (send_last)
                ASN1.encode_context_object_id(buffer, 0, lastReceivedObjectIdentifier.type, lastReceivedObjectIdentifier.instance);
        }

        public static void EncodeGetEventInformationAcknowledge(EncodeBuffer buffer, BacnetGetEventInformationData[] events, bool moreEvents)
        {
            /* service ack follows */
            /* Tag 0: listOfEventSummaries */
            ASN1.encode_opening_tag(buffer, 0);
            foreach(BacnetGetEventInformationData event_data in events)
            {
                /* Tag 0: objectIdentifier */
                ASN1.encode_context_object_id(buffer, 0, event_data.objectIdentifier.type, event_data.objectIdentifier.instance);
                /* Tag 1: eventState */
                ASN1.encode_context_enumerated(buffer, 1, (uint)event_data.eventState);
                /* Tag 2: acknowledgedTransitions */
                ASN1.encode_context_bitstring(buffer, 2, event_data.acknowledgedTransitions);
                /* Tag 3: eventTimeStamps */
                ASN1.encode_opening_tag(buffer, 3);
                for (int i = 0; i < 3; i++)
                {
                    ASN1.bacapp_encode_timestamp(buffer, event_data.eventTimeStamps[i]);
                }
                ASN1.encode_closing_tag(buffer, 3);
                /* Tag 4: notifyType */
                ASN1.encode_context_enumerated(buffer, 4, (uint)event_data.notifyType);
                /* Tag 5: eventEnable */
                ASN1.encode_context_bitstring(buffer, 5, event_data.eventEnable);
                /* Tag 6: eventPriorities */
                ASN1.encode_opening_tag(buffer, 6);
                for (int i = 0; i < 3; i++)
                {
                    ASN1.encode_application_unsigned(buffer, event_data.eventPriorities[i]);
                }
                ASN1.encode_closing_tag(buffer, 6);
            }
            ASN1.encode_closing_tag(buffer, 0);
            ASN1.encode_context_boolean(buffer, 1, moreEvents);
        }

        public static void EncodeLifeSafetyOperation(EncodeBuffer buffer, uint processId, string requestingSrc, uint operation, BacnetObjectId targetObject)
        {
            /* tag 0 - requestingProcessId */
            ASN1.encode_context_unsigned(buffer, 0, processId);
            /* tag 1 - requestingSource */
            ASN1.encode_context_character_string(buffer, 1, requestingSrc);
            /* Operation */
            ASN1.encode_context_enumerated(buffer, 2, operation);
            /* Object ID */
            ASN1.encode_context_object_id(buffer, 3, targetObject.type, targetObject.instance);
        }

        public static void EncodePrivateTransferConfirmed(EncodeBuffer buffer, uint vendorID, uint serviceNumber, byte[] data)
        {
            ASN1.encode_context_unsigned(buffer, 0, vendorID);
            ASN1.encode_context_unsigned(buffer, 1, serviceNumber);
            ASN1.encode_opening_tag(buffer, 2);
            buffer.Add(data, data.Length);
            ASN1.encode_closing_tag(buffer, 2);
        }

        public static void EncodePrivateTransferUnconfirmed(EncodeBuffer buffer, uint vendorID, uint serviceNumber, byte[] data)
        {
            ASN1.encode_context_unsigned(buffer, 0, vendorID);
            ASN1.encode_context_unsigned(buffer, 1, serviceNumber);
            ASN1.encode_opening_tag(buffer, 2);
            buffer.Add(data, data.Length);
            ASN1.encode_closing_tag(buffer, 2);
        }

        public static void EncodePrivateTransferAcknowledge(EncodeBuffer buffer, uint vendorID, uint serviceNumber, byte[] data)
        {
            ASN1.encode_context_unsigned(buffer, 0, vendorID);
            ASN1.encode_context_unsigned(buffer, 1, serviceNumber);
            ASN1.encode_opening_tag(buffer, 2);
            buffer.Add(data, data.Length);
            ASN1.encode_closing_tag(buffer, 2);
        }

        public static void EncodeDeviceCommunicationControl(EncodeBuffer buffer, uint timeDuration, uint enable_disable, string password)
        {
            /* optional timeDuration */
            if (timeDuration > 0)
                ASN1.encode_context_unsigned(buffer, 0, timeDuration);

            /* enable disable */
            ASN1.encode_context_enumerated(buffer, 1, enable_disable);

            /* optional password */
            if (!string.IsNullOrEmpty(password))
            {
                /* FIXME: must be at least 1 character, limited to 20 characters */
                ASN1.encode_context_character_string(buffer, 2, password);
            }
        }

        public static int DecodeDeviceCommunicationControl(byte[] buffer, int offset, int apdu_len, out uint timeDuration, out uint enable_disable, out string password)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            timeDuration = 0;
            enable_disable = 0;
            password = "";

            /* Tag 0: timeDuration, in minutes --optional--
             * But if not included, take it as indefinite,
             * which we return as "very large" */
            if (ASN1.decode_is_context_tag(buffer, offset + len, 0))
            {
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out timeDuration);
            }

            /* Tag 1: enable_disable */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 1))
                return -1;
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out enable_disable);

            /* Tag 2: password --optional-- */
            if (len < apdu_len)
            {
                if (!ASN1.decode_is_context_tag(buffer, offset + len, 2))
                    return -1;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += ASN1.decode_character_string(buffer, offset + len, apdu_len - (offset + len), len_value_type, out password);
            }

            return len;
        }

        public static void EncodeReinitializeDevice(EncodeBuffer buffer, BacnetReinitializedStates state, string password)
        {
            ASN1.encode_context_enumerated(buffer, 0, (uint)state);

            /* optional password */
            if (!string.IsNullOrEmpty(password))
            {
                /* FIXME: must be at least 1 character, limited to 20 characters */
                ASN1.encode_context_character_string(buffer, 1, password);
            }
        }

        public static int DecodeReinitializeDevice(byte[] buffer, int offset, int apdu_len, out BacnetReinitializedStates state, out string password)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;
            uint value;

            state = BacnetReinitializedStates.BACNET_REINIT_IDLE;
            password = "";

            /* Tag 0: reinitializedStateOfDevice */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            len += ASN1.decode_enumerated(buffer, offset + len, len_value_type, out value);
            state = (BacnetReinitializedStates)value;
            /* Tag 1: password - optional */
            if (len < apdu_len)
            {
                if (!ASN1.decode_is_context_tag(buffer, offset + len, 1))
                    return -1;
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += ASN1.decode_character_string(buffer, offset + len, apdu_len - (offset + len), len_value_type, out password);
            }

            return len;
        }

        public enum BacnetReadRangeRequestTypes
        {
            RR_BY_POSITION = 1,
            RR_BY_SEQUENCE = 2,
            RR_BY_TIME = 4,
            RR_READ_ALL = 8,
        }

        public static void EncodeReadRange(EncodeBuffer buffer, BacnetObjectId object_id, uint property_id, uint arrayIndex, BacnetReadRangeRequestTypes requestType, uint position, DateTime time, int count)
        {
            ASN1.encode_context_object_id(buffer, 0, object_id.type, object_id.instance);
            ASN1.encode_context_enumerated(buffer, 1, property_id);

            /* optional array index */
            if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
            }

            /* Build the appropriate (optional) range parameter based on the request type */
            switch (requestType)
            {
                case BacnetReadRangeRequestTypes.RR_BY_POSITION:
                    ASN1.encode_opening_tag(buffer, 3);
                    ASN1.encode_application_unsigned(buffer, position);
                    ASN1.encode_application_signed(buffer, count);
                    ASN1.encode_closing_tag(buffer, 3);
                    break;

                case BacnetReadRangeRequestTypes.RR_BY_SEQUENCE:
                    ASN1.encode_opening_tag(buffer, 6);
                    ASN1.encode_application_unsigned(buffer, position);
                    ASN1.encode_application_signed(buffer, count);
                    ASN1.encode_closing_tag(buffer, 6);
                    break;

                case BacnetReadRangeRequestTypes.RR_BY_TIME:
                    ASN1.encode_opening_tag(buffer, 7);
                    ASN1.encode_application_date(buffer, time);
                    ASN1.encode_application_time(buffer, time);
                    ASN1.encode_application_signed(buffer, count);
                    ASN1.encode_closing_tag(buffer, 7);
                    break;

                case BacnetReadRangeRequestTypes.RR_READ_ALL:  /* to attempt a read of the whole array or list, omit the range parameter */
                    break;

                default:
                    break;
            }
        }

        public static void EncodeReadRangeAcknowledge(EncodeBuffer buffer, BacnetObjectId object_id, uint property_id, uint arrayIndex, BacnetBitString ResultFlags, uint ItemCount, byte[] application_data, BacnetReadRangeRequestTypes requestType, uint FirstSequence)
        {
            /* service ack follows */
            ASN1.encode_context_object_id(buffer, 0, object_id.type, object_id.instance);
            ASN1.encode_context_enumerated(buffer, 1, property_id);
            /* context 2 array index is optional */
            if (arrayIndex != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 2, arrayIndex);
            }
            /* Context 3 BACnet Result Flags */
            ASN1.encode_context_bitstring(buffer, 3, ResultFlags);
            /* Context 4 Item Count */
            ASN1.encode_context_unsigned(buffer, 4, ItemCount);
            /* Context 5 Property list - reading the standard it looks like an empty list still 
             * requires an opening and closing tag as the tagged parameter is not optional
             */
            ASN1.encode_opening_tag(buffer, 5);
            if (ItemCount != 0)
            {
                buffer.Add(application_data, application_data.Length);
            }
            ASN1.encode_closing_tag(buffer, 5);

            if ((ItemCount != 0) && (requestType != BacnetReadRangeRequestTypes.RR_BY_POSITION) && (requestType != BacnetReadRangeRequestTypes.RR_READ_ALL))
            {
                /* Context 6 Sequence number of first item */
                ASN1.encode_context_unsigned(buffer, 6, FirstSequence);
            }
        }

        public static void EncodeReadProperty(EncodeBuffer buffer, BacnetObjectId object_id, uint property_id, uint array_index = ASN1.BACNET_ARRAY_ALL)
        {
            if ((int)object_id.type <= ASN1.BACNET_MAX_OBJECT)
            {
                /* check bounds so that we could create malformed
                   messages for testing */
                ASN1.encode_context_object_id(buffer, 0, object_id.type, object_id.instance);
            }
            if (property_id <= (uint)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID)
            {
                /* check bounds so that we could create malformed
                   messages for testing */
                ASN1.encode_context_enumerated(buffer, 1, property_id);
            }
            /* optional array index */
            if (array_index != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 2, array_index);
            }
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

        public static int DecodeAtomicReadFileAcknowledge(byte[] buffer, int offset, int apdu_len, out bool end_of_file, out bool is_stream, out int position, out uint count, out byte[] target_buffer, out int target_offset)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;
            int tag_len = 0;

            end_of_file = false;
            is_stream = false;
            position = -1;
            count = 0;
            target_buffer = null;
            target_offset = -1;

            len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)
                return -1;
            end_of_file = len_value_type > 0;
            if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 0))
            {
                is_stream = true;
                /* a tag number is not extended so only one octet */
                len++;
                /* fileStartPosition */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                    return -1;
                len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                /* fileData */
                tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                len += tag_len;
                if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                    return -1;
                //len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
                target_buffer = buffer;
                target_offset = offset + len;
                count = len_value_type;
                len += (int)count;
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                    return -1;
                /* a tag number is not extended so only one octet */
                len++;
            }
            else if (ASN1.decode_is_opening_tag_number(buffer, offset + len, 1))
            {
                is_stream = false;
                throw new NotImplementedException("Non stream File transfers are not supported");
                ///* a tag number is not extended so only one octet */
                //len++;
                ///* fileStartRecord */
                //tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                //len += tag_len;
                //if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT)
                //    return -1;
                //len += ASN1.decode_signed(buffer, offset + len, len_value_type, out position);
                ///* returnedRecordCount */
                //tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                //len += tag_len;
                //if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)
                //    return -1;
                //len += ASN1.decode_unsigned(buffer, offset + len, len_value_type, out count);
                //for (i = 0; i < count; i++)
                //{
                //    /* fileData */
                //    tag_len = ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value_type);
                //    len += tag_len;
                //    if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING)
                //        return -1;
                //    len += ASN1.decode_octet_string(buffer, offset + len, buffer.Length, target_buffer, target_offset, len_value_type);
                //}
                //if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1))
                //    return -1;
                ///* a tag number is not extended so only one octet */
                //len++;
            }
            else
                return -1;

            return len;
        }

        public static int DecodeReadProperty(byte[] buffer, int offset, int apdu_len, out BacnetObjectId object_id, out BacnetPropertyReference property)
        {
            int len = 0;
            ushort type = 0;
            byte tag_number = 0;
            uint len_value_type = 0;

            object_id = new BacnetObjectId();
            property = new BacnetPropertyReference();

            /* Tag 0: Object ID          */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out type, out object_id.instance);
            object_id.type = (BacnetObjectTypes)type;
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

        public static void EncodeReadPropertyAcknowledge(EncodeBuffer buffer, BacnetObjectId object_id, uint property_id, uint array_index, IEnumerable<BacnetValue> value_list)
        {
            /* service ack follows */
            ASN1.encode_context_object_id(buffer, 0, object_id.type, object_id.instance);
            ASN1.encode_context_enumerated(buffer, 1, property_id);
            /* context 2 array index is optional */
            if (array_index != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 2, array_index);
            }

            /* Value */
            ASN1.encode_opening_tag(buffer, 3);
            foreach (BacnetValue value in value_list)
            {
                ASN1.bacapp_encode_application_data(buffer, value);
            }
            ASN1.encode_closing_tag(buffer, 3);
        }

        public static int DecodeReadPropertyAcknowledge(byte[] buffer, int offset, int apdu_len, out BacnetObjectId object_id, out BacnetPropertyReference property, out IList<BacnetValue> value_list)
        {
            byte tag_number = 0;
            uint len_value_type = 0;
            int tag_len = 0;    /* length of tag decode */
            int len = 0;        /* total length of decodes */

            object_id = new BacnetObjectId();
            property = new BacnetPropertyReference();
            value_list = new List<BacnetValue>();

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

                BacnetValue value;
                while ((apdu_len - len) > 1)
                {
                    tag_len = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset, object_id.type, (BacnetPropertyIds)property.propertyIdentifier, out value);
                    if (tag_len < 0) return -1;
                    len += tag_len;
                    value_list.Add(value);
                }
            }
            else
                return -1;

            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
                return -1;
            len++;

            return len;
        }

        public static void EncodeReadPropertyMultiple(EncodeBuffer buffer, IList<BacnetReadAccessSpecification> properties)
        {
            foreach(BacnetReadAccessSpecification value in properties)
                ASN1.encode_read_access_specification(buffer, value);
        }

        public static void EncodeReadPropertyMultiple(EncodeBuffer buffer, BacnetObjectId object_id, IList<BacnetPropertyReference> properties)
        {
            EncodeReadPropertyMultiple(buffer, new BacnetReadAccessSpecification[] { new BacnetReadAccessSpecification(object_id, properties) });
        }

        public static int DecodeReadPropertyMultiple(byte[] buffer, int offset, int apdu_len, out IList<BacnetReadAccessSpecification> properties)
        {
            int len = 0;
            int tmp;

            List<BacnetReadAccessSpecification> values = new List<BacnetReadAccessSpecification>();
            properties = null;

            while ((apdu_len - len) > 0)
            {
                BacnetReadAccessSpecification value;
                tmp = ASN1.decode_read_access_specification(buffer, offset + len, apdu_len - len, out value);
                if (tmp < 0) return -1;
                len += tmp;
                values.Add(value);
            }

            properties = values;
            return len;
        }

        public static void EncodeReadPropertyMultipleAcknowledge(EncodeBuffer buffer, IList<BacnetReadAccessResult> values)
        {
            foreach (BacnetReadAccessResult value in values)
                ASN1.encode_read_access_result(buffer, value);
        }

        public static int DecodeReadPropertyMultipleAcknowledge(byte[] buffer, int offset, int apdu_len, out IList<BacnetReadAccessResult> values)
        {
            int len = 0;
            int tmp;

            List<BacnetReadAccessResult> _values = new List<BacnetReadAccessResult>();
            values = null;

            while ((apdu_len - len) > 0)
            {
                BacnetReadAccessResult value;
                tmp = ASN1.decode_read_access_result(buffer, offset + len, apdu_len - len, out value);
                if (tmp < 0) return -1;
                len += tmp;
                _values.Add(value);
            }

            values = _values;
            return len;
        }

        public static void EncodeWriteProperty(EncodeBuffer buffer, BacnetObjectId object_id, uint property_id, uint array_index, uint priority, IEnumerable<BacnetValue> value_list)
        {
            ASN1.encode_context_object_id(buffer, 0, object_id.type, object_id.instance);
            ASN1.encode_context_enumerated(buffer, 1, property_id);

            /* optional array index; ALL is -1 which is assumed when missing */
            if (array_index != ASN1.BACNET_ARRAY_ALL)
            {
                ASN1.encode_context_unsigned(buffer, 2, array_index);
            }

            /* propertyValue */
            ASN1.encode_opening_tag(buffer, 3);
            foreach (BacnetValue value in value_list)
            {
                ASN1.bacapp_encode_application_data(buffer, value);
            }
            ASN1.encode_closing_tag(buffer, 3);

            /* optional priority - 0 if not set, 1..16 if set */
            if (priority != ASN1.BACNET_NO_PRIORITY)
            {
                ASN1.encode_context_unsigned(buffer, 4, priority);
            }
        }

        public static int DecodeCOVNotifyUnconfirmed(byte[] buffer, int offset, int apdu_len, out uint subscriberProcessIdentifier, out BacnetObjectId initiatingDeviceIdentifier, out BacnetObjectId monitoredObjectIdentifier, out uint timeRemaining, out ICollection<BacnetPropertyValue> values)
        {
            int len = 0;
            byte tag_number = 0;
            uint len_value = 0;
            uint decoded_value;

            subscriberProcessIdentifier = 0;
            initiatingDeviceIdentifier = new BacnetObjectId();
            monitoredObjectIdentifier = new BacnetObjectId();
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
            LinkedList<BacnetPropertyValue> _values = new LinkedList<BacnetPropertyValue>();
            while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 4))
            {
                BacnetPropertyValue new_entry = new BacnetPropertyValue();

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
                List<BacnetValue> b_values = new List<BacnetValue>();
                while (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
                {
                    BacnetValue b_value;
                    int tmp = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset,monitoredObjectIdentifier.type, (BacnetPropertyIds)new_entry.property.propertyIdentifier, out b_value);
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

        public static int DecodeWriteProperty(byte[] buffer, int offset, int apdu_len, out BacnetObjectId object_id, out BacnetPropertyValue value)
        {
            int len = 0;
            int tag_len = 0;
            byte tag_number = 0;
            uint len_value_type = 0;
            uint unsigned_value = 0;

            object_id = new BacnetObjectId();
            value = new BacnetPropertyValue();

            /* Tag 0: Object ID          */
            if (!ASN1.decode_is_context_tag(buffer, offset + len, 0))
                return -1;
            len++;
            len += ASN1.decode_object_id(buffer, offset + len, out object_id.type, out object_id.instance);
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
            len++;
            
            //data
            List<BacnetValue> _value_list = new List<BacnetValue>();
            while ((apdu_len - len) > 1 && !ASN1.decode_is_closing_tag_number(buffer, offset + len, 3))
            {
                BacnetValue b_value;
                int l = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset, object_id.type, (BacnetPropertyIds)value.property.propertyIdentifier, out b_value);
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

        public static void EncodeWritePropertyMultiple(EncodeBuffer buffer, BacnetObjectId object_id, ICollection<BacnetPropertyValue> value_list)
        {
            ASN1.encode_context_object_id(buffer, 0, object_id.type, object_id.instance);
            /* Tag 1: sequence of WriteAccessSpecification */
            ASN1.encode_opening_tag(buffer, 1);

            foreach(BacnetPropertyValue p_value in value_list)
            {
                /* Tag 0: Property */
                ASN1.encode_context_enumerated(buffer, 0, p_value.property.propertyIdentifier);

                /* Tag 1: array index */
                if (p_value.property.propertyArrayIndex != ASN1.BACNET_ARRAY_ALL)
                    ASN1.encode_context_unsigned(buffer, 1, p_value.property.propertyArrayIndex);

                /* Tag 2: Value */
                ASN1.encode_opening_tag(buffer, 2);
                foreach (BacnetValue value in p_value.value)
                {
                    ASN1.bacapp_encode_application_data(buffer, value);
                }
                ASN1.encode_closing_tag(buffer, 2);

                /* Tag 3: Priority */
                if (p_value.priority != ASN1.BACNET_NO_PRIORITY)
                    ASN1.encode_context_unsigned(buffer, 3, p_value.priority);
            }

            ASN1.encode_closing_tag(buffer, 1);
        }

        public static int DecodeWritePropertyMultiple(byte[] buffer, int offset, int apdu_len, out BacnetObjectId object_id, out ICollection<BacnetPropertyValue> values_refs)
        {
            int len = 0;
            byte tag_number;
            uint len_value;
            uint ulVal;
            uint property_id;

            object_id = new BacnetObjectId();
            values_refs = null;

            /* Context tag 0 - Object ID */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
            if ((tag_number == 0) && (apdu_len > len))
            {
                apdu_len -= len;
                if (apdu_len >= 4)
                {
                    len += ASN1.decode_object_id(buffer, offset + len, out object_id.type, out object_id.instance);
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

            LinkedList<BacnetPropertyValue> _values = new LinkedList<BacnetPropertyValue>();
            while ((apdu_len - len) > 1)
            {
                BacnetPropertyValue new_entry = new BacnetPropertyValue();

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
                new_entry.property = new BacnetPropertyReference(property_id, ulVal);

                /* tag 2 - Property Value */
                if ((tag_number == 2) && (ASN1.decode_is_opening_tag(buffer, offset + len - 1)))
                {
                    List<BacnetValue> values = new List<BacnetValue>();
                    while(!ASN1.decode_is_closing_tag(buffer, offset + len))
                    {
                        BacnetValue value;
                        int l = ASN1.bacapp_decode_application_data(buffer, offset + len, apdu_len + offset, object_id.type, (BacnetPropertyIds)property_id, out value);
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

        public static void EncodeTimeSync(EncodeBuffer buffer, DateTime time)
        {
            ASN1.encode_application_date(buffer, time);
            ASN1.encode_application_time(buffer, time);
        }

        public static int DecodeTimeSync(byte[] buffer, int offset, int length, out DateTime dateTime)
        {
            int len = 0;
            byte tag_number;
            uint len_value;
            DateTime d_date, t_date;

            dateTime = new DateTime(1, 1, 1);

            /* date */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE)
                return -1;
            len += ASN1.decode_date(buffer, offset + len, out d_date);
            /* time */
            len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tag_number, out len_value);
            if (tag_number != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME)
                return -1;
            len += ASN1.decode_bacnet_time(buffer, offset + len, out t_date);

            //merge
            dateTime = new DateTime(d_date.Year, d_date.Month, d_date.Day, t_date.Hour, t_date.Minute, t_date.Second, t_date.Millisecond);

            return len;
        }

        public static void EncodeError(EncodeBuffer buffer, BacnetErrorClasses error_class, BacnetErrorCodes error_code)
        {
            ASN1.encode_application_enumerated(buffer, (uint)error_class);
            ASN1.encode_application_enumerated(buffer, (uint)error_code);
        }

        public static int DecodeError(byte[] buffer, int offset, int length, out BacnetErrorClasses error_class, out BacnetErrorCodes error_code)
        {
            int org_offset = offset;
            uint tmp;

            byte tag_number;
            uint len_value_type;
            offset += ASN1.decode_tag_number_and_value(buffer, offset, out tag_number, out len_value_type);
            /* FIXME: we could validate that the tag is enumerated... */
            offset += ASN1.decode_enumerated(buffer, offset, len_value_type, out tmp);
            error_class = (BacnetErrorClasses)tmp;
            offset += ASN1.decode_tag_number_and_value(buffer, offset, out tag_number, out len_value_type);
            /* FIXME: we could validate that the tag is enumerated... */
            offset += ASN1.decode_enumerated(buffer, offset, len_value_type, out tmp);
            error_code = (BacnetErrorCodes)tmp;

            return offset - org_offset;
        }
    }
}
