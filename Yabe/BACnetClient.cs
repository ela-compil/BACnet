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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO.BACnet.Serialize;
using System.Diagnostics;

namespace System.IO.BACnet
{
    /// <summary>
    /// This can be both client and server
    /// </summary>
    public class BacnetClient : IDisposable
    {
        private IBacnetTransport m_client;
        private ushort m_vendor_id = 260;
        private int m_timeout;
        private int m_transmit_timeout = 30000;     //long transmit timeout due to MSTP
        private int m_retries;
        private byte m_invoke_id = 0;
        private BacnetMaxSegments m_max_segments = BacnetMaxSegments.MAX_SEG0;
        private byte m_last_sequence_number = 0;
        private byte m_proposed_window_size = 10;
        private bool m_default_segmentation_handling;
        private LinkedList<byte[]> m_segments = new LinkedList<byte[]>();       //only used when 'DefaultSegmentationHandling' = true
        private LastSegmentACK m_last_segment_ack = new LastSegmentACK();
        private bool m_force_window_size = false;
        private uint m_writepriority=0;

        public static byte DEFAULT_HOP_COUNT = 0xFF;

        public IBacnetTransport Transport { get { return m_client; } }
        public int Timeout { get { return m_timeout; } set { m_timeout = value; } }
        public int TransmitTimeout { get { return m_transmit_timeout; } set { m_transmit_timeout = value; } }
        public int Retries { get { return m_retries; } set { m_retries = value; } }
        public uint WritePriority { get { return m_writepriority; } set { if (value<17) m_writepriority = value; } }
        public BacnetMaxSegments MaxSegments { get { return m_max_segments; } set { m_max_segments = value; } }
        public byte ProposedWindowSize { get { return m_proposed_window_size; } set { m_proposed_window_size = value; } }
        public bool ForceWindowSize { get { return m_force_window_size; } set { m_force_window_size = value; } }
        public bool DefaultSegmentationHandling { get { return m_default_segmentation_handling; } set { m_default_segmentation_handling = value; } }

        private class LastSegmentACK
        {
            public byte invoke_id;
            public byte sequence_number;
            public byte window_size;
            public BacnetAddress adr;
            private System.Threading.ManualResetEvent m_wait = new Threading.ManualResetEvent(false);
            private object m_lockObject = new object();
            public void Set(BacnetAddress adr, byte invoke_id, byte sequence_number, byte window_size)
            {
                lock (m_lockObject)
                {
                    this.adr = adr;
                    this.invoke_id = invoke_id;
                    this.sequence_number = sequence_number;
                    this.window_size = window_size;
                    m_wait.Set();
                }
            }
            public bool Wait(BacnetAddress adr, byte invoke_id, int timeout)
            {
                System.Threading.Monitor.Enter(m_lockObject);
                while (!adr.Equals(this.adr) || this.invoke_id != invoke_id)
                {
                    m_wait.Reset();
                    System.Threading.Monitor.Exit(m_lockObject);
                    if (!m_wait.WaitOne(timeout)) return false;
                    System.Threading.Monitor.Enter(m_lockObject);
                }
                System.Threading.Monitor.Exit(m_lockObject);
                this.adr = null;
                return true;
            }
        }

        public BacnetClient(int port = 0xBAC0, int timeout = 1000, int retries = 3) : 
            this(new BacnetIpUdpProtocolTransport(port), timeout, retries)
        {
        }

        public BacnetClient(string port_name, int baud_rate, int timeout = 1000, int retries = 3) :
            this(new BacnetMstpProtocolTransport(port_name, baud_rate), timeout, retries)
        {
        }

        public BacnetClient(IBacnetTransport transport, int timeout = 1000, int retries = 3)
        {
            m_client = transport;
            m_timeout = timeout;
            m_retries = retries;
            DefaultSegmentationHandling = true;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is BacnetClient)) return false;
            BacnetClient a = (BacnetClient)obj;
            return m_client.Equals(a.m_client);
        }

        public override int GetHashCode()
        {
            return m_client.GetHashCode();
        }

        public override string ToString()
        {
            return m_client.ToString();
        }

        private EncodeBuffer GetEncodeBuffer(int start_offset)
        {
            return new EncodeBuffer(new byte[m_client.MaxBufferLength], m_client.HeaderLength);
        }

        public void Start()
        {
            m_client.Start();
            m_client.MessageRecieved += new MessageRecievedHandler(OnRecieve);
            Trace.TraceInformation("Started communication");
        }

        public delegate void ConfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte invoke_id, byte[] buffer, int offset, int length);
        public event ConfirmedServiceRequestHandler OnConfirmedServiceRequest;

        public delegate void ReadPropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property, BacnetMaxSegments max_segments);
        public event ReadPropertyRequestHandler OnReadPropertyRequest;
        public delegate void ReadPropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments max_segments);
        public event ReadPropertyMultipleRequestHandler OnReadPropertyMultipleRequest;
        public delegate void WritePropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyValue value, BacnetMaxSegments max_segments);
        public event WritePropertyRequestHandler OnWritePropertyRequest;
        public delegate void WritePropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments);
        public event WritePropertyMultipleRequestHandler OnWritePropertyMultipleRequest;
        public delegate void AtomicWriteFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, bool is_stream, BacnetObjectId object_id, int position, uint block_count, byte[][] blocks, int[] counts, BacnetMaxSegments max_segments);
        public event AtomicWriteFileRequestHandler OnAtomicWriteFileRequest;
        public delegate void AtomicReadFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, bool is_stream, BacnetObjectId object_id, int position, uint count, BacnetMaxSegments max_segments);
        public event AtomicReadFileRequestHandler OnAtomicReadFileRequest;
        public delegate void SubscribeCOVRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetMaxSegments max_segments);
        public event SubscribeCOVRequestHandler OnSubscribeCOV;
        public delegate void SubscribeCOVPropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference monitoredProperty, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement, BacnetMaxSegments max_segments);
        public event SubscribeCOVPropertyRequestHandler OnSubscribeCOVProperty;
        public delegate void DeviceCommunicationControlRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint time_duration, uint enable_disable, string password, BacnetMaxSegments max_segments);
        public event DeviceCommunicationControlRequestHandler OnDeviceCommunicationControl;
        public delegate void ReinitializedRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetReinitializedStates state, string password, BacnetMaxSegments max_segments);
        public event ReinitializedRequestHandler OnReinitializedDevice;

        protected void ProcessConfirmedServiceRequest(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte invoke_id, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("ConfirmedServiceRequest", null);

                if (OnConfirmedServiceRequest != null) 
                    OnConfirmedServiceRequest(this, adr, type, service, max_segments, max_adpu, invoke_id, buffer, offset, length);

                //don't send segmented messages, if client don't want it
                if ((type & BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED) == 0)
                    max_segments = BacnetMaxSegments.MAX_SEG0;

                if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY && OnReadPropertyRequest != null)
                {
                    BacnetObjectId object_id;
                    BacnetPropertyReference property;
                    if (Services.DecodeReadProperty(buffer, offset, length, out object_id, out property) >= 0)
                        OnReadPropertyRequest(this, adr, invoke_id, object_id, property, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY && OnWritePropertyRequest != null)
                {
                    BacnetObjectId object_id;
                    BacnetPropertyValue value;
                    if (Services.DecodeWriteProperty(buffer, offset, length, out object_id, out value) >= 0)
                        OnWritePropertyRequest(this, adr, invoke_id, object_id, value, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeWriteProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE && OnReadPropertyMultipleRequest != null)
                {
                    IList<BacnetReadAccessSpecification> properties;
                    if (Services.DecodeReadPropertyMultiple(buffer, offset, length, out properties) >= 0)
                        OnReadPropertyMultipleRequest(this, adr, invoke_id, properties, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadPropertyMultiple");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE && OnWritePropertyMultipleRequest != null)
                {
                    BacnetObjectId object_id;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeWritePropertyMultiple(buffer, offset, length, out object_id, out values) >= 0)
                        OnWritePropertyMultipleRequest(this, adr, invoke_id, object_id, values, max_segments);
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
                        OnCOVNotification(this, adr, invoke_id, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, true, values, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode COVNotify");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE && OnAtomicWriteFileRequest != null)
                {
                    bool is_stream;
                    BacnetObjectId object_id;
                    int position;
                    uint block_count;
                    byte[][] blocks;
                    int[] counts;
                    if (Services.DecodeAtomicWriteFile(buffer, offset, length, out is_stream, out object_id, out position, out block_count, out blocks, out counts) >= 0)
                        OnAtomicWriteFileRequest(this, adr, invoke_id, is_stream, object_id, position, block_count, blocks, counts, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode AtomicWriteFile");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE && OnAtomicReadFileRequest != null)
                {
                    bool is_stream;
                    BacnetObjectId object_id;
                    int position;
                    uint count;
                    if (Services.DecodeAtomicReadFile(buffer, offset, length, out is_stream, out object_id, out position, out count) >= 0)
                        OnAtomicReadFileRequest(this, adr, invoke_id, is_stream, object_id, position, count, max_segments);
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
                        OnSubscribeCOV(this, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, cancellationRequest, issueConfirmedNotifications, lifetime, max_segments);
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
                        OnSubscribeCOVProperty(this, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, monitoredProperty, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode SubscribeCOVProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL && OnDeviceCommunicationControl != null)
                {
                    uint timeDuration;
                    uint enable_disable;
                    string password;
                    if (Services.DecodeDeviceCommunicationControl(buffer, offset, length, out timeDuration, out enable_disable, out password) >= 0)
                        OnDeviceCommunicationControl(this, adr, invoke_id, timeDuration, enable_disable, password, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode DeviceCommunicationControl");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE && OnReinitializedDevice != null)
                {
                    BacnetReinitializedStates state;
                    string password;
                    if (Services.DecodeReinitializeDevice(buffer, offset, length, out state, out password) >= 0)
                        OnReinitializedDevice(this, adr, invoke_id, state, password, max_segments);
                    else
                        Trace.TraceWarning("Couldn't decode ReinitializeDevice");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessConfirmedServiceRequest: " + ex.Message);
            }
        }

        public delegate void UnconfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length);
        public event UnconfirmedServiceRequestHandler OnUnconfirmedServiceRequest;
        public delegate void IamHandler(BacnetClient sender, BacnetAddress adr, UInt32 device_id, UInt32 max_apdu, BacnetSegmentations segmentation, UInt16 vendor_id);
        public event IamHandler OnIam;
        public delegate void WhoIsHandler(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit);
        public event WhoIsHandler OnWhoIs;
        public delegate void TimeSynchronizeHandler(BacnetClient sender, BacnetAddress adr, DateTime dateTime, bool utc);
        public event TimeSynchronizeHandler OnTimeSynchronize;

        //used by both 'confirmed' and 'unconfirmed' notify
        public delegate void COVNotificationHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool need_confirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments);
        public event COVNotificationHandler OnCOVNotification;

        protected void ProcessUnconfirmedServiceRequest(BacnetAddress adr, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("UnconfirmedServiceRequest", null);
                if (OnUnconfirmedServiceRequest != null) OnUnconfirmedServiceRequest(this, adr, type, service, buffer, offset, length);
                if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM && OnIam != null)
                {
                    uint device_id;
                    uint max_adpu;
                    BacnetSegmentations segmentation;
                    ushort vendor_id;
                    if (Services.DecodeIamBroadcast(buffer, offset, out device_id, out max_adpu, out segmentation, out vendor_id) >= 0)
                        OnIam(this, adr, device_id, max_adpu, segmentation, vendor_id);
                    else
                        Trace.TraceWarning("Couldn't decode IamBroadcast");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS && OnWhoIs != null)
                {
                    int low_limit;
                    int high_limit;
                    if (Services.DecodeWhoIsBroadcast(buffer, offset, length, out low_limit, out high_limit) >= 0)
                        OnWhoIs(this, adr, low_limit, high_limit);
                    else
                        Trace.TraceWarning("Couldn't decode WhoIsBroadcast");
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
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessUnconfirmedServiceRequest: " + ex.Message);
            }
        }

        public delegate void SimpleAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] data, int data_offset, int data_length);
        public event SimpleAckHandler OnSimpleAck;

        protected void ProcessSimpleAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("SimpleAck", null);
                if (OnSimpleAck != null) OnSimpleAck(this, adr, type, service, invoke_id, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessSimpleAck: " + ex.Message);
            }
        }

        public delegate void ComplexAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] buffer, int offset, int length);
        public event ComplexAckHandler OnComplexAck;

        protected void ProcessComplexAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("ComplexAck", null);
                if (OnComplexAck != null) OnComplexAck(this, adr, type, service, invoke_id, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessComplexAck: " + ex.Message);
            }
        }

        public delegate void ErrorHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, BacnetErrorClasses error_class, BacnetErrorCodes error_code, byte[] buffer, int offset, int length);
        public event ErrorHandler OnError;

        protected void ProcessError(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("Error", null);

                BacnetErrorClasses error_class;
                BacnetErrorCodes error_code;
                if (Services.DecodeError(buffer, offset, length, out error_class, out error_code) < 0)
                    Trace.TraceWarning("Couldn't decode Error");

                if (OnError != null) OnError(this, adr, type, service, invoke_id, error_class, error_code, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessError: " + ex.Message);
            }
        }

        public delegate void AbortHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invoke_id, byte reason, byte[] buffer, int offset, int length);
        public event AbortHandler OnAbort;

        protected void ProcessAbort(BacnetAddress adr, BacnetPduTypes type, byte invoke_id, byte reason, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("Abort", null);
                if (OnAbort != null) OnAbort(this, adr, type, invoke_id, reason, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessAbort: " + ex.Message);
            }
        }

        public delegate void SegmentAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte original_invoke_id, byte sequence_number, byte actual_window_size, byte[] buffer, int offset, int length);
        public event SegmentAckHandler OnSegmentAck;

        protected void ProcessSegmentAck(BacnetAddress adr, BacnetPduTypes type, byte original_invoke_id, byte sequence_number, byte actual_window_size, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("SegmentAck", null);
                if (OnSegmentAck != null) OnSegmentAck(this, adr, type, original_invoke_id, sequence_number, actual_window_size, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessSegmentAck: " + ex.Message);
            }
        }

        public delegate void SegmentHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte sequence_number, bool first, bool more_follows, byte[] buffer, int offset, int length);
        public event SegmentHandler OnSegment;

        private void ProcessSegment(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, bool server, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length)
        {
            bool first = false;
            if (sequence_number == 0 && m_last_sequence_number == 0)
            {
                first = true;
            }
            else
            {
                //send negative ack
                if (sequence_number != (m_last_sequence_number + 1))
                {
                    SegmentAckResponse(adr, true, server, invoke_id, m_last_sequence_number, proposed_window_number);
                    Trace.WriteLine("Segment sequence out of order", null);
                    return;
                }
            }
            m_last_sequence_number = sequence_number;

            bool more_follows = (type & BacnetPduTypes.MORE_FOLLOWS) == BacnetPduTypes.MORE_FOLLOWS;
            if (!more_follows) m_last_sequence_number = 0;  //reset last sequence_number

            //send ACK
            if ((sequence_number % proposed_window_number) == 0 || !more_follows)
            {
                if (m_force_window_size) proposed_window_number = m_proposed_window_size;
                SegmentAckResponse(adr, false, server, invoke_id, sequence_number, proposed_window_number);
            }

            //Send on
            if (OnSegment != null)
                OnSegment(this, adr, type, service, invoke_id, max_segments, max_adpu, sequence_number, first, more_follows, buffer, offset, length);

            //default segment assembly. We run this seperately from the above handler, to make sure that it comes after!
            if (m_default_segmentation_handling)
                PerformDefaultSegmentHandling(this, adr, type, service, invoke_id, max_segments, max_adpu, sequence_number, first, more_follows, buffer, offset, length);
        }

        private byte[] AssembleSegments()
        {
            int count = 0;
            foreach (byte[] arr in m_segments)
                count += arr.Length;
            byte[] ret = new byte[count];
            count = 0;
            foreach (byte[] arr in m_segments)
            {
                Array.Copy(arr, 0, ret, count, arr.Length);
                count += arr.Length;
            }
            return ret;
        }

        /// <summary>
        /// This is a simple handling that stores all segments in memory and assembles them when done
        /// </summary>
        private void PerformDefaultSegmentHandling(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte sequence_number, bool first, bool more_follows, byte[] buffer, int offset, int length)
        {
            if (first)
            {
                //clear any leftover segments
                m_segments.Clear();

                //copy buffer + encode new adpu header
                type &= ~BacnetPduTypes.SEGMENTED_MESSAGE;
                int adpu_header_len = 3;
                if ((type & BacnetPduTypes.PDU_TYPE_MASK) == BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST) adpu_header_len = 4;
                byte[] copy = new byte[length + adpu_header_len];
                Array.Copy(buffer, offset, copy, adpu_header_len, length);
                if ((type & BacnetPduTypes.PDU_TYPE_MASK) == BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST)
                    APDU.EncodeConfirmedServiceRequest(new EncodeBuffer(copy, 0), type, service, max_segments, max_adpu, invoke_id, 0, 0);
                else
                    APDU.EncodeComplexAck(new EncodeBuffer(copy, 0), type, service, invoke_id, 0, 0);
                m_segments.AddLast(copy);       //doesn't include BVLC or NPDU
            }
            else
            {
                //copy only content part
                byte[] copy = new byte[length];
                Array.Copy(buffer, offset, copy, 0, copy.Length);
                m_segments.AddLast(copy);
            }

            //process when finished
            if (!more_follows)
            {
                //assemble whole part
                byte[] apdu_buffer = AssembleSegments();
                m_segments.Clear();

                //process
                ProcessApdu(adr, type, apdu_buffer, 0, apdu_buffer.Length);
            }
        }

        private void ProcessApdu(BacnetAddress adr, BacnetPduTypes type, byte[] buffer, int offset, int length)
        {
            switch (type & BacnetPduTypes.PDU_TYPE_MASK)
            {
                case BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST:
                    {
                        BacnetUnconfirmedServices service;
                        int apdu_header_len = APDU.DecodeUnconfirmedServiceRequest(buffer, offset, out type, out service);
                        offset += apdu_header_len;
                        length -= apdu_header_len;
                        ProcessUnconfirmedServiceRequest(adr, type, service, buffer, offset, length);
                    }
                    break;
                case BacnetPduTypes.PDU_TYPE_SIMPLE_ACK:
                    {
                        BacnetConfirmedServices service;
                        byte invoke_id;
                        int apdu_header_len = APDU.DecodeSimpleAck(buffer, offset, out type, out service, out invoke_id);
                        offset += apdu_header_len;
                        length -= apdu_header_len;
                        ProcessSimpleAck(adr, type, service, invoke_id, buffer, offset, length);
                    }
                    break;
                case BacnetPduTypes.PDU_TYPE_COMPLEX_ACK:
                    {
                        BacnetConfirmedServices service;
                        byte invoke_id;
                        byte sequence_number;
                        byte proposed_window_number;
                        int apdu_header_len = APDU.DecodeComplexAck(buffer, offset, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                        offset += apdu_header_len;
                        length -= apdu_header_len;
                        if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) == 0) //don't process segmented messages here
                        {
                            ProcessComplexAck(adr, type, service, invoke_id, buffer, offset, length);
                        }
                        else
                        {
                            ProcessSegment(adr, type, service, invoke_id, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU50, false, sequence_number, proposed_window_number, buffer, offset, length);
                        }
                    }
                    break;
                case BacnetPduTypes.PDU_TYPE_SEGMENT_ACK:
                    {
                        byte original_invoke_id;
                        byte sequence_number;
                        byte actual_window_size;
                        int apdu_header_len = APDU.DecodeSegmentAck(buffer, offset, out type, out original_invoke_id, out sequence_number, out actual_window_size);
                        offset += apdu_header_len;
                        length -= apdu_header_len;
                        m_last_segment_ack.Set(adr, original_invoke_id, sequence_number, actual_window_size);
                        ProcessSegmentAck(adr, type, original_invoke_id, sequence_number, actual_window_size, buffer, offset, length);
                    }
                    break;
                case BacnetPduTypes.PDU_TYPE_ERROR:
                    {
                        BacnetConfirmedServices service;
                        byte invoke_id;
                        int apdu_header_len = APDU.DecodeError(buffer, offset, out type, out service, out invoke_id);
                        offset += apdu_header_len;
                        length -= apdu_header_len;
                        ProcessError(adr, type, service, invoke_id, buffer, offset, length);
                    }
                    break;
                case BacnetPduTypes.PDU_TYPE_REJECT:
                case BacnetPduTypes.PDU_TYPE_ABORT:
                    {
                        byte invoke_id;
                        byte reason;
                        int apdu_header_len = APDU.DecodeAbort(buffer, offset, out type, out invoke_id, out reason);
                        offset += apdu_header_len;
                        length -= apdu_header_len;
                        ProcessAbort(adr, type, invoke_id, reason, buffer, offset, length);
                    }
                    break;
                case BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                    {
                        BacnetConfirmedServices service;
                        BacnetMaxSegments max_segments;
                        BacnetMaxAdpu max_adpu;
                        byte invoke_id;
                        byte sequence_number;
                        byte proposed_window_number;
                        int apdu_header_len = APDU.DecodeConfirmedServiceRequest(buffer, offset, out type, out service, out max_segments, out max_adpu, out invoke_id, out sequence_number, out proposed_window_number);
                        offset += apdu_header_len;
                        length -= apdu_header_len;
                        if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) == 0) //don't process segmented messages here
                        {
                            ProcessConfirmedServiceRequest(adr, type, service, max_segments, max_adpu, invoke_id, buffer, offset, length);
                        }
                        else
                        {
                            ProcessSegment(adr, type, service, invoke_id, max_segments, max_adpu, true, sequence_number, proposed_window_number, buffer, offset, length);
                        }
                    }
                    break;
                default:
                    Trace.TraceWarning("Something else arrived: " + type);
                    break;
            }
        }

        private void OnRecieve(IBacnetTransport sender, byte[] buffer, int offset, int msg_length, BacnetAddress remote_address)
        {
            try
            {
                //finish recieve
                if (m_client == null) return;   //we're disposed 

                //parse
                if (msg_length > 0)
                {
                    BacnetNpduControls npdu_function;
                    BacnetAddress destination, source;
                    byte hop_count;
                    BacnetNetworkMessageTypes nmt;
                    ushort vendor_id;
                    int npdu_len = NPDU.Decode(buffer, offset, out npdu_function, out destination, out source, out hop_count, out nmt, out vendor_id);

                    // Modif FC
                    remote_address.RoutedSource = source;

                    if (npdu_len >= 0)
                    {
                        offset += npdu_len;
                        msg_length -= npdu_len;
                        BacnetPduTypes apdu_type;
                        apdu_type = APDU.GetDecodedType(buffer, offset);

                        //APDU
                        ProcessApdu(remote_address, apdu_type, buffer, offset, msg_length);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in OnRecieve: " + ex.Message);
            }
        }

        // Modif FC
        public void RegisterAsForeignDevice(String BBMD_IP, short TTL, int Port = 0xbac0)
        {
            if (!(m_client is BacnetIpUdpProtocolTransport))
            {
                Trace.TraceWarning("Wrong Transport : IP only");
                return;
            }

            try
            {
                System.Net.IPEndPoint ep = new Net.IPEndPoint(Net.IPAddress.Parse(BBMD_IP), Port);

                EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
                BVLC.Encode(b.buffer, 0, BacnetBvlcFunctions.BVLC_REGISTER_FOREIGN_DEVICE, 6);
                b.buffer[4] = (byte)((TTL & 0xFF00) >> 8);
                b.buffer[5] = (byte)(TTL & 0xFF);

                Trace.WriteLine("Sending Register as a Foreign Device ... ", null);
                (m_client as BacnetIpUdpProtocolTransport).Send(b.buffer, 6, ep);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error on RegisterAsForeignDevice" + ex.Message);
            }
        }

        public void RemoteWhoIs(String BBMD_IP, int Port = 0xbac0, int low_limit = -1, int high_limit = -1)
        {
            if (!(m_client is BacnetIpUdpProtocolTransport))
            {
                Trace.TraceWarning("Wrong Transport : IP only");
                return;
            }

            try
            {
                System.Net.IPEndPoint ep = new Net.IPEndPoint(Net.IPAddress.Parse(BBMD_IP), Port);

                EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
                BacnetAddress broadcast = m_client.GetBroadcastAddress();
                NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
                APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
                Services.EncodeWhoIsBroadcast(b, low_limit, high_limit);
                BVLC.Encode(b.buffer, 0, BacnetBvlcFunctions.BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK, b.offset);

                Trace.WriteLine("Sending Whois to remote BBMD ", null);
                (m_client as BacnetIpUdpProtocolTransport).Send(b.buffer, b.offset, ep);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Sending Whois to remote BBMD " + ex.Message);
            }

        }
        public void WhoIs(int low_limit = -1, int high_limit = -1)
        {
            Trace.WriteLine("Sending WhoIs ... ", null);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            BacnetAddress broadcast = m_client.GetBroadcastAddress();
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
            Services.EncodeWhoIsBroadcast(b, low_limit, high_limit);

            m_client.Send(b.buffer, m_client.HeaderLength, b.offset - m_client.HeaderLength, broadcast, false, 0);
        }

        public void Iam(uint device_id, BacnetSegmentations segmentation)
        {
            Trace.WriteLine("Sending Iam ... ", null);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            BacnetAddress broadcast = m_client.GetBroadcastAddress();
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);
            Services.EncodeIamBroadcast(b, device_id, (uint)GetMaxApdu(), segmentation, m_vendor_id);

            m_client.Send(b.buffer, m_client.HeaderLength, b.offset - m_client.HeaderLength, broadcast, false, 0);
        }

        public void SynchronizeTime(BacnetAddress adr, DateTime dateTime, bool utc)
        {
            Trace.WriteLine("Sending Time Synchronize ... ", null);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            if(!utc)
                APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION);
            else
                APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_UTC_TIME_SYNCHRONIZATION);
            Services.EncodeTimeSync(b, dateTime);

            m_client.Send(b.buffer, m_client.HeaderLength, b.offset - m_client.HeaderLength, adr, false, 0);
        }

        public int GetMaxApdu()
        {
            int max_apdu;
            switch (m_client.MaxAdpuLength)
            {
                case BacnetMaxAdpu.MAX_APDU1476:
                    max_apdu = 1476;
                    break;
                case BacnetMaxAdpu.MAX_APDU1024:
                    max_apdu = 1024;
                    break;
                case BacnetMaxAdpu.MAX_APDU480:
                    max_apdu = 480;
                    break;
                case BacnetMaxAdpu.MAX_APDU206:
                    max_apdu = 206;
                    break;
                case BacnetMaxAdpu.MAX_APDU128:
                    max_apdu = 128;
                    break;
                case BacnetMaxAdpu.MAX_APDU50:
                    max_apdu = 50;
                    break;
                default:
                    throw new NotImplementedException();
            }

            //max udp payload IRL seems to differ from the expectations in BACnet
            //so we have to adjust it. (In order to fulfill the standard)
            const int max_npdu_header_length = 4;       //usually it's '2', but it can also be more than '4'. Beware!
            return Math.Min(max_apdu, m_client.MaxBufferLength - m_client.HeaderLength - max_npdu_header_length);
        }

        public int GetFileBufferMaxSize()
        {
            //6 should be the max_apdu_header_length for Confirmed (with segmentation)
            //12 should be the max_atomic_write_file
            return GetMaxApdu() - 18;
        }

        public bool WriteFileRequest(BacnetAddress adr, BacnetObjectId object_id, ref int position, int count, byte[] file_buffer, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginWriteFileRequest(adr, object_id, position, count, file_buffer, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndWriteFileRequest(result, out position, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            return false;
        }

        public IAsyncResult BeginWriteFileRequest(BacnetAddress adr, BacnetObjectId object_id, int position, int count, byte[] file_buffer, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending AtomicWriteFileRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeAtomicWriteFile(b, true, object_id, position, 1, new byte[][] { file_buffer }, new int[] { count });

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndWriteFileRequest(IAsyncResult result, out int position, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
                //decode
                bool is_stream;
                if (Services.DecodeAtomicWriteFileAcknowledge(res.Result, 0, res.Result.Length, out is_stream, out position) < 0)
                    ex = new Exception("Decode");
            }
            else
            {
                position = -1;
            }

            res.Dispose();
        }

        public IAsyncResult BeginReadFileRequest(BacnetAddress adr, BacnetObjectId object_id, int position, uint count, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending AtomicReadFileRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            //encode
            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeAtomicReadFile(b, true, object_id, position, count);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndReadFileRequest(IAsyncResult result, out uint count, out int position, out bool end_of_file, out byte[] file_buffer, out int file_buffer_offset, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
                //decode
                bool is_stream;
                if (Services.DecodeAtomicReadFileAcknowledge(res.Result, 0, res.Result.Length, out end_of_file, out is_stream, out position, out count, out file_buffer, out file_buffer_offset) < 0)
                    ex = new Exception("Decode");
            }
            else
            {
                count = 0;
                end_of_file = true;
                position = -1;
                file_buffer_offset = -1;
                file_buffer = new byte[0];
            }

            res.Dispose();
        }

        public bool ReadFileRequest(BacnetAddress adr, BacnetObjectId object_id, ref int position, ref uint count, out bool end_of_file, out byte[] file_buffer, out int file_buffer_offset, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginReadFileRequest(adr, object_id, position, count, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndReadFileRequest(result, out count, out position, out end_of_file, out file_buffer, out file_buffer_offset, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    if (r < (m_retries - 1))
                        result.Resend();
                }
            }
            position = -1;
            count = 0;
            file_buffer = null;
            end_of_file = true;
            file_buffer_offset = -1;
            return false;
        }

        public bool SubscribeCOVRequest(BacnetAddress adr, BacnetObjectId object_id, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, uint lifetime, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginSubscribeCOVRequest(adr, object_id, subscribe_id, cancel, issue_confirmed_notifications, lifetime, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndSubscribeCOVRequest(result, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            return false;
        }

        public IAsyncResult BeginSubscribeCOVRequest(BacnetAddress adr, BacnetObjectId object_id, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, uint lifetime, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending SubscribeCOVRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeSubscribeCOV(b, subscribe_id, object_id, cancel, issue_confirmed_notifications, lifetime);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndSubscribeCOVRequest(IAsyncResult result, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {

            }
            else
            {

            }

            res.Dispose();
        }

        public bool SubscribePropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyReference monitored_property, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginSubscribePropertyRequest(adr, object_id, monitored_property, subscribe_id, cancel, issue_confirmed_notifications, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndSubscribePropertyRequest(result, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            return false;
        }

        public IAsyncResult BeginSubscribePropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyReference monitored_property, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending SubscribePropertyRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeSubscribeProperty(b, subscribe_id, object_id, cancel, issue_confirmed_notifications, 0, monitored_property, false, 0f);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndSubscribePropertyRequest(IAsyncResult result, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {

            }
            else
            {

            }

            res.Dispose();
        }

        public bool ReadPropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, out IList<BacnetValue> value_list, byte invoke_id = 0, uint array_index = ASN1.BACNET_ARRAY_ALL)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginReadPropertyRequest(adr, object_id, property_id, true, invoke_id, array_index))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndReadPropertyRequest(result, out value_list, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            value_list = null;
            return false;
        }

        public IAsyncResult BeginReadPropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, bool wait_for_transmit, byte invoke_id = 0, uint array_index = ASN1.BACNET_ARRAY_ALL)
        {
            Trace.WriteLine("Sending ReadPropertyRequest ... ", null);
            if(invoke_id == 0) invoke_id = unchecked ( m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeReadProperty(b, object_id, (uint)property_id, array_index);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndReadPropertyRequest(IAsyncResult result, out IList<BacnetValue> value_list, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
                //decode
                BacnetObjectId response_object_id;
                BacnetPropertyReference response_property;
                if (Services.DecodeReadPropertyAcknowledge(res.Result, 0, res.Result.Length, out response_object_id, out response_property, out value_list) < 0)
                    ex = new Exception("Decode");
            }
            else
            {
                value_list = null;
            }

            res.Dispose();
        }

        public bool WritePropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, IEnumerable<BacnetValue> value_list, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginWritePropertyRequest(adr, object_id, property_id, value_list, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndWritePropertyRequest(result, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            value_list = null;
            return false;
        }

        public IAsyncResult BeginWritePropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, IEnumerable<BacnetValue> value_list, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending WritePropertyRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeWriteProperty(b, object_id, (uint)property_id, ASN1.BACNET_ARRAY_ALL, m_writepriority, value_list);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndWritePropertyRequest(IAsyncResult result, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
            }
            else
            {
            }

            res.Dispose();
        }

        public bool ReadPropertyMultipleRequest(BacnetAddress adr, BacnetObjectId object_id, IList<BacnetPropertyReference> property_id_and_array_index, out IList<BacnetReadAccessResult> values, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginReadPropertyMultipleRequest(adr, object_id, property_id_and_array_index, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndReadPropertyMultipleRequest(result, out values, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            values = null;
            return false;
        }

        public IAsyncResult BeginReadPropertyMultipleRequest(BacnetAddress adr, BacnetObjectId object_id, IList<BacnetPropertyReference> property_id_and_array_index, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending ReadPropertyMultipleRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeReadPropertyMultiple(b, object_id, property_id_and_array_index);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndReadPropertyMultipleRequest(IAsyncResult result, out IList<BacnetReadAccessResult> values, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
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

        public bool DeviceCommunicationControlRequest(BacnetAddress adr, uint timeDuration, uint enable_disable, string password, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginDeviceCommunicationControlRequest(adr, timeDuration, enable_disable, password, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndDeviceCommunicationControlRequest(result, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            return false;
        }

        public IAsyncResult BeginDeviceCommunicationControlRequest(BacnetAddress adr, uint timeDuration, uint enable_disable, string password, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending DeviceCommunicationControlRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeDeviceCommunicationControl(b, timeDuration, enable_disable, password);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndDeviceCommunicationControlRequest(IAsyncResult result, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
            }
            else
            {   
            }

            res.Dispose();
        }

        public bool ReinitializeRequest(BacnetAddress adr, BacnetReinitializedStates state, string password, byte invoke_id = 0)
        {
            using (BacnetAsyncResult result = (BacnetAsyncResult)BeginReinitializeRequest(adr, state, password, true, invoke_id))
            {
                for (int r = 0; r < m_retries; r++)
                {
                    if (result.WaitForDone(m_timeout))
                    {
                        Exception ex;
                        EndReinitializeRequest(result, out ex);
                        if (ex != null) throw ex;
                        else return true;
                    }
                    result.Resend();
                }
            }
            return false;
        }

        public IAsyncResult BeginReinitializeRequest(BacnetAddress adr, BacnetReinitializedStates state, string password, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending ReinitializeRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeReinitializeDevice(b, state, password);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndReinitializeRequest(IAsyncResult result, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (ex == null && !res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
            }
            else
            {
            }

            res.Dispose();
        }

        public IAsyncResult BeginConfirmedNotify(BacnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, IList<BacnetPropertyValue> values, bool wait_for_transmit, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending Notify (confirmed) ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | (m_max_segments != BacnetMaxSegments.MAX_SEG0 ? BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED : 0), BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, m_max_segments, m_client.MaxAdpuLength, invoke_id, 0, 0);
            Services.EncodeCOVNotifyConfirmed(b, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);

            //send
            BacnetAsyncResult ret = new BacnetAsyncResult(this, adr, invoke_id, b.buffer, b.offset - m_client.HeaderLength, wait_for_transmit, m_transmit_timeout);
            ret.Resend();

            return ret;
        }

        public void EndConfirmedNotify(IAsyncResult result, out Exception ex)
        {
            BacnetAsyncResult res = (BacnetAsyncResult)result;
            ex = res.Error;
            if (!res.WaitForDone(m_timeout))
                ex = new Exception("Wait Timeout");

            if (ex == null)
            {
            }
            else
            {
            }
        }

        public bool Notify(BacnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool issueConfirmedNotifications, IList<BacnetPropertyValue> values)
        {
            if (!issueConfirmedNotifications)
            {
                Trace.WriteLine("Sending Notify (unconfirmed) ... ", null);
                EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
                NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
                APDU.EncodeUnconfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);
                Services.EncodeCOVNotifyUnconfirmed(b, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);
               // Modif F. Chaxel
                
                int sendbytes=m_client.Send(b.buffer, m_client.HeaderLength, b.offset - m_client.HeaderLength, adr, false, 0);

                if (sendbytes == b.offset)
                    return true;
                else
                    return false;
            }
            else
            {
                using (BacnetAsyncResult result = (BacnetAsyncResult)BeginConfirmedNotify(adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values, true))
                {
                    for (int r = 0; r < m_retries; r++)
                    {
                        if (result.WaitForDone(m_timeout))
                        {
                            Exception ex;
                            EndConfirmedNotify(result, out ex);
                            if (ex != null) throw ex;
                            else return true;
                        }
                        result.Resend();
                    }
                }
                return false;
            }
        }

        public class Segmentation
        {
            public EncodeBuffer buffer;
            public byte sequence_number;
            public byte window_size;
            public byte max_segments;
        }

        public static byte GetSegmentsCount(BacnetMaxSegments max_segments)
        {
            switch (max_segments)
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

        public static BacnetMaxSegments GetSegmentsCount(byte max_segments)
        {
            if (max_segments == 0)
                return BacnetMaxSegments.MAX_SEG0;
            else if (max_segments <= 2)
                return BacnetMaxSegments.MAX_SEG2;
            else if (max_segments <= 4)
                return BacnetMaxSegments.MAX_SEG4;
            else if (max_segments <= 8)
                return BacnetMaxSegments.MAX_SEG8;
            else if (max_segments <= 16)
                return BacnetMaxSegments.MAX_SEG16;
            else if (max_segments <= 32)
                return BacnetMaxSegments.MAX_SEG32;
            else if (max_segments <= 64)
                return BacnetMaxSegments.MAX_SEG64;
            else
                return BacnetMaxSegments.MAX_SEG65;
        }

        public Segmentation GetSegmentBuffer(BacnetMaxSegments max_segments)
        {
            if (max_segments == BacnetMaxSegments.MAX_SEG0) return null;
            Segmentation ret = new Segmentation();
            ret.buffer = GetEncodeBuffer(m_client.HeaderLength);
            ret.max_segments = GetSegmentsCount(max_segments);
            ret.window_size = m_proposed_window_size;
            return ret;
        }

        private EncodeBuffer EncodeSegmentHeader(BacnetAddress adr, byte invoke_id, Segmentation segmentation, BacnetConfirmedServices service, bool more_follows)
        {
            EncodeBuffer buffer;
            bool is_segmented = false;
            if (segmentation == null)
                buffer = GetEncodeBuffer(m_client.HeaderLength);
            else
            {
                buffer = segmentation.buffer;
                is_segmented = segmentation.sequence_number > 0 | more_follows;
            }
            buffer.Reset(m_client.HeaderLength);

            //encode
            NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);

            //set segments limits
            buffer.max_offset = buffer.offset + GetMaxApdu();
            int apdu_header = APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK | (is_segmented ? BacnetPduTypes.SEGMENTED_MESSAGE | BacnetPduTypes.SERVER : 0) | (more_follows ? BacnetPduTypes.MORE_FOLLOWS : 0), service, invoke_id, segmentation != null ? segmentation.sequence_number : (byte)0, segmentation != null ? segmentation.window_size : (byte)0);
            buffer.min_limit = (GetMaxApdu() - apdu_header) * (segmentation != null ? segmentation.sequence_number : 0);

            return buffer;
        }

        private bool EncodeSegment(BacnetAddress adr, byte invoke_id, Segmentation segmentation, BacnetConfirmedServices service, out EncodeBuffer buffer, Action<EncodeBuffer> apdu_content_encode)
        {
            //encode (regular)
            buffer = EncodeSegmentHeader(adr, invoke_id, segmentation, service, false);
            apdu_content_encode(buffer);

            bool more_follows = (buffer.result & EncodeResult.NotEnoughBuffer) > 0;
            if (segmentation != null && more_follows)
            {
                //reencode in segmented
                EncodeSegmentHeader(adr, invoke_id, segmentation, service, true);
                apdu_content_encode(buffer);
                return true;
            }
            else if (more_follows)
                return true;
            else
            {
                return segmentation != null ? segmentation.sequence_number > 0 : false;
            }
        }

        private void SendComplexAck(BacnetAddress adr, byte invoke_id, Segmentation segmentation, BacnetConfirmedServices service, Action<EncodeBuffer> apdu_content_encode)
        {
            Trace.WriteLine("Sending " + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(service.ToString().ToLower()) + " ... ", null);

            //encode
            EncodeBuffer buffer;
            if (EncodeSegment(adr, invoke_id, segmentation, service, out buffer, apdu_content_encode))
            {
                //client doesn't support segments
                if (segmentation == null)
                {
                    Trace.TraceInformation("Segmenation denied");
                    ErrorResponse(adr, service, invoke_id, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                    buffer.result = EncodeResult.Good;     //don't continue the segmentation
                    return;
                }

                //first segment? validate max segments
                if (segmentation.sequence_number == 0)  //only validate first segment
                {
                    if (segmentation.max_segments != 0xFF && segmentation.buffer.offset > (segmentation.max_segments * (GetMaxApdu() - 5)))      //5 is adpu header
                    {
                        Trace.TraceInformation("Too much segmenation");
                        ErrorResponse(adr, service, invoke_id, BacnetErrorClasses.ERROR_CLASS_SERVICES, BacnetErrorCodes.ERROR_CODE_ABORT_APDU_TOO_LONG);
                        buffer.result = EncodeResult.Good;     //don't continue the segmentation
                        return;
                    }
                    else
                        Trace.WriteLine("Segmentation required", null);
                }

                //increment before ack can do so (race condition)
                unchecked { segmentation.sequence_number++; };
            }

            //send
            m_client.Send(buffer.buffer, m_client.HeaderLength, buffer.GetLength() - m_client.HeaderLength, adr, false, 0);
        }

        public void ReadPropertyResponse(BacnetAddress adr, byte invoke_id, Segmentation segmentation, BacnetObjectId object_id, BacnetPropertyReference property, IEnumerable<BacnetValue> value)
        {
            SendComplexAck(adr, invoke_id, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, (b) =>
            {
                Services.EncodeReadPropertyAcknowledge(b, object_id, property.propertyIdentifier, property.propertyArrayIndex, value);
            });
        }

        public void ReadPropertyMultipleResponse(BacnetAddress adr, byte invoke_id, Segmentation segmentation, IList<BacnetReadAccessResult> values)
        {
            SendComplexAck(adr, invoke_id, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, (b) => 
            { 
                Services.EncodeReadPropertyMultipleAcknowledge(b, values); 
            });
        }

        public void ReadFileResponse(BacnetAddress adr, byte invoke_id, Segmentation segmentation, int position, uint count, bool end_of_file, byte[] file_buffer)
        {
            SendComplexAck(adr, invoke_id, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, (b) =>
            {
                Services.EncodeAtomicReadFileAcknowledge(b, true, end_of_file, position, 1, new byte[][] { file_buffer }, new int[] { (int)count });
            });
        }

        public void WriteFileResponse(BacnetAddress adr, byte invoke_id, Segmentation segmentation, int position)
        {
            SendComplexAck(adr, invoke_id, segmentation, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, (b) =>
            {
                Services.EncodeAtomicWriteFileAcknowledge(b, true, position);
            });
        }

        public void ErrorResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invoke_id, BacnetErrorClasses error_class, BacnetErrorCodes error_code)
        {
            Trace.WriteLine("Sending ErrorResponse ... ", null);
            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeError(b, BacnetPduTypes.PDU_TYPE_ERROR, service, invoke_id);
            Services.EncodeError(b, error_class, error_code);
            m_client.Send(b.buffer, m_client.HeaderLength, b.offset - m_client.HeaderLength, adr, false, 0);
        }

        public void SimpleAckResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invoke_id)
        {
            Trace.WriteLine("Sending SimpleAckResponse ... ", null);
            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeSimpleAck(b, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, service, invoke_id);
            m_client.Send(b.buffer, m_client.HeaderLength, b.offset - m_client.HeaderLength, adr, false, 0);
        }

        public void SegmentAckResponse(BacnetAddress adr, bool negative, bool server, byte original_invoke_id, byte sequence_number, byte actual_window_size)
        {
            Trace.WriteLine("Sending SimpleAckResponse ... ", null);
            EncodeBuffer b = GetEncodeBuffer(m_client.HeaderLength);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeSegmentAck(b, BacnetPduTypes.PDU_TYPE_SEGMENT_ACK | (negative ? BacnetPduTypes.NEGATIVE_ACK : 0) | (server ? BacnetPduTypes.SERVER : 0), original_invoke_id, sequence_number, actual_window_size);
            m_client.Send(b.buffer, m_client.HeaderLength, b.offset - m_client.HeaderLength, adr, false, 0);
        }

        public bool WaitForAllTransmits(int timeout)
        {
            return m_client.WaitForAllTransmits(timeout);
        }

        public bool WaitForSegmentAck(BacnetAddress adr, byte invoke_id, Segmentation segmentation, int timeout)
        {
            bool signaled = m_last_segment_ack.Wait(adr, invoke_id, timeout);
            if (signaled)
            {
                segmentation.sequence_number = (byte)((m_last_segment_ack.sequence_number + 1) % 256);
                segmentation.window_size = m_last_segment_ack.window_size;
            }
            return signaled;
        }

        public void Dispose()
        {
            m_client.Dispose();
            m_client = null;
        }
    }

    #region BacnetAsyncResult

    public class BacnetAsyncResult : IAsyncResult, IDisposable
    {
        private BacnetClient m_comm;
        private BacnetAddress m_adr;
        private byte m_wait_invoke_id;
        private Exception m_error;
        private byte[] m_result;
        private byte[] m_transmit_buffer;
        private int m_transmit_length;
        private bool m_wait_for_transmit;
        private int m_transmit_timeout;

        public byte[] Result { get { return m_result; } }
        public Exception Error
        {
            get { return m_error; }
            set
            {
                m_error = value;
                CompletedSynchronously = true;
                ((System.Threading.ManualResetEvent)AsyncWaitHandle).Set();
            }
        }
        public bool Segmented { get; private set; }

        public object AsyncState { get; set; }
        public Threading.WaitHandle AsyncWaitHandle { get; private set; }
        public bool CompletedSynchronously { get; private set; }
        public bool IsCompleted { get { return AsyncWaitHandle.WaitOne(0); } }

        public BacnetAsyncResult(BacnetClient comm, BacnetAddress adr, byte invoke_id, byte[] transmit_buffer, int transmit_length, bool wait_for_transmit, int transmit_timeout)
        {
            m_transmit_timeout = transmit_timeout;
            m_adr = adr;
            m_wait_for_transmit = wait_for_transmit;
            m_transmit_buffer = transmit_buffer;
            m_transmit_length = transmit_length;
            AsyncWaitHandle = new System.Threading.ManualResetEvent(false);
            m_comm = comm;
            m_wait_invoke_id = invoke_id;
            m_comm.OnComplexAck += new BacnetClient.ComplexAckHandler(m_comm_OnComplexAck);
            m_comm.OnError += new BacnetClient.ErrorHandler(m_comm_OnError);
            m_comm.OnAbort += new BacnetClient.AbortHandler(m_comm_OnAbort);
            m_comm.OnSimpleAck += new BacnetClient.SimpleAckHandler(m_comm_OnSimpleAck);
            m_comm.OnSegment += new BacnetClient.SegmentHandler(m_comm_OnSegment);
        }

        public void Resend()
        {
            try
            {
                if (m_comm.Transport.Send(m_transmit_buffer, m_comm.Transport.HeaderLength, m_transmit_length, m_adr, m_wait_for_transmit, m_transmit_timeout) < 0)
                {
                    Error = new System.IO.IOException("Write Timeout");
                }
            }
            catch (Exception ex)
            {
                Error = new Exception("Write Exception: " + ex.Message);
            }
        }

        private void m_comm_OnSegment(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte sequence_number, bool first, bool more_follows, byte[] buffer, int offset, int length)
        {
            if (invoke_id == m_wait_invoke_id)
            {
                Segmented = true;
                ((System.Threading.ManualResetEvent)AsyncWaitHandle).Set();
            }
        }

        private void m_comm_OnSimpleAck(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] data, int data_offset, int data_length)
        {
            if (invoke_id == m_wait_invoke_id)
            {
                ((System.Threading.ManualResetEvent)AsyncWaitHandle).Set();
            }
        }

        private void m_comm_OnAbort(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invoke_id, byte reason, byte[] buffer, int offset, int length)
        {
            if (invoke_id == m_wait_invoke_id)
            {
                Error = new Exception("Abort from device: " + reason);
            }
        }

        private void m_comm_OnError(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, BacnetErrorClasses error_class, BacnetErrorCodes error_code, byte[] buffer, int offset, int length)
        {
            if (invoke_id == m_wait_invoke_id)
            {
                Error = new Exception("Error from device: " + error_class + " - " + error_code);
            }
        }

        private void m_comm_OnComplexAck(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] buffer, int offset, int length)
        {
            if (invoke_id == m_wait_invoke_id)
            {
                Segmented = false;
                m_result = new byte[length];
                if (length > 0) Array.Copy(buffer, offset, m_result, 0, length);
                ((System.Threading.ManualResetEvent)AsyncWaitHandle).Set();     //notify waiter even if segmented
            }
        }

        /// <summary>
        /// Will continue waiting until all segments are recieved
        /// </summary>
        public bool WaitForDone(int timeout)
        {
            while (true)
            {
                if (!AsyncWaitHandle.WaitOne(timeout))
                    return false;
                if (Segmented)
                    ((System.Threading.ManualResetEvent)AsyncWaitHandle).Reset();
                else
                    return true;
            }
        }

        public void Dispose()
        {
            if (m_comm == null) return;
            m_comm.OnComplexAck -= m_comm_OnComplexAck;
            m_comm.OnError -= m_comm_OnError;
            m_comm.OnAbort -= m_comm_OnAbort;
            m_comm.OnSimpleAck -= m_comm_OnSimpleAck;
            m_comm.OnSegment -= m_comm_OnSegment;
            m_comm = null;
        }
    }

    #endregion
}
