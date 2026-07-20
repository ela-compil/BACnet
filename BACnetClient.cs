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
using System.Collections.Concurrent;

namespace System.IO.BACnet;

public delegate void MessageRecievedHandler(IBacnetTransport sender, byte[] buffer, int offset, int msgLength, BacnetAddress remoteAddress);

/// <summary>
/// BACnet network client or server
/// </summary>
public class BacnetClient : IDisposable
{
    private int _retries;
    private int _invokeId;

    private readonly LastSegmentAck _lastSegmentAck = new();
    private uint _writepriority;

    /// <summary>
    /// Dictionary of List of Tuples with sequence-number and byte[] per invoke-id
    /// TODO: invoke-id should be PER (remote) DEVICE!
    /// </summary>
    private Dictionary<byte, List<Tuple<byte, byte[]>>> _segmentsPerInvokeId = new();
    private ConcurrentDictionary<byte, object> _locksPerInvokeId = new();
    private Dictionary<byte, byte> _expectedSegmentsPerInvokeId = new();

    public const int DEFAULT_UDP_PORT = 0xBAC0;
    public const int DEFAULT_TIMEOUT = 1000;
    public const int DEFAULT_RETRIES = 3;

    public IBacnetTransport Transport { get; }
    public ushort VendorId { get; set; } = 260;
    public int Timeout { get; set; }
    public int TransmitTimeout { get; set; } = 30000;
    public BacnetMaxSegments MaxSegments { get; set; } = BacnetMaxSegments.MAX_SEG0;
    public byte ProposedWindowSize { get; set; } = 10;
    public bool ForceWindowSize { get; set; }
    public bool DefaultSegmentationHandling { get; set; } = true;
    public ILogger Log { get; set; } = BacnetLogging.CreateLogger<BacnetClient>();

    /// <summary>
    /// Used as the number of tentatives
    /// </summary>
    public int Retries
    {
        get => _retries;
        set => _retries = Math.Max(1, value);
    }

    public uint WritePriority
    {
        get => _writepriority;
        set { if (value < 17) _writepriority = value; }
    }

    // These members allows to access undecoded buffer by the application
    // layer, when the basic undecoding process is not really able to do the job
    // in particular with application_specific_encoding values
    public byte[] raw_buffer;
    public int raw_offset, raw_length;

    public class Segmentation
    {
        // ReSharper disable InconsistentNaming
        // was public before refactor so can't change this
        public EncodeBuffer buffer;
        public byte sequence_number;
        public byte window_size;
        public byte max_segments;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Max-APDU-length-accepted advertised by the requester whose request
        /// this response answers. Segments are sized to fit it (135 §5.2.1.2);
        /// null means unknown and only our own transport limit applies.
        /// </summary>
        public BacnetMaxAdpu? RequesterMaxAdpu { get; set; }
    }

    private class LastSegmentAck
    {
        private readonly ManualResetEvent _wait = new(false);
        private readonly object _lockObject = new();
        private BacnetAddress _address;
        private byte _invokeId;

        public byte SequenceNumber;
        public byte WindowSize;

        public void Set(BacnetAddress adr, byte invokeId, byte sequenceNumber, byte windowSize)
        {
            lock (_lockObject)
            {
                _address = adr;
                _invokeId = invokeId;
                SequenceNumber = sequenceNumber;
                WindowSize = windowSize;
                _wait.Set();
            }
        }

        public bool Wait(BacnetAddress adr, byte invokeId, int timeout)
        {
            Monitor.Enter(_lockObject);
            while (!adr.Equals(this._address) || this._invokeId != invokeId)
            {
                _wait.Reset();
                Monitor.Exit(_lockObject);
                if (!_wait.WaitOne(timeout)) return false;
                Monitor.Enter(_lockObject);
            }
            Monitor.Exit(_lockObject);
            _address = null;
            return true;
        }
    }

    public BacnetClient(int port = DEFAULT_UDP_PORT, int timeout = DEFAULT_TIMEOUT, int retries = DEFAULT_RETRIES)
        : this(new BacnetIpUdpProtocolTransport(port), timeout, retries)
    {
    }

    public BacnetClient(IBacnetTransport transport, int timeout = DEFAULT_TIMEOUT, int retries = DEFAULT_RETRIES)
    {
        Transport = transport;
        Timeout = timeout;
        Retries = retries;
    }

    public override bool Equals(object obj)
    {
        return Transport.Equals((obj as BacnetClient)?.Transport);
    }

    public override int GetHashCode()
    {
        return Transport.GetHashCode();
    }

    public override string ToString()
    {
        return Transport.ToString();
    }

    public EncodeBuffer GetEncodeBuffer(int startOffset)
    {
        return new EncodeBuffer(new byte[Transport.MaxBufferLength], startOffset);
    }

    public void Start()
    {
        Transport.Start();
        Transport.MessageRecieved += OnRecieve;
        Log.LogInformation("Started communication");
    }

    public delegate void ConfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length);
    public event ConfirmedServiceRequestHandler OnConfirmedServiceRequest;
    public delegate void ReadPropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, BacnetPropertyReference property, BacnetMaxSegments maxSegments);
    public event ReadPropertyRequestHandler OnReadPropertyRequest;
    public delegate void ReadPropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments maxSegments);
    public event ReadPropertyMultipleRequestHandler OnReadPropertyMultipleRequest;
    public delegate void WritePropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, BacnetPropertyValue value, BacnetMaxSegments maxSegments);
    public event WritePropertyRequestHandler OnWritePropertyRequest;
    public delegate void WritePropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, ICollection<BacnetWriteAccessSpecification> properties, BacnetMaxSegments maxSegments);
    public event WritePropertyMultipleRequestHandler OnWritePropertyMultipleRequest;
    public delegate void AtomicWriteFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, bool isStream, BacnetObjectId objectId, int position, uint blockCount, byte[][] blocks, int[] counts, BacnetMaxSegments maxSegments);
    public event AtomicWriteFileRequestHandler OnAtomicWriteFileRequest;
    public delegate void AtomicReadFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, bool isStream, BacnetObjectId objectId, int position, uint count, BacnetMaxSegments maxSegments);
    public event AtomicReadFileRequestHandler OnAtomicReadFileRequest;
    public delegate void SubscribeCOVRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetMaxSegments maxSegments);
    public event SubscribeCOVRequestHandler OnSubscribeCOV;
    public delegate void EventNotificationCallbackHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetEventNotificationData eventData, bool needConfirm);
    public event EventNotificationCallbackHandler OnEventNotify;
    public delegate void SubscribeCOVPropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference monitoredProperty, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement, BacnetMaxSegments maxSegments);
    public event SubscribeCOVPropertyRequestHandler OnSubscribeCOVProperty;
    public delegate void DeviceCommunicationControlRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, uint timeDuration, uint enableDisable, string password, BacnetMaxSegments maxSegments);
    public event DeviceCommunicationControlRequestHandler OnDeviceCommunicationControl;
    public delegate void ReinitializedRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetReinitializedStates state, string password, BacnetMaxSegments maxSegments);
    public event ReinitializedRequestHandler OnReinitializedDevice;
    public delegate void ReadRangeHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, BacnetPropertyReference property, BacnetReadRangeRequestTypes requestType, uint position, DateTime time, int count, BacnetMaxSegments maxSegments);
    public event ReadRangeHandler OnReadRange;
    public delegate void CreateObjectRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments);
    public event CreateObjectRequestHandler OnCreateObjectRequest;
    public delegate void DeleteObjectRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, BacnetMaxSegments maxSegments);
    public event DeleteObjectRequestHandler OnDeleteObjectRequest;
    public delegate void GetAlarmSummaryOrEventInformationRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, bool getEvent, BacnetObjectId objectId, BacnetMaxAdpu maxApdu, BacnetMaxSegments max_segments);
    public event GetAlarmSummaryOrEventInformationRequestHandler OnGetAlarmSummaryOrEventInformation;
    public delegate void AlarmAcknowledgeRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, uint ackProcessIdentifier, BacnetObjectId eventObjectIdentifier, uint eventStateAcked, string ackSource, BacnetGenericTime eventTimeStamp, BacnetGenericTime ackTimeStamp);
    public event AlarmAcknowledgeRequestHandler OnAlarmAcknowledge;

    /// <summary>
    /// Requester's max-APDU-length-accepted of the confirmed request currently
    /// being dispatched on this thread, so that GetSegmentBuffer() called from
    /// inside a request event handler picks it up without every handler
    /// delegate having to carry the value (135 §5.2.1.2: responses must not
    /// exceed the requester's max APDU). Thread-static because requests from
    /// different peers are dispatched concurrently.
    /// </summary>
    [ThreadStatic]
    private static BacnetMaxAdpu? _currentRequestMaxAdpu;

    protected void ProcessConfirmedServiceRequest(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"ConfirmedServiceRequest {service}");

            _currentRequestMaxAdpu = maxAdpu;

            raw_buffer = buffer;
            raw_length = length;
            raw_offset = offset;

            OnConfirmedServiceRequest?.Invoke(this, address, type, service, maxSegments, maxAdpu, invokeId, buffer, offset, length);

            //don't send segmented messages, if client don't want it
            if ((type & BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED) == 0)
                maxSegments = BacnetMaxSegments.MAX_SEG0;

            if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY && OnReadPropertyRequest != null)
            {
                int thsRejectReason;

                if ((thsRejectReason = Services.DecodeReadProperty(buffer, offset, length, out var objectId, out var property)) >= 0)
                {
                    OnReadPropertyRequest(this, address, invokeId, objectId, property, maxSegments);
                }
                else
                {
                    switch (thsRejectReason)
                    {
                        case -1:
                            SendConfirmedServiceReject(address, invokeId, BacnetRejectReason.MISSING_REQUIRED_PARAMETER);
                            break;
                        case -2:
                            SendConfirmedServiceReject(address, invokeId, BacnetRejectReason.INVALID_TAG);
                            break;
                        case -3:
                            SendConfirmedServiceReject(address, invokeId, BacnetRejectReason.TOO_MANY_ARGUMENTS);
                            break;
                    }
                    Log.LogWarning("Couldn't decode DecodeReadProperty");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY && OnWritePropertyRequest != null)
            {
                if (Services.DecodeWriteProperty(address, buffer, offset, length, out var objectId, out var value) >= 0)
                    OnWritePropertyRequest(this, address, invokeId, objectId, value, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    //SendConfirmedServiceReject(adr, invokeId, BacnetRejectReason.OTHER); 
                    Log.LogWarning("Couldn't decode DecodeWriteProperty");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE && OnReadPropertyMultipleRequest != null)
            {
                if (Services.DecodeReadPropertyMultiple(buffer, offset, length, out var properties) >= 0)
                    OnReadPropertyMultipleRequest(this, address, invokeId, properties, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DecodeReadPropertyMultiple");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE && OnWritePropertyMultipleRequest != null)
            {
                if (Services.DecodeWritePropertyMultiple(address, buffer, offset, length, out var properties) >= 0)
                    OnWritePropertyMultipleRequest(this, address, invokeId, properties, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DecodeWritePropertyMultiple");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
            {
                if (Services.DecodeCOVNotifyUnconfirmed(address, buffer, offset, length, out var subscriberProcessIdentifier, out var initiatingDeviceIdentifier, out var monitoredObjectIdentifier, out var timeRemaining, out var values) >= 0)
                    OnCOVNotification(this, address, invokeId, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, true, values, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode COVNotify");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE && OnAtomicWriteFileRequest != null)
            {
                if (Services.DecodeAtomicWriteFile(buffer, offset, length, out var isStream, out var objectId, out var position, out var blockCount, out var blocks, out var counts) >= 0)
                    OnAtomicWriteFileRequest(this, address, invokeId, isStream, objectId, position, blockCount, blocks, counts, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode AtomicWriteFile");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE && OnAtomicReadFileRequest != null)
            {
                if (Services.DecodeAtomicReadFile(buffer, offset, length, out var isStream, out var objectId, out var position, out var count) >= 0)
                    OnAtomicReadFileRequest(this, address, invokeId, isStream, objectId, position, count, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode AtomicReadFile");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV && OnSubscribeCOV != null)
            {
                if (Services.DecodeSubscribeCOV(buffer, offset, length, out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime) >= 0)
                    OnSubscribeCOV(this, address, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, cancellationRequest, issueConfirmedNotifications, lifetime, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode SubscribeCOV");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY && OnSubscribeCOVProperty != null)
            {
                if (Services.DecodeSubscribeProperty(buffer, offset, length, out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var monitoredProperty, out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime, out var covIncrement) >= 0)
                    OnSubscribeCOVProperty(this, address, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, monitoredProperty, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode SubscribeCOVProperty");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL && OnDeviceCommunicationControl != null)
            {
                // DAL
                if (Services.DecodeDeviceCommunicationControl(buffer, offset, length, out var timeDuration, out var enableDisable, out var password) >= 0)
                    OnDeviceCommunicationControl(this, address, invokeId, timeDuration, enableDisable, password, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DeviceCommunicationControl");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE && OnReinitializedDevice != null)
            {
                // DAL
                if (Services.DecodeReinitializeDevice(buffer, offset, length, out var state, out var password) >= 0)
                    OnReinitializedDevice(this, address, invokeId, state, password, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode ReinitializeDevice");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION && OnEventNotify != null) // F. Chaxel
            {
                if (Services.DecodeEventNotifyData(buffer, offset, length, out var eventData) >= 0)
                {
                    OnEventNotify(this, address, invokeId, eventData, true);
                }
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode confirmed Event/Alarm Notification");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE && OnReadRange != null)
            {
                if (Services.DecodeReadRange(buffer, offset, length, out var objectId, out var property, out var requestType, out var position, out var time, out var count) >= 0)
                    OnReadRange(this, address, invokeId, objectId, property, requestType, position, time, count, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode ReadRange");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT && OnCreateObjectRequest != null)
            {
                if (Services.DecodeCreateObject(address, buffer, offset, length, out var objectId, out var values) >= 0)
                    OnCreateObjectRequest(this, address, invokeId, objectId, values, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode CreateObject");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT && OnDeleteObjectRequest != null)
            {
                if (Services.DecodeDeleteObject(buffer, offset, length, out var objectId) >= 0)
                    OnDeleteObjectRequest(this, address, invokeId, objectId, maxSegments);
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode DecodeDeleteObject");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY && OnGetAlarmSummaryOrEventInformation != null)
            {
                // DAL -- added the core code required but since I couldn't test it we just reject this service
                // rejecting it shouldn't be too bad a thing since GetAlarmSummary has been retired anyway...
                // if someone needs it they can uncomment the related code and test.
#if false
                    BacnetObjectId objectId = default(BacnetObjectId);
                    objectId.Type = BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE;
                    if (Services.DecodeAlarmSummaryOrEventRequest(buffer, offset, length, false, ref objectId) >= 0)
                    {
                        OnGetAlarmSummaryOrEventInformation(this, address, invokeId, false, objectId, maxAdpu, maxSegments);
                    }
                    else
                    {
                        // DAL
                        SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                        //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.LogWarning("Couldn't decode GetAlarmSummary");
                    }
#else
                SendConfirmedServiceReject(address, invokeId, BacnetRejectReason.UNRECOGNIZED_SERVICE);
#endif
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION && OnGetAlarmSummaryOrEventInformation != null)
            {
                // DAL
                BacnetObjectId objectId = default;
                objectId.Type = BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE;
                if (Services.DecodeAlarmSummaryOrEventRequest(buffer, offset, length, true, ref objectId) >= 0)
                {
                    OnGetAlarmSummaryOrEventInformation(this, address, invokeId, true, objectId, maxAdpu, maxSegments);
                }
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode GetEventInformation");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM && OnAlarmAcknowledge != null)
            {
                // DAL
                if (Services.DecodeAlarmAcknowledge(buffer, offset, length, out uint ackProcessIdentifier, out BacnetObjectId eventObjectIdentifier, out uint eventStateAcked, out string ackSource, out BacnetGenericTime eventTimeStamp, out BacnetGenericTime ackTimeStamp) >= 0)
                {
                    OnAlarmAcknowledge(this, address, invokeId, ackProcessIdentifier, eventObjectIdentifier, eventStateAcked, ackSource, eventTimeStamp, ackTimeStamp);
                }
                else
                {
                    // DAL
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                    Log.LogWarning("Couldn't decode AlarmAcknowledge");
                }
            }
            else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER && OnPrivateTransfer != null)
            {
                if (Services.DecodePrivateTransfer(buffer, offset, length, out var vendorId, out var serviceNumber, out var serviceParameters) >= 0)
                    OnPrivateTransfer(this, address, invokeId, vendorId, serviceNumber, serviceParameters, true, maxSegments);
                else
                {
                    SendAbort(address, invokeId, BacnetAbortReason.OTHER);
                    Log.LogWarning("Couldn't decode PrivateTransfer");
                }
            }
            else
            {
                // DAL
                SendConfirmedServiceReject(address, invokeId, BacnetRejectReason.UNRECOGNIZED_SERVICE);
                Log.LogDebug($"Confirmed service not handled: {service}");
            }
        }
        catch (Exception ex)
        {
            // DAL
            SendAbort(address, invokeId, BacnetAbortReason.OTHER);
            //ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
            Log.LogError(ex, "Error in ProcessConfirmedServiceRequest");
        }
        finally
        {
            _currentRequestMaxAdpu = null;
        }
    }

    public delegate void UnconfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length);
    public event UnconfirmedServiceRequestHandler OnUnconfirmedServiceRequest;
    public delegate void WhoHasHandler(BacnetClient sender, BacnetAddress adr, int lowLimit, int highLimit, BacnetObjectId? objId, string objName);
    public event WhoHasHandler OnWhoHas;
    public delegate void IamHandler(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxAPDU, BacnetSegmentations segmentation, ushort vendorId);
    public event IamHandler OnIam;
    public delegate void WhoIsHandler(BacnetClient sender, BacnetAddress adr, int lowLimit, int highLimit);
    public event WhoIsHandler OnWhoIs;
    public delegate void TimeSynchronizeHandler(BacnetClient sender, BacnetAddress adr, DateTime dateTime, bool utc);
    public event TimeSynchronizeHandler OnTimeSynchronize;

    //used by both 'confirmed' and 'unconfirmed' notify
    public delegate void COVNotificationHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool needConfirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments);
    public event COVNotificationHandler OnCOVNotification;

    public delegate void PrivateTransferHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, uint vendorId, uint serviceNumber, byte[] serviceParameters, bool needConfirm, BacnetMaxSegments maxSegments);
    public event PrivateTransferHandler OnPrivateTransfer;

    protected void ProcessUnconfirmedServiceRequest(BacnetAddress address, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug("UnconfirmedServiceRequest");
            OnUnconfirmedServiceRequest?.Invoke(this, address, type, service, buffer, offset, length);
            if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM && OnIam != null)
            {
                if (Services.DecodeIamBroadcast(buffer, offset, out var deviceId, out var maxAdpu, out var segmentation, out var vendorId) >= 0)
                    OnIam(this, address, deviceId, maxAdpu, segmentation, vendorId);
                else
                    Log.LogWarning("Couldn't decode IamBroadcast");
            }
            else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS && OnWhoIs != null)
            {
                if (Services.DecodeWhoIsBroadcast(buffer, offset, length, out var lowLimit, out var highLimit) >= 0)
                    OnWhoIs(this, address, lowLimit, highLimit);
                else
                    Log.LogWarning("Couldn't decode WhoIsBroadcast");
            }
            // added by thamersalek
            else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS && OnWhoHas != null)
            {
                if (Services.DecodeWhoHasBroadcast(buffer, offset, length, out var lowLimit, out var highLimit, out var objId, out var objName) >= 0)
                    OnWhoHas(this, address, lowLimit, highLimit, objId, objName);
                else
                    Log.LogWarning("Couldn't decode WhoHasBroadcast");
            }
            else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
            {
                if (Services.DecodeCOVNotifyUnconfirmed(address, buffer, offset, length, out var subscriberProcessIdentifier, out var initiatingDeviceIdentifier, out var monitoredObjectIdentifier, out var timeRemaining, out var values) >= 0)
                    OnCOVNotification(this, address, 0, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, false, values, BacnetMaxSegments.MAX_SEG0);
                else
                    Log.LogWarning("Couldn't decode COVNotifyUnconfirmed");
            }
            else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
            {
                if (Services.DecodeTimeSync(buffer, offset, length, out var dateTime) >= 0)
                    OnTimeSynchronize(this, address, dateTime, false);
                else
                    Log.LogWarning("Couldn't decode TimeSynchronize");
            }
            else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
            {
                if (Services.DecodeTimeSync(buffer, offset, length, out var dateTime) >= 0)
                    OnTimeSynchronize(this, address, dateTime, true);
                else
                    Log.LogWarning("Couldn't decode TimeSynchronize");
            }
            else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION && OnEventNotify != null) // F. Chaxel
            {
                if (Services.DecodeEventNotifyData(buffer, offset, length, out var eventData) >= 0)
                    OnEventNotify(this, address, 0, eventData, false);
                else
                    Log.LogWarning("Couldn't decode unconfirmed Event/Alarm Notification");
            }
            else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_PRIVATE_TRANSFER && OnPrivateTransfer != null)
            {
                if (Services.DecodePrivateTransfer(buffer, offset, length, out var vendorId, out var serviceNumber, out var serviceParameters) >= 0)
                    OnPrivateTransfer(this, address, 0, vendorId, serviceNumber, serviceParameters, false, BacnetMaxSegments.MAX_SEG0);
                else
                    Log.LogWarning("Couldn't decode unconfirmed PrivateTransfer");
            }
            else
            {
                Log.LogDebug($"Unconfirmed service not handled: {service}");
                // SendUnConfirmedServiceReject(adr); ? exists ?
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error in ProcessUnconfirmedServiceRequest");
        }
    }

    public delegate void SimpleAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength);
    public event SimpleAckHandler OnSimpleAck;

    protected void ProcessSimpleAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received SimpleAck for {service}");
            OnSimpleAck?.Invoke(this, adr, type, service, invokeId, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error in ProcessSimpleAck");
        }
    }

    public delegate void ComplexAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length);
    public event ComplexAckHandler OnComplexAck;

    protected void ProcessComplexAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received ComplexAck for {service}");
            OnComplexAck?.Invoke(this, adr, type, service, invokeId, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, $"Error in {nameof(ProcessComplexAck)}");
        }
    }

    public delegate void ErrorHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, byte[] buffer, int offset, int length);
    public event ErrorHandler OnError;

    protected void ProcessError(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        try
        {
            if (Services.DecodeError(buffer, offset, out var errorClass, out var errorCode) < 0)
            {
                Log.LogWarning("Couldn't decode received Error");
                return;
            }

            Log.LogDebug($"Received Error {errorClass} {errorCode}");
            OnError?.Invoke(this, adr, type, service, invokeId, errorClass, errorCode, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, $"Error in {nameof(ProcessError)}");
        }
    }

    public delegate void AbortHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetAbortReason reason, byte[] buffer, int offset, int length);
    public event AbortHandler OnAbort;

    protected void ProcessAbort(BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetAbortReason reason, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received Abort, reason: {reason}");
            OnAbort?.Invoke(this, adr, type, invokeId, reason, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error in ProcessAbort");
        }
    }

    public delegate void RejectHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetRejectReason reason, byte[] buffer, int offset, int length);
    public event RejectHandler OnReject;

    protected void ProcessReject(BacnetAddress adr, BacnetPduTypes type, byte invokeId, BacnetRejectReason reason, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug($"Received Reject, reason: {reason}");
            OnReject?.Invoke(this, adr, type, invokeId, reason, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error in ProcessReject");
        }
    }

    public delegate void SegmentAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length);
    public event SegmentAckHandler OnSegmentAck;

    protected void ProcessSegmentAck(BacnetAddress adr, BacnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length)
    {
        try
        {
            Log.LogDebug("Received SegmentAck");
            OnSegmentAck?.Invoke(this, adr, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error in ProcessSegmentAck");
        }
    }

    public delegate void SegmentHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length);
    public event SegmentHandler OnSegment;

    private void ProcessSegment(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, bool server, byte sequenceNumber, byte proposedWindowNumber, byte[] buffer, int offset, int length)
    {
        lock (_locksPerInvokeId.GetOrAdd(invokeId, () => new object()))
        {
            ProcessSegmentLocked(address, type, service, invokeId, maxSegments, maxAdpu, server, sequenceNumber,
                proposedWindowNumber, buffer, offset, length);
        }
    }

    private void ProcessSegmentLocked(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service,
        byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, bool server, byte sequenceNumber,
        byte proposedWindowNumber, byte[] buffer, int offset, int length)
    {
        Log.LogTrace($@"Processing Segment #{sequenceNumber} of invoke-id #{invokeId}");

        if (!_segmentsPerInvokeId.ContainsKey(invokeId))
            _segmentsPerInvokeId[invokeId] = new List<Tuple<byte, byte[]>>();

        if (!_expectedSegmentsPerInvokeId.ContainsKey(invokeId))
            _expectedSegmentsPerInvokeId[invokeId] = byte.MaxValue;

        var moreFollows = (type & BacnetPduTypes.MORE_FOLLOWS) == BacnetPduTypes.MORE_FOLLOWS;

        if (!moreFollows)
            _expectedSegmentsPerInvokeId[invokeId] = (byte)(sequenceNumber + 1);

        //send ACK
        if (sequenceNumber % proposedWindowNumber == 0 || !moreFollows)
        {
            if (ForceWindowSize)
                proposedWindowNumber = ProposedWindowSize;

            SegmentAckResponse(adr, false, server, invokeId, sequenceNumber, proposedWindowNumber);
        }

        //Send on
        OnSegment?.Invoke(this, adr, type, service, invokeId, maxSegments, maxAdpu, sequenceNumber, buffer, offset, length);

        //default segment assembly. We run this seperately from the above handler, to make sure that it comes after!
        if (DefaultSegmentationHandling)
            PerformDefaultSegmentHandling(adr, type, service, invokeId, maxSegments, maxAdpu, sequenceNumber, buffer, offset, length);
    }

    /// <summary>
    /// This is a simple handling that stores all segments in memory and assembles them when done
    /// </summary>
    private void PerformDefaultSegmentHandling(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length)
    {
        var segments = _segmentsPerInvokeId[invokeId];

        if (sequenceNumber == 0)
        {
            //copy buffer + encode new adpu header
            type &= ~BacnetPduTypes.SEGMENTED_MESSAGE;
            var confirmedServiceRequest = (type & BacnetPduTypes.PDU_TYPE_MASK) == BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST;
            var adpuHeaderLen = confirmedServiceRequest ? 4 : 3;

            var copy = new byte[length + adpuHeaderLen];
            Array.Copy(buffer, offset, copy, adpuHeaderLen, length);
            var encodedBuffer = new EncodeBuffer(copy, 0);

            if (confirmedServiceRequest)
                APDU.EncodeConfirmedServiceRequest(encodedBuffer, type, service, maxSegments, maxAdpu, invokeId);
            else
                APDU.EncodeComplexAck(encodedBuffer, type, service, invokeId);

            segments.Add(Tuple.Create(sequenceNumber, copy)); // doesn't include BVLC or NPDU
        }
        else
        {
            //copy only content part
            segments.Add(Tuple.Create(sequenceNumber, buffer.Skip(offset).Take(length).ToArray()));
        }

        //process when finished
        if (segments.Count < _expectedSegmentsPerInvokeId[invokeId])
            return;

        //assemble whole part
        var apduBuffer = segments.OrderBy(s => s.Item1).SelectMany(s => s.Item2).ToArray();
        segments.Clear();
        _expectedSegmentsPerInvokeId[invokeId] = byte.MaxValue;

        //process
        ProcessApdu(adr, type, apduBuffer, 0, apduBuffer.Length);
    }

    private void ProcessApdu(BacnetAddress adr, BacnetPduTypes type, byte[] buffer, int offset, int length)
    {
		switch (type & BacnetPduTypes.PDU_TYPE_MASK)
        {
            case BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST:
                {
                    var apduHeaderLen = APDU.DecodeUnconfirmedServiceRequest(buffer, offset, out type, out var service);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessUnconfirmedServiceRequest(adr, type, service, buffer, offset, length);
                }
                break;

            case BacnetPduTypes.PDU_TYPE_SIMPLE_ACK:
                {
                    var apduHeaderLen = APDU.DecodeSimpleAck(buffer, offset, out type, out var service, out var invokeId);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessSimpleAck(adr, type, service, invokeId, buffer, offset, length);
                }
                break;

            case BacnetPduTypes.PDU_TYPE_COMPLEX_ACK:
                {
                    var apduHeaderLen = APDU.DecodeComplexAck(buffer, offset, out type, out var service, out var invokeId,
                        out var sequenceNumber, out var proposedWindowNumber);

                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) == 0) //don't process segmented messages here
                    {
                        ProcessComplexAck(adr, type, service, invokeId, buffer, offset, length);
                    }
                    else
                    {
                        ProcessSegment(adr, type, service, invokeId, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU50, false,
                            sequenceNumber, proposedWindowNumber, buffer, offset, length);
                    }
                }
                break;

            case BacnetPduTypes.PDU_TYPE_SEGMENT_ACK:
                {
                    var apduHeaderLen = APDU.DecodeSegmentAck(buffer, offset, out type, out var originalInvokeId,
                        out var sequenceNumber, out var actualWindowSize);

                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    _lastSegmentAck.Set(adr, originalInvokeId, sequenceNumber, actualWindowSize);
                    ProcessSegmentAck(adr, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
                }
                break;

            case BacnetPduTypes.PDU_TYPE_ERROR:
                {
                    var apduHeaderLen = APDU.DecodeError(buffer, offset, out type, out var service, out var invokeId);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessError(adr, type, service, invokeId, buffer, offset, length);
                }
                break;

            case BacnetPduTypes.PDU_TYPE_ABORT:
                {
                    var apduHeaderLen = APDU.DecodeAbort(buffer, offset, out type, out var invokeId, out var reason);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessAbort(adr, type, invokeId, reason, buffer, offset, length);
                }
                break;

            case BacnetPduTypes.PDU_TYPE_REJECT:
                {
                    var apduHeaderLen = APDU.DecodeReject(buffer, offset, out type, out var invokeId, out var reason);
                    offset += apduHeaderLen;
                    length -= apduHeaderLen;
                    ProcessReject(adr, type, invokeId, reason, buffer, offset, length);
                }
                break;

            case BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                {
                    var apduHeaderLen = APDU.DecodeConfirmedServiceRequest(buffer, offset, out type, out var service,
                        out var maxSegments, out var maxAdpu, out var invokeId, out var sequenceNumber, out var proposedWindowNumber);

                    offset += apduHeaderLen;
                    length -= apduHeaderLen;

                    if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) == 0) //don't process segmented messages here
                    {
                        ProcessConfirmedServiceRequest(adr, type, service, maxSegments, maxAdpu, invokeId, buffer, offset, length);
                    }
                    else
                    {
                        ProcessSegment(adr, type, service, invokeId, maxSegments, maxAdpu, true, sequenceNumber, proposedWindowNumber, buffer, offset, length);
                    }
                }
                break;

            default:
                Log.LogWarning($"Something else arrived: {type}");
                break;
        }
    }

    // DAL
    public void SendNetworkMessage(BacnetAddress adr, byte[] buffer, int bufLen, BacnetNetworkMessageTypes messageType, ushort vendorId = 0)
    {
        if (adr == null)
        {
            adr = Transport.GetBroadcastAddress();
        }
        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BacnetNpduControls.NetworkLayerMessage, adr, null, 255, messageType, vendorId);
        b.Add(buffer, bufLen);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }
    public void SendIAmRouterToNetwork(ushort[] networks)
    {
        var b = GetEncodeBuffer(0);
        for (int i = 0; i < networks.Length; i++)
        {
            ASN1.encode_unsigned16(b, networks[i]);
        }
        SendNetworkMessage(null, b.buffer, b.offset, BacnetNetworkMessageTypes.NETWORK_MESSAGE_I_AM_ROUTER_TO_NETWORK);
    }

    public void SendInitializeRoutingTableAck(BacnetAddress adr, ushort[] networks)
    {
        var b = GetEncodeBuffer(0);
        if (networks != null)
        {
            for (int i = 0; i < networks.Length; i++)
            {
                ASN1.encode_unsigned16(b, networks[i]);
            }
        }
        SendNetworkMessage(adr, b.buffer, b.offset, BacnetNetworkMessageTypes.NETWORK_MESSAGE_INIT_RT_TABLE_ACK);
    }
    public void SendRejectToNetwork(BacnetAddress adr, ushort[] networks)
    {
        var b = GetEncodeBuffer(0);
        /* Sending our DNET doesn't make a lot of sense, does it? */
        for (int i = 0; i < networks.Length; i++)
        {
            ASN1.encode_unsigned16(b, networks[i]);
        }
        SendNetworkMessage(adr, b.buffer, b.offset, BacnetNetworkMessageTypes.NETWORK_MESSAGE_REJECT_MESSAGE_TO_NETWORK);
    }
    public delegate void NetworkMessageHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, BacnetNetworkMessageTypes npduMessageType, byte[] buffer, int offset, int messageLength);
    public event NetworkMessageHandler OnNetworkMessage;
    public delegate void WhoIsRouterToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event WhoIsRouterToNetworkHandler OnWhoIsRouterToNetworkMessage;
    public delegate void IAmRouterToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event IAmRouterToNetworkHandler OnIAmRouterToNetworkMessage;
    public delegate void ICouldBeRouterToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event ICouldBeRouterToNetworkHandler OnICouldBeRouterToNetworkMessage;
    public delegate void RejectMessageToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event RejectMessageToNetworkHandler OnRejectMessageToNetworkMessage;
    public delegate void RouterBusyToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event RouterBusyToNetworkHandler OnRouterBusyToNetworkMessage;
    public delegate void RouterAvailableToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event RouterAvailableToNetworkHandler OnRouterAvailableToNetworkMessage;
    public delegate void InitRtTableToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event InitRtTableToNetworkHandler OnInitRtTableToNetworkMessage;
    public delegate void InitRtTableAckToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event InitRtTableAckToNetworkHandler OnInitRtTableAckToNetworkMessage;
    public delegate void EstablishConnectionToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event EstablishConnectionToNetworkHandler OnEstablishConnectionToNetworkMessage;
    public delegate void DisconnectConnectionToNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event DisconnectConnectionToNetworkHandler OnDisconnectConnectionToNetworkMessage;
    public delegate void UnrecognizedNetworkHandler(BacnetClient sender, BacnetAddress adr, BacnetNpduControls npduFunction, byte[] buffer, int offset, int messageLength);
    public event UnrecognizedNetworkHandler OnUnrecognizedNetworkMessage;

    private void ProcessNetworkMessage(BacnetAddress adr, BacnetNpduControls npduFunction, BacnetNetworkMessageTypes npduMessageType, byte[] buffer, int offset, int messageLength)
    {
        // DAL I don't want to make a generic router, but I do want to put in enough infrastructure
        // that I can build on it to route multiple devices in the normal bacnet way.
        OnNetworkMessage?.Invoke(this, adr, npduFunction, npduMessageType, buffer, offset, messageLength);
        switch (npduMessageType)
        {
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK:
                OnWhoIsRouterToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_I_AM_ROUTER_TO_NETWORK:
                OnIAmRouterToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_I_COULD_BE_ROUTER_TO_NETWORK:
                OnICouldBeRouterToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_REJECT_MESSAGE_TO_NETWORK:
                OnRejectMessageToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_ROUTER_BUSY_TO_NETWORK:
                OnRouterBusyToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_ROUTER_AVAILABLE_TO_NETWORK:
                OnRouterAvailableToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_INIT_RT_TABLE:
                OnInitRtTableToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_INIT_RT_TABLE_ACK:
                OnInitRtTableAckToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_ESTABLISH_CONNECTION_TO_NETWORK:
                OnEstablishConnectionToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            case BacnetNetworkMessageTypes.NETWORK_MESSAGE_DISCONNECT_CONNECTION_TO_NETWORK:
                OnDisconnectConnectionToNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
            default:
                /* An unrecognized message is bad; send an error response. */
                OnUnrecognizedNetworkMessage?.Invoke(this, adr, npduFunction, buffer, offset, messageLength);
                break;
        }
    }

    private void OnRecieve(IBacnetTransport sender, byte[] buffer, int offset, int msgLength, BacnetAddress remoteAddress)
    {
        try
        {
            if (Transport == null)
                return; //we're disposed 

            if (msgLength <= 0)
                return;

            // parse
            var npduLen = NPDU.Decode(buffer, offset, out var npduFunction, out var destination, out var source, out _, out var npduMessageType, out _);

            // Modif FC
            remoteAddress.RoutedSource = source;

            // DAL
            remoteAddress.RoutedDestination = destination;

            if (npduLen <= 0)
                return;

            offset += npduLen;
            msgLength -= npduLen;

            if (msgLength < 0) // could be 0 for an already parsed
                return;

            if (npduFunction.HasFlag(BacnetNpduControls.NetworkLayerMessage))
            {
                Log.LogInformation("Network Layer message received");
                // DAL
                ProcessNetworkMessage(remoteAddress, npduFunction, npduMessageType, buffer, offset, msgLength);
                return;
            }

            if (msgLength <= 0)
                return;

            var apduType = APDU.GetDecodedType(buffer, offset);
            ProcessApdu(remoteAddress, apduType, buffer, offset, msgLength);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error in OnRecieve");
        }
    }

    // Modif FC
    public void RegisterAsForeignDevice(string bbmdIp, short ttl, int port = DEFAULT_UDP_PORT)
    {
        try
        {
            var ep = new IPEndPoint(IPAddress.Parse(bbmdIp), port);
            var sent = false;

            switch (Transport)
            {
                case BacnetIpUdpProtocolTransport t:
                    sent = t.SendRegisterAsForeignDevice(ep, ttl);
                    break;

                case BacnetIpV6UdpProtocolTransport t:
                    sent = t.SendRegisterAsForeignDevice(ep, ttl);
                    break;
            }

            if (sent)
                Log.LogDebug($"Sending Register as a Foreign Device to {bbmdIp}");
            else
                Log.LogWarning("The given address do not match with the IP version");
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error on RegisterAsForeignDevice (Wrong Transport, not IP ?)");
        }
    }

    public void RemoteWhoIs(string bbmdIP, int port = DEFAULT_UDP_PORT, int lowLimit = -1, int highLimit = -1, BacnetAddress source = null)
    {
        try
        {
            var ep = new IPEndPoint(IPAddress.Parse(bbmdIP), port);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            var broadcast = Transport.GetBroadcastAddress();
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast, source);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
            Services.EncodeWhoIsBroadcast(b, lowLimit, highLimit);

            var sent = false;

            switch (Transport)
            {
                case BacnetIpUdpProtocolTransport t:
                    sent = t.SendRemoteWhois(b.buffer, ep, b.offset);
                    break;

                case BacnetIpV6UdpProtocolTransport t:
                    sent = t.SendRemoteWhois(b.buffer, ep, b.offset);
                    break;
            }

            if (sent)
                Log.LogDebug($"Sending Remote Whois to {bbmdIP}");
            else
                Log.LogWarning("The given address do not match with the IP version");
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Error on Sending Whois to remote BBMD (Wrong Transport, not IP ?)");
        }

    }

    public void WhoIs(int lowLimit = -1, int highLimit = -1, BacnetAddress receiver = null, BacnetAddress source = null)
    {
        BacnetAddress npduDestination;
        if (receiver == null)
        {
            // _receiver could be an unicast @ : for direct acces
            // usefull on BIP for a known IP:Port, unknown device Id
            receiver = Transport.GetBroadcastAddress();
            npduDestination = receiver;
            Log.LogDebug("Broadcasting WhoIs");
        }
        else
        {
            // a receiver behind a BACnet router: address the NPDU to its
            // network/MAC while the frame goes to the router that fronts it
            npduDestination = receiver.RoutedSource ?? receiver;
            Log.LogDebug($"Sending WhoIs to {receiver.ToString(false)}");
        }

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, npduDestination, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
        Services.EncodeWhoIsBroadcast(b, lowLimit, highLimit);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
    }

    public void Iam(uint deviceId, BacnetSegmentations segmentation = BacnetSegmentations.SEGMENTATION_BOTH, BacnetAddress receiver = null, BacnetAddress source = null)
    {
        BacnetAddress npduDestination;
        if (receiver == null)
        {
            receiver = Transport.GetBroadcastAddress();
            npduDestination = receiver;
            Log.LogDebug($"Broadcasting Iam {deviceId}");
        }
        else
        {
            // answering a Who-Is that came through a BACnet router: the NPDU
            // destination is the original source network/address while the
            // frame itself goes back to the router that forwarded it, so the
            // requester receives the I-Am as 135 §16.10.4 requires
            npduDestination = receiver.RoutedSource ?? receiver;
            Log.LogDebug($"Sending Iam {deviceId} to {receiver.ToString(false)}");
        }

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, npduDestination, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);
        Services.EncodeIamBroadcast(b, deviceId, (uint)GetMaxApdu(), segmentation, VendorId);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
    }

    public void WhoHas(BacnetObjectId objId, int lowLimit = -1, int highLimit = -1, BacnetAddress receiver = null, BacnetAddress source = null)
    {
        WhoHasCore(objId, null, lowLimit, highLimit, receiver, source);
    }

    public void WhoHas(string objName, int lowLimit = -1, int highLimit = -1, BacnetAddress receiver = null, BacnetAddress source = null)
    {
        WhoHasCore(null, objName, lowLimit, highLimit, receiver, source);
    }

    private void WhoHasCore(BacnetObjectId? objId, string objName, int lowLimit, int highLimit, BacnetAddress receiver, BacnetAddress source)
    {
        BacnetAddress npduDestination;
        if (receiver == null)
        {
            receiver = Transport.GetBroadcastAddress();
            npduDestination = receiver;
            Log.LogDebug($"Broadcasting WhoHas {objId?.ToString() ?? objName}");
        }
        else
        {
            // a receiver behind a BACnet router: address the NPDU to its
            // network/MAC while the frame goes to the router that fronts it
            npduDestination = receiver.RoutedSource ?? receiver;
            Log.LogDebug($"Sending WhoHas {objId?.ToString() ?? objName} to {receiver.ToString(false)}");
        }

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, npduDestination, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS);
        Services.EncodeWhoHasBroadcast(b, lowLimit, highLimit, objId, objName);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
    }

    // ReSharper disable once InconsistentNaming
    // note: receiver comes after source (unlike Iam/WhoIs/WhoHas) so that
    // pre-4.0 positional `source` callers keep compiling with the same
    // meaning; harmonize the order in 5.0 alongside the namespace rename
    public void IHave(BacnetObjectId deviceId, BacnetObjectId objId, string objName, BacnetAddress source = null, BacnetAddress receiver = null)
    {
        BacnetAddress npduDestination;
        if (receiver == null)
        {
            receiver = Transport.GetBroadcastAddress();
            npduDestination = receiver;
            Log.LogDebug($"Broadcasting IHave {objName} {objId}");
        }
        else
        {
            // answering a Who-Has that came through a BACnet router: the NPDU
            // destination is the original source network/address while the
            // frame goes back to the router that forwarded it (135 §16.9)
            npduDestination = receiver.RoutedSource ?? receiver;
            Log.LogDebug($"Sending IHave {objName} {objId} to {receiver.ToString(false)}");
        }

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, npduDestination, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_HAVE);
        Services.EncodeIhaveBroadcast(b, deviceId, objId, objName);

        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
    }

    public void SendUnconfirmedEventNotification(BacnetAddress adr, BacnetEventNotificationData eventData, BacnetAddress source = null)
    {
        Log.LogDebug($"Sending Event Notification {eventData.eventType} {eventData.eventObjectIdentifier}");

        var b = GetEncodeBuffer(Transport.HeaderLength);
        // adr.RoutedSource: peers behind a BACnet router get the NPDU addressed
        // to their network/MAC while the frame goes to the fronting router
        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource ?? adr, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION);
        Services.EncodeEventNotifyUnconfirmed(b, eventData);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SendUnconfirmedPrivateTransfer(BacnetAddress adr, uint vendorId, uint serviceNumber, byte[] serviceParameters = null, BacnetAddress source = null)
    {
        Log.LogDebug($"Sending UnconfirmedPrivateTransfer vendor {vendorId} service {serviceNumber}");

        var b = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource ?? adr, source);
        APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_PRIVATE_TRANSFER);
        Services.EncodePrivateTransferUnconfirmed(b, vendorId, serviceNumber, serviceParameters);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SendConfirmedServiceReject(BacnetAddress adr, byte invokeId, BacnetRejectReason reason)
    {
        Log.LogDebug($"Sending Service reject: {reason}");

        var b = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeError(b, BacnetPduTypes.PDU_TYPE_REJECT, (BacnetConfirmedServices)reason, invokeId);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SendAbort(BacnetAddress adr, byte invokeId, BacnetAbortReason reason)
    {
        // DAL
        Log.LogDebug($"Sending Service reject: {reason}");

        var b = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeError(b, BacnetPduTypes.PDU_TYPE_ABORT, (BacnetConfirmedServices)reason, invokeId);
        Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SynchronizeTime(BacnetAddress adr, DateTime dateTime, BacnetAddress source = null)
    {
        Log.LogDebug($"Sending Time Synchronize: {dateTime} {dateTime.Kind.ToString().ToUpper()}");

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource ?? adr, source);
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, dateTime.Kind == DateTimeKind.Utc
                ? BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION
                : BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION);
        Services.EncodeTimeSync(buffer, dateTime);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }


    public int GetMaxApdu()
    {
        return GetMaxApdu(Transport.MaxAdpuLength);
    }
    // DAL
    public int GetMaxApdu(BacnetMaxAdpu apduLength)
    {
        int maxAPDU;
        switch (apduLength)
        {
            case BacnetMaxAdpu.MAX_APDU1476:
                maxAPDU = 1476;
                break;
            case BacnetMaxAdpu.MAX_APDU1024:
                maxAPDU = 1024;
                break;
            case BacnetMaxAdpu.MAX_APDU480:
                maxAPDU = 480;
                break;
            case BacnetMaxAdpu.MAX_APDU206:
                maxAPDU = 206;
                break;
            case BacnetMaxAdpu.MAX_APDU128:
                maxAPDU = 128;
                break;
            case BacnetMaxAdpu.MAX_APDU50:
                maxAPDU = 50;
                break;
            default:
                throw new NotImplementedException();
        }

        //max udp payload IRL seems to differ from the expectations in BACnet
        //so we have to adjust it. (In order to fulfill the standard)
        const int maxNPDUHeaderLength = 4;       //usually it's '2', but it can also be more than '4'. Beware!
        return Math.Min(maxAPDU, Transport.MaxBufferLength - Transport.HeaderLength - maxNPDUHeaderLength);
    }

    public int GetFileBufferMaxSize()
    {
        //6 should be the max_apdu_header_length for Confirmed (with segmentation)
        //12 should be the max_atomic_write_file
        return GetMaxApdu() - 18;
    }

    public bool WriteFileRequest(BacnetAddress adr, BacnetObjectId objectId, ref int position, int count, byte[] fileBuffer, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginWriteFileRequest(adr, objectId, position, count, fileBuffer, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWriteFileRequest(result, out position, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginWriteFileRequest(BacnetAddress adr, BacnetObjectId objectId, int position, int count, byte[] fileBuffer, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending AtomicWriteFileRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAtomicWriteFile(buffer, true, objectId, position, 1, new[] { fileBuffer }, new[] { count });

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndWriteFileRequest(IAsyncResult result, out int position, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            //decode
            if (Services.DecodeAtomicWriteFileAcknowledge(res.Result, 0, res.Result.Length, out _, out position) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            position = -1;
        }

        res.Dispose();
    }

    public IAsyncResult BeginReadFileRequest(BacnetAddress adr, BacnetObjectId objectId, int position, uint count, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending AtomicReadFileRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        //encode
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAtomicReadFile(buffer, true, objectId, position, count);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReadFileRequest(IAsyncResult result, out uint count, out int position, out bool endOfFile, out byte[] fileBuffer, out int fileBufferOffset, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            //decode
            if (Services.DecodeAtomicReadFileAcknowledge(res.Result, 0, res.Result.Length, out endOfFile, out _, out position, out count, out fileBuffer, out fileBufferOffset) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            count = 0;
            endOfFile = true;
            position = -1;
            fileBufferOffset = -1;
            fileBuffer = new byte[0];
        }

        res.Dispose();
    }

    public bool ReadFileRequest(BacnetAddress adr, BacnetObjectId objectId, ref int position, ref uint count, out bool endOfFile, out byte[] fileBuffer, out int fileBufferOffset, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginReadFileRequest(adr, objectId, position, count, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadFileRequest(result, out count, out position, out endOfFile, out fileBuffer, out fileBufferOffset, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        position = -1;
        count = 0;
        fileBuffer = null;
        endOfFile = true;
        fileBufferOffset = -1;
        return false;
    }

    // Read range by postion
    public IAsyncResult BeginReadRangeRequest(BacnetAddress adr, BacnetObjectId objectId, uint idxBegin, uint quantity, bool waitForTransmit, byte invokeId = 0)
    {
        return BeginReadRangeRequestCore(adr, objectId, BacnetReadRangeRequestTypes.RR_BY_POSITION, DateTime.Now, idxBegin, quantity, waitForTransmit, invokeId);
    }

    // Read range by start time
    public IAsyncResult BeginReadRangeRequest(BacnetAddress adr, BacnetObjectId objectId, DateTime readFrom, uint quantity, bool waitForTransmit, byte invokeId = 0)
    {
        return BeginReadRangeRequestCore(adr, objectId, BacnetReadRangeRequestTypes.RR_BY_TIME, readFrom, 1, quantity, waitForTransmit, invokeId);
    }

    private IAsyncResult BeginReadRangeRequestCore(BacnetAddress adr, BacnetObjectId objectId, BacnetReadRangeRequestTypes bacnetReadRangeRequestTypes, DateTime readFrom, uint idxBegin, uint quantity, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending ReadRangeRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        //encode
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadRange(buffer, objectId, (uint)BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL, bacnetReadRangeRequestTypes, idxBegin, readFrom, (int)quantity);
        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // Fc
    public void EndReadRangeRequest(IAsyncResult result, out byte[] trendbuffer, out uint itemCount, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        itemCount = 0;
        trendbuffer = null;

        ex = res.Error;
        if (ex == null && !res.WaitForDone(40 * 1000))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            itemCount = Services.DecodeReadRangeAcknowledge(res.Result, 0, res.Result.Length, out trendbuffer);
            if (itemCount == 0)
                ex = new Exception("Decode");
        }

        res.Dispose();
    }

    // Fc
    public bool ReadRangeRequest(BacnetAddress adr, BacnetObjectId objectId, DateTime readFrom, ref uint quantity, out byte[] range, byte invokeId = 0)
    {
        return ReadRangeRequestCore(BacnetReadRangeRequestTypes.RR_BY_TIME, adr, objectId, 1, readFrom, ref quantity, out range, invokeId);
    }
    public bool ReadRangeRequest(BacnetAddress adr, BacnetObjectId objectId, uint idxBegin, ref uint quantity, out byte[] range, byte invokeId = 0)
    {
        return ReadRangeRequestCore(BacnetReadRangeRequestTypes.RR_BY_POSITION, adr, objectId, idxBegin, DateTime.Now, ref quantity, out range, invokeId);
    }

    private bool ReadRangeRequestCore(BacnetReadRangeRequestTypes requestType, BacnetAddress adr, BacnetObjectId objectId, uint idxBegin, DateTime readFrom, ref uint quantity, out byte[] range, byte invokeId = 0)
    {
        Func<IAsyncResult> getResult;
        uint quantityCopy = quantity;
        switch (requestType)
        {
            case BacnetReadRangeRequestTypes.RR_BY_TIME:
                getResult = () => BeginReadRangeRequest(adr, objectId, readFrom, quantityCopy, true, invokeId);
                break;

            case BacnetReadRangeRequestTypes.RR_BY_POSITION:
                getResult = () => BeginReadRangeRequest(adr, objectId, idxBegin, quantityCopy, true, invokeId);
                break;

            default:
                throw new NotImplementedException($"BacnetReadRangeRequestTypes-Type {requestType} not supported in {nameof(ReadRangeRequestCore)}!");
        }

        range = null;
        using (var result = getResult() as BacnetAsyncResult)
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadRangeRequest(result, out range, out quantity, out var ex); // quantity read could be less than demanded
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public bool SubscribeCOVRequest(BacnetAddress adr, BacnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginSubscribeCOVRequest(adr, objectId, subscribeId, cancel, issueConfirmedNotifications, lifetime, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndSubscribeCOVRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginSubscribeCOVRequest(BacnetAddress adr, BacnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending SubscribeCOVRequest {objectId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeSubscribeCOV(buffer, subscribeId, objectId, cancel, issueConfirmedNotifications, lifetime);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndSubscribeCOVRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.WaitForDone(Timeout) ? res.Error : new Exception("Wait Timeout");
        res.Dispose();
    }

    // DAL
    public bool SendConfirmedEventNotificationRequest(BacnetAddress adr, BacnetEventNotificationData eventData, byte invokeId = 0, BacnetAddress source = null)
    {
        using (var result = (BacnetAsyncResult)BeginSendConfirmedEventNotificationRequest(adr, eventData, true, invokeId, source))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndSendConfirmedEventNotificationRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    // DAL
    public IAsyncResult BeginSendConfirmedEventNotificationRequest(BacnetAddress adr, BacnetEventNotificationData eventData, bool waitForTransmit, byte invokeId = 0, BacnetAddress source = null)
    {
        Log.LogDebug($"Sending Confirmed Event Notification {eventData.eventType} {eventData.eventObjectIdentifier}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr, source);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeEventNotifyConfirmed(buffer, eventData);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // DAL
    public void EndSendConfirmedEventNotificationRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool SubscribePropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference monitoredProperty, uint subscribeId, bool cancel, bool issueConfirmedNotifications, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginSubscribePropertyRequest(adr, objectId, monitoredProperty, subscribeId, cancel, issueConfirmedNotifications, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndSubscribePropertyRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginSubscribePropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference monitoredProperty, uint subscribeId, bool cancel, bool issueConfirmedNotifications, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending SubscribePropertyRequest {objectId}.{monitoredProperty}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeSubscribeProperty(buffer, subscribeId, objectId, cancel, issueConfirmedNotifications, 0, monitoredProperty, false, 0f);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndSubscribePropertyRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool ReadPropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, out IList<BacnetValue> valueList, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        using (var result = (BacnetAsyncResult)BeginReadPropertyRequest(adr, objectId, propertyId, true, invokeId, arrayIndex))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadPropertyRequest(result, out valueList, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        valueList = null;
        return false;
    }

    // Awaits a request already put on the wire by a Begin* call, honouring the same per-attempt Timeout
    // and Retries as the synchronous request methods, but without blocking a thread while the reply is
    // outstanding. Returns false if every attempt timed out; device-level errors surface through End*.
    private async Task<bool> SendRequestAsync(BacnetAsyncResult result, CancellationToken cancellationToken)
    {
        for (var r = 0; r < _retries; r++)
        {
            if (await result.GetResultOrTimeout(Timeout, cancellationToken).ConfigureAwait(false))
                return true;
            if (r < _retries - 1)
                result.Resend();
        }
        return false;
    }

    public Task<IList<BacnetValue>> ReadPropertyAsync(BacnetAddress address, BacnetObjectTypes objType, uint objInstance,
        BacnetPropertyIds propertyId, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        var objectId = new BacnetObjectId(objType, objInstance);
        return ReadPropertyAsync(address, objectId, propertyId, invokeId, arrayIndex);
    }

    public async Task<IList<BacnetValue>> ReadPropertyAsync(BacnetAddress address, BacnetObjectId objectId,
        BacnetPropertyIds propertyId, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        using var result = (BacnetAsyncResult)BeginReadPropertyRequest(address, objectId, propertyId, true, invokeId, arrayIndex);
        if (!await SendRequestAsync(result, CancellationToken.None).ConfigureAwait(false))
            throw new Exception($"Failed to read property {propertyId} of {objectId} from {address}");

        EndReadPropertyRequest(result, out var valueList, out var ex);
        if (ex != null)
            throw ex;

        return valueList;
    }

    public IAsyncResult BeginReadPropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, bool waitForTransmit, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        Log.LogDebug($"Sending ReadPropertyRequest {objectId} {propertyId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, address.RoutedSource, address.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadProperty(buffer, objectId, (uint)propertyId, arrayIndex);

        //send
        var ret = new BacnetAsyncResult(this, address, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReadPropertyRequest(IAsyncResult result, out IList<BacnetValue> valueList, out Exception ex)
    {
	    var res = (BacnetAsyncResult) result;

	    ex = null;
	    if (!res.WaitForDone(Timeout))
		    ex = new Exception("Wait Timeout");

	    if (ex == null)
		    ex = res.Error;

	    if (ex == null)
	    {
            //decode
		    if (Services.DecodeReadPropertyAcknowledge(res.Address, res.Result, 0, res.Result.Length, out _, out _, out valueList) < 0)
			    ex = new Exception("Decode");
	    }
	    else
	    {
		    valueList = null;
	    }

	    res.Dispose();
    }

    public bool WritePropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, byte invokeId = 0, byte? priority = null, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        using (var result = (BacnetAsyncResult)BeginWritePropertyRequest(adr, objectId, propertyId, valueList, true, invokeId, priority, arrayIndex))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWritePropertyRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public bool WritePropertyMultipleRequest(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginWritePropertyMultipleRequest(adr, objectId, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWritePropertyRequest(result, out var ex); // Share the same with single write
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginWritePropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, bool waitForTransmit, byte invokeId = 0, byte? priority = null, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        if (priority.HasValue && (priority < 1 || priority > 16))
            throw new ArgumentOutOfRangeException(nameof(priority), "WritePropertyRequest priority must be between 1 and 16");

        Log.LogDebug($"Sending WritePropertyRequest {objectId} {propertyId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeWriteProperty(buffer, objectId, (uint)propertyId, arrayIndex, priority ?? _writepriority, valueList);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public IAsyncResult BeginWritePropertyMultipleRequest(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending WritePropertyMultipleRequest {objectId}");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        //BacnetNpduControls.PriorityNormalMessage 
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);

        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeWritePropertyMultiple(buffer, objectId, valueList);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndWritePropertyRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    // By Chritopher Günter : Write multiple properties on multiple objects
    public bool WritePropertyMultipleRequest(BacnetAddress adr, ICollection<BacnetReadAccessResult> valueList, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginWritePropertyMultipleRequest(adr, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndWritePropertyRequest(result, out var ex); // Share the same with single write
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginWritePropertyMultipleRequest(BacnetAddress adr, ICollection<BacnetReadAccessResult> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        var objectIds = string.Join(", ", valueList.Select(v => v.objectIdentifier));
        Log.LogDebug($"Sending WritePropertyMultipleRequest {objectIds}");

        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        //BacnetNpduControls.PriorityNormalMessage 
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);

        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeWriteObjectMultiple(buffer, valueList);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public bool ReadPropertyMultipleRequest(BacnetAddress address, BacnetObjectId objectId, IList<BacnetPropertyReference> propertyIdAndArrayIndex, out IList<BacnetReadAccessResult> values, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginReadPropertyMultipleRequest(address, objectId, propertyIdAndArrayIndex, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadPropertyMultipleRequest(result, out values, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        values = null;
        return false;
    }

    public Task<IList<BacnetPropertyValue>> ReadPropertyMultipleAsync(BacnetAddress address,
        BacnetObjectTypes objType, uint objInstance, params BacnetPropertyIds[] propertyIds)
    {
        var objectId = new BacnetObjectId(objType, objInstance);
        return ReadPropertyMultipleAsync(address, objectId, propertyIds);
    }

    public async Task<IList<BacnetPropertyValue>> ReadPropertyMultipleAsync(BacnetAddress address,
        BacnetObjectId objectId, params BacnetPropertyIds[] propertyIds)
    {
        var propertyReferences = propertyIds
            .Select(p => new BacnetPropertyReference((uint)p, ASN1.BACNET_ARRAY_ALL))
            .ToList();

        using var result = (BacnetAsyncResult)BeginReadPropertyMultipleRequest(address, objectId, propertyReferences, true);
        if (!await SendRequestAsync(result, CancellationToken.None).ConfigureAwait(false))
            throw new Exception($"Failed to read multiple properties of {objectId} from {address}");

        EndReadPropertyMultipleRequest(result, out var values, out var ex);
        if (ex != null)
            throw ex;

        return values.Single().values;
    }

    public IAsyncResult BeginReadPropertyMultipleRequest(BacnetAddress adr, BacnetObjectId objectId, IList<BacnetPropertyReference> propertyIdAndArrayIndex, bool waitForTransmit, byte invokeId = 0)
    {
        var propertyIds = string.Join(", ", propertyIdAndArrayIndex.Select(v => (BacnetPropertyIds)v.propertyIdentifier));
        Log.LogDebug($"Sending ReadPropertyMultipleRequest {objectId} {propertyIds}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadPropertyMultiple(buffer, objectId, propertyIdAndArrayIndex);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // Another way to read multiple properties on multiples objects, if supported by devices
    public bool ReadPropertyMultipleRequest(BacnetAddress address, IList<BacnetReadAccessSpecification> properties, out IList<BacnetReadAccessResult> values, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginReadPropertyMultipleRequest(address, properties, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReadPropertyMultipleRequest(result, out values, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        values = null;
        return false;
    }

    public IAsyncResult BeginReadPropertyMultipleRequest(BacnetAddress adr, IList<BacnetReadAccessSpecification> properties, bool waitForTransmit, byte invokeId = 0)
    {
        var objectIds = string.Join(", ", properties.Select(v => v.objectIdentifier));
        Log.LogDebug($"Sending ReadPropertyMultipleRequest {objectIds}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReadPropertyMultiple(buffer, properties);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReadPropertyMultipleRequest(IAsyncResult result, out IList<BacnetReadAccessResult> values, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            //decode
            if (Services.DecodeReadPropertyMultipleAcknowledge(res.Address, res.Result, 0, res.Result.Length, out values) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            values = null;
        }

        res.Dispose();
    }

    private BacnetPduTypes PduConfirmedServiceRequest()
    {
        return MaxSegments != BacnetMaxSegments.MAX_SEG0
            ? BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED
            : BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST;
    }

    // By Christopher Günter
    public bool CreateObjectRequest(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList = null, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginCreateObjectRequest(adr, objectId, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndCreateObjectRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginCreateObjectRequest(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending CreateObjectRequest");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeCreateObject(buffer, objectId, valueList);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndCreateObjectRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool DeleteObjectRequest(BacnetAddress adr, BacnetObjectId objectId, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginDeleteObjectRequest(adr, objectId, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndDeleteObjectRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }

        return false;
    }

    public IAsyncResult BeginDeleteObjectRequest(BacnetAddress adr, BacnetObjectId objectId, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending DeleteObjectRequest");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);

        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        //NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply , adr.RoutedSource);

        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeDeleteObject(buffer, objectId);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndDeleteObjectRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool AddListElementRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginAddListElementRequest(adr, objectId, reference, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {

                if (result.WaitForDone(Timeout))
                {
                    EndAddListElementRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        //values = null;
        return false;
    }

    public bool RemoveListElementRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginRemoveListElementRequest(adr, objectId, reference, valueList, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndAddListElementRequest(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        //values = null;
        return false;
    }

    public IAsyncResult BeginRemoveListElementRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending RemoveListElementRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAddListElement(buffer, objectId, reference.propertyIdentifier, reference.propertyArrayIndex, valueList);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public IAsyncResult BeginAddListElementRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending AddListElementRequest {objectId} {(BacnetPropertyIds)reference.propertyIdentifier}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_ADD_LIST_ELEMENT, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAddListElement(buffer, objectId, reference.propertyIdentifier, reference.propertyArrayIndex, valueList);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndAddListElementRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    // Fc
    // Read or Write without APDU Data encoding nor Decoding (just Request type, Object id and Property id)
    // Data is given by the caller starting with the Tag 3 (or maybe another one), and ending with it
    // return buffer start also with the Tag 3
    public bool RawEncodedDecodedPropertyConfirmedRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetConfirmedServices serviceId, ref byte[] inOutBuffer, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginRawEncodedDecodedPropertyConfirmedRequest(adr, objectId, propertyId, serviceId, inOutBuffer, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndRawEncodedDecodedPropertyConfirmedRequest(result, serviceId, out inOutBuffer, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        inOutBuffer = null;
        return false;
    }

    // Fc
    public IAsyncResult BeginRawEncodedDecodedPropertyConfirmedRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetConfirmedServices serviceId, byte[] inOutBuffer, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending RawEncodedRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), serviceId, MaxSegments, Transport.MaxAdpuLength, invokeId);

        ASN1.encode_context_object_id(buffer, 0, objectId.type, objectId.instance);
        ASN1.encode_context_enumerated(buffer, 1, (byte)propertyId);

        // No content encoding to do
        if (inOutBuffer != null)
            buffer.Add(inOutBuffer, inOutBuffer.Length);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    // Fc
    public void EndRawEncodedDecodedPropertyConfirmedRequest(IAsyncResult result, BacnetConfirmedServices serviceId, out byte[] inOutBuffer, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        inOutBuffer = null;

        if (ex == null)
        {
            if (serviceId == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY)
            {
                //decode
                const int offset = 0;
                var buffer = res.Result;

                ex = new Exception("Decode");

                if (!ASN1.decode_is_context_tag(buffer, offset, 0))
                    return;
                var len = 1;
                len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes _, out _);
                /* Tag 1: Property ID */
                len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
                if (tagNumber != 1)
                    return;
                len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out _);

                inOutBuffer = new byte[buffer.Length - len];
                Array.Copy(buffer, len, inOutBuffer, 0, inOutBuffer.Length);

                ex = null;
            }
        }

        res.Dispose();
    }

    public bool DeviceCommunicationControlRequest(BacnetAddress adr, uint timeDuration, uint enableDisable, string password, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginDeviceCommunicationControlRequest(adr, timeDuration, enableDisable, password, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndDeviceCommunicationControlRequest(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginDeviceCommunicationControlRequest(BacnetAddress adr, uint timeDuration, uint enableDisable, string password, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending DeviceCommunicationControlRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeDeviceCommunicationControl(buffer, timeDuration, enableDisable, password);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndDeviceCommunicationControlRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public bool PrivateTransferRequest(BacnetAddress adr, uint vendorId, uint serviceNumber, byte[] serviceParameters, out byte[] resultBlock, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginPrivateTransferRequest(adr, vendorId, serviceNumber, serviceParameters, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndPrivateTransferRequest(result, out _, out _, out resultBlock, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        resultBlock = null;
        return false;
    }

    public IAsyncResult BeginPrivateTransferRequest(BacnetAddress adr, uint vendorId, uint serviceNumber, byte[] serviceParameters, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending PrivateTransferRequest vendor {vendorId} service {serviceNumber}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodePrivateTransferConfirmed(buffer, vendorId, serviceNumber, serviceParameters);

        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndPrivateTransferRequest(IAsyncResult result, out uint vendorId, out uint serviceNumber, out byte[] resultBlock, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;

        ex = null;
        if (!res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
            ex = res.Error;

        vendorId = 0;
        serviceNumber = 0;
        resultBlock = null;

        if (ex == null)
        {
            if (Services.DecodePrivateTransfer(res.Result, 0, res.Result.Length, out vendorId, out serviceNumber, out resultBlock) < 0)
                ex = new Exception("Decode");
        }

        res.Dispose();
    }

    // FChaxel
    public bool GetAlarmSummaryOrEventRequest(BacnetAddress adr, bool getEvent, ref IList<BacnetGetEventInformationData> alarms, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginGetAlarmSummaryOrEventRequest(adr, getEvent, alarms, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndGetAlarmSummaryOrEventRequest(result, getEvent, ref alarms, out var moreEvent, out var ex);
                    if (ex != null)
                        return false;
                    return !moreEvent || GetAlarmSummaryOrEventRequest(adr, getEvent, ref alarms);
                }

                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public Task<IList<BacnetGetEventInformationData>> GetEventsAsync(BacnetAddress address, byte invokeId = 0)
    {
        return GetAlarmSummaryOrEventAsync(address, true, invokeId);
    }

    public IAsyncResult BeginGetAlarmSummaryOrEventRequest(BacnetAddress adr, bool getEvent, IList<BacnetGetEventInformationData> alarms, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending Alarm summary request");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);

        var service = getEvent
            ? BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION
            : BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY;

        APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), service, MaxSegments, Transport.MaxAdpuLength, invokeId);

        // Get Next, never true if GetAlarmSummary is usee
        if (alarms.Count != 0)
            ASN1.encode_context_object_id(buffer, 0, alarms[alarms.Count - 1].objectIdentifier.type, alarms[alarms.Count - 1].objectIdentifier.instance);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndGetAlarmSummaryOrEventRequest(IAsyncResult result, bool getEvent, ref IList<BacnetGetEventInformationData> alarms, out bool moreEvent, out Exception ex)
    {
        moreEvent = false;
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        if (ex == null)
        {
            if (Services.DecodeAlarmSummaryOrEvent(res.Result, 0, res.Result.Length, getEvent, ref alarms, out moreEvent) < 0)
                ex = new Exception("Decode");
        }
        else
        {
            ex = new Exception("Service not available");
        }

        res.Dispose();
    }
    // DAL
    public void GetAlarmSummaryOrEventInformationResponse(BacnetAddress adr, bool getEvent, byte invoke_id, Segmentation segmentation, BacnetGetEventInformationData[] data, bool more_events)
    {
        // 'getEvent' is not currently used.   Can be used if ever implementing GetAlarmSummary.
        // response could be segmented
        // but if you don't want it segmented (which would be normal usage)
        // you have to compute the message data and the 'more' flag
        // outside this function.
        HandleSegmentationResponse(adr, invoke_id, segmentation, (o) =>
        {
            SendComplexAck(adr, invoke_id, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION, (b) =>
            {
                Services.EncodeGetEventInformationAcknowledge(b, data, more_events);
            });
        });
    }

    // FChaxel
    public bool AlarmAcknowledgement(BacnetAddress adr, BacnetObjectId objId, BacnetEventStates eventState, string ackText, BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginAlarmAcknowledgement(adr, objId, eventState, ackText, evTimeStamp, ackTimeStamp, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndAlarmAcknowledgement(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginAlarmAcknowledgement(BacnetAddress adr, BacnetObjectId objId, BacnetEventStates eventState, string ackText, BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending AlarmAcknowledgement");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeAlarmAcknowledge(buffer, 57, objId, (uint)eventState, ackText, evTimeStamp, ackTimeStamp);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndAlarmAcknowledgement(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (!res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");
    }

    public bool ReinitializeRequest(BacnetAddress adr, BacnetReinitializedStates state, string password, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginReinitializeRequest(adr, state, password, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndReinitializeRequest(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginReinitializeRequest(BacnetAddress adr, BacnetReinitializedStates state, string password, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending ReinitializeRequest");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeReinitializeDevice(buffer, state, password);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndReinitializeRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    public IAsyncResult BeginConfirmedNotify(BacnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IList<BacnetPropertyValue> values, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug("Sending Notify (confirmed)");
        if (invokeId == 0) invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeCOVNotifyConfirmed(buffer, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);

        //send
        var ret = new BacnetAsyncResult(this, adr, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndConfirmedNotify(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (!res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");
    }

    public bool Notify(BacnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool issueConfirmedNotifications, IList<BacnetPropertyValue> values)
    {
        if (!issueConfirmedNotifications)
        {
            Log.LogDebug("Sending Notify (unconfirmed)");
            var buffer = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);
            Services.EncodeCOVNotifyUnconfirmed(buffer, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);
            // Modif F. Chaxel

            var sendbytes = Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
            return sendbytes == buffer.offset;
        }

        using (var result = (BacnetAsyncResult)BeginConfirmedNotify(adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values, true))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndConfirmedNotify(result, out var ex);
                    if (ex != null)
                        throw ex;
                    return true;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }

        return false;
    }

    public bool LifeSafetyOperationRequest(BacnetAddress address, BacnetObjectId objectId, string requestingSrc, BacnetLifeSafetyOperations operation, byte invokeId = 0)
    {
        using (var result = (BacnetAsyncResult)BeginLifeSafetyOperationRequest(address, objectId, 0, requestingSrc, operation, true, invokeId))
        {
            for (var r = 0; r < _retries; r++)
            {
                if (result.WaitForDone(Timeout))
                {
                    EndLifeSafetyOperationRequest(result, out var ex);
                    return ex == null;
                }
                if (r < Retries - 1)
                    result.Resend();
            }
        }
        return false;
    }

    public IAsyncResult BeginLifeSafetyOperationRequest(BacnetAddress address, BacnetObjectId objectId, uint processId, string requestingSrc, BacnetLifeSafetyOperations operation, bool waitForTransmit, byte invokeId = 0)
    {
        Log.LogDebug($"Sending {ToTitleCase(operation)} {objectId}");
        if (invokeId == 0)
            invokeId = (byte)Interlocked.Increment(ref _invokeId);

        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, address.RoutedSource, address.RoutedDestination);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION, MaxSegments, Transport.MaxAdpuLength, invokeId);
        Services.EncodeLifeSafetyOperation(buffer, processId, requestingSrc, (uint)operation, objectId);

        //send
        var ret = new BacnetAsyncResult(this, address, invokeId, buffer.buffer, buffer.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
        ret.Resend();

        return ret;
    }

    public void EndLifeSafetyOperationRequest(IAsyncResult result, out Exception ex)
    {
        var res = (BacnetAsyncResult)result;
        ex = res.Error;
        if (ex == null && !res.WaitForDone(Timeout))
            ex = new Exception("Wait Timeout");

        res.Dispose();
    }

    // ==============================================================================================
    // Asynchronous request methods.
    //
    // Each mirrors its synchronous XxxRequest sibling but never blocks a thread while the reply is
    // outstanding: the request is put on the wire by the matching Begin* call and awaited through the
    // TaskCompletionSource in BacnetAsyncResult. Per-attempt Timeout and Retries behave exactly as on
    // the synchronous path. Failure is reported by throwing — a TimeoutException when every attempt
    // times out, or the device-reported exception (abort, reject, error or decode failure) otherwise.
    // The confirmed request itself already carries a source-unique invoke-id, so many of these may be
    // in flight concurrently on a single client without a lock.
    // ==============================================================================================

    /// <summary>
    /// Writes a block to a File object (AtomicWriteFile) and returns the new file position.
    /// </summary>
    public async Task<int> WriteFileAsync(BacnetAddress adr, BacnetObjectId objectId, int position, int count, byte[] fileBuffer, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginWriteFileRequest(adr, objectId, position, count, fileBuffer, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to write file {objectId} on {adr}");

        EndWriteFileRequest(result, out var newPosition, out var ex);
        if (ex != null)
            throw ex;

        return newPosition;
    }

    /// <summary>
    /// Reads a block from a File object (AtomicReadFile).
    /// </summary>
    public async Task<(int position, uint count, bool endOfFile, byte[] fileBuffer, int fileBufferOffset)> ReadFileAsync(BacnetAddress adr, BacnetObjectId objectId, int position, uint count, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginReadFileRequest(adr, objectId, position, count, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to read file {objectId} from {adr}");

        EndReadFileRequest(result, out var readCount, out var readPosition, out var endOfFile, out var buffer, out var bufferOffset, out var ex);
        if (ex != null)
            throw ex;

        return (readPosition, readCount, endOfFile, buffer, bufferOffset);
    }

    // Read range by start time
    /// <summary>
    /// Reads a range of items from a list or log property, starting at the given time.
    /// </summary>
    public async Task<(byte[] range, uint count)> ReadRangeAsync(BacnetAddress adr, BacnetObjectId objectId, DateTime readFrom, uint quantity, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginReadRangeRequest(adr, objectId, readFrom, quantity, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to read range of {objectId} from {adr}");

        EndReadRangeRequest(result, out var range, out var count, out var ex);
        if (ex != null)
            throw ex;

        return (range, count);
    }

    // Read range by position
    /// <summary>
    /// Reads a range of items from a list or log property, starting at the given index.
    /// </summary>
    public async Task<(byte[] range, uint count)> ReadRangeAsync(BacnetAddress adr, BacnetObjectId objectId, uint idxBegin, uint quantity, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginReadRangeRequest(adr, objectId, idxBegin, quantity, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to read range of {objectId} from {adr}");

        EndReadRangeRequest(result, out var range, out var count, out var ex);
        if (ex != null)
            throw ex;

        return (range, count);
    }

    /// <summary>
    /// Subscribes to (or cancels) change-of-value notifications for an object.
    /// </summary>
    public async Task SubscribeCOVAsync(BacnetAddress adr, BacnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginSubscribeCOVRequest(adr, objectId, subscribeId, cancel, issueConfirmedNotifications, lifetime, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to subscribe COV of {objectId} on {adr}");

        EndSubscribeCOVRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Sends a confirmed event notification.
    /// </summary>
    public async Task SendConfirmedEventNotificationAsync(BacnetAddress adr, BacnetEventNotificationData eventData, byte invokeId = 0, BacnetAddress source = null, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginSendConfirmedEventNotificationRequest(adr, eventData, true, invokeId, source);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to send confirmed event notification to {adr}");

        EndSendConfirmedEventNotificationRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Subscribes to (or cancels) change-of-value notifications for a single property.
    /// </summary>
    public async Task SubscribePropertyAsync(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference monitoredProperty, uint subscribeId, bool cancel, bool issueConfirmedNotifications, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginSubscribePropertyRequest(adr, objectId, monitoredProperty, subscribeId, cancel, issueConfirmedNotifications, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to subscribe property {objectId}.{monitoredProperty} on {adr}");

        EndSubscribePropertyRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Writes a single property of an object.
    /// </summary>
    public async Task WritePropertyAsync(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, byte invokeId = 0, byte? priority = null, uint arrayIndex = ASN1.BACNET_ARRAY_ALL, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginWritePropertyRequest(adr, objectId, propertyId, valueList, true, invokeId, priority, arrayIndex);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to write property {propertyId} of {objectId} on {adr}");

        EndWritePropertyRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Writes several properties of a single object.
    /// </summary>
    public async Task WritePropertyMultipleAsync(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginWritePropertyMultipleRequest(adr, objectId, valueList, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to write multiple properties of {objectId} on {adr}");

        EndWritePropertyRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Writes several properties across several objects.
    /// </summary>
    public async Task WritePropertyMultipleAsync(BacnetAddress adr, ICollection<BacnetReadAccessResult> valueList, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginWritePropertyMultipleRequest(adr, valueList, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to write multiple properties on {adr}");

        EndWritePropertyRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Reads several properties of a single object.
    /// </summary>
    public async Task<IList<BacnetReadAccessResult>> ReadPropertyMultipleAsync(BacnetAddress adr, BacnetObjectId objectId, IList<BacnetPropertyReference> propertyIdAndArrayIndex, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginReadPropertyMultipleRequest(adr, objectId, propertyIdAndArrayIndex, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to read multiple properties of {objectId} from {adr}");

        EndReadPropertyMultipleRequest(result, out var values, out var ex);
        if (ex != null)
            throw ex;

        return values;
    }

    /// <summary>
    /// Reads several properties across several objects.
    /// </summary>
    public async Task<IList<BacnetReadAccessResult>> ReadPropertyMultipleAsync(BacnetAddress adr, IList<BacnetReadAccessSpecification> properties, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginReadPropertyMultipleRequest(adr, properties, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to read multiple properties from {adr}");

        EndReadPropertyMultipleRequest(result, out var values, out var ex);
        if (ex != null)
            throw ex;

        return values;
    }

    /// <summary>
    /// Creates an object on the remote device.
    /// </summary>
    public async Task CreateObjectAsync(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList = null, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginCreateObjectRequest(adr, objectId, valueList, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to create object {objectId} on {adr}");

        EndCreateObjectRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Deletes an object on the remote device.
    /// </summary>
    public async Task DeleteObjectAsync(BacnetAddress adr, BacnetObjectId objectId, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginDeleteObjectRequest(adr, objectId, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to delete object {objectId} on {adr}");

        EndDeleteObjectRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Adds one or more elements to a list property.
    /// </summary>
    public async Task AddListElementAsync(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginAddListElementRequest(adr, objectId, reference, valueList, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to add list element to {objectId}.{(BacnetPropertyIds)reference.propertyIdentifier} on {adr}");

        EndAddListElementRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Removes one or more elements from a list property.
    /// </summary>
    public async Task RemoveListElementAsync(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginRemoveListElementRequest(adr, objectId, reference, valueList, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to remove list element from {objectId}.{(BacnetPropertyIds)reference.propertyIdentifier} on {adr}");

        EndAddListElementRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Sends a confirmed request with a caller-supplied, pre-encoded payload and returns the raw response body.
    /// </summary>
    public async Task<byte[]> RawEncodedDecodedPropertyConfirmedAsync(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetConfirmedServices serviceId, byte[] inOutBuffer = null, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginRawEncodedDecodedPropertyConfirmedRequest(adr, objectId, propertyId, serviceId, inOutBuffer, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed raw confirmed request {serviceId} of {objectId} on {adr}");

        EndRawEncodedDecodedPropertyConfirmedRequest(result, serviceId, out var outBuffer, out var ex);
        if (ex != null)
            throw ex;

        return outBuffer;
    }

    /// <summary>
    /// Enables or disables the remote device's BACnet communication.
    /// </summary>
    public async Task DeviceCommunicationControlAsync(BacnetAddress adr, uint timeDuration, uint enableDisable, string password, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginDeviceCommunicationControlRequest(adr, timeDuration, enableDisable, password, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed device communication control on {adr}");

        EndDeviceCommunicationControlRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Sends a vendor-specific confirmed PrivateTransfer request and returns the device's response.
    /// </summary>
    public async Task<(uint vendorId, uint serviceNumber, byte[] resultBlock)> PrivateTransferAsync(BacnetAddress adr, uint vendorId, uint serviceNumber, byte[] serviceParameters, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginPrivateTransferRequest(adr, vendorId, serviceNumber, serviceParameters, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed private transfer (vendor {vendorId}, service {serviceNumber}) on {adr}");

        EndPrivateTransferRequest(result, out var outVendorId, out var outServiceNumber, out var resultBlock, out var ex);
        if (ex != null)
            throw ex;

        return (outVendorId, outServiceNumber, resultBlock);
    }

    /// <summary>
    /// Reads the device's alarm summary or event information, following the get-next chain to completion.
    /// </summary>
    public async Task<IList<BacnetGetEventInformationData>> GetAlarmSummaryOrEventAsync(BacnetAddress adr, bool getEvent, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        IList<BacnetGetEventInformationData> alarms = new List<BacnetGetEventInformationData>();
        var currentInvokeId = invokeId;
        bool moreEvent;

        do
        {
            using var result = (BacnetAsyncResult)BeginGetAlarmSummaryOrEventRequest(adr, getEvent, alarms, true, currentInvokeId);
            if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
                throw new TimeoutException($"Failed to get {(getEvent ? "events" : "alarms")} from {adr}");

            EndGetAlarmSummaryOrEventRequest(result, getEvent, ref alarms, out moreEvent, out var ex);
            if (ex != null)
                throw ex;

            currentInvokeId = 0; // let each follow-up "get next" round take a fresh invoke-id
        } while (moreEvent);

        return alarms;
    }

    /// <summary>
    /// Reinitializes the remote device (e.g. warm or cold start).
    /// </summary>
    public async Task ReinitializeAsync(BacnetAddress adr, BacnetReinitializedStates state, string password, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginReinitializeRequest(adr, state, password, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to reinitialize {adr}");

        EndReinitializeRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Sends a life-safety operation (e.g. silence or reset) for an object.
    /// </summary>
    public async Task LifeSafetyOperationAsync(BacnetAddress address, BacnetObjectId objectId, string requestingSrc, BacnetLifeSafetyOperations operation, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginLifeSafetyOperationRequest(address, objectId, 0, requestingSrc, operation, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed life safety operation {operation} on {objectId} at {address}");

        EndLifeSafetyOperationRequest(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Acknowledges an alarm or event for an object.
    /// </summary>
    public async Task AlarmAcknowledgementAsync(BacnetAddress adr, BacnetObjectId objId, BacnetEventStates eventState, string ackText, BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp, byte invokeId = 0, CancellationToken cancellationToken = default)
    {
        using var result = (BacnetAsyncResult)BeginAlarmAcknowledgement(adr, objId, eventState, ackText, evTimeStamp, ackTimeStamp, true, invokeId);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to acknowledge alarm on {objId} at {adr}");

        EndAlarmAcknowledgement(result, out var ex);
        if (ex != null)
            throw ex;
    }

    /// <summary>
    /// Sends a change-of-value notification, confirmed or unconfirmed.
    /// </summary>
    public async Task NotifyAsync(BacnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool issueConfirmedNotifications, IList<BacnetPropertyValue> values, CancellationToken cancellationToken = default)
    {
        if (!issueConfirmedNotifications)
        {
            Notify(adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, false, values);
            return;
        }

        using var result = (BacnetAsyncResult)BeginConfirmedNotify(adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values, true);
        if (!await SendRequestAsync(result, cancellationToken).ConfigureAwait(false))
            throw new TimeoutException($"Failed to notify {monitoredObjectIdentifier} on {adr}");

        EndConfirmedNotify(result, out var ex);
        if (ex != null)
            throw ex;
    }

    public static byte GetSegmentsCount(BacnetMaxSegments maxSegments)
    {
        switch (maxSegments)
        {
            case BacnetMaxSegments.MAX_SEG0:
                return 0;
            case BacnetMaxSegments.MAX_SEG2:
                return 2;
            case BacnetMaxSegments.MAX_SEG4:
                return 4;
            case BacnetMaxSegments.MAX_SEG8:
                return 8;
            case BacnetMaxSegments.MAX_SEG16:
                return 16;
            case BacnetMaxSegments.MAX_SEG32:
                return 32;
            case BacnetMaxSegments.MAX_SEG64:
                return 64;
            case BacnetMaxSegments.MAX_SEG65:
                return 0xFF;
            default:
                throw new Exception("Not an option");
        }
    }

    public static BacnetMaxSegments GetSegmentsCount(byte maxSegments)
    {
        if (maxSegments == 0)
            return BacnetMaxSegments.MAX_SEG0;
        if (maxSegments <= 2)
            return BacnetMaxSegments.MAX_SEG2;
        if (maxSegments <= 4)
            return BacnetMaxSegments.MAX_SEG4;
        if (maxSegments <= 8)
            return BacnetMaxSegments.MAX_SEG8;
        if (maxSegments <= 16)
            return BacnetMaxSegments.MAX_SEG16;
        if (maxSegments <= 32)
            return BacnetMaxSegments.MAX_SEG32;
        if (maxSegments <= 64)
            return BacnetMaxSegments.MAX_SEG64;

        return BacnetMaxSegments.MAX_SEG65;
    }

    public Segmentation GetSegmentBuffer(BacnetMaxSegments maxSegments)
    {
        // when called from inside a confirmed-request event handler (the usual
        // pattern), pick up that request's max-APDU-length-accepted
        return GetSegmentBuffer(maxSegments, _currentRequestMaxAdpu);
    }

    public Segmentation GetSegmentBuffer(BacnetMaxSegments maxSegments, BacnetMaxAdpu? requesterMaxAdpu)
    {
        if (maxSegments == BacnetMaxSegments.MAX_SEG0)
            return null;

        return new Segmentation
        {
            buffer = GetEncodeBuffer(Transport.HeaderLength),
            max_segments = GetSegmentsCount(maxSegments),
            window_size = ProposedWindowSize,
            RequesterMaxAdpu = requesterMaxAdpu
        };
    }

    /// <summary>
    /// APDU size limit for a response: our own transport limit, capped by the
    /// requester's max-APDU-length-accepted when known (135 §5.2.1.2).
    /// </summary>
    private int GetMaxApdu(Segmentation segmentation)
    {
        var maxApdu = GetMaxApdu();
        if (segmentation?.RequesterMaxAdpu != null)
            maxApdu = Math.Min(maxApdu, GetMaxApdu(segmentation.RequesterMaxAdpu.Value));
        return maxApdu;
    }

    private EncodeBuffer EncodeSegmentHeader(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetConfirmedServices service, bool moreFollows)
    {
        EncodeBuffer buffer;
        var isSegmented = false;
        if (segmentation == null)
            buffer = GetEncodeBuffer(Transport.HeaderLength);
        else
        {
            buffer = segmentation.buffer;
            isSegmented = segmentation.sequence_number > 0 | moreFollows;
        }
        buffer.Reset(Transport.HeaderLength);

        //encode
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);

        //set segments limits
        var maxApdu = GetMaxApdu(segmentation);
        // never open an encode window past the physical buffer: the APDU limit
        // can exceed the transport payload (e.g. 1476 vs the default 1472),
        // which previously wrote out of bounds. The min_limit chunk arithmetic
        // below must use the same clamped value on every segment.
        maxApdu = Math.Min(maxApdu, buffer.buffer.Length - buffer.offset);
        buffer.max_offset = buffer.offset + maxApdu;
        // note: no SERVER flag - bits 1-0 of a ComplexACK type octet are
        // reserved-zero (135 §20.1.5); the server flag only exists in SegmentACK
        var apduHeader = APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK | (isSegmented ? BacnetPduTypes.SEGMENTED_MESSAGE : 0) | (moreFollows ? BacnetPduTypes.MORE_FOLLOWS : 0), service, invokeId, segmentation?.sequence_number ?? 0, segmentation?.window_size ?? 0);
        buffer.min_limit = (maxApdu - apduHeader) * (segmentation?.sequence_number ?? 0);

        return buffer;
    }

    private bool EncodeSegment(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetConfirmedServices service, out EncodeBuffer buffer, Action<EncodeBuffer> apduContentEncode)
    {
        //encode (regular)
        buffer = EncodeSegmentHeader(adr, invokeId, segmentation, service, false);
        apduContentEncode(buffer);

        var moreFollows = (buffer.result & EncodeResult.NotEnoughBuffer) > 0;
        if (segmentation != null && moreFollows)
        {
            //reencode in segmented
            EncodeSegmentHeader(adr, invokeId, segmentation, service, true);
            apduContentEncode(buffer);
            return true;
        }

        if (moreFollows)
            return true;

        return segmentation != null && segmentation.sequence_number > 0;
    }

    /// <summary>
    /// Handle the segmentation of several too hugh response (if it's accepted by the client) 
    /// used by ReadRange, ReadProperty, ReadPropertyMultiple & ReadFile responses
    /// </summary>
    private void HandleSegmentationResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, Action<Segmentation> transmit)
    {
        // send first
        transmit(segmentation);

        if (segmentation == null || segmentation.buffer.result == EncodeResult.Good)
            return;

        // start new thread to handle the segment sequence (if required)
        ThreadPool.QueueUserWorkItem(o =>
        {
            var oldMaxInfoFrames = Transport.MaxInfoFrames;
            Transport.MaxInfoFrames = segmentation.window_size; // increase max_info_frames, to increase throughput. This might be against 'standard'

                while (true)
            {
                var moreFollows = (segmentation.buffer.result & EncodeResult.NotEnoughBuffer) > 0;

                    // wait for segmentACK
                    if ((segmentation.sequence_number - 1) % segmentation.window_size == 0 || !moreFollows)
                {
                    if (!WaitForAllTransmits(TransmitTimeout))
                    {
                        Log.LogWarning("Transmit timeout");
                        break;
                    }

                    var currentNumber = segmentation.sequence_number;

                    if (!WaitForSegmentAck(adr, invokeId, segmentation, Timeout))
                    {
                        Log.LogWarning("Didn't get segmentACK");
                        break;
                    }

                    if (segmentation.sequence_number != currentNumber)
                    {
                        Log.LogDebug("Oh, a retransmit");
                        moreFollows = true;
                    }
                }
                else
                {
                        // a negative segmentACK perhaps
                        var currentNumber = segmentation.sequence_number;
                    WaitForSegmentAck(adr, invokeId, segmentation, 0); // don't wait

                        if (segmentation.sequence_number != currentNumber)
                        Log.LogDebug("Oh, a retransmit");
                }

                if (moreFollows)
                        // lock (m_lockObject) transmit(segmentation);
                        transmit(segmentation);
                else
                    break;
            }

            Transport.MaxInfoFrames = oldMaxInfoFrames;
        });
    }

    private void SendComplexAck(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetConfirmedServices service, Action<EncodeBuffer> apduContentEncode)
    {
        Log.LogDebug($"Sending {ToTitleCase(service)}");

        //encode
        if (EncodeSegment(adr, invokeId, segmentation, service, out var buffer, apduContentEncode))
        {
            //client doesn't support segments
            if (segmentation == null)
            {
                Log.LogInformation("Segmentation denied");
                // DAL
                SendAbort(adr, invokeId, BacnetAbortReason.SEGMENTATION_NOT_SUPPORTED);
                //ErrorResponse(adr, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                buffer.result = EncodeResult.Good;     //don't continue the segmentation
                return;
            }

            //first segment? validate max segments
            if (segmentation.sequence_number == 0)  //only validate first segment
            {
                if (segmentation.max_segments != 0xFF && segmentation.buffer.offset > segmentation.max_segments * (GetMaxApdu(segmentation) - 5))      //5 is adpu header
                {
                    Log.LogInformation("Too much segmenation");
                    // DAL
                    SendAbort(adr, invokeId, BacnetAbortReason.APDU_TOO_LONG);
                    //ErrorResponse(adr, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                    buffer.result = EncodeResult.Good;     //don't continue the segmentation
                    return;
                }
                Log.LogDebug("Segmentation required");
            }

            //increment before ack can do so (race condition)
            unchecked { segmentation.sequence_number++; };
        }

        //send
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.GetLength() - Transport.HeaderLength, adr, false, 0);
    }

    public void ReadPropertyResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetObjectId objectId, BacnetPropertyReference property, IEnumerable<BacnetValue> value)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, b =>
            {
                Services.EncodeReadPropertyAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, value);
            });
        });
    }

    public void CreateObjectResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetObjectId objectId)
    {
        SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, b =>
        {
            Services.EncodeCreateObjectAcknowledge(b, objectId);
        });
    }

    public void ReadPropertyMultipleResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, IList<BacnetReadAccessResult> values)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, b =>
            {
                Services.EncodeReadPropertyMultipleAcknowledge(b, values);
            });
        });
    }

    public void ReadRangeResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetObjectId objectId, BacnetPropertyReference property, BacnetResultFlags status, uint itemCount, byte[] applicationData, BacnetReadRangeRequestTypes requestType, uint firstSequenceNo)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, b =>
            {
                // BACnetResultFlags is a fixed 3-bit BIT STRING {firstItem, lastItem, moreItems}; pass the
                // width explicitly so all-false (or high-bit-clear) flags still encode three bits instead of
                // an empty bitstring (reported by @BennoMeijer, PR #15).
                Services.EncodeReadRangeAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, BacnetBitString.ConvertFromInt((uint)status, 3), itemCount, applicationData, requestType, firstSequenceNo);
            });
        });
    }

    public void ReadFileResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, int position, uint count, bool endOfFile, byte[] fileBuffer)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, b =>
            {
                Services.EncodeAtomicReadFileAcknowledge(b, true, endOfFile, position, 1, new[] { fileBuffer }, new[] { (int)count });
            });
        });
    }

    public void WriteFileResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, int position)
    {
        SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, b =>
        {
            Services.EncodeAtomicWriteFileAcknowledge(b, true, position);
        });
    }

    public void PrivateTransferResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, uint vendorId, uint serviceNumber, byte[] resultBlock = null)
    {
        HandleSegmentationResponse(adr, invokeId, segmentation, o =>
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER, b =>
            {
                Services.EncodePrivateTransferAcknowledge(b, vendorId, serviceNumber, resultBlock);
            });
        });
    }

    public void ErrorResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode)
    {
        Log.LogDebug($"Sending ErrorResponse for {service}: {errorCode}");
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeError(buffer, BacnetPduTypes.PDU_TYPE_ERROR, service, invokeId);
        Services.EncodeError(buffer, errorClass, errorCode);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void PrivateTransferErrorResponse(BacnetAddress adr, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, uint vendorId, uint serviceNumber, byte[] errorParameters = null)
    {
        Log.LogDebug($"Sending PrivateTransferErrorResponse: {errorCode}");
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeError(buffer, BacnetPduTypes.PDU_TYPE_ERROR, BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER, invokeId);
        Services.EncodePrivateTransferError(buffer, errorClass, errorCode, vendorId, serviceNumber, errorParameters);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SimpleAckResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invokeId)
    {
        Log.LogDebug($"Sending SimpleAckResponse for {service}");
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, service, invokeId);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }

    public void SegmentAckResponse(BacnetAddress adr, bool negative, bool server, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize)
    {
        Log.LogDebug("Sending SegmentAckResponse");
        var buffer = GetEncodeBuffer(Transport.HeaderLength);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, adr.RoutedDestination);
        APDU.EncodeSegmentAck(buffer, BacnetPduTypes.PDU_TYPE_SEGMENT_ACK | (negative ? BacnetPduTypes.NEGATIVE_ACK : 0) | (server ? BacnetPduTypes.SERVER : 0), originalInvokeId, sequenceNumber, actualWindowSize);
        Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, adr, false, 0);
    }

    public bool WaitForAllTransmits(int timeout)
    {
        return Transport.WaitForAllTransmits(timeout);
    }

    public bool WaitForSegmentAck(BacnetAddress adr, byte invokeId, Segmentation segmentation, int timeout)
    {
        if (!_lastSegmentAck.Wait(adr, invokeId, timeout))
            return false;

        segmentation.sequence_number = (byte)((_lastSegmentAck.SequenceNumber + 1) % 256);
        segmentation.window_size = _lastSegmentAck.WindowSize;
        return true;
    }

    private static string ToTitleCase(object obj)
    {
        var cultureTextInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
        return cultureTextInfo.ToTitleCase($"{obj}".ToLower());
    }

    public void Dispose()
    {
        Transport.Dispose();
    }
}
