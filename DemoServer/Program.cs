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
using System.IO.BACnet;
using System.IO.BACnet.Storage;
using System.Diagnostics;

namespace DemoServer
{
    class Program
    {
        private static DeviceStorage m_storage;
        private static BacnetClient m_udp_server;
        private static BacnetClient m_pipe_server;
        private static Dictionary<BacnetObjectId, List<Subscription>> m_subscriptions = new Dictionary<BacnetObjectId, List<Subscription>>();
        private static object m_lockObject = new object();

        static void Main(string[] args)
        {
            try
            {
                //init
                Trace.Listeners.Add(new ConsoleTraceListener());
                m_storage = DeviceStorage.Load("DeviceStorage.xml");
                m_storage.ChangeOfValue += new DeviceStorage.ChangeOfValueHandler(m_storage_ChangeOfValue);
                m_storage.ReadOverride += new DeviceStorage.ReadOverrideHandler(m_storage_ReadOverride);

                //create udp service point
                BacnetIpUdpProtocolTransport udp_transport = new BacnetIpUdpProtocolTransport(0xBAC0, false);       //set to true to force "single socket" usage
                m_udp_server = new BacnetClient(udp_transport);
                m_udp_server.OnWhoIs += new BacnetClient.WhoIsHandler(OnWhoIs);
                m_udp_server.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(OnReadPropertyRequest);
                m_udp_server.OnWritePropertyRequest += new BacnetClient.WritePropertyRequestHandler(OnWritePropertyRequest);
                m_udp_server.OnReadPropertyMultipleRequest += new BacnetClient.ReadPropertyMultipleRequestHandler(OnReadPropertyMultipleRequest);
                m_udp_server.OnWritePropertyMultipleRequest += new BacnetClient.WritePropertyMultipleRequestHandler(OnWritePropertyMultipleRequest);
                m_udp_server.OnAtomicWriteFileRequest += new BacnetClient.AtomicWriteFileRequestHandler(OnAtomicWriteFileRequest);
                m_udp_server.OnAtomicReadFileRequest += new BacnetClient.AtomicReadFileRequestHandler(OnAtomicReadFileRequest);
                m_udp_server.OnSubscribeCOV += new BacnetClient.SubscribeCOVRequestHandler(OnSubscribeCOV);
                m_udp_server.OnSubscribeCOVProperty += new BacnetClient.SubscribeCOVPropertyRequestHandler(OnSubscribeCOVProperty);
                m_udp_server.Start();

                //create pipe (MSTP) service point
                BacnetPipeTransport pipe_transport = new BacnetPipeTransport("COM1003", true);
                BacnetMstpProtocolTransport mstp_transport = new BacnetMstpProtocolTransport(pipe_transport, 0, 127, 1);
                mstp_transport.StateLogging = false;        //if you enable this, it will display a lot of information about the StateMachine
                m_pipe_server = new BacnetClient(mstp_transport);
                m_pipe_server.OnWhoIs += new BacnetClient.WhoIsHandler(OnWhoIs);
                m_pipe_server.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(OnReadPropertyRequest);
                m_pipe_server.OnWritePropertyRequest += new BacnetClient.WritePropertyRequestHandler(OnWritePropertyRequest);
                m_pipe_server.OnReadPropertyMultipleRequest += new BacnetClient.ReadPropertyMultipleRequestHandler(OnReadPropertyMultipleRequest);
                m_pipe_server.OnWritePropertyMultipleRequest += new BacnetClient.WritePropertyMultipleRequestHandler(OnWritePropertyMultipleRequest);
                m_pipe_server.OnAtomicWriteFileRequest += new BacnetClient.AtomicWriteFileRequestHandler(OnAtomicWriteFileRequest);
                m_pipe_server.OnAtomicReadFileRequest += new BacnetClient.AtomicReadFileRequestHandler(OnAtomicReadFileRequest);
                m_pipe_server.OnSubscribeCOV += new BacnetClient.SubscribeCOVRequestHandler(OnSubscribeCOV);
                m_pipe_server.OnSubscribeCOVProperty += new BacnetClient.SubscribeCOVPropertyRequestHandler(OnSubscribeCOVProperty);
                m_pipe_server.Start();

                //display info
                Console.WriteLine("DemoServer startet ...");
                Console.WriteLine("Udp service point - port: 0x" + udp_transport.SharedPort.ToString("X4") + "" + (udp_transport.ExclusivePort != udp_transport.SharedPort ? " and 0x" + udp_transport.ExclusivePort.ToString("X4") : ""));
                Console.WriteLine("MSTP service point - name: \\\\.pipe\\" + pipe_transport.Name + ", source_address: " + mstp_transport.SourceAddress + ", max_master: " + mstp_transport.MaxMaster + ", max_info_frames: " + mstp_transport.MaxInfoFrames);
                Console.WriteLine("");

                //send greeting
                m_udp_server.Iam(m_storage.DeviceId);
                m_pipe_server.Iam(m_storage.DeviceId);

                //endless loop of nothing
                Console.WriteLine("Press the ANY key to exit!");
                while (!Console.KeyAvailable)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Press the ANY key ... once more");
                Console.ReadKey();
            }
        }

        private static void m_storage_ReadOverride(BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, out IList<BacnetValue> value, out DeviceStorage.ErrorCodes status, out bool handled)
        {
            handled = true;
            value = new BacnetValue[0];
            status = DeviceStorage.ErrorCodes.Good;

            if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && property_id == BacnetPropertyIds.PROP_OBJECT_LIST)
            {
                //object list
                BacnetValue[] list = new BacnetValue[m_storage.Objects.Length];
                for (int i = 0; i < list.Length; i++)
                {
                    list[i].Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
                    list[i].Value = new BacnetObjectId(m_storage.Objects[i].Type, m_storage.Objects[i].Instance);
                }
                value = list;
            }
            else if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && object_id.instance == m_storage.DeviceId && property_id == BacnetPropertyIds.PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED)
            {
                BacnetValue v = new BacnetValue();
                v.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING;
                BacnetBitString b = new BacnetBitString();
                b.SetBit((byte)BacnetObjectTypes.MAX_ASHRAE_OBJECT_TYPE, false); //set all false
                b.SetBit((byte)BacnetObjectTypes.OBJECT_ANALOG_INPUT, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_ANALOG_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_BINARY_INPUT, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_BINARY_OUTPUT, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_BINARY_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_DEVICE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_FILE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_MULTI_STATE_OUTPUT, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_BITSTRING_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_CHARACTERSTRING_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_OCTETSTRING_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_DATE_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_DATETIME_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_TIME_VALUE, true);
                b.SetBit((byte)BacnetObjectTypes.OBJECT_INTEGER_VALUE, true);
                //there're prolly more, who knows
                v.Value = b;
                value = new BacnetValue[] { v };
            }
            else if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && object_id.instance == m_storage.DeviceId && property_id == BacnetPropertyIds.PROP_PROTOCOL_SERVICES_SUPPORTED)
            {
                BacnetValue v = new BacnetValue();
                v.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING;
                BacnetBitString b = new BacnetBitString();
                b.SetBit((byte)BacnetServicesSupported.MAX_BACNET_SERVICES_SUPPORTED, false); //set all false
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_I_AM, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_WHO_IS, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_READ_PROP_MULTIPLE, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_READ_PROPERTY, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_WRITE_PROPERTY, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_WRITE_PROP_MULTIPLE, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_ATOMIC_READ_FILE, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_ATOMIC_WRITE_FILE, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_CONFIRMED_COV_NOTIFICATION, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_UNCONFIRMED_COV_NOTIFICATION, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_SUBSCRIBE_COV, true);
                b.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_SUBSCRIBE_COV_PROPERTY, true);
                v.Value = b;
                value = new BacnetValue[] { v };
            }
            else if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && object_id.instance == m_storage.DeviceId && property_id == BacnetPropertyIds.PROP_SEGMENTATION_SUPPORTED)
            {
                BacnetValue v = new BacnetValue();
                v.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;
                v.Value = (uint)BacnetSegmentations.SEGMENTATION_BOTH;
                value = new BacnetValue[] { v };
            }
            else if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && object_id.instance == m_storage.DeviceId && property_id == BacnetPropertyIds.PROP_SYSTEM_STATUS)
            {
                BacnetValue v = new BacnetValue();
                v.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;
                v.Value = (uint)BacnetDeviceStatus.STATUS_OPERATIONAL;      //can we be in any other mode I wonder?
                value = new BacnetValue[] { v };
            }
            else if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && object_id.instance == m_storage.DeviceId && property_id == BacnetPropertyIds.PROP_ACTIVE_COV_SUBSCRIPTIONS)
            {
                List<BacnetValue> list = new List<BacnetValue>();
                foreach (KeyValuePair<BacnetObjectId, List<Subscription>> entry in m_subscriptions)
                {
                    foreach (Subscription sub in entry.Value)
                    {
                        //encode
                        System.IO.BACnet.Serialize.EncodeBuffer buffer = new System.IO.BACnet.Serialize.EncodeBuffer();
                        System.IO.BACnet.Serialize.ASN1.encode_cov_subscription(buffer, sub.reciever_address, sub.subscriberProcessIdentifier, sub.monitoredObjectIdentifier, sub.monitoredProperty, sub.issueConfirmedNotifications, sub.lifetime, sub.covIncrement);

                        //add
                        BacnetValue v = new BacnetValue();
                        v.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_COV_SUBSCRIPTION;
                        v.Value = buffer.ToArray();
                        list.Add(v);
                    }
                }
                value = list;
            }
            else if (object_id.type == BacnetObjectTypes.OBJECT_OCTETSTRING_VALUE && object_id.instance == 0 && property_id == BacnetPropertyIds.PROP_PRESENT_VALUE)
            {
                //this is our huge blob
                BacnetValue v = new BacnetValue();
                v.Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING;
                byte[] blob = new byte[2000];
                for(int i = 0; i < blob.Length; i++)
                    blob[i] = (i % 2 == 0) ? (byte)'A' : (byte)'B';
                v.Value = blob;
                value = new BacnetValue[] { v };
            }
            else
            {
                handled = false;
            }
        }

        private class Subscription
        {
            public BacnetClient reciever;
            public BacnetAddress reciever_address;
            public uint subscriberProcessIdentifier;
            public BacnetObjectId monitoredObjectIdentifier;
            public BacnetPropertyReference monitoredProperty;
            public bool issueConfirmedNotifications;
            public uint lifetime;
            public DateTime start;
            public float covIncrement;
            public Subscription(BacnetClient reciever, BacnetAddress reciever_address, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference property, bool issueConfirmedNotifications, uint lifetime, float covIncrement)
            {
                this.reciever = reciever;
                this.reciever_address = reciever_address;
                this.subscriberProcessIdentifier = subscriberProcessIdentifier;
                this.monitoredObjectIdentifier = monitoredObjectIdentifier;
                this.monitoredProperty = property;
                this.issueConfirmedNotifications = issueConfirmedNotifications;
                this.lifetime = lifetime;
                this.start = DateTime.Now;
                this.covIncrement = covIncrement;
            }
            public uint GetTimeRemaining()
            {
                if (lifetime == 0) return 0;
                else return (uint)(DateTime.Now - start).TotalSeconds;
            }
        }

        private static void RemoveOldSubscriptions()
        {
            LinkedList<BacnetObjectId> to_be_deleted = new LinkedList<BacnetObjectId>();
            foreach (KeyValuePair<BacnetObjectId, List<Subscription>> entry in m_subscriptions)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    if (entry.Value[i].GetTimeRemaining() < 0)
                    {
                        entry.Value.RemoveAt(i);
                        i--;
                    }
                }
                if (entry.Value.Count == 0)
                    to_be_deleted.AddLast(entry.Key);
            }
            foreach (BacnetObjectId obj_id in to_be_deleted)
                m_subscriptions.Remove(obj_id);
        }

        private static Subscription HandleSubscriptionRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, uint property_id, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement)
        {
            //remove old leftovers
            RemoveOldSubscriptions();

            //find existing
            List<Subscription> subs = null;
            Subscription sub = null;
            if (m_subscriptions.ContainsKey(monitoredObjectIdentifier))
            {
                subs = m_subscriptions[monitoredObjectIdentifier];
                foreach (Subscription s in subs)
                {
                    if (s.reciever == sender && s.reciever_address == adr && s.subscriberProcessIdentifier == subscriberProcessIdentifier && s.monitoredObjectIdentifier.Equals(monitoredObjectIdentifier) && s.monitoredProperty.propertyIdentifier == property_id)
                    {
                        sub = s;
                        break;
                    }
                }
            }

            //cancel
            if (cancellationRequest && sub != null)
            {
                subs.Remove(sub);
                if (subs.Count == 0)
                    m_subscriptions.Remove(sub.monitoredObjectIdentifier);

                //send confirm
                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invoke_id);

                return null;
            }

            //create if needed
            if (sub == null)
                sub = new Subscription(sender, adr, subscriberProcessIdentifier, monitoredObjectIdentifier, new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL), issueConfirmedNotifications, lifetime, covIncrement);
            if (subs == null)
            {
                subs = new List<Subscription>();
                subs.Add(sub);
                m_subscriptions.Add(sub.monitoredObjectIdentifier, subs);
            }

            //update perhaps
            sub.issueConfirmedNotifications = issueConfirmedNotifications;
            sub.lifetime = lifetime;
            sub.start = DateTime.Now;

            return sub;
        }

        private static void OnSubscribeCOV(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    //create 
                    Subscription sub = HandleSubscriptionRequest(sender, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, (uint)BacnetPropertyIds.PROP_ALL, cancellationRequest, issueConfirmedNotifications, lifetime, 0);

                    //send confirm
                    sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invoke_id);

                    //also send first values
                    if (!cancellationRequest)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                                {
                                    IList<BacnetPropertyValue> values;
                                    m_storage.ReadPropertyAll(sub.monitoredObjectIdentifier, out values);
                                    if (!sender.Notify(adr, sub.subscriberProcessIdentifier, m_storage.DeviceId, sub.monitoredObjectIdentifier, sub.GetTimeRemaining(), sub.issueConfirmedNotifications, values))
                                        Trace.TraceError("Couldn't send notify");
                                }, null);
                    }
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void OnSubscribeCOVProperty(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference monitoredProperty, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    //create 
                    Subscription sub = HandleSubscriptionRequest(sender, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, (uint)BacnetPropertyIds.PROP_ALL, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement);

                    //send confirm
                    sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, invoke_id);

                    //also send first values
                    if (!cancellationRequest)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                        {
                            IList<BacnetValue> _values;
                            m_storage.ReadProperty(sub.monitoredObjectIdentifier, (BacnetPropertyIds)sub.monitoredProperty.propertyIdentifier, sub.monitoredProperty.propertyArrayIndex, out _values);
                            List<BacnetPropertyValue> values = new List<BacnetPropertyValue>();
                            BacnetPropertyValue tmp = new BacnetPropertyValue();
                            tmp.property = sub.monitoredProperty;
                            tmp.value = _values;
                            values.Add(tmp);
                            if (!sender.Notify(adr, sub.subscriberProcessIdentifier, m_storage.DeviceId, sub.monitoredObjectIdentifier, sub.GetTimeRemaining(), sub.issueConfirmedNotifications, values))
                                Trace.TraceError("Couldn't send notify");
                        }, null);
                    }
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void m_storage_ChangeOfValue(DeviceStorage sender, BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, IList<BacnetValue> value)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                lock (m_lockObject)
                {
                    //remove old leftovers
                    RemoveOldSubscriptions();

                    //find subscription
                    if (!m_subscriptions.ContainsKey(object_id)) return;
                    List<Subscription> subs = m_subscriptions[object_id];

                    //convert
                    List<BacnetPropertyValue> values = new List<BacnetPropertyValue>();
                    BacnetPropertyValue tmp = new BacnetPropertyValue();
                    tmp.property = new BacnetPropertyReference((uint)property_id, array_index);
                    tmp.value = value;
                    values.Add(tmp);

                    //send to all
                    foreach (Subscription sub in subs)
                    {
                        if (sub.monitoredProperty.propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL || sub.monitoredProperty.propertyIdentifier == (uint)property_id)
                        {
                            //send notify
                            if (!sub.reciever.Notify(sub.reciever_address, sub.subscriberProcessIdentifier, m_storage.DeviceId, sub.monitoredObjectIdentifier, sub.GetTimeRemaining(), sub.issueConfirmedNotifications, values))
                                Trace.TraceError("Couldn't send notify");
                        }
                    }
                }
            }, null);
        }

        private static void HandleSegmentationResponse(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetMaxSegments max_segments, Action<BacnetClient.Segmentation> transmit)
        {
            BacnetClient.Segmentation segmentation = sender.GetSegmentBuffer(max_segments);

            //send first
            transmit(segmentation);

            if (segmentation == null || segmentation.buffer.result == System.IO.BACnet.Serialize.EncodeResult.Good) return;

            //start new thread to handle the segment sequence
            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                byte old_max_info_frames = sender.Transport.MaxInfoFrames;
                sender.Transport.MaxInfoFrames = segmentation.window_size;      //increase max_info_frames, to increase throughput. This might be against 'standard'
                while (true)
                {
                    bool more_follows = (segmentation.buffer.result & System.IO.BACnet.Serialize.EncodeResult.NotEnoughBuffer) > 0;

                    //wait for segmentACK
                    if ((segmentation.sequence_number - 1) % segmentation.window_size == 0 || !more_follows)
                    {
                        if (!sender.WaitForAllTransmits(sender.TransmitTimeout))
                        {
                            Trace.TraceWarning("Transmit timeout");
                            break;
                        }
                        byte current_number = segmentation.sequence_number;
                        if (!sender.WaitForSegmentAck(adr, invoke_id, segmentation, sender.Timeout))
                        {
                            Trace.TraceWarning("Didn't get segmentACK");
                            break;
                        }
                        if (segmentation.sequence_number != current_number)
                        {
                            Trace.WriteLine("Oh, a retransmit", null);
                            more_follows = true;
                        }
                    }
                    else
                    {
                        //a negative segmentACK perhaps
                        byte current_number = segmentation.sequence_number;
                        sender.WaitForSegmentAck(adr, invoke_id, segmentation, 0);      //don't wait
                        if (segmentation.sequence_number != current_number)
                        {
                            Trace.WriteLine("Oh, a retransmit", null);
                            more_follows = true;
                        }
                    }

                    if (more_follows)
                        lock (m_lockObject) transmit(segmentation);
                    else
                        break;
                }
                sender.Transport.MaxInfoFrames = old_max_info_frames;
            });
        }

        private static void OnAtomicReadFileRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, bool is_stream, BacnetObjectId object_id, int position, uint count, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    if (object_id.type != BacnetObjectTypes.OBJECT_FILE) throw new Exception("File Reading on non file objects ... bah!");
                    else if (object_id.instance != 0) throw new Exception("Don't know this file");

                    //this is a test file for performance measuring
                    int filesize = m_storage.ReadPropertyValue(object_id, BacnetPropertyIds.PROP_FILE_SIZE);        //test file is ~10mb
                    bool end_of_file = (position + count) >= filesize;
                    count = (uint)Math.Min(count, filesize - position);
                    int max_filebuffer_size = sender.GetFileBufferMaxSize();
                    if (count > max_filebuffer_size && max_segments > 0)
                    {
                        //create segmented message!!!
                    }
                    else
                    {
                        count = (uint)Math.Min(count, max_filebuffer_size);     //trim
                    }

                    //fill file with bogus content 
                    byte[] file_buffer = new byte[count];
                    byte[] bogus = new byte[] { (byte)'F', (byte)'I', (byte)'L', (byte)'L' };
                    for (int i = 0; i < count; i += bogus.Length)
                        Array.Copy(bogus, 0, file_buffer, i, Math.Min(bogus.Length, count - i));

                    //send
                    HandleSegmentationResponse(sender, adr, invoke_id, max_segments, (seg) =>
                    {
                        sender.ReadFileResponse(adr, invoke_id, seg, position, count, end_of_file, file_buffer);
                    });
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void OnAtomicWriteFileRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, bool is_stream, BacnetObjectId object_id, int position, uint block_count, byte[][] blocks, int[] counts, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    if (object_id.type != BacnetObjectTypes.OBJECT_FILE) throw new Exception("File Reading on non file objects ... bah!");
                    else if (object_id.instance != 0) throw new Exception("Don't know this file");

                    //this is a test file for performance measuring
                    //don't do anything with the content

                    //adjust size though
                    int filesize = m_storage.ReadPropertyValue(object_id, BacnetPropertyIds.PROP_FILE_SIZE);
                    int new_filesize = position + counts[0];
                    if (new_filesize > filesize) m_storage.WritePropertyValue(object_id, BacnetPropertyIds.PROP_FILE_SIZE, new_filesize);
                    if (counts[0] == 0) m_storage.WritePropertyValue(object_id, BacnetPropertyIds.PROP_FILE_SIZE, 0);      //clear file

                    //send confirm
                    HandleSegmentationResponse(sender, adr, invoke_id, max_segments, (seg) =>
                    {
                        sender.WriteFileResponse(adr, invoke_id, seg, position);
                    });
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void OnWritePropertyMultipleRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    m_storage.WritePropertyMultiple(object_id, values);
                    sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, invoke_id);
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void OnReadPropertyMultipleRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, IList<BacnetObjectId> object_ids, IList<IList<BacnetPropertyReference>> property_id_and_array_index, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    IList<BacnetPropertyValue> value;
                    int o_count = 0;
                    List<ICollection<BacnetPropertyValue>> values = new List<ICollection<BacnetPropertyValue>>();
                    foreach (BacnetObjectId object_id in object_ids)
                    {
                        IList<BacnetPropertyReference> property = property_id_and_array_index[o_count];
                        if (property.Count == 1 && property[0].propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL)
                        {
                            m_storage.ReadPropertyAll(object_id, out value);
                            property_id_and_array_index[o_count] = property;
                        }
                        else
                            m_storage.ReadPropertyMultiple(object_id, property, out value);
                        values.Add(value);
                        o_count++;
                    }

                    HandleSegmentationResponse(sender, adr, invoke_id, max_segments, (seg) => 
                    { 
                        sender.ReadPropertyMultipleResponse(adr, invoke_id, seg, object_ids, values); 
                    });
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void OnWritePropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyValue value, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    DeviceStorage.ErrorCodes code = m_storage.WriteProperty(object_id, (BacnetPropertyIds)value.property.propertyIdentifier, value.property.propertyArrayIndex, value.value);
                    if (code == DeviceStorage.ErrorCodes.Good)
                        sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id);
                    else
                        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void OnReadPropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property, BacnetMaxSegments max_segments)
        {
            lock (m_lockObject)
            {
                try
                {
                    IList<BacnetValue> value;
                    DeviceStorage.ErrorCodes code = m_storage.ReadProperty(object_id, (BacnetPropertyIds)property.propertyIdentifier, property.propertyArrayIndex, out value);
                    if (code == DeviceStorage.ErrorCodes.Good)
                        sender.ReadPropertyResponse(adr, invoke_id, sender.GetSegmentBuffer(max_segments), object_id, property, value);
                    else
                        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private static void OnWhoIs(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            lock (m_lockObject)
            {
                sender.Iam(m_storage.DeviceId);
            }
        }
    }
}
