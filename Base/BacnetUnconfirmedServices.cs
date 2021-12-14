namespace System.IO.BACnet;

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
