namespace System.IO.BACnet;

/// <summary>
/// Reason the transaction with the indicated invoke ID is being aborted.
/// </summary>
/// <remarks>
/// Enumerated values 0-63 are reserved for definition by ASHRAE.
/// Enumerated values 64-255 may be used by others.
/// </remarks>
public enum BacnetAbortReason
{
    /// <summary>
    /// This abort reason is returned for a reason other than any of those previously enumerated.
    /// </summary>
    OTHER = 0,

    /// <summary>
    /// A buffer capacity has been exceeded.
    /// </summary>
    BUFFER_OVERFLOW = 1,

    /// <summary>
    /// Generated in response to an APDU that is not expected in the present
    /// state of the Transaction State Machine.
    /// </summary>
    INVALID_APDU_IN_THIS_STATE = 2,

    /// <summary>
    /// The transaction shall be aborted to permit higher priority processing.
    /// </summary>
    PREEMPTED_BY_HIGHER_PRIORITY_TASK = 3,

    /// <summary>
    /// Generated in response to an APDU that has its segmentation bit set to TRUE
    /// when the receiving device does not support segmentation. It is also generated
    /// when a BACnet-ComplexACKPDU is large enough to require segmentation but it
    /// cannot be transmitted because either the transmitting device or the receiving
    /// device does not support segmentation. 
    /// </summary>
    SEGMENTATION_NOT_SUPPORTED = 4,

    /// <summary>
    /// The Transaction is aborted due to receipt of a security error.
    /// </summary>
    SECURITY_ERROR = 5,

    /// <summary>
    /// The transaction is aborted due to receipt of a PDU secured differently
    /// than the original PDU of the transaction.
    /// </summary>
    INSUFFICIENT_SECURITY = 6,

    /// <summary>
    ///  A device receives a request that is segmented, or receives any segment of
    /// a segmented request, where the Proposed Window Size field of the PDU header
    /// is either zero or greater than 127.
    /// </summary>
    WINDOW_SIZE_OUT_OF_RANGE = 7,

    /// <summary>
    /// A device receives a confirmed request but its application layer has
    /// not responded within the published APDU Timeout period.
    /// </summary>
    APPLICATION_EXCEEDED_REPLY_TIME = 8,

    /// <summary>
    /// A device receives a request but cannot start processing because it has run
    /// out of some internal resource. 
    /// </summary>
    OUT_OF_RESOURCES = 9,

    /// <summary>
    /// A transaction state machine timer exceeded the timeout applicable for the
    /// current state, causing the transaction machine to abort the transaction.
    /// </summary>
    TSM_TIMEOUT = 10,

    /// <summary>
    /// An APDU was received from the local application program whose overall
    /// size exceeds the maximum transmittable length or exceeds the maximum
    /// number of segments accepted by the server.
    /// </summary>
    APDU_TOO_LONG = 11
}
