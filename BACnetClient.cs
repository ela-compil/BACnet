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

using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
        private byte _lastSequenceNumber;

        /// <summary>
        /// only used when 'DefaultSegmentationHandling' = true
        /// </summary>
        private readonly LinkedList<byte[]> _segments = new LinkedList<byte[]>();

        private readonly LastSegmentAck _lastSegmentAck = new LastSegmentAck();
        private uint _writepriority;

        public const byte DEFAULT_HOP_COUNT = 0xFF;
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

        /// <summary>
        /// Used as the number of tentatives
        /// </summary>
        public int Retries
        {
            get { return _retries; }
            set { _retries = Math.Max(1, value); }
        }

        public uint WritePriority
        {
            get { return _writepriority; }
            set { if (value < 17) _writepriority = value; }
        }

        // These members allows to access undecoded buffer by the application
        // layer, when the basic undecoding process is not really able to do the job
        // in particular with application_specific_encoding values
        public byte[] raw_buffer;
        public int raw_offset, raw_length;

        private class LastSegmentAck
        {
            private readonly ManualResetEvent _wait = new ManualResetEvent(false);
            private readonly object _lockObject = new object();
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

#if XAMARIN
#else
        public BacnetClient(string portName, int baudRate, int timeout = DEFAULT_TIMEOUT, int retries = DEFAULT_RETRIES)
            : this(new BacnetMstpProtocolTransport(portName, baudRate), timeout, retries)
        {
        }
#endif
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
            Trace.TraceInformation("Started communication");
        }

        public delegate void ConfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length);
        public event ConfirmedServiceRequestHandler OnConfirmedServiceRequest;
        public delegate void ReadPropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, BacnetPropertyReference property, BacnetMaxSegments maxSegments);
        public event ReadPropertyRequestHandler OnReadPropertyRequest;
        public delegate void ReadPropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments maxSegments);
        public event ReadPropertyMultipleRequestHandler OnReadPropertyMultipleRequest;
        public delegate void WritePropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, BacnetPropertyValue value, BacnetMaxSegments maxSegments);
        public event WritePropertyRequestHandler OnWritePropertyRequest;
        public delegate void WritePropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, BacnetObjectId objectId, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments);
        public event WritePropertyMultipleRequestHandler OnWritePropertyMultipleRequest;
        public delegate void AtomicWriteFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, bool isStream, BacnetObjectId objectId, int position, uint blockCount, byte[][] blocks, int[] counts, BacnetMaxSegments maxSegments);
        public event AtomicWriteFileRequestHandler OnAtomicWriteFileRequest;
        public delegate void AtomicReadFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, bool isStream, BacnetObjectId objectId, int position, uint count, BacnetMaxSegments maxSegments);
        public event AtomicReadFileRequestHandler OnAtomicReadFileRequest;
        public delegate void SubscribeCOVRequestHandler(BacnetClient sender, BacnetAddress adr, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetMaxSegments maxSegments);
        public event SubscribeCOVRequestHandler OnSubscribeCOV;
        public delegate void EventNotificationCallbackHandler(BacnetClient sender, BacnetAddress adr, BacnetEventNotificationData eventData);
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

        protected void ProcessConfirmedServiceRequest(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("ConfirmedServiceRequest", null);

                raw_buffer = buffer;
                raw_length = length;
                raw_offset = offset;

                OnConfirmedServiceRequest?.Invoke(this, adr, type, service, maxSegments, maxAdpu, invokeId, buffer, offset, length);

                //don't send segmented messages, if client don't want it
                if ((type & BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED) == 0)
                    maxSegments = BacnetMaxSegments.MAX_SEG0;

                if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY && OnReadPropertyRequest != null)
                {
                    BacnetObjectId objectId;
                    BacnetPropertyReference property;
                    if (Services.DecodeReadProperty(buffer, offset, length, out objectId, out property) >= 0)
                        OnReadPropertyRequest(this, adr, invokeId, objectId, property, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY && OnWritePropertyRequest != null)
                {
                    BacnetObjectId objectId;
                    BacnetPropertyValue value;
                    if (Services.DecodeWriteProperty(buffer, offset, length, out objectId, out value) >= 0)
                        OnWritePropertyRequest(this, adr, invokeId, objectId, value, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeWriteProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE && OnReadPropertyMultipleRequest != null)
                {
                    IList<BacnetReadAccessSpecification> properties;
                    if (Services.DecodeReadPropertyMultiple(buffer, offset, length, out properties) >= 0)
                        OnReadPropertyMultipleRequest(this, adr, invokeId, properties, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadPropertyMultiple");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE && OnWritePropertyMultipleRequest != null)
                {
                    BacnetObjectId objectId;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeWritePropertyMultiple(buffer, offset, length, out objectId, out values) >= 0)
                        OnWritePropertyMultipleRequest(this, adr, invokeId, objectId, values, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeWritePropertyMultiple");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId initiatingDeviceIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    uint timeRemaining;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeCOVNotifyUnconfirmed(buffer, offset, length, out subscriberProcessIdentifier, out initiatingDeviceIdentifier, out monitoredObjectIdentifier, out timeRemaining, out values) >= 0)
                        OnCOVNotification(this, adr, invokeId, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, true, values, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode COVNotify");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE && OnAtomicWriteFileRequest != null)
                {
                    bool isStream;
                    BacnetObjectId objectId;
                    int position;
                    uint blockCount;
                    byte[][] blocks;
                    int[] counts;
                    if (Services.DecodeAtomicWriteFile(buffer, offset, length, out isStream, out objectId, out position, out blockCount, out blocks, out counts) >= 0)
                        OnAtomicWriteFileRequest(this, adr, invokeId, isStream, objectId, position, blockCount, blocks, counts, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode AtomicWriteFile");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE && OnAtomicReadFileRequest != null)
                {
                    bool isStream;
                    BacnetObjectId objectId;
                    int position;
                    uint count;
                    if (Services.DecodeAtomicReadFile(buffer, offset, length, out isStream, out objectId, out position, out count) >= 0)
                        OnAtomicReadFileRequest(this, adr, invokeId, isStream, objectId, position, count, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode AtomicReadFile");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV && OnSubscribeCOV != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    bool cancellationRequest;
                    bool issueConfirmedNotifications;
                    uint lifetime;
                    if (Services.DecodeSubscribeCOV(buffer, offset, length, out subscriberProcessIdentifier, out monitoredObjectIdentifier, out cancellationRequest, out issueConfirmedNotifications, out lifetime) >= 0)
                        OnSubscribeCOV(this, adr, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, cancellationRequest, issueConfirmedNotifications, lifetime, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode SubscribeCOV");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY && OnSubscribeCOVProperty != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    BacnetPropertyReference monitoredProperty;
                    bool cancellationRequest;
                    bool issueConfirmedNotifications;
                    uint lifetime;
                    float covIncrement;
                    if (Services.DecodeSubscribeProperty(buffer, offset, length, out subscriberProcessIdentifier, out monitoredObjectIdentifier, out monitoredProperty, out cancellationRequest, out issueConfirmedNotifications, out lifetime, out covIncrement) >= 0)
                        OnSubscribeCOVProperty(this, adr, invokeId, subscriberProcessIdentifier, monitoredObjectIdentifier, monitoredProperty, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode SubscribeCOVProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL && OnDeviceCommunicationControl != null)
                {
                    uint timeDuration;
                    uint enableDisable;
                    string password;
                    if (Services.DecodeDeviceCommunicationControl(buffer, offset, length, out timeDuration, out enableDisable, out password) >= 0)
                        OnDeviceCommunicationControl(this, adr, invokeId, timeDuration, enableDisable, password, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode DeviceCommunicationControl");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE && OnReinitializedDevice != null)
                {
                    BacnetReinitializedStates state;
                    string password;
                    if (Services.DecodeReinitializeDevice(buffer, offset, length, out state, out password) >= 0)
                        OnReinitializedDevice(this, adr, invokeId, state, password, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode ReinitializeDevice");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION && OnEventNotify != null) // F. Chaxel
                {
                    BacnetEventNotificationData eventData;
                    if (Services.DecodeEventNotifyData(buffer, offset, length, out eventData) >= 0)
                        OnEventNotify(this, adr, eventData);
                    else
                        Trace.TraceWarning("Couldn't decode Event/Alarm Notification");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE && OnReadRange != null)
                {
                    BacnetObjectId objectId;
                    BacnetPropertyReference property;
                    BacnetReadRangeRequestTypes requestType;
                    uint position;
                    DateTime time;
                    int count;
                    if (Services.DecodeReadRange(buffer, offset, length, out objectId, out property, out requestType, out position, out time, out count) >= 0)
                        OnReadRange(this, adr, invokeId, objectId, property, requestType, position, time, count, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode ReadRange");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT && OnCreateObjectRequest != null)
                {
                    BacnetObjectId objectId;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeCreateObject(buffer, offset, length, out objectId, out values) >= 0)
                        OnCreateObjectRequest(this, adr, invokeId, objectId, values, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode CreateObject");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT && OnDeleteObjectRequest != null)
                {

                    BacnetObjectId objectId;
                    if (Services.DecodeDeleteObject(buffer, offset, length, out objectId) >= 0)
                        OnDeleteObjectRequest(this, adr, invokeId, objectId, maxSegments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeDeleteObject");
                }
                else
                {
                    Trace.TraceWarning($"Confirmed service not handled: {service}");
                    SendConfirmedServiceReject(adr, invokeId, BacnetRejectReasons.REJECT_REASON_UNRECOGNIZED_SERVICE);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in ProcessConfirmedServiceRequest: {ex.Message}");
            }
        }

        public delegate void UnconfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length);
        public event UnconfirmedServiceRequestHandler OnUnconfirmedServiceRequest;
        public delegate void WhoHasHandler(BacnetClient sender, BacnetAddress adr, int lowLimit, int highLimit, BacnetObjectId objId, string objName);
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

        protected void ProcessUnconfirmedServiceRequest(BacnetAddress adr, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("UnconfirmedServiceRequest", null);
                OnUnconfirmedServiceRequest?.Invoke(this, adr, type, service, buffer, offset, length);
                if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM && OnIam != null)
                {
                    uint deviceId;
                    uint maxAdpu;
                    BacnetSegmentations segmentation;
                    ushort vendorId;
                    if (Services.DecodeIamBroadcast(buffer, offset, out deviceId, out maxAdpu, out segmentation, out vendorId) >= 0)
                        OnIam(this, adr, deviceId, maxAdpu, segmentation, vendorId);
                    else
                        Trace.TraceWarning("Couldn't decode IamBroadcast");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS && OnWhoIs != null)
                {
                    int lowLimit;
                    int highLimit;
                    if (Services.DecodeWhoIsBroadcast(buffer, offset, length, out lowLimit, out highLimit) >= 0)
                        OnWhoIs(this, adr, lowLimit, highLimit);
                    else
                        Trace.TraceWarning("Couldn't decode WhoIsBroadcast");
                }
                // added by thamersalek
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS && OnWhoHas != null)
                {
                    int lowLimit;
                    int highLimit;
                    BacnetObjectId objId;
                    string objName;

                    if (Services.DecodeWhoHasBroadcast(buffer, offset, length, out lowLimit, out highLimit, out objId, out objName) >= 0)
                        OnWhoHas(this, adr, lowLimit, highLimit, objId, objName);
                    else
                        Trace.TraceWarning("Couldn't decode WhoHasBroadcast");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId initiatingDeviceIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    uint timeRemaining;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeCOVNotifyUnconfirmed(buffer, offset, length, out subscriberProcessIdentifier, out initiatingDeviceIdentifier, out monitoredObjectIdentifier, out timeRemaining, out values) >= 0)
                        OnCOVNotification(this, adr, 0, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, false, values, BacnetMaxSegments.MAX_SEG0);
                    else
                        Trace.TraceWarning("Couldn't decode COVNotifyUnconfirmed");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
                {
                    DateTime dateTime;
                    if (Services.DecodeTimeSync(buffer, offset, length, out dateTime) >= 0)
                        OnTimeSynchronize(this, adr, dateTime, false);
                    else
                        Trace.TraceWarning("Couldn't decode TimeSynchronize");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION && OnTimeSynchronize != null)
                {
                    DateTime dateTime;
                    if (Services.DecodeTimeSync(buffer, offset, length, out dateTime) >= 0)
                        OnTimeSynchronize(this, adr, dateTime, true);
                    else
                        Trace.TraceWarning("Couldn't decode TimeSynchronize");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION && OnEventNotify!=null) // F. Chaxel
                {
                    BacnetEventNotificationData eventData;
                    if (Services.DecodeEventNotifyData(buffer, offset, length, out eventData) >= 0)
                        OnEventNotify(this, adr, eventData);
                    else
                        Trace.TraceWarning("Couldn't decode Event/Alarm Notification");
                }
                else
                {
                    Trace.TraceWarning($"Unconfirmed service not handled: {service}");
                    // SendUnConfirmedServiceReject(adr); ? exists ?
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in ProcessUnconfirmedServiceRequest: {ex.Message}");
            }
        }

        public delegate void SimpleAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength);
        public event SimpleAckHandler OnSimpleAck;

        protected void ProcessSimpleAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("SimpleAck", null);
                OnSimpleAck?.Invoke(this, adr, type, service, invokeId, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in ProcessSimpleAck: {ex.Message}");
            }
        }

        public delegate void ComplexAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length);
        public event ComplexAckHandler OnComplexAck;

        protected void ProcessComplexAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("ComplexAck", null);
                OnComplexAck?.Invoke(this, adr, type, service, invokeId, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in ProcessComplexAck: {ex.Message}");
            }
        }

        public delegate void ErrorHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode, byte[] buffer, int offset, int length);
        public event ErrorHandler OnError;

        protected void ProcessError(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("Error", null);

                BacnetErrorClasses errorClass;
                BacnetErrorCodes errorCode;

                if (Services.DecodeError(buffer, offset, length, out errorClass, out errorCode) < 0)
                    Trace.TraceWarning("Couldn't decode Error");

                OnError?.Invoke(this, adr, type, service, invokeId, errorClass, errorCode, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in ProcessError: {ex.Message}");
            }
        }

        public delegate void AbortHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invokeId, byte reason, byte[] buffer, int offset, int length);
        public event AbortHandler OnAbort;

        protected void ProcessAbort(BacnetAddress adr, BacnetPduTypes type, byte invokeId, byte reason, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("Abort", null);
                OnAbort?.Invoke(this, adr, type, invokeId, reason, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in ProcessAbort: {ex.Message}");
            }
        }

        public delegate void SegmentAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length);
        public event SegmentAckHandler OnSegmentAck;

        protected void ProcessSegmentAck(BacnetAddress adr, BacnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("SegmentAck", null);
                OnSegmentAck?.Invoke(this, adr, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in ProcessSegmentAck: {ex.Message}");
            }
        }

        public delegate void SegmentHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, byte sequenceNumber, bool first, bool moreFollows, byte[] buffer, int offset, int length);
        public event SegmentHandler OnSegment;

        private void ProcessSegment(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, bool server, byte sequenceNumber, byte proposedWindowNumber, byte[] buffer, int offset, int length)
        {
            var first = false;

            if (sequenceNumber == 0 && _lastSequenceNumber == 0)
            {
                first = true;
            }
            else
            {
                //send negative ack
                if (sequenceNumber != _lastSequenceNumber + 1)
                {
                    SegmentAckResponse(adr, true, server, invokeId, _lastSequenceNumber, proposedWindowNumber);
                    Trace.WriteLine("Segment sequence out of order", null);
                    return;
                }
            }

            _lastSequenceNumber = sequenceNumber;

            var moreFollows = (type & BacnetPduTypes.MORE_FOLLOWS) == BacnetPduTypes.MORE_FOLLOWS;

            if (!moreFollows)
                _lastSequenceNumber = 0;  //reset last sequenceNumber

            //send ACK
            if (sequenceNumber % proposedWindowNumber == 0 || !moreFollows)
            {
                if (ForceWindowSize)
                    proposedWindowNumber = ProposedWindowSize;

                SegmentAckResponse(adr, false, server, invokeId, sequenceNumber, proposedWindowNumber);
            }

            //Send on
            OnSegment?.Invoke(this, adr, type, service, invokeId, maxSegments, maxAdpu, sequenceNumber, first, moreFollows, buffer, offset, length);

            //default segment assembly. We run this seperately from the above handler, to make sure that it comes after!
            if (DefaultSegmentationHandling)
                PerformDefaultSegmentHandling(adr, type, service, invokeId, maxSegments, maxAdpu, first, moreFollows, buffer, offset, length);
        }

        private byte[] AssembleSegments()
        {
            return _segments.Aggregate(new byte[0], (current, next) => 
                current.Concat(next).ToArray());
        }

        /// <summary>
        /// This is a simple handling that stores all segments in memory and assembles them when done
        /// </summary>
        private void PerformDefaultSegmentHandling(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, BacnetMaxSegments maxSegments, BacnetMaxAdpu maxAdpu, bool first, bool moreFollows, byte[] buffer, int offset, int length)
        {
            if (first)
            {
                //clear any leftover segments
                _segments.Clear();

                //copy buffer + encode new adpu header
                type &= ~BacnetPduTypes.SEGMENTED_MESSAGE;
                var confirmedServiceRequest = (type & BacnetPduTypes.PDU_TYPE_MASK) == BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST;
                var adpuHeaderLen = confirmedServiceRequest ? 4 : 3;

                var copy = new byte[length + adpuHeaderLen];
                Array.Copy(buffer, offset, copy, adpuHeaderLen, length);
                var encodedBuffer = new EncodeBuffer(copy, 0);

                if (confirmedServiceRequest)
                    APDU.EncodeConfirmedServiceRequest(encodedBuffer, type, service, maxSegments, maxAdpu, invokeId, 0, 0);
                else
                    APDU.EncodeComplexAck(encodedBuffer, type, service, invokeId, 0, 0);

                _segments.AddLast(copy); // doesn't include BVLC or NPDU
            }
            else
            {
                //copy only content part
                _segments.AddLast(buffer.Skip(offset).Take(length).ToArray());
            }

            //process when finished
            if (moreFollows)
                return;

            //assemble whole part
            var apduBuffer = AssembleSegments();
            _segments.Clear();

            //process
            ProcessApdu(adr, type, apduBuffer, 0, apduBuffer.Length);
        }

        private void ProcessApdu(BacnetAddress adr, BacnetPduTypes type, byte[] buffer, int offset, int length)
        {
            switch (type & BacnetPduTypes.PDU_TYPE_MASK)
            {
                case BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST:
                    {
                        BacnetUnconfirmedServices service;
                        var apduHeaderLen = APDU.DecodeUnconfirmedServiceRequest(buffer, offset, out type, out service);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessUnconfirmedServiceRequest(adr, type, service, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_SIMPLE_ACK:
                    {
                        BacnetConfirmedServices service;
                        byte invokeId;
                        var apduHeaderLen = APDU.DecodeSimpleAck(buffer, offset, out type, out service, out invokeId);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessSimpleAck(adr, type, service, invokeId, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_COMPLEX_ACK:
                    {
                        BacnetConfirmedServices service;
                        byte invokeId;
                        byte sequenceNumber;
                        byte proposedWindowNumber;
                        var apduHeaderLen = APDU.DecodeComplexAck(buffer, offset, out type, out service, out invokeId, out sequenceNumber, out proposedWindowNumber);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) == 0) //don't process segmented messages here
                        {
                            ProcessComplexAck(adr, type, service, invokeId, buffer, offset, length);
                        }
                        else
                        {
                            ProcessSegment(adr, type, service, invokeId, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU50, false, sequenceNumber, proposedWindowNumber, buffer, offset, length);
                        }
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_SEGMENT_ACK:
                    {
                        byte originalInvokeId;
                        byte sequenceNumber;
                        byte actualWindowSize;
                        var apduHeaderLen = APDU.DecodeSegmentAck(buffer, offset, out type, out originalInvokeId, out sequenceNumber, out actualWindowSize);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        _lastSegmentAck.Set(adr, originalInvokeId, sequenceNumber, actualWindowSize);
                        ProcessSegmentAck(adr, type, originalInvokeId, sequenceNumber, actualWindowSize, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_ERROR:
                    {
                        BacnetConfirmedServices service;
                        byte invokeId;
                        var apduHeaderLen = APDU.DecodeError(buffer, offset, out type, out service, out invokeId);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessError(adr, type, service, invokeId, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_REJECT:
                case BacnetPduTypes.PDU_TYPE_ABORT:
                    {
                        byte invokeId;
                        byte reason;
                        var apduHeaderLen = APDU.DecodeAbort(buffer, offset, out type, out invokeId, out reason);
                        offset += apduHeaderLen;
                        length -= apduHeaderLen;
                        ProcessAbort(adr, type, invokeId, reason, buffer, offset, length);
                    }
                    break;

                case BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                    {
                        BacnetConfirmedServices service;
                        BacnetMaxSegments maxSegments;
                        BacnetMaxAdpu maxAdpu;
                        byte invokeId;
                        byte sequenceNumber;
                        byte proposedWindowNumber;
                        var apduHeaderLen = APDU.DecodeConfirmedServiceRequest(buffer, offset, out type, out service, out maxSegments, out maxAdpu, out invokeId, out sequenceNumber, out proposedWindowNumber);
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
                    Trace.TraceWarning($"Something else arrived: {type}");
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

                BacnetNpduControls npduFunction;
                BacnetAddress destination, source;
                byte hopCount;
                BacnetNetworkMessageTypes nmt;
                ushort vendorId;

                // parse
                var npduLen = NPDU.Decode(buffer, offset, out npduFunction, out destination, out source, out hopCount, out nmt, out vendorId);

                // Modif FC
                remoteAddress.RoutedSource = source;

                if ((npduFunction & BacnetNpduControls.NetworkLayerMessage) == BacnetNpduControls.NetworkLayerMessage)
                {
                    Trace.TraceInformation("Network Layer message received");
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
                Trace.TraceError($"Error in OnRecieve: {ex.Message}");
            }
        }

        // Modif FC
        public void RegisterAsForeignDevice(string bbmdIP, short ttl, int port = DEFAULT_UDP_PORT)
        {
            try
            {
                var ep = new IPEndPoint(IPAddress.Parse(bbmdIP), port);

                // dynamic avoid reference to BacnetIpUdpProtocolTransport or BacnetIpV6UdpProtocolTransport classes
                dynamic clientDyn = Transport;
                bool sent = clientDyn.SendRegisterAsForeignDevice(ep, ttl);

                if (sent)
                    Trace.WriteLine("Sending Register as a Foreign Device ... ", null);
                else
                    Trace.TraceWarning("The given address do not match with the IP version");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error on RegisterAsForeignDevice (Wrong Transport, not IP ?) {ex.Message}");
            }
        }

        public void RemoteWhoIs(string bbmdIP, int port = DEFAULT_UDP_PORT, int lowLimit = -1, int highLimit = -1)
        {

            try
            {
                var ep = new IPEndPoint(IPAddress.Parse(bbmdIP), port);

                var b = GetEncodeBuffer(Transport.HeaderLength);
                var broadcast = Transport.GetBroadcastAddress();
                NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
                APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
                Services.EncodeWhoIsBroadcast(b, lowLimit, highLimit);

                // dynamic avoid reference to BacnetIpUdpProtocolTransport or BacnetIpV6UdpProtocolTransport classes
                dynamic clientDyn = Transport;
                bool sent = clientDyn.SendRemoteWhois(b.buffer, ep, b.offset);

                if (sent == false)
                    Trace.TraceWarning("The given address do not match with the IP version");
                else
                    Trace.WriteLine("Sending Remote Whois ... ", null);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error on Sending Whois to remote BBMD (Wrong Transport, not IP ?) {ex.Message}");
            }

        }

        public void WhoIs(int lowLimit = -1, int highLimit = -1, BacnetAddress receiver = null)
        {
            Trace.WriteLine("Sending WhoIs ... ", null);

            var b = GetEncodeBuffer(Transport.HeaderLength);

            // _receiver could be an unicast @ : for direct acces 
            // usefull on BIP for a known IP:Port, unknown device Id
            if (receiver == null)
                receiver = Transport.GetBroadcastAddress();

            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, receiver, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
            Services.EncodeWhoIsBroadcast(b, lowLimit, highLimit);

            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, receiver, false, 0);
        }

        public void Iam(uint deviceId, BacnetSegmentations segmentation)
        {
            Trace.WriteLine("Sending Iam ... ", null);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            var broadcast=Transport.GetBroadcastAddress();
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);
            Services.EncodeIamBroadcast(b, deviceId, (uint)GetMaxApdu(), segmentation, VendorId);

            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, broadcast, false, 0);
        }

        // ReSharper disable once InconsistentNaming
        public void IHave(BacnetObjectId deviceId, BacnetObjectId objId, string objName)
        {
            Trace.WriteLine("Sending IHave ... ", null);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            var broadcast = Transport.GetBroadcastAddress();
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_HAVE);
            Services.EncodeIhaveBroadcast(b, deviceId, objId, objName);

            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, broadcast, false, 0);

        }

        public void SendUnconfirmedEventNotification(BacnetAddress adr, BacnetEventNotificationData eventData)
        {
            Trace.WriteLine("Sending Event Notification ... ", null);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION);
            Services.EncodeEventNotifyUnconfirmed(b, eventData);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
        }

        public void SendConfirmedServiceReject(BacnetAddress adr, byte invokeId, BacnetRejectReasons reason)
        {
            Trace.WriteLine("Sending Service reject ... ", null);

            var b = GetEncodeBuffer(Transport.HeaderLength);

            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeError(b, BacnetPduTypes.PDU_TYPE_REJECT, (BacnetConfirmedServices)reason, invokeId);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
        }

        public void SynchronizeTime(BacnetAddress adr, DateTime dateTime, bool utc)
        {
            Trace.WriteLine("Sending Time Synchronize ... ", null);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, utc
                    ? BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION
                    : BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION);
            Services.EncodeTimeSync(b, dateTime);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
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

        public bool WriteFileRequest(BacnetAddress adr, BacnetObjectId objectId, ref int position, int count, byte[] fileBuffer, byte invokeId = 0)
        {
            using (var result = (BacnetAsyncResult)BeginWriteFileRequest(adr, objectId, position, count, fileBuffer, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndWriteFileRequest(result, out position, out ex);
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
            Trace.WriteLine("Sending AtomicWriteFileRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeAtomicWriteFile(b, true, objectId, position, 1, new[] { fileBuffer }, new[] { count });

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                bool isStream;
                if (Services.DecodeAtomicWriteFileAcknowledge(res.Result, 0, res.Result.Length, out isStream, out position) < 0)
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
            Trace.WriteLine("Sending AtomicReadFileRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            //encode
            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeAtomicReadFile(b, true, objectId, position, count);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                bool isStream;
                if (Services.DecodeAtomicReadFileAcknowledge(res.Result, 0, res.Result.Length, out endOfFile, out isStream, out position, out count, out fileBuffer, out fileBufferOffset) < 0)
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
                        Exception ex;
                        EndReadFileRequest(result, out count, out position, out endOfFile, out fileBuffer, out fileBufferOffset, out ex);
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

        // Fc
        public IAsyncResult BeginReadRangeRequest(BacnetAddress adr, BacnetObjectId objectId,  uint idxBegin, uint quantity, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending ReadRangeRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            //encode
            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeReadRange(b, objectId, (uint)BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL, BacnetReadRangeRequestTypes.RR_BY_POSITION, idxBegin, DateTime.Now, (int)quantity);
            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
            if (ex == null && !res.WaitForDone(40*1000))
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
        public bool ReadRangeRequest(BacnetAddress adr, BacnetObjectId objectId, uint idxBegin, ref uint quantity, out byte[] range, byte invokeId = 0)
        {
            range = null;
            using (var result = (BacnetAsyncResult)BeginReadRangeRequest(adr, objectId, idxBegin, quantity, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndReadRangeRequest(result, out range, out quantity, out ex); // quantity read could be less than demanded
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
                        Exception ex;
                        EndSubscribeCOVRequest(result, out ex);
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
            Trace.WriteLine("Sending SubscribeCOVRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeSubscribeCOV(b, subscribeId, objectId, cancel, issueConfirmedNotifications, lifetime);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
            ret.Resend();

            return ret;
        }

        public void EndSubscribeCOVRequest(IAsyncResult result, out Exception ex)
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
                        Exception ex;
                        EndSubscribePropertyRequest(result, out ex);
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
            Trace.WriteLine("Sending SubscribePropertyRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeSubscribeProperty(b, subscribeId, objectId, cancel, issueConfirmedNotifications, 0, monitoredProperty, false, 0f);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                        Exception ex;
                        EndReadPropertyRequest(result, out valueList, out ex);
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

        public Task<IList<BacnetValue>> ReadPropertyAsync(BacnetAddress address, BacnetObjectTypes objType, uint objInstance,
            BacnetPropertyIds propertyId, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            var objectId = new BacnetObjectId(objType, objInstance);
            return ReadPropertyAsync(address, objectId, propertyId, invokeId, arrayIndex);
        }

        public Task<IList<BacnetValue>> ReadPropertyAsync(BacnetAddress address, BacnetObjectId objectId,
            BacnetPropertyIds propertyId, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            IList<BacnetValue> result;

            return Task<IList<BacnetValue>>.Factory.StartNew(() =>
            {
                if (!ReadPropertyRequest(address, objectId, propertyId, out result, invokeId, arrayIndex))
                    throw new Exception($"Failed to read property {propertyId} of {objectId} from {address}");

                return result;
            });
        }

        public IAsyncResult BeginReadPropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, bool waitForTransmit, byte invokeId = 0, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            Trace.WriteLine("Sending ReadPropertyRequest ... ", null);
            if(invokeId == 0)
                invokeId = unchecked (_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeReadProperty(b, objectId, (uint)propertyId, arrayIndex);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
            ret.Resend();

            return ret;
        }

        public void EndReadPropertyRequest(IAsyncResult result, out IList<BacnetValue> valueList, out Exception ex)
        {
            var res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(Timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
                //decode
                BacnetObjectId responseObjectId;
                BacnetPropertyReference responseProperty;
                if (Services.DecodeReadPropertyAcknowledge(res.Result, 0, res.Result.Length, out responseObjectId, out responseProperty, out valueList) < 0)
                    ex = new Exception("Decode");
            }
            else
            {
                valueList = null;
            }

            res.Dispose();
        }

        public bool WritePropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, byte invokeId = 0)
        {
            using (var result = (BacnetAsyncResult)BeginWritePropertyRequest(adr, objectId, propertyId, valueList, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndWritePropertyRequest(result, out ex);
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
                        Exception ex;
                        EndWritePropertyRequest(result, out ex); // Share the same with single write
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

        public IAsyncResult BeginWritePropertyRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, IEnumerable<BacnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending WritePropertyRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST , BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeWriteProperty(b, objectId, (uint)propertyId, ASN1.BACNET_ARRAY_ALL, _writepriority, valueList);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
            ret.Resend();

            return ret;
        }

        public IAsyncResult BeginWritePropertyMultipleRequest(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending WritePropertyMultipleRequest ... ", null);
            if (invokeId == 0) invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            //BacnetNpduControls.PriorityNormalMessage 
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);

            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeWritePropertyMultiple(b, objectId, valueList);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                        Exception ex;
                        EndWritePropertyRequest(result, out ex); // Share the same with single write
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
            Trace.WriteLine("Sending WritePropertyMultipleRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            //BacnetNpduControls.PriorityNormalMessage 
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);

            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeWriteObjectMultiple(b, valueList);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                        Exception ex;
                        EndReadPropertyMultipleRequest(result, out values, out ex);
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

        public Task<IList<BacnetPropertyValue>> ReadPropertyMultipleAsync(BacnetAddress address,
            BacnetObjectId objectId, params BacnetPropertyIds[] propertyIds)
        {
            IList<BacnetReadAccessResult> result;

            var propertyReferences = propertyIds.Select(p =>
                new BacnetPropertyReference((uint)p, ASN1.BACNET_ARRAY_ALL));

            return Task<IList<BacnetPropertyValue>>.Factory.StartNew(() =>
            {
                if (!ReadPropertyMultipleRequest(address, objectId, propertyReferences.ToList(), out result))
                    throw new Exception($"Failed to read multiple properties of {objectId} from {address}");

                return result.Single().values;
            });
        }

        public IAsyncResult BeginReadPropertyMultipleRequest(BacnetAddress adr, BacnetObjectId objectId, IList<BacnetPropertyReference> propertyIdAndArrayIndex, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending ReadPropertyMultipleRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeReadPropertyMultiple(b, objectId, propertyIdAndArrayIndex);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                        Exception ex;
                        EndReadPropertyMultipleRequest(result, out values, out ex);
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
            Trace.WriteLine("Sending ReadPropertyMultipleRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeReadPropertyMultiple(b, properties);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                if (Services.DecodeReadPropertyMultipleAcknowledge(res.Result, 0, res.Result.Length, out values) < 0)
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

        //*********************************************************************************
        // By Christopher Günter
        public bool CreateObjectRequest(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, byte invokeId = 0)
        {
            using (var result = (BacnetAsyncResult)BeginCreateObjectRequest(adr, objectId, valueList, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndCreateObjectRequest(result, out ex);
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
        //***********************************************************************

        public IAsyncResult BeginCreateObjectRequest(BacnetAddress adr, BacnetObjectId objectId, ICollection<BacnetPropertyValue> valueList, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending CreateObjectRequest ... ", null);
            if (invokeId == 0) invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);

            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);


            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeCreateProperty(b, objectId, valueList);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
            ret.Resend();

            return ret;
        }
        //***********************************************************************************************************
        public void EndCreateObjectRequest(IAsyncResult result, out Exception ex)
        {
            var res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(Timeout))
                ex = new Exception("Wait Timeout");

            res.Dispose();
        }

        //***************************************************************************************************
        public bool DeleteObjectRequest(BacnetAddress adr, BacnetObjectId objectId, byte invokeId = 0)
        {
            using (var result = (BacnetAsyncResult)BeginDeleteObjectRequest(adr, objectId, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndDeleteObjectRequest(result, out ex);
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

        //******************************************************
        public IAsyncResult BeginDeleteObjectRequest(BacnetAddress adr, BacnetObjectId objectId, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending DeleteObjectRequest ... ", null);
            if (invokeId == 0) invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);

            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            //NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply , adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);

            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);

            ASN1.encode_application_object_id(b, objectId.type, objectId.instance);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
            ret.Resend();

            return ret;
        }

        //************************************************************
        public void EndDeleteObjectRequest(IAsyncResult result, out Exception ex)
        {
            var res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(Timeout))
                ex = new Exception("Wait Timeout");

            res.Dispose();
        }
        //*************************************************************

        public bool AddListElementRequest(BacnetAddress adr, BacnetObjectId objectId,BacnetPropertyReference reference, IList<BacnetValue> valueList, byte invokeId = 0)
		{
			using (var result = (BacnetAsyncResult)BeginAddListElementRequest(adr, objectId,reference,valueList, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                	
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndAddListElementRequest(result, out ex);
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
        //**********************************************************************
        public bool RemoveListElementRequest(BacnetAddress adr, BacnetObjectId objectId,BacnetPropertyReference reference, IList<BacnetValue> valueList, byte invokeId = 0)
		{
			using (var result = (BacnetAsyncResult)BeginRemoveListElementRequest(adr, objectId,reference,valueList, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                	
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndAddListElementRequest(result, out ex);
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
        //***********************************************************************
        public IAsyncResult BeginRemoveListElementRequest(BacnetAddress adr, BacnetObjectId objectId,BacnetPropertyReference reference, IList<BacnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending RemoveListElementRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeAddListElement(b, objectId, reference.propertyIdentifier, reference.propertyArrayIndex,valueList);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
            ret.Resend();

            return ret;
        }

        //******************************************************************************
        public IAsyncResult BeginAddListElementRequest(BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyReference reference, IList<BacnetValue> valueList, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending AddListElementRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_ADD_LIST_ELEMENT, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeAddListElement(b, objectId, reference.propertyIdentifier, reference.propertyArrayIndex, valueList);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
            ret.Resend();

            return ret;
        }
        //*****************************************************************************
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
                        Exception ex;
                        EndRawEncodedDecodedPropertyConfirmedRequest(result, serviceId, out inOutBuffer, out ex);
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
            Trace.WriteLine("Sending RawEncodedRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), serviceId , MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);

            ASN1.encode_context_object_id(b, 0, objectId.type, objectId.instance);
            ASN1.encode_context_enumerated(b, 1, (byte)propertyId);

            // No content encoding to do
            if (inOutBuffer!=null)
                b.Add(inOutBuffer, inOutBuffer.Length);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                    BacnetObjectTypes type;
                    uint instance;
                    byte tagNumber;
                    uint lenValueType;
                    uint propertyIdentifier;

                    ex = new Exception("Decode");

                    if (!ASN1.decode_is_context_tag(buffer, offset, 0))
                        return;
                    var len = 1;
                    len += ASN1.decode_object_id(buffer, offset + len, out type, out instance);
                    /* Tag 1: Property ID */
                    len += ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
                    if (tagNumber != 1)
                        return;
                    len += ASN1.decode_enumerated(buffer, offset + len, lenValueType, out propertyIdentifier);

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
                        Exception ex;
                        EndDeviceCommunicationControlRequest(result, out ex);
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
            Trace.WriteLine("Sending DeviceCommunicationControlRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeDeviceCommunicationControl(b, timeDuration, enableDisable, password);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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

        // FChaxel
        public bool GetAlarmSummaryOrEventRequest(BacnetAddress adr, bool getEvent, ref IList<BacnetGetEventInformationData> alarms, byte invokeId = 0)
        {
            using (var result = (BacnetAsyncResult)BeginGetAlarmSummaryOrEventRequest(adr, getEvent, alarms, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        bool moreEvent;

                        EndGetAlarmSummaryOrEventRequest(result, getEvent, ref alarms, out moreEvent, out ex);
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
            IList<BacnetGetEventInformationData> result = new List<BacnetGetEventInformationData>();

            return Task<IList<BacnetGetEventInformationData>>.Factory.StartNew(() =>
            {
                if (!GetAlarmSummaryOrEventRequest(address, true, ref result, invokeId))
                    throw new Exception($"Failed to get events from {address}");

                return result;
            });
        }

        public IAsyncResult BeginGetAlarmSummaryOrEventRequest(BacnetAddress adr, bool getEvent, IList<BacnetGetEventInformationData> alarms, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending Alarm summary request... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);

            var service = getEvent
                ? BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION
                : BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY;

            APDU.EncodeConfirmedServiceRequest(b, PduConfirmedServiceRequest(), service, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);

            // Get Next, never true if GetAlarmSummary is usee
            if (alarms.Count != 0)
                ASN1.encode_context_object_id(b, 0, alarms[alarms.Count - 1].objectIdentifier.type, alarms[alarms.Count - 1].objectIdentifier.instance);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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

        // FChaxel
        public bool AlarmAcknowledgement(BacnetAddress adr, BacnetObjectId objId, BacnetEventNotificationData.BacnetEventStates eventState, string ackText, BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp, byte invokeId = 0)
        {
            using (var result = (BacnetAsyncResult)BeginAlarmAcknowledgement(adr, objId, eventState, ackText, evTimeStamp, ackTimeStamp, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndAlarmAcknowledgement(result, out ex);
                        return ex == null;
                    }
                    if (r < Retries - 1)
                        result.Resend();
                }
            }
            return false;
        }

        public IAsyncResult BeginAlarmAcknowledgement(BacnetAddress adr, BacnetObjectId objId, BacnetEventNotificationData.BacnetEventStates eventState, string ackText, BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending AlarmAcknowledgement ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeAlarmAcknowledge(b, 57, objId, (uint)eventState, ackText, evTimeStamp, ackTimeStamp);
            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                        Exception ex;
                        EndReinitializeRequest(result, out ex);
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
            Trace.WriteLine("Sending ReinitializeRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeReinitializeDevice(b, state, password);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
            Trace.WriteLine("Sending Notify (confirmed) ... ", null);
            if (invokeId == 0) invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeCOVNotifyConfirmed(b, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);

            //send
            var ret = new BacnetAsyncResult(this, adr, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
                Trace.WriteLine("Sending Notify (unconfirmed) ... ", null);
                var b = GetEncodeBuffer(Transport.HeaderLength);
                NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
                APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);
                Services.EncodeCOVNotifyUnconfirmed(b, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);
               // Modif F. Chaxel
                
                var sendbytes=Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
                return sendbytes == b.offset;
            }

            using (var result = (BacnetAsyncResult)BeginConfirmedNotify(adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values, true))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndConfirmedNotify(result, out ex);
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

        public bool LifeSafetyOperationRequest(BacnetAddress address, BacnetObjectId objectId, string requestingSrc, BacnetEventNotificationData.BacnetLifeSafetyOperations operation, byte invokeId = 0)
        {
            using (var result = (BacnetAsyncResult)BeginLifeSafetyOperationRequest(address, objectId, 0, requestingSrc, operation, true, invokeId))
            {
                for (var r = 0; r < _retries; r++)
                {
                    if (result.WaitForDone(Timeout))
                    {
                        Exception ex;
                        EndLifeSafetyOperationRequest(result, out ex);
                        return ex == null;
                    }
                    if (r < Retries - 1)
                        result.Resend();
                }
            }
            return false;
        }

        public IAsyncResult BeginLifeSafetyOperationRequest(BacnetAddress address, BacnetObjectId objectId, uint processId, string requestingSrc, BacnetEventNotificationData.BacnetLifeSafetyOperations operation, bool waitForTransmit, byte invokeId = 0)
        {
            Trace.WriteLine("Sending BeginLifeSafetyOperationRequest ... ", null);
            if (invokeId == 0)
                invokeId = unchecked(_invokeId++);

            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, address.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION, MaxSegments, Transport.MaxAdpuLength, invokeId, 0, 0);
            Services.EncodeLifeSafetyOperation(b, processId, requestingSrc, (uint)operation, objectId);

            //send
            var ret = new BacnetAsyncResult(this, address, invokeId, b.buffer, b.offset - Transport.HeaderLength, waitForTransmit, TransmitTimeout);
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
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);

            //set segments limits
            buffer.max_offset = buffer.offset + GetMaxApdu();
            var apduHeader = APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK | (isSegmented ? BacnetPduTypes.SEGMENTED_MESSAGE | BacnetPduTypes.SERVER : 0) | (moreFollows ? BacnetPduTypes.MORE_FOLLOWS : 0), service, invokeId, segmentation?.sequence_number ?? 0, segmentation?.window_size ?? 0);
            buffer.min_limit = (GetMaxApdu() - apduHeader) * (segmentation?.sequence_number ?? 0);

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

        private void SendComplexAck(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetConfirmedServices service, Action<EncodeBuffer> apduContentEncode)
        {
            Trace.WriteLine($"Sending {Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(service.ToString().ToLower())} ... ", null);

            //encode
            EncodeBuffer buffer;
            if (EncodeSegment(adr, invokeId, segmentation, service, out buffer, apduContentEncode))
            {
                //client doesn't support segments
                if (segmentation == null)
                {
                    Trace.TraceInformation("Segmenation denied");
                    ErrorResponse(adr, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                    buffer.result = EncodeResult.Good;     //don't continue the segmentation
                    return;
                }

                //first segment? validate max segments
                if (segmentation.sequence_number == 0)  //only validate first segment
                {
                    if (segmentation.max_segments != 0xFF && segmentation.buffer.offset > segmentation.max_segments * (GetMaxApdu() - 5))      //5 is adpu header
                    {
                        Trace.TraceInformation("Too much segmenation");
                        ErrorResponse(adr, service, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                        buffer.result = EncodeResult.Good;     //don't continue the segmentation
                        return;
                    }
                    Trace.WriteLine("Segmentation required", null);
                }

                //increment before ack can do so (race condition)
                unchecked { segmentation.sequence_number++; };
            }

            //send
            Transport.Send(buffer.buffer, Transport.HeaderLength, buffer.GetLength() - Transport.HeaderLength, adr, false, 0);
        }

        public void ReadPropertyResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetObjectId objectId, BacnetPropertyReference property, IEnumerable<BacnetValue> value)
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, b =>
            {
                Services.EncodeReadPropertyAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, value);
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
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, b => 
            { 
                Services.EncodeReadPropertyMultipleAcknowledge(b, values); 
            });
        }

        public void ReadRangeResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, BacnetObjectId objectId, BacnetPropertyReference property, BacnetResultFlags status, uint itemCount, byte[] applicationData, BacnetReadRangeRequestTypes requestType, uint firstSequenceNo)
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, (b) =>
            {
                Services.EncodeReadRangeAcknowledge(b, objectId, property.propertyIdentifier, property.propertyArrayIndex, BacnetBitString.ConvertFromInt((uint)status), itemCount, applicationData, requestType, firstSequenceNo);
            });
        }

        public void ReadFileResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, int position, uint count, bool endOfFile, byte[] fileBuffer)
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, b =>
            {
                Services.EncodeAtomicReadFileAcknowledge(b, true, endOfFile, position, 1, new[] { fileBuffer }, new[] { (int)count });
            });
        }

        public void WriteFileResponse(BacnetAddress adr, byte invokeId, Segmentation segmentation, int position)
        {
            SendComplexAck(adr, invokeId, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, b =>
            {
                Services.EncodeAtomicWriteFileAcknowledge(b, true, position);
            });
        }

        public void ErrorResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invokeId, BacnetErrorClasses errorClass, BacnetErrorCodes errorCode)
        {
            Trace.WriteLine("Sending ErrorResponse ... ", null);
            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeError(b, BacnetPduTypes.PDU_TYPE_ERROR, service, invokeId);
            Services.EncodeError(b, errorClass, errorCode);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
        }

        public void SimpleAckResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invokeId)
        {
            Trace.WriteLine("Sending SimpleAckResponse ... ", null);
            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeSimpleAck(b, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, service, invokeId);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
        }

        public void SegmentAckResponse(BacnetAddress adr, bool negative, bool server, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize)
        {
            Trace.WriteLine("Sending SimpleAckResponse ... ", null);
            var b = GetEncodeBuffer(Transport.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, VendorId);
            APDU.EncodeSegmentAck(b, BacnetPduTypes.PDU_TYPE_SEGMENT_ACK | (negative ? BacnetPduTypes.NEGATIVE_ACK : 0) | (server ? BacnetPduTypes.SERVER : 0), originalInvokeId, sequenceNumber, actualWindowSize);
            Transport.Send(b.buffer, Transport.HeaderLength, b.offset - Transport.HeaderLength, adr, false, 0);
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

        public void Dispose()
        {
            Transport.Dispose();
        }
    }
}
