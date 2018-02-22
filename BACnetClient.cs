using System.Collections.Generic;
using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using System.IO.BACnet.Serialize;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;

namespace System.IO.BACnet
{
    public delegate void MessageRecievedHandler(IBacnetTransport sender, byte[] buffer, int offset, int msgLength, BacnetAddress remoteAddress);

    /// <summary>
    /// BACnet network client or server
    /// </summary>
    public class BacnetClient : IDisposable
    {
        private int _retries;
        private byte _invokeId;

        private readonly LastSegmentAck _lastSegmentAck = new LastSegmentAck();
        private uint _writepriority;

        /// <summary>
        /// Dictionary of List of Tuples with sequence-number and byte[] per invoke-id
        /// TODO: invoke-id should be PER (remote) DEVICE!
        /// </summary>
        private Dictionary<byte, List<Tuple<byte, byte[]>>> _segmentsPerInvokeId = new Dictionary<byte, List<Tuple<byte, byte[]>>>();
        private Dictionary<byte, object> _locksPerInvokeId = new Dictionary<byte, object>();
        private Dictionary<byte, byte> _expectedSegmentsPerInvokeId = new Dictionary<byte, byte>();

        public const int DEFAULT_UDP_PORT = 0xBAC0;
        public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(1);
        public const int DEFAULT_RETRIES = 3;

        public IBacnetTransport Transport { get; }
        public ushort VendorId { get; set; } = 260;
        public TimeSpan Timeout { get; set; }
        public TimeSpan TransmitTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public BacnetMaxSegments MaxSegments { get; set; } = BacnetMaxSegments.MAX_SEG0;
        public byte ProposedWindowSize { get; set; } = 10;
        public bool ForceWindowSize { get; set; }
        public bool DefaultSegmentationHandling { get; set; } = true;
        public ILog Log { get; set; } = LogManager.GetLogger<BacnetClient>();

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
        }

        private class LastSegmentAck
        {
            private readonly ManualResetEvent _wait = new ManualResetEvent(false);
            private readonly object _lockObject = new object();
            private BacnetAddress _address;
            private byte _invokeId;

            public byte SequenceNumber;
            public byte WindowSize;
            
            public void Set(BacnetAddress address, byte invokeId, byte sequenceNumber, byte windowSize)
            {
                lock (_lockObject)
                {
                    _address = address;
                    _invokeId = invokeId;
                    SequenceNumber = sequenceNumber;
                    WindowSize = windowSize;
                    _wait.Set();
                }
            }

            public bool Wait(BacnetAddress address, byte invokeId, TimeSpan timeout)
            {
                Monitor.Enter(_lockObject);
                while (!address.Equals(_address) || _invokeId != invokeId)
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

        public BacnetClient(int port = DEFAULT_UDP_PORT, TimeSpan? timeout = null, int retries = DEFAULT_RETRIES)
            : this(new BacnetIpUdpProtocolTransport(port), timeout, retries)
        {
        }

        public BacnetClient(IBacnetTransport transport, TimeSpan? timeout = null, int retries = DEFAULT_RETRIES)
        {
            Transport = transport;
            Timeout = timeout ?? DEFAULT_TIMEOUT;
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
            Log.Info("Started communication");
        }

        public delegate void ConfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length);
        public event ConfirmedServiceRequestHandler OnConfirmedServiceRequest;
        public delegate void ReadPropertyRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, BacnetObjectId objectId, BacnetPropertyReference property, BacnetMaxSegments maxSegments);
        public event ReadPropertyRequestHandler OnReadPropertyRequest;
        public delegate void ReadPropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments maxSegments);
        public event ReadPropertyMultipleRequestHandler OnReadPropertyMultipleRequest;
        public delegate void WritePropertyRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, BacnetObjectId objectId, BacnetPropertyValue value, BacnetMaxSegments maxSegments);
        public event WritePropertyRequestHandler OnWritePropertyRequest;
        public delegate void WritePropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, BacnetObjectId objectId, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments);
        public event WritePropertyMultipleRequestHandler OnWritePropertyMultipleRequest;
        public delegate void AtomicWriteFileRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, bool isStream, BacnetObjectId objectId, int position, uint blockCount, byte[][] blocks, int[] counts, BacnetMaxSegments maxSegments);
        public event AtomicWriteFileRequestHandler OnAtomicWriteFileRequest;
        public delegate void AtomicReadFileRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, bool isStream, BacnetObjectId objectId, int position, uint count, BacnetMaxSegments maxSegments);
        public event AtomicReadFileRequestHandler OnAtomicReadFileRequest;
        public delegate void SubscribeCOVRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetMaxSegments maxSegments);
        public event SubscribeCOVRequestHandler OnSubscribeCOV;
        public delegate void EventNotificationCallbackHandler(BacnetClient sender, BacnetAddress address, byte invokeId, NotificationData eventData, bool needConfirm);
        public event EventNotificationCallbackHandler OnEventNotify;
        public delegate void SubscribeCOVPropertyRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference monitoredProperty, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement, BacnetMaxSegments maxSegments);
        public event SubscribeCOVPropertyRequestHandler OnSubscribeCOVProperty;
        public delegate void DeviceCommunicationControlRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, uint timeDuration, uint enableDisable, string password, BacnetMaxSegments maxSegments);
        public event DeviceCommunicationControlRequestHandler OnDeviceCommunicationControl;
        public delegate void ReinitializedRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, BacnetReinitializedStates state, string password, BacnetMaxSegments maxSegments);
        public event ReinitializedRequestHandler OnReinitializedDevice;
        public delegate void ReadRangeHandler(BacnetClient sender, BacnetAddress address, byte invokeId, BacnetObjectId objectId, BacnetPropertyReference property, BacnetReadRangeRequestTypes requestType, uint position, DateTime time, int count, BacnetMaxSegments maxSegments);
        public event ReadRangeHandler OnReadRange;
        public delegate void CreateObjectRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, BacnetObjectId objectId, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments);
        public event CreateObjectRequestHandler OnCreateObjectRequest;
        public delegate void DeleteObjectRequestHandler(BacnetClient sender, BacnetAddress address, byte invokeId, BacnetObjectId objectId, BacnetMaxSegments maxSegments);
        public event DeleteObjectRequestHandler OnDeleteObjectRequest;

        protected void ProcessConfirmedServiceRequest(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                Log.Debug($"ConfirmedServiceRequest {service}");

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
                        Log.Warn("Couldn't decode DecodeReadProperty");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY && OnWritePropertyRequest != null)
                {
                    if (Services.DecodeWriteProperty(address, buffer, offset, length, out var objectId, out var value) >= 0)
                        OnWritePropertyRequest(this, address, invokeId, objectId, value, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        //SendConfirmedServiceReject(address, invokeId, BacnetRejectReason.OTHER); 
                        Log.Warn("Couldn't decode DecodeWriteProperty");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE && OnReadPropertyMultipleRequest != null)
                {
                    if (Services.DecodeReadPropertyMultiple(buffer, offset, length, out var properties) >= 0)
                        OnReadPropertyMultipleRequest(this, address, invokeId, properties, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode DecodeReadPropertyMultiple");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE && OnWritePropertyMultipleRequest != null)
                {
                    if (Services.DecodeWritePropertyMultiple(address, buffer, offset, length, out var objectId, out var values) >= 0)
                        OnWritePropertyMultipleRequest(this, address, invokeId, objectId, values, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode DecodeWritePropertyMultiple");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    if (Services.DecodeCOVNotifyUnconfirmed(address, buffer, offset, length, out var subscriberProcessIdentifier, out var initiatingDeviceIdentifier, out var monitoredObjectIdentifier, out var timeRemaining, out var values) >= 0)
                        OnCOVNotification(this, address, invokeId, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, true, values, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode COVNotify");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE && OnAtomicWriteFileRequest != null)
                {
                    if (Services.DecodeAtomicWriteFile(buffer, offset, length, out var isStream, out var objectId, out var position, out var blockCount, out var blocks, out var counts) >= 0)
                        OnAtomicWriteFileRequest(this, address, invokeId, isStream, objectId, position, blockCount, blocks, counts, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode AtomicWriteFile");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE && OnAtomicReadFileRequest != null)
                {
                    if (Services.DecodeAtomicReadFile(buffer, offset, length, out var isStream, out var objectId, out var position, out var count) >= 0)
                        OnAtomicReadFileRequest(this, address, invokeId, isStream, objectId, position, count, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode AtomicReadFile");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV && OnSubscribeCOV != null)
                {
                    if (Services.DecodeSubscribeCOV(buffer, offset, length, out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime) >= 0)
                        OnSubscribeCOV(this, address, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, cancellationRequest, issueConfirmedNotifications, lifetime, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode SubscribeCOV");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY && OnSubscribeCOVProperty != null)
                {
                    if (Services.DecodeSubscribeProperty(buffer, offset, length, out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var monitoredProperty, out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime, out var covIncrement) >= 0)
                        OnSubscribeCOVProperty(this, address, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, monitoredProperty, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode SubscribeCOVProperty");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL && OnDeviceCommunicationControl != null)
                {
                    if (Services.DecodeDeviceCommunicationControl(buffer, offset, length, out var timeDuration, out var enableDisable, out var password) >= 0)
                        OnDeviceCommunicationControl(this, address, invokeId, timeDuration, enableDisable, password, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode DeviceCommunicationControl");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE && OnReinitializedDevice != null)
                {
                    if (Services.DecodeReinitializeDevice(buffer, offset, length, out var state, out var password) >= 0)
                        OnReinitializedDevice(this, address, invokeId, state, password, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode ReinitializeDevice");
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
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode Event/Alarm Notification");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE && OnReadRange != null)
                {
                    if (Services.DecodeReadRange(buffer, offset, length, out var objectId, out var property, out var requestType, out var position, out var time, out var count) >= 0)
                        OnReadRange(this, address, invokeId, objectId, property, requestType, position, time, count, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode ReadRange");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT && OnCreateObjectRequest != null)
                {
                    if (Services.DecodeCreateObject(address, buffer, offset, length, out var objectId, out var values) >= 0)
                        OnCreateObjectRequest(this, address, invokeId, objectId, values, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode CreateObject");
                    }
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT && OnDeleteObjectRequest != null)
                {
                    if (Services.DecodeDeleteObject(buffer, offset, length, out var objectId) >= 0)
                        OnDeleteObjectRequest(this, address, invokeId, objectId, maxSegments);
                    else
                    {
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                        Log.Warn("Couldn't decode DecodeDeleteObject");
                    }
                }
                else
                {
                    ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_REJECT_UNRECOGNIZED_SERVICE);
                    Log.Warn($"Confirmed service not handled: {service}");
                }
            }
            catch (Exception ex)
            {
                ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                Log.Error("Error in ProcessConfirmedServiceRequest", ex);
            }
        }

        public delegate void UnconfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length);
        public event UnconfirmedServiceRequestHandler OnUnconfirmedServiceRequest;
        public delegate void WhoHasHandler(BacnetClient sender, BacnetAddress address, int lowLimit, int highLimit, BacnetObjectId objId, string objName);
        public event WhoHasHandler OnWhoHas;
        public delegate void IamHandler(BacnetClient sender, BacnetAddress address, uint deviceId, uint maxAPDU, BacnetSegmentations segmentation, ushort vendorId);
        public event IamHandler OnIam;
        public delegate void WhoIsHandler(BacnetClient sender, BacnetAddress address, int lowLimit, int highLimit);
        public event WhoIsHandler OnWhoIs;
        public delegate void TimeSynchronizeHandler(BacnetClient sender, BacnetAddress address, DateTime dateTime, bool utc);
        public event TimeSynchronizeHandler OnTimeSynchronize;

        //used by both 'confirmed' and 'unconfirmed' notify
        public delegate void COVNotificationHandler(BacnetClient sender, BacnetAddress address, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool needConfirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments);
        public event COVNotificationHandler OnCOVNotification;

        protected void ProcessUnconfirmedServiceRequest(BacnetAddress address, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length)
        {
            try
            {
                Log.Debug("UnconfirmedServiceRequest");
                OnUnconfirmedServiceRequest?.Invoke(this, address, type, service, buffer, offset, length);
                if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM && OnIam != null)
                {
                    if (Services.DecodeIamBroadcast(buffer, offset, out var deviceId, out var maxAdpu, out var segmentation, out var vendorId) >= 0)
                        OnIam(this, address, deviceId, maxAdpu, segmentation, vendorId);
                    else
                        Log.Warn("Couldn't decode IamBroadcast");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS && OnWhoIs != null)
                {
                    if (Services.DecodeWhoIsBroadcast(buffer, offset, length, out var lowLimit, out var highLimit) >= 0)
                        OnWhoIs(this, address, lowLimit, highLimit);
                    else
                        Log.Warn("Couldn't decode WhoIsBroadcast");
                }
                // added by thamersalek
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS && OnWhoHas != null)
                {
                    if (Services.DecodeWhoHasBroadcast(buffer, offset, length, out var lowLimit, out var highLimit, out var objId, out var objName) >= 0)
                        OnWhoHas(this, address, lowLimit, highLimit, objId, objName);
                    else
                        Log.Warn("Couldn't decode WhoHasBroadcast");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    if (Services.DecodeCOVNotifyUnconfirmed(address, buffer, offset, length, out var subscriberProcessIdentifier, out var initiatingDeviceIdentifier, out var monitoredObjectIdentifier, out var timeRemaining, out var values) >= 0)
                        OnCOVNotification(this, address, 0, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, false, values, BacnetMaxSegments.MAX_SEG0);
                    else
                        Log.Warn("Couldn't decode COVNotifyUnconfirmed");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
                {
                    if (Services.DecodeTimeSync(buffer, offset, length, out var dateTime) >= 0)
                        OnTimeSynchronize(this, address, dateTime, false);
                    else
                        Log.Warn("Couldn't decode TimeSynchronize");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
                {
                    if (Services.DecodeTimeSync(buffer, offset, length, out var dateTime) >= 0)
                        OnTimeSynchronize(this, address, dateTime, true);
                    else
                        Log.Warn("Couldn't decode TimeSynchronize");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION && OnEventNotify != null) // F. Chaxel
                {
                    if (Services.DecodeEventNotifyData(buffer, offset, length, out var eventData) >= 0)
                        OnEventNotify(this, address, 0, eventData, false);
                    else
                        Log.Warn("Couldn't decode Event/Alarm Notification");
                }
                else
                {
                    Log.Warn($"Unconfirmed service not handled: {service}");
                    // SendUnConfirmedServiceReject(address); ? exists ?
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in ProcessUnconfirmedServiceRequest", ex);
            }
        }

        public delegate void SimpleAckHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength);
        public event SimpleAckHandler OnSimpleAck;

        protected void ProcessSimpleAck(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                Log.Debug($"Received SimpleAck for {service}");
                OnSimpleAck?.Invoke(this, address, type, service, invokeId, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Log.Error("Error in ProcessSimpleAck", ex);
            }
        }

        public delegate void ComplexAckHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length);
        public event ComplexAckHandler OnComplexAck;

        protected void ProcessComplexAck(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                Log.Debug($"Received ComplexAck for {service}");
                OnComplexAck?.Invoke(this, address, type, service, invokeId, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {nameof(ProcessComplexAck)}", ex);
            }
        }

        public delegate void ErrorHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, byte[] buffer, int offset, int length);
        public event ErrorHandler OnError;

        protected void ProcessError(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                if (Services.DecodeError(buffer, offset, length, out var errorClass, out var errorCode) < 0)
                    Log.Warn("Couldn't decode received Error");

                Log.Debug($"Received Error {errorClass} {errorCode}");
                OnError?.Invoke(this, address, type, service, invokeId, errorClass, errorCode, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {nameof(ProcessError)}", ex);
            }
        }

        public delegate void AbortHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, byte invokeId, BacnetAbortReason reason, byte[] buffer, int offset, int length);
        public event AbortHandler OnAbort;

        protected void ProcessAbort(BacnetAddress address, BacnetPduTypes type, byte invokeId, BacnetAbortReason reason, byte[] buffer, int offset, int length)
        {
            try
            {
                Log.Debug($"Received Abort, reason: {reason}");
                OnAbort?.Invoke(this, address, type, invokeId, reason, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Log.Error("Error in ProcessAbort", ex);
            }
        }

        public delegate void RejectHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, byte invokeId, BacnetRejectReason reason, byte[] buffer, int offset, int length);
        public event RejectHandler OnReject;

        protected void ProcessReject(BacnetAddress address, BacnetPduTypes type, byte invokeId, BacnetRejectReason reason, byte[] buffer, int offset, int length)
        {
            try
            {
                Log.Debug($"Received Reject, reason: {reason}");
                OnReject?.Invoke(this, address, type, invokeId, reason, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Log.Error("Error in ProcessReject", ex);
            }
        }

        public delegate void SegmentAckHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length);
        public event SegmentAckHandler OnSegmentAck;

        protected void ProcessSegmentAck(BacnetAddress address, BacnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length)
        {
            try
            {
                Log.Debug("Received SegmentAck");
                OnSegmentAck?.Invoke(this, address, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Log.Error("Error in ProcessSegmentAck", ex);
            }
        }

        public delegate void SegmentHandler(BacnetClient sender, BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length);
        public event SegmentHandler OnSegment;

        private void ProcessSegment(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, bool server, byte sequenceNumber, byte proposedWindowNumber, byte[] buffer, int offset, int length)
        {
            if (!_locksPerInvokeId.TryGetValue(invokeId, out var lockObj))
            {
                lockObj = new object();
                _locksPerInvokeId[invokeId] = lockObj;
            }

            lock (lockObj)
            {
                ProcessSegmentLocked(address, type, service, invokeId, maxSegments, maxAdpu, server, sequenceNumber,
                    proposedWindowNumber, buffer, offset, length);
            }
        }

        private void ProcessSegmentLocked(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service,
            byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, bool server, byte sequenceNumber,
            byte proposedWindowNumber, byte[] buffer, int offset, int length)
        {
            Log.Trace($@"Processing Segment #{sequenceNumber} of invoke-id #{invokeId}");

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

                SegmentAckResponse(address, false, server, invokeId, sequenceNumber, proposedWindowNumber);
            }

            //Send on
            OnSegment?.Invoke(this, address, type, service, invokeId, maxSegments, maxAdpu, sequenceNumber, buffer, offset, length);

            //default segment assembly. We run this seperately from the above handler, to make sure that it comes after!
            if (DefaultSegmentationHandling)
                PerformDefaultSegmentHandling(address, type, service, invokeId, maxSegments, maxAdpu, sequenceNumber, buffer, offset, length);
        }

        /// <summary>
        /// This is a simple handling that stores all segments in memory and assembles them when done
        /// </summary>
        private void PerformDefaultSegmentHandling(BacnetAddress address, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length)
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
            ProcessApdu(address, type, apduBuffer, 0, apduBuffer.Length);
        }

        private void ProcessApdu(BacnetAddress address, BacnetPduTypes type, byte[] buffer, int offset, int length)
        {
            switch (type & BacnetPduTypes.PDU_TYPE_MASK)
            {
                case BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST:
                    {
                        var apduHeaderLen = APDU.DecodeUnconfirmedServiceRequest(buffer, offset, out type, out var service);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessUnconfirmedServiceRequest(address, type, service, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_SIMPLE_ACK:
                    {
                        var apduHeaderLen = APDU.DecodeSimpleAck(buffer, offset, out type, out var service, out var invokeId);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessSimpleAck(address, type, service, invokeId, buffer, offset, length);
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
                            ProcessComplexAck(address, type, service, invokeId, buffer, offset, length);
                        }
                        else
                        {
                            ProcessSegment(address, type, service, invokeId, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU50, false,
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
                        _lastSegmentAck.Set(address, originalInvokeId, sequenceNumber, actualWindowSize);
                        ProcessSegmentAck(address, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_ERROR:
                    {
                        var apduHeaderLen = APDU.DecodeError(buffer, offset, out type, out var service, out var invokeId);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessError(address, type, service, invokeId, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_ABORT:
                    {
                        var apduHeaderLen = APDU.DecodeAbort(buffer, offset, out type, out var invokeId, out var reason);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessAbort(address, type, invokeId, reason, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_REJECT:
                    {
                        var apduHeaderLen = APDU.DecodeReject(buffer, offset, out type, out var invokeId, out var reason);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessReject(address, type, invokeId, reason, buffer, offset, length);
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
                            ProcessConfirmedServiceRequest(address, type, service, maxSegments, maxAdpu, invokeId, buffer, offset, length);
                        }
                        else
                        {
                            ProcessSegment(address, type, service, invokeId, maxSegments, maxAdpu, true, sequenceNumber, proposedWindowNumber, buffer, offset, length);
                        }
                    }
                    break;

                default:
                    Log.Warn($"Something else arrived: {type}");
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
                var npduLen = NPDU.Decode(buffer, offset, out var npduFunction, out _, out var source, out _, out _, out _);

                // Modif FC
                remoteAddress.RoutedSource = source;

                if (npduFunction.HasFlag(BacnetNpduControls.NetworkLayerMessage))
                {
                    Log.Info("Network Layer message received");
                    return; // Network Layer message discarded
                }

                if (npduLen <= 0)
                    return;

                offset += npduLen;
                msgLength -= npduLen;

                if (msgLength <= 0)
                    return;

                var apduType = APDU.GetDecodedType(buffer, offset);
                ProcessApdu(remoteAddress, apduType, buffer, offset, msgLength);
            }
            catch (Exception ex)
            {
                Log.Error("Error in OnRecieve", ex);
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
                    Log.Debug($"Sending Register as a Foreign Device to {bbmdIp}");
                else
                    Log.Warn("The given address do not match with the IP version");
            }
            catch (Exception ex)
            {
                Log.Error("Error on RegisterAsForeignDevice (Wrong Transport, not IP ?)", ex);
            }
        }

        public void RemoteWhoIs(string bbmdIP, int port = DEFAULT_UDP_PORT, int lowLimit = -1, int highLimit = -1)
        {
            try
            {
                var ep = new IPEndPoint(IPAddress.Parse(bbmdIP), port);

                var b = GetEncodeBuffer(Transport.HeaderLength);
                var broadcast = Transport.GetBroadcastAddress();
                NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast);
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
                    Log.Debug($"Sending Remote Whois to {bbmdIP}");
                else
                    Log.Warn("The given address do not match with the IP version");
            }
            catch (Exception ex)
            {
                Log.Error("Error on Sending Whois to remote BBMD (Wrong Transport, not IP ?)", ex);
            }

        }

        public void WhoIs(int lowLimit = -1, int highLimit = -1, BacnetAddress receiver = null)
        {
            if (receiver == null)
            {
                // _receiver could be an unicast @ : for direct acces 
                // usefull on BIP for a known IP:Port, unknown device Id
                receiver = Transport.GetBroadcastAddress();
                Log.Debug("Broadcasting WhoIs");
            }
            else
            {
                Log.Debug($"Sending WhoIs to {receiver}");
            }

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, receiver);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
            Services.EncodeWhoIsBroadcast(b, lowLimit, highLimit);

            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
        }

        public void Iam(uint deviceId, BacnetSegmentations segmentation = BacnetSegmentations.SEGMENTATION_BOTH, BacnetAddress receiver = null)
        {
            if (receiver == null)
            {
                receiver = Transport.GetBroadcastAddress();
                Log.Debug($"Broadcasting Iam {deviceId}");
            }
            else
            {
                Log.Debug($"Sending Iam {deviceId} to {receiver}");
            }

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, receiver);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);
            Services.EncodeIamBroadcast(b, deviceId, (uint)GetMaxApdu(), segmentation, VendorId);

            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
        }

        // ReSharper disable once InconsistentNaming
        public void IHave(BacnetObjectId deviceId, BacnetObjectId objId, string objName)
        {
            Log.Debug($"Broadcasting IHave {objName} {objId}");

            var b = GetEncodeBuffer(Transport.HeaderLength);
            var broadcast = Transport.GetBroadcastAddress();
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_HAVE);
            Services.EncodeIhaveBroadcast(b, deviceId, objId, objName);

            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, broadcast, false, 0);

        }

        public void SendUnconfirmedEventNotification(BacnetAddress address, StateTransition eventData)
        {
            Log.Debug($"Sending Event Notification {eventData.EventType} {eventData.EventObjectIdentifier}");

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, address);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION);
            Services.EncodeEventNotifyData(b, eventData);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, address, false, 0);
        }

        public void SendConfirmedServiceReject(BacnetAddress address, byte invokeId, BacnetRejectReason reason)
        {
            Log.Debug($"Sending Service reject: {reason}");

            var b = GetEncodeBuffer(Transport.HeaderLength);

            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, address);
            APDU.EncodeError(b, BacnetPduTypes.PDU_TYPE_REJECT, (BacnetConfirmedServices)reason, invokeId);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, address, false, 0);
        }

        public void SynchronizeTime(BacnetAddress address, DateTime dateTime)
        {
            Log.Debug($"Sending Time Synchronize: {dateTime} {dateTime.Kind.ToString().ToUpper()}");

            var buffer = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, address);
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, dateTime.Kind == DateTimeKind.Utc
                    ? BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION
                    : BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION);
            Services.EncodeTimeSync(buffer, dateTime);
            Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, address, false, 0);
        }

        public int GetMaxApdu()
        {
            int maxAPDU;
            switch (Transport.MaxAdpuLength)
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

        public void WriteFileRequest(BacnetAddress address, BacnetObjectId objectId, ref int position, int count, byte[] fileBuffer)
        {
            using (var request = BeginWriteFileRequest(address, objectId, position, count, fileBuffer, true))
                EndWriteFileRequest(request, out position);
        }

        public BacnetAsyncResult BeginWriteFileRequest(BacnetAddress address, BacnetObjectId objectId, int position, int count, byte[] fileBuffer, bool waitForTransmit = false)
        {
            Log.Debug("Sending AtomicWriteFileRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE,
                buffer => Services.EncodeAtomicWriteFile(buffer, true, objectId, position, 1, new[] { fileBuffer }, new[] { count }), waitForTransmit);
        }

        public void EndWriteFileRequest(BacnetAsyncResult request, out int position)
        {
            using (request)
            {
                position = request.GetResult(Timeout, Retries, r =>
                {
                    if (Services.DecodeAtomicWriteFileAcknowledge(r.Result, 0, r.Result.Length, out _, out var positionValue) < 0)
                        throw new Exception("Failed to decode AtomicWriteFileAcknowledge");
                    return positionValue;
                });
            }
        }

        public BacnetAsyncResult BeginReadFileRequest(BacnetAddress address, BacnetObjectId objectId, int position, uint count, bool waitForTransmit = false)
        {
            Log.Debug("Sending AtomicReadFileRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE,
                buffer => Services.EncodeAtomicReadFile(buffer, true, objectId, position, count), waitForTransmit);
        }

        public void EndReadFileRequest(BacnetAsyncResult result, out uint count, out int position, out bool endOfFile, out byte[] fileBuffer, out int fileBufferOffset)
        {
            using (result)
            {
                var values = result.GetResult(Timeout, Retries, res =>
                {
                    var decodedBytesCount = Services.DecodeAtomicReadFileAcknowledge(res.Result, 0, res.Result.Length, out var endOfFileValue, out _,
                        out var positionValue, out var countValue, out var fileBufferValue, out var fileBufferOffsetValue);

                    if (decodedBytesCount < 0)
                        throw new Exception("Failed to decode AtomicReadFileAcknowledge");

                    return Tuple.Create(countValue, positionValue, endOfFileValue, fileBufferValue, fileBufferOffsetValue);
                });

                count = values.Item1;
                position = values.Item2;
                endOfFile = values.Item3;
                fileBuffer = values.Item4;
                fileBufferOffset = values.Item5;
            }
        }

        public void ReadFileRequest(BacnetAddress address, BacnetObjectId objectId, ref int position, ref uint count, out bool endOfFile, out byte[] fileBuffer, out int fileBufferOffset)
        {
            using (var request = BeginReadFileRequest(address, objectId, position, count, true))
                EndReadFileRequest(request, out count, out position, out endOfFile, out fileBuffer, out fileBufferOffset);
        }

        public BacnetAsyncResult BeginReadRangeRequest(BacnetAddress address, BacnetObjectId objectId,  uint idxBegin, uint quantity, bool waitForTransmit = false)
        {
            Log.Debug("Sending ReadRangeRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE,
                buffer => Services.EncodeReadRange(buffer, objectId, (uint)BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL,
                    BacnetReadRangeRequestTypes.RR_BY_POSITION, idxBegin, DateTime.Now, (int)quantity), waitForTransmit);
        }

        public void EndReadRangeRequest(BacnetAsyncResult request, out byte[] trendBuffer, out uint itemCount)
        {
            using (request)
            {
                var result = request.GetResult(TimeSpan.FromSeconds(40), Retries, r =>
                {
                    var itemCountValue = Services.DecodeReadRangeAcknowledge(r.Result, 0, r.Result.Length, out var trendBufferValue);
                    if (itemCountValue == 0)
                        throw new Exception("Failed to decode ReadRangeAcknowledge");
                    return Tuple.Create(itemCountValue, trendBufferValue);
                });

                itemCount = result.Item1;
                trendBuffer = result.Item2;
            }
        }

        public void ReadRangeRequest(BacnetAddress address, BacnetObjectId objectId, uint idxBegin, ref uint quantity, out byte[] range)
        {
            using (var asyncResult = BeginReadRangeRequest(address, objectId, idxBegin, quantity, true))
                EndReadRangeRequest(asyncResult, out range, out quantity); // quantity read could be less than demanded
        }

        public void SubscribeCOVRequest(BacnetAddress address, BacnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime)
        {
            using (var asyncResult = BeginSubscribeCOVRequest(address, objectId, subscribeId, cancel, issueConfirmedNotifications, lifetime, true))
                EndSubscribeCOVRequest(asyncResult);
        }

        public BacnetAsyncResult BeginSubscribeCOVRequest(BacnetAddress address, BacnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime, bool waitForTransmit = false)
        {
            Log.Debug($"Sending SubscribeCOVRequest {objectId}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV,
                buffer => Services.EncodeSubscribeCOV(buffer, subscribeId, objectId, cancel, issueConfirmedNotifications, lifetime), waitForTransmit);
        }

        public void EndSubscribeCOVRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public void SubscribePropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyReference monitoredProperty, uint subscribeId, bool cancel, bool issueConfirmedNotifications)
        {
            using (var asyncResult = BeginSubscribePropertyRequest(address, objectId, monitoredProperty, subscribeId, cancel, issueConfirmedNotifications, true))
                EndSubscribePropertyRequest(asyncResult);
        }

        public BacnetAsyncResult BeginSubscribePropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyReference monitoredProperty, uint subscribeId, bool cancel, bool issueConfirmedNotifications, bool waitForTransmit)
        {
            Log.Debug($"Sending SubscribePropertyRequest {objectId}.{monitoredProperty}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY,
                buffer => Services.EncodeSubscribeProperty(buffer, subscribeId, objectId, cancel, issueConfirmedNotifications, 0, monitoredProperty, false, 0f), waitForTransmit);
        }

        public void EndSubscribePropertyRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public BacnetValue ReadPropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, uint index)
        {
            if (index == ASN1.BACNET_ARRAY_ALL) throw new ArgumentOutOfRangeException(nameof(index));
            using (var asyncResult = BeginReadPropertyRequest(address, objectId, propertyId, index))
                return EndReadPropertyRequest(asyncResult).Single();
        }

        public IList<BacnetValue> ReadPropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId)
        {
            using (var asyncResult = BeginReadPropertyRequest(address, objectId, propertyId))
                return EndReadPropertyRequest(asyncResult);
        }

        public Task<BacnetValue> ReadPropertyAsync(BacnetAddress address, BacnetObjectTypes objType, uint objInstance, BacnetPropertyIds propertyId, uint index)
            => ReadPropertyAsync(address, new BacnetObjectId(objType, objInstance), propertyId, index);

        public Task<BacnetValue> ReadPropertyAsync(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, uint index)
        {
            if (index == ASN1.BACNET_ARRAY_ALL) throw new ArgumentOutOfRangeException(nameof(index));
            var exceptionMessage = $"Failed to read property {propertyId}{(index == ASN1.BACNET_ARRAY_ALL? "" : $"[{index}]")} of {objectId} from {address}";
            return CallAsync(() => ReadPropertyRequest(address, objectId, propertyId, index), exceptionMessage);
        }

        public Task<IList<BacnetValue>> ReadPropertyAsync(BacnetAddress address, BacnetObjectTypes objType, uint objInstance, BacnetPropertyIds propertyId)
            => ReadPropertyAsync(address, new BacnetObjectId(objType, objInstance), propertyId);

        public Task<IList<BacnetValue>> ReadPropertyAsync(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId)
        {
            var exceptionMessage = $"Failed to read property {propertyId} of {objectId} from {address}";
            return CallAsync(() => ReadPropertyRequest(address, objectId, propertyId), exceptionMessage);
        }

        public BacnetAsyncResult BeginReadPropertyRequest(BacnetAddress address, BacnetObjectId objectId,
            BacnetPropertyIds propertyId, uint index = ASN1.BACNET_ARRAY_ALL, bool waitForTransmit = true)
        {
            var propertyIndex = index == ASN1.BACNET_ARRAY_ALL ? "" : $"[{index}]";
            Log.Debug($"Sending ReadPropertyRequest {objectId} {propertyId}{propertyIndex}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY,
                buffer => Services.EncodeReadProperty(buffer, objectId, (uint)propertyId, index), waitForTransmit);
        }

        public IList<BacnetValue> EndReadPropertyRequest(BacnetAsyncResult request)
        {
            using (request)
            {
                 return request.GetResult(Timeout, Retries, r =>
                {
                    var byteCount = Services.DecodeReadPropertyAcknowledge(r.Address, r.Result, 0, r.Result.Length,
                        out _, out _, out var valueList);

                    if (byteCount < 0)
                        throw new Exception("Failed to decode ReadPropertyAcknowledge");

                    return valueList;
                });   
            }
        }

        public void WritePropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetValue value)
        {
            WritePropertyRequest(address, objectId, propertyId, new[] { value });
        }

        public void WritePropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList)
        {
            using (var asyncResult = BeginWritePropertyRequest(address, objectId, propertyId, valueList))
                EndWritePropertyRequest(asyncResult);
        }

        public void WritePropertyMultipleRequest(BacnetAddress address, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList)
        {
            using (var asyncResult = BeginWritePropertyMultipleRequest(address, objectId, valueList))
                EndWritePropertyMultipleRequest(asyncResult);
        }

        public BacnetAsyncResult BeginWritePropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, bool waitForTransmit = true)
        {
            Log.Debug($"Sending WritePropertyRequest {objectId} {propertyId}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY,
                buffer => Services.EncodeWriteProperty(buffer, objectId, (uint)propertyId, ASN1.BACNET_ARRAY_ALL, _writepriority, valueList), waitForTransmit);
        }

        public void EndWritePropertyRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public BacnetAsyncResult BeginWritePropertyMultipleRequest(BacnetAddress address, BacnetObjectId objectId,
            ICollection<BacnetPropertyValue> valueList, bool waitForTransmit = true)
        {
            Log.Debug($"Sending WritePropertyMultipleRequest {objectId}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE,
                buffer => Services.EncodeWritePropertyMultiple(buffer, objectId, valueList), waitForTransmit);
        }

        public void EndWritePropertyMultipleRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public void WritePropertyMultipleRequest(BacnetAddress address, ICollection<BacnetReadAccessResult> valueList)
        {
            using (var asyncResult = BeginWritePropertyMultipleRequest(address, valueList))
                EndWritePropertyMultipleRequest(asyncResult);
        }

        public BacnetAsyncResult BeginWritePropertyMultipleRequest(BacnetAddress address, ICollection<BacnetReadAccessResult> valueList, bool waitForTransmit = true)
        {
            var objectIds = string.Join(", ", valueList.Select(v => v.objectIdentifier));
            Log.Debug($"Sending WritePropertyMultipleRequest {objectIds}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE,
                buffer => Services.EncodeWriteObjectMultiple(buffer, valueList), waitForTransmit);
        }

        public IList<BacnetReadAccessResult> ReadPropertyMultipleRequest(BacnetAddress address, BacnetObjectId objectId, IList<BacnetPropertyReference> propertyIdAndIndex)
        {
            using (var asyncResult = BeginReadPropertyMultipleRequest(address, objectId, propertyIdAndIndex))
                return EndReadPropertyMultipleRequest(asyncResult);
        }

        public Task<IList<BacnetPropertyValue>> ReadPropertyMultipleAsync(BacnetAddress address, BacnetObjectTypes objType, uint objInstance, params BacnetPropertyIds[] propertyIds)
        {
            var objectId = new BacnetObjectId(objType, objInstance);
            return ReadPropertyMultipleAsync(address, objectId, propertyIds);
        }

        public Task<IList<BacnetPropertyValue>> ReadPropertyMultipleAsync(BacnetAddress address, BacnetObjectId objectId, params BacnetPropertyIds[] propertyIds)
        {
            var propertyReferences = propertyIds.Select(p => new BacnetPropertyReference((uint)p, ASN1.BACNET_ARRAY_ALL)).ToArray(); 
            return CallAsync(() => ReadPropertyMultipleRequest(address, objectId, propertyReferences).Single().values,
                $"Failed to read multiple properties of {objectId} from {address}");
        }

        public IList<BacnetReadAccessResult> ReadPropertyMultipleRequest(BacnetAddress address, IList<BacnetReadAccessSpecification> properties)
        {
            using (var asyncResult = BeginReadPropertyMultipleRequest(address, properties))
                return EndReadPropertyMultipleRequest(asyncResult);
        }

        public BacnetAsyncResult BeginReadPropertyMultipleRequest(BacnetAddress address, BacnetObjectId objectId, IList<BacnetPropertyReference> propertyIdAndIndex, bool waitForTransmit = true)
        {
            var properties = new[] { new BacnetReadAccessSpecification(objectId, propertyIdAndIndex) };
            return BeginReadPropertyMultipleRequest(address, properties, waitForTransmit);
        }

        public BacnetAsyncResult BeginReadPropertyMultipleRequest(BacnetAddress address, IList<BacnetReadAccessSpecification> properties, bool waitForTransmit = true)
        {
            var objectIds = string.Join(", ", properties.Select(v => v.objectIdentifier));
            Log.Debug($"Sending ReadPropertyMultipleRequest {objectIds}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE,
                buffer => Services.EncodeReadPropertyMultiple(buffer, properties), waitForTransmit);
        }

        public IList<BacnetReadAccessResult> EndReadPropertyMultipleRequest(BacnetAsyncResult request)
        {
            using (request)
            {
                return request.GetResult(Timeout, Retries, r =>
                {
                    var byteCount =
                        Services.DecodeReadPropertyMultipleAcknowledge(
                            r.Address, r.Result, 0, r.Result.Length, out var values);

                    if (byteCount < 0)
                        throw new Exception("Failed to decode ReadPropertyMultipleAcknowledge");

                    return values;
                });
            }
        }

        public void CreateObjectRequest(BacnetAddress address, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList = null)
        {
            using (var request = BeginCreateObjectRequest(address, objectId, valueList, true))
                EndCreateObjectRequest(request);
        }

        public BacnetAsyncResult BeginCreateObjectRequest(BacnetAddress address, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, bool waitForTransmit = false)
        {
            Log.Debug("Sending CreateObjectRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT,
                buffer => Services.EncodeCreateProperty(buffer, objectId, valueList), waitForTransmit);
        }

        public void EndCreateObjectRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public void DeleteObjectRequest(BacnetAddress address, BacnetObjectId objectId)
        {
            using (var result = BeginDeleteObjectRequest(address, objectId, true))
                EndDeleteObjectRequest(result);
        }

        public BacnetAsyncResult BeginDeleteObjectRequest(BacnetAddress address, BacnetObjectId objectId, bool waitForTransmit = false)
        {
            Log.Debug("Sending DeleteObjectRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT,
                buffer => ASN1.encode_application_object_id(buffer, objectId.Type, objectId.Instance), waitForTransmit);
        }

        public void EndDeleteObjectRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public void AddListElementRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList)
        {
            using (var request = BeginAddListElementRequest(address, objectId,reference,valueList, true))
                EndAddListElementRequest(request);
        }

        public void RemoveListElementRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList)
        {
            using (var request = BeginRemoveListElementRequest(address, objectId, reference, valueList, true))
                EndRemoveListElementRequest(request);
        }

        public BacnetAsyncResult BeginRemoveListElementRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, bool waitForTransmit = false)
        {
            Log.Debug("Sending RemoveListElementRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT,
                buffer => Services.EncodeAddListElement(buffer, objectId, reference.propertyIdentifier, reference.propertyArrayIndex, valueList), waitForTransmit);
        }

        public BacnetAsyncResult BeginAddListElementRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, bool waitForTransmit = false)
        {
            Log.Debug($"Sending AddListElementRequest {objectId} {(BacnetPropertyIds)reference.propertyIdentifier}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_ADD_LIST_ELEMENT,
                buffer => Services.EncodeAddListElement(buffer, objectId, reference.propertyIdentifier, reference.propertyArrayIndex, valueList), waitForTransmit);
        }

        public void EndAddListElementRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public void EndRemoveListElementRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        /// <summary>
        /// Read or Write without APDU Data encoding nor Decoding (just Request type, Object id and Property id)
        /// </summary>
        /// <remarks>
        /// Data is given by the caller starting with the Tag 3 (or maybe another one), and ending with it
        /// </remarks>
        /// <returns>Return buffer start also with the Tag 3</returns>
        public byte[] RawEncodedDecodedPropertyConfirmedRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetConfirmedServices serviceId, byte[] inBuffer)
        {
            using (var result = BeginRawEncodedDecodedPropertyConfirmedRequest(address, objectId, propertyId, serviceId, inBuffer, true))
                return EndRawEncodedDecodedPropertyConfirmedRequest(result, serviceId);
        }

        public BacnetAsyncResult BeginRawEncodedDecodedPropertyConfirmedRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetConfirmedServices serviceId, byte[] inBuffer, bool waitForTransmit = false)
        {
            Log.Debug("Sending RawEncodedDecodedProperty");
            return BeginConfirmedServiceRequest(address, serviceId, buffer =>
            {
                ASN1.encode_context_object_id(buffer, 0, objectId.Type, objectId.Instance);
                ASN1.encode_context_enumerated(buffer, 1, (byte) propertyId);
                if (inBuffer?.Length > 0)
                    buffer.Add(inBuffer, inBuffer.Length); // No content encoding
            }, waitForTransmit);
        }

        public byte[] EndRawEncodedDecodedPropertyConfirmedRequest(BacnetAsyncResult request, BacnetConfirmedServices serviceId)
        {
            using (request)
            {
                return request.GetResult(Timeout, Retries, r =>
                {
                    if (serviceId != BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY)
                        return null;

                    //decode
                    const int offset = 0;
                    var buffer = r.Result;

                    if (!ASN1.decode_is_context_tag(buffer, offset, 0))
                        throw new Exception("Failed to decode");
                    var len = 1;
                    len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes _, out _);
                    /* Tag 1: Property ID */
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
                    if (tagNumber != 1)
                        throw new Exception("Failed to decode");
                    len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out _);

                    var outBuffer = new byte[buffer.Length - len];
                    Array.Copy(buffer, len, outBuffer, 0, outBuffer.Length);
                    return outBuffer;
                });
            }
        }

        public void DeviceCommunicationControlRequest(BacnetAddress address, uint timeDuration, uint enableDisable, string password)
        {
            using (var request = BeginDeviceCommunicationControlRequest(address, timeDuration, enableDisable, password, true))
                EndDeviceCommunicationControlRequest(request);
        }

        public BacnetAsyncResult BeginDeviceCommunicationControlRequest(BacnetAddress address, uint timeDuration, uint enableDisable, string password, bool waitForTransmit = false)
        {
            Log.Debug("Sending DeviceCommunicationControlRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL,
                buffer => Services.EncodeDeviceCommunicationControl(buffer, timeDuration, enableDisable, password), waitForTransmit);
        }

        public void EndDeviceCommunicationControlRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public IList<BacnetAlarmSummaryData> GetAlarmSummaryRequest(BacnetAddress address)
        {
            using (var request = BeginGetAlarmSummaryRequest(address, true))
                return EndGetAlarmSummaryRequest(request);
        }

        public IList<BacnetGetEventInformationData> GetEventsRequest(BacnetAddress address)
        {
            var events = new List<BacnetGetEventInformationData>();

            while (true)
            {
                var lastEventObjectId = events.Count > 0 ? events.Last().objectIdentifier : default(BacnetObjectId?);
                using (var request = BeginGetEventsRequest(address, lastEventObjectId, true))
                {
                    events.AddRange(EndGetEventsRequest(request, out var moreEvents));
                    if (!moreEvents) break;
                }
            }

            return events;
        }

        public Task<IList<BacnetGetEventInformationData>> GetEventsAsync(BacnetAddress address)
        {
            return CallAsync(() => GetEventsRequest(address), $"Failed to get events from {address}");
        }

        public BacnetAsyncResult BeginGetAlarmSummaryRequest(BacnetAddress address, bool waitForTransmit = false)
        {
            Log.Debug("Sending AlarmSummary request");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY, null, waitForTransmit);
        }

        public BacnetAsyncResult BeginGetEventsRequest(BacnetAddress address, BacnetObjectId? lastEventObjectId = null, bool waitForTransmit = false)
        {
            Log.Debug("Sending Events request");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION,
                buffer => Services.EncodeGetEventInformation(buffer, lastEventObjectId), waitForTransmit);
        }

        public IList<BacnetAlarmSummaryData> EndGetAlarmSummaryRequest(BacnetAsyncResult request)
        {
            using (request)
            {
                return request.GetResult(Timeout, Retries, r =>
                {
                    IList<BacnetAlarmSummaryData> alarms = new List<BacnetAlarmSummaryData>();
                    if (Services.DecodeAlarmSummary(r.Result, 0, r.Result.Length, ref alarms) < 0)
                        throw new Exception("Failed to decode AlarmSummary");
                    return alarms;
                });
            }
        }

        public IList<BacnetGetEventInformationData> EndGetEventsRequest(BacnetAsyncResult request, out bool moreEvents)
        {
            using (request)
            {
                var result = request.GetResult(Timeout, Retries, r =>
                {
                    IList<BacnetGetEventInformationData> events = new List<BacnetGetEventInformationData>();
                    if (Services.DecodeEventInformation(r.Result, 0, r.Result.Length, ref events, out var moreEventsValue) < 0)
                        throw new Exception("Failed to decode Events");
                    return Tuple.Create(events, moreEventsValue);
                });

                moreEvents = result.Item2;
                return result.Item1;
            }
        }

        public void AlarmAcknowledgement(BacnetAddress address, BacnetObjectId objId, BacnetEventStates eventState, string ackText, BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp)
        {
            using (var request = BeginAlarmAcknowledgement(address, objId, eventState, ackText, evTimeStamp, ackTimeStamp, true))
                EndAlarmAcknowledgement(request);
        }

        public BacnetAsyncResult BeginAlarmAcknowledgement(BacnetAddress address, BacnetObjectId objId, BacnetEventStates eventState, string ackText,
            BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp, bool waitForTransmit = false)
        {
            Log.Debug("Sending AlarmAcknowledgement");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM,
                buffer => Services.EncodeAlarmAcknowledge(buffer, 57, objId, (uint)eventState, ackText, evTimeStamp, ackTimeStamp), waitForTransmit);
        }

        public void EndAlarmAcknowledgement(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public void ReinitializeRequest(BacnetAddress address, BacnetReinitializedStates state, string password)
        {
            using (var request = BeginReinitializeRequest(address, state, password, true))
                EndReinitializeRequest(request);
        }

        public BacnetAsyncResult BeginReinitializeRequest(BacnetAddress address, BacnetReinitializedStates state, string password, bool waitForTransmit = false)
        {
            Log.Debug("Sending ReinitializeRequest");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE,
                buffer => Services.EncodeReinitializeDevice(buffer, state, password), waitForTransmit);
        }

        public void EndReinitializeRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public BacnetAsyncResult BeginConfirmedNotify(BacnetAddress address, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier,
            BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IList<BacnetPropertyValue> values, bool waitForTransmit = false)
        {
            Log.Debug("Sending Notify (confirmed)");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION,
                buffer => Services.EncodeCOVNotifyConfirmed(buffer, subscriberProcessIdentifier, initiatingDeviceIdentifier,
                monitoredObjectIdentifier, timeRemaining, values), waitForTransmit);
        }

        public void EndConfirmedNotify(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

        public bool Notify(BacnetAddress address, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier,
            uint timeRemaining, bool issueConfirmedNotifications, IList<BacnetPropertyValue> values)
        {
            if (!issueConfirmedNotifications)
            {
                Log.Debug("Sending Notify (unconfirmed)");
                var buffer = GetEncodeBuffer(Transport.HeaderLength);
                NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, address.RoutedSource);
                APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);
                Services.EncodeCOVNotifyUnconfirmed(buffer, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);
               // Modif F. Chaxel
                
                var sendbytes=Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, address, false, 0);
                return sendbytes == buffer.offset;
            }

            using (var result = BeginConfirmedNotify(address, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values, true))
                EndConfirmedNotify(result);

            return false;
        }

        public void LifeSafetyOperationRequest(BacnetAddress address, BacnetObjectId objectId, string requestingSrc, BacnetLifeSafetyOperations operation)
        {
            using (var request = BeginLifeSafetyOperationRequest(address, objectId, 0, requestingSrc, operation, true))
                EndLifeSafetyOperationRequest(request);
        }

        public BacnetAsyncResult BeginLifeSafetyOperationRequest(BacnetAddress address, BacnetObjectId objectId, uint processId, string requestingSrc, 
            BacnetLifeSafetyOperations operation, bool waitForTransmit = false)
        {
            Log.Debug($"Sending {ToTitleCase(operation)} {objectId}");
            return BeginConfirmedServiceRequest(address, BacnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION,
                buffer => Services.EncodeLifeSafetyOperation(buffer, processId, requestingSrc, (uint)operation, objectId), waitForTransmit);
        }

        public void EndLifeSafetyOperationRequest(BacnetAsyncResult request) => EndConfirmedServiceRequest(request);

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
            if (maxSegments == BacnetMaxSegments.MAX_SEG0)
                return null;

            return new Segmentation
            {
                buffer = GetEncodeBuffer(Transport.HeaderLength),
                max_segments = GetSegmentsCount(maxSegments),
                window_size = ProposedWindowSize
            };
        }

        private EncodeBuffer EncodeSegmentHeader(BacnetAddress address, byte invokeId, Segmentation segmentation, BacnetConfirmedServices service, bool moreFollows)
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
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, address.RoutedSource);

            //set segments limits
            buffer.max_offset = buffer.offset + GetMaxApdu();
            var apduHeader = APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK | (isSegmented ? BacnetPduTypes.SEGMENTED_MESSAGE | BacnetPduTypes.SERVER : 0) | (moreFollows ? BacnetPduTypes.MORE_FOLLOWS : 0), service, invokeId, segmentation?.sequence_number ?? 0, segmentation?.window_size ?? 0);
            buffer.min_limit = (GetMaxApdu() - apduHeader) * (segmentation?.sequence_number ?? 0);

            return buffer;
        }

        private bool EncodeSegment(BacnetAddress address, byte invokeId, Segmentation segmentation, BacnetConfirmedServices service, out EncodeBuffer buffer, Action<EncodeBuffer> apduContentEncode)
        {
            //encode (regular)
            buffer = EncodeSegmentHeader(address, invokeId, segmentation, service, false);
            apduContentEncode(buffer);

            var moreFollows = (buffer.result & EncodeResult.NotEnoughBuffer) > 0;
            if (segmentation != null && moreFollows)
            {
                //reencode in segmented
                EncodeSegmentHeader(address, invokeId, segmentation, service, true);
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
        private void HandleSegmentationResponse(BacnetAddress address, byte invokeId, Segmentation segmentation, Action<Segmentation> transmit)
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
                            Log.Warn("Transmit timeout");
                            break;
                        }

                        var currentNumber = segmentation.sequence_number;

                        if (!WaitForSegmentAck(address, invokeId, segmentation, Timeout))
                        {
                            Log.Warn("Didn't get segmentACK");
                            break;
                        }

                        if (segmentation.sequence_number != currentNumber)
                        {
                            Log.Debug("Oh, a retransmit");
                            moreFollows = true;
                        }
                    }
                    else
                    {
                        // a negative segmentACK perhaps
                        var currentNumber = segmentation.sequence_number;
                        WaitForSegmentAck(address, invokeId, segmentation, TimeSpan.Zero); // don't wait

                        if (segmentation.sequence_number != currentNumber)
                            Log.Debug("Oh, a retransmit");
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

        private void SendComplexAck(BacnetAddress address, byte invokeId, Segmentation segmentation, BacnetConfirmedServices service, Action<EncodeBuffer> apduContentEncode)
        {
            Log.Debug($"Sending {ToTitleCase(service)}");

            //encode
            if (EncodeSegment(address, invokeId, segmentation, service, out var buffer, apduContentEncode))
            {
                //client doesn't support segments
                if (segmentation == null)
                {
                    Log.Info("Segmenation denied");
                    ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                    buffer.result = EncodeResult.Good;     //don't continue the segmentation
                    return;
                }

                //first segment? validate max segments
                if (segmentation.sequence_number == 0)  //only validate first segment
                {
                    if (segmentation.max_segments != 0xFF && segmentation.buffer.offset > segmentation.max_segments * (GetMaxApdu() - 5))      //5 is adpu header
                    {
                        Log.Info("Too much segmenation");
                        ErrorResponse(address, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                        buffer.result = EncodeResult.Good;     //don't continue the segmentation
                        return;
                    }
                    Log.Debug("Segmentation required");
                }

                //increment before ack can do so (race condition)
                unchecked { segmentation.sequence_number++; };
            }

            //send
            Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.GetLength() - Transport.HeaderLength, address, false, 0);
        }

        public void ReadPropertyResponse(BacnetAddress address, byte invokeId, Segmentation segmentation, BacnetObjectId objectId, BacnetPropertyReference property, IEnumerable<BacnetValue> value)
        {
            HandleSegmentationResponse(address, invokeId, segmentation, o =>
            {
                SendComplexAck(address, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, b =>
                {
                    Services.EncodeReadPropertyAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, value);
                });
            });
        }

        public void CreateObjectResponse(BacnetAddress address, byte invokeId, Segmentation segmentation, BacnetObjectId objectId)
        {
            SendComplexAck(address, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, b =>
            {
                Services.EncodeCreateObjectAcknowledge(b, objectId);
            });
        }

        public void ReadPropertyMultipleResponse(BacnetAddress address, byte invokeId, Segmentation segmentation, IList<BacnetReadAccessResult> values)
        {
            HandleSegmentationResponse(address, invokeId, segmentation, o =>
            {
                SendComplexAck(address, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, b =>
                {
                    Services.EncodeReadPropertyMultipleAcknowledge(b, values);
                });
            });
        }

        public void ReadRangeResponse(BacnetAddress address, byte invokeId, Segmentation segmentation, BacnetObjectId objectId, BacnetPropertyReference property, BacnetResultFlags status, uint itemCount, byte[] applicationData, BacnetReadRangeRequestTypes requestType, uint firstSequenceNo)
        {
            HandleSegmentationResponse(address, invokeId, segmentation, o =>
            {
                SendComplexAck(address, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, b =>
                {
                    Services.EncodeReadRangeAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, BacnetBitString.ConvertFromInt((uint)status), itemCount, applicationData, requestType, firstSequenceNo);
                });
            });
        }

        public void ReadFileResponse(BacnetAddress address, byte invokeId, Segmentation segmentation, int position, uint count, bool endOfFile, byte[] fileBuffer)
        {
            HandleSegmentationResponse(address, invokeId, segmentation, o =>
            {
                SendComplexAck(address, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, b =>
                {
                    Services.EncodeAtomicReadFileAcknowledge(b, true, endOfFile, position, 1, new[] { fileBuffer }, new[] { (int)count });
                });
            });
        }

        public void WriteFileResponse(BacnetAddress address, byte invokeId, Segmentation segmentation, int position)
        {
            SendComplexAck(address, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, b =>
            {
                Services.EncodeAtomicWriteFileAcknowledge(b, true, position);
            });
        }

        public void ErrorResponse(BacnetAddress address, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode)
        {
            Log.Debug($"Sending ErrorResponse for {service}: {errorCode}");
            var buffer = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, address.RoutedSource);
            APDU.EncodeError(buffer, BacnetPduTypes.PDU_TYPE_ERROR, service, invokeId);
            Services.EncodeError(buffer, errorClass, errorCode);
            Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, address, false, 0);
        }

        public void SimpleAckResponse(BacnetAddress address, BacnetConfirmedServices service, byte invokeId)
        {
            Log.Debug($"Sending SimpleAckResponse for {service}");
            var buffer = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, address.RoutedSource);
            APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, service, invokeId);
            Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, address, false, 0);
        }

        public void SegmentAckResponse(BacnetAddress address, bool negative, bool server, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize)
        {
            Log.Debug("Sending SegmentAckResponse");
            var buffer = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, address.RoutedSource);
            APDU.EncodeSegmentAck(buffer, BacnetPduTypes.PDU_TYPE_SEGMENT_ACK | (negative ? BacnetPduTypes.NEGATIVE_ACK : 0) | (server ? BacnetPduTypes.SERVER : 0), originalInvokeId, sequenceNumber, actualWindowSize);
            Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.offset - Transport.HeaderLength, address, false, 0);
        }

        public bool WaitForAllTransmits(TimeSpan timeout)
        {
            return Transport.WaitForAllTransmits((int)timeout.TotalMilliseconds);
        }

        public bool WaitForSegmentAck(BacnetAddress address, byte invokeId, Segmentation segmentation, TimeSpan timeout)
        {
            if (!_lastSegmentAck.Wait(address, invokeId, timeout))
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

        public BacnetAsyncResult BeginConfirmedServiceRequest(BacnetAddress address, BacnetConfirmedServices service, Action<EncodeBuffer> encode, bool waitForTransmit = true)
        {
            var invokeId = unchecked(_invokeId++);
            var buffer = GetEncodeBuffer(Transport.HeaderLength);
            var function = BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply;
            
            NPDU.Encode(buffer, function, address.RoutedSource);
            APDU.EncodeConfirmedServiceRequest(buffer, PduConfirmedServiceRequest(), service, MaxSegments, Transport.MaxAdpuLength, invokeId);
            encode?.Invoke(buffer);

            var transmitLength = buffer.offset - Transport.HeaderLength;
            var asyncResult = new BacnetAsyncResult(this, address, invokeId, buffer.buffer, transmitLength, waitForTransmit, TransmitTimeout);
            return asyncResult.Send();
        }

        private BacnetPduTypes PduConfirmedServiceRequest()
        {
            return MaxSegments != BacnetMaxSegments.MAX_SEG0
                ? BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED
                : BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST;
        }

        public void EndConfirmedServiceRequest(BacnetAsyncResult request)
        {
            using (request)
            {
                request.GetResult(Timeout, Retries);
            }
        }

        private static Task<TResult> CallAsync<TResult>(Func<TResult> func, string exceptionMessage)
        {
            return Task<TResult>.Factory.StartNew(() =>
            {
                try
                {
                    return func();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new Exception(exceptionMessage, e);
                }
            });
        }

        public void Dispose()
        {
            Transport.Dispose();
        }
    }
}
