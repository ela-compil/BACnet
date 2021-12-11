namespace System.IO.BACnet;

/// <summary>
/// Possible reason for rejecting the PDU.
/// </summary>
/// <remarks>
/// Enumerated values 0-63 are reserved for definition by ASHRAE.
/// Enumerated values 64-255 may be used by others.
/// </remarks>
public enum BacnetRejectReason : byte
{
    /// <summary>
    /// Generated in response to a confirmed request APDU that contains a syntax error
    /// for which an error code has not been explicitly defined.
    /// </summary>
    OTHER = 0,

    /// <summary>
    /// A buffer capacity has been exceeded.
    /// </summary>
    BUFFER_OVERFLOW = 1,

    /// <summary>
    /// Generated in response to a confirmed request APDU that omits a conditional
    /// service argument that should be present or contains a conditional service
    /// argument that should not be present. This condition could also elicit
    /// a Reject PDU with a Reject Reason of <see cref="INVALID_TAG"/>.
    /// </summary>
    INCONSISTENT_PARAMETERS = 2,

    /// <summary>
    /// Generated in response to a confirmed request APDU in which the encoding
    /// of one or more of the service parameters does not follow the correct
    /// type specification. This condition could also elicit a Reject PDU
    /// with a Reject Reason of <see cref="INVALID_TAG"/>.
    /// </summary>
    INVALID_PARAMETER_DATA_TYPE = 3,

    /// <summary>
    /// While parsing a message, an invalid tag was encountered. Since an invalid tag
    /// could confuse the parsing logic, any of the following Reject Reasons may also
    /// be generated in response to a confirmed request containing an invalid tag:
    /// <list type="bullet">   
    /// <item><description><see cref="INCONSISTENT_PARAMETERS"/></description></item>
    /// <item><description><see cref="INVALID_PARAMETER_DATA_TYPE"/></description></item>
    /// <item><description><see cref="MISSING_REQUIRED_PARAMETER"/></description></item>
    /// <item><description><see cref="TOO_MANY_ARGUMENTS"/></description></item>
    /// </list>
    /// </summary>
    INVALID_TAG = 4,

    /// <summary>
    /// Generated in response to a confirmed request APDU that is missing at least one
    /// mandatory service argument. This condition could also elicit a Reject PDU with
    /// a Reject Reason of <see cref="INVALID_TAG"/>.
    /// </summary>
    MISSING_REQUIRED_PARAMETER = 5,

    /// <summary>
    /// Generated in response to a confirmed request APDU that conveys a parameter
    /// whose value is outside the range defined for this service.
    /// </summary>
    PARAMETER_OUT_OF_RANGE = 6,

    /// <summary>
    /// Generated in response to a confirmed request APDU in which the total number
    /// of service arguments is greater than specified for the service. This condition
    /// could also elicit a Reject PDU with a Reject Reason of <see cref="INVALID_TAG"/>.
    /// </summary>
    TOO_MANY_ARGUMENTS = 7,

    /// <summary>
    /// Generated in response to a confirmed request APDU in which one or more of
    /// the service parameters is decoded as an enumeration that is not defined by
    /// the type specification of this parameter.
    /// </summary>
    UNDEFINED_ENUMERATION = 8,

    /// <summary>
    /// Generated in response to a confirmed request APDU in which the Service Choice
    /// field specifies an unknown or unsupported service
    /// </summary>
    RECOGNIZED_SERVICE = 9
}
