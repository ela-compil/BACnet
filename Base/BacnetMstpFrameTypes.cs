namespace System.IO.BACnet;

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
    FRAME_TYPE_PROPRIETARY_MAX = 255
}
