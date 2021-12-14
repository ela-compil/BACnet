namespace System.IO.BACnet;

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
    MAX_BACNET_SERVICES_SUPPORTED = 41
}
