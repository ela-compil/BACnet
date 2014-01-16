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

namespace BACnetServer
{
    class Program
    {
        private static DeviceStorage m_storage;
        private static BacnetClient m_udp_server;
        private static BacnetClient m_pipe_server;

        static void Main(string[] args)
        {
            try
            {
                //init
                Trace.Listeners.Add(new ConsoleTraceListener());
                m_storage = DeviceStorage.Load("DeviceStorage.xml");

                //create udp service point
                BacnetIpUdpProtocolTransport udp_transport = new BacnetIpUdpProtocolTransport(0xBAC0, false);       //set to true to force "single socket" usage
                m_udp_server = new BacnetClient(udp_transport);
                m_udp_server.OnWhoIs += new BacnetClient.WhoIsHandler(OnWhoIs);
                m_udp_server.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(OnReadPropertyRequest);
                m_udp_server.OnWritePropertyRequest += new BacnetClient.WritePropertyRequestHandler(OnWritePropertyRequest);
                m_udp_server.OnReadPropertyMultipleRequest += new BacnetClient.ReadPropertyMultipleRequestHandler(OnReadPropertyMultipleRequest);
                m_udp_server.OnWritePropertyMultipleRequest += new BacnetClient.WritePropertyMultipleRequestHandler(OnWritePropertyMultipleRequest);
                m_udp_server.Start();

                //create pipe (MSTP) service point
                BacnetPipeTransport pipe_transport = new BacnetPipeTransport("COM1003", true);
                BacnetMstpProtocolTransport mstp_transport = new BacnetMstpProtocolTransport(pipe_transport, 0);
                m_pipe_server = new BacnetClient(mstp_transport);
                m_pipe_server.OnWhoIs += new BacnetClient.WhoIsHandler(OnWhoIs);
                m_pipe_server.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(OnReadPropertyRequest);
                m_pipe_server.OnWritePropertyRequest += new BacnetClient.WritePropertyRequestHandler(OnWritePropertyRequest);
                m_pipe_server.OnReadPropertyMultipleRequest += new BacnetClient.ReadPropertyMultipleRequestHandler(OnReadPropertyMultipleRequest);
                m_pipe_server.OnWritePropertyMultipleRequest += new BacnetClient.WritePropertyMultipleRequestHandler(OnWritePropertyMultipleRequest);
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

        private static void OnWritePropertyMultipleRequest(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, BACNET_OBJECT_ID object_id, ICollection<BACNET_PROPERTY_VALUE> values)
        {
            try
            {
                m_storage.WritePropertyMultiple(object_id, values);
                sender.SimpleAckResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, invoke_id);
            }
            catch (Exception)
            {
                sender.ErrorResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, invoke_id, 0, 0);
            }
        }

        private static void OnReadPropertyMultipleRequest(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, IList<BACNET_OBJECT_ID> object_ids, IList<IList<BACNET_PROPERTY_REFERENCE>> property_id_and_array_index)
        {
            try
            {
                IList<BACNET_PROPERTY_VALUE> value;
                int o_count = 0;
                List<ICollection<BACNET_PROPERTY_VALUE>> values = new List<ICollection<BACNET_PROPERTY_VALUE>>(); 
                foreach (BACNET_OBJECT_ID object_id in object_ids)
                {
                    IList<BACNET_PROPERTY_REFERENCE> property = property_id_and_array_index[o_count];
                    if (property.Count == 1 && property[0].propertyIdentifier == (uint)BACNET_PROPERTY_ID.PROP_ALL)
                    {
                        m_storage.ReadPropertyAll(object_id, out value);
                        property_id_and_array_index[o_count] = property;
                    }
                    else
                        m_storage.ReadPropertyMultiple(object_id, property, out value);
                    values.Add(value);
                    o_count++;
                }
                sender.ReadPropertyMultipleResponse(adr, invoke_id, object_ids, values);
            }
            catch (Exception)
            {
                sender.ErrorResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, 0, 0);
            }
        }

        private static void OnWritePropertyRequest(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_VALUE value)
        {
            try
            {
                DeviceStorage.ErrorCodes code = m_storage.WriteProperty(object_id, (BACNET_PROPERTY_ID)value.property.propertyIdentifier, value.property.propertyArrayIndex, value.value);
                if (code == DeviceStorage.ErrorCodes.Good)
                    sender.SimpleAckResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id);
                else
                    sender.ErrorResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id, 0, 0);
            }
            catch (Exception)
            {
                sender.ErrorResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id, 0, 0);
            }
        }

        private static void OnReadPropertyRequest(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_REFERENCE property)
        {
            try
            {
                IList<BACNET_VALUE> value;
                DeviceStorage.ErrorCodes code = m_storage.ReadProperty(object_id, (BACNET_PROPERTY_ID)property.propertyIdentifier, property.propertyArrayIndex, out value);
                if (code == DeviceStorage.ErrorCodes.Good)
                    sender.ReadPropertyResponse(adr, invoke_id, object_id, property, value);
                else
                    sender.ErrorResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, 0, 0);
            }
            catch (Exception)
            {
                sender.ErrorResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, 0, 0);
            }
        }

        private static void OnWhoIs(BacnetClient sender, BACNET_ADDRESS adr, int low_limit, int high_limit)
        {
            sender.Iam(m_storage.DeviceId);
        }
    }
}
