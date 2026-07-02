/**************************************************************************
*                           MIT License
* 
* Copyright (C)  Frederic Chaxel <fchaxel@free.fr> 
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
using System.IO.BACnet;
using System.Threading;
using System.IO.BACnet.Storage;
using System.Diagnostics;

namespace BasicLowEndServer
{
    //
    // Low-end, slow device server simulator : only for Yabe testing
    //
    // ReadPropertyMultiple not supported & PROP_LIST content should be requested one by one
    // ... but the IP stack can queue requests, not all can do that
    // 
    class Program
    {
        static BacnetClient bacnet_client;
        static DeviceStorage m_storage;

        static int DelaysMS = 50; // delay before each response to read property request
        static int NbObj = 50;     // number of objects in the dictionnary

        /*****************************************************************************************************/
        static void Main(string[] args)
        {

            Trace.Listeners.Add(new ConsoleTraceListener());

            if (args.Length >= 1) 
                Int32.TryParse(args[0], out NbObj);

            if (args.Length >= 2)
                Int32.TryParse(args[1], out DelaysMS);

            if (NbObj < 10) NbObj = 10;
            if (DelaysMS < 0) DelaysMS = 0;

            try
            {
                StartActivity();
                Console.WriteLine($"\r\nStarted with {NbObj} objects and {DelaysMS}ms added delay before network responses\r\n");
                Console.ReadKey();
            }
            catch { }
        }

        /*****************************************************************************************************/
        static void StartActivity()
        {
            // Load the device descriptor from the embedded resource file
            // Get myId as own device id
            m_storage = DeviceStorage.Load("BasicLowEndServer.DeviceDescriptor.xml");

            // Simply make several superficial copies of the second object in the dictionnaray
            var BacObjects = m_storage.Objects;
            Array.Resize(ref BacObjects, NbObj);

            var original = m_storage.Objects[2]; // the first object after DEVICE & GROUP

            for (int i = 0; i < NbObj-2; i++)
            {
                System.IO.BACnet.Storage.Object newobject = new System.IO.BACnet.Storage.Object();
                newobject.Instance = (uint)(100 + i);
                newobject.Properties = original.Properties; // Ref to the same array, no deap copy, don't care it's a test
                BacObjects[i + 2] = newobject;
            }
            m_storage.Objects = BacObjects;

            m_storage.ReadOverride += new DeviceStorage.ReadOverrideHandler(m_storage_ReadOverride);

            // Bacnet on UDP/IP/Ethernet
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false));
            bacnet_client.OnWhoIs += new BacnetClient.WhoIsHandler(handler_OnWhoIs);
            bacnet_client.OnIam += new BacnetClient.IamHandler(bacnet_client_OnIam);
            bacnet_client.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(handler_OnReadPropertyRequest);

            bacnet_client.Start();    // go
            // Send Iam
            bacnet_client.Iam(m_storage.DeviceId);

        }

        static void bacnet_client_OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            //ignore Iams from other devices. (Also loopbacks)
        }
        /*****************************************************************************************************/
        private static void m_storage_ReadOverride(BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, out IList<BacnetValue> value, out DeviceStorage.ErrorCodes status, out bool handled)
        {
            handled = true;
            value = new BacnetValue[0];
            status = DeviceStorage.ErrorCodes.Good;

            // Force the remote device to query one by one the PROP_OBJECT_LIST
            if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && property_id == BacnetPropertyIds.PROP_OBJECT_LIST)
            {
                if (array_index == 0)
                {
                    //object list count 
                    value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, (uint)m_storage.Objects.Length) };
                }
                else if (array_index != System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                {
                    //object list index 
                    value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, new BacnetObjectId(m_storage.Objects[array_index - 1].Type, m_storage.Objects[array_index - 1].Instance)) };    
                }
                else
                {
                    // Reject ReadProperties of the full content
                    value = null;
                    status = DeviceStorage.ErrorCodes.GenericError;
                }
            }

            else
                handled = false;

            Thread.Sleep(DelaysMS); // Slow device
        }
       
        /*****************************************************************************************************/
        static void handler_OnWhoIs(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            if (low_limit != -1 && m_storage.DeviceId < low_limit) return;
            else if (high_limit != -1 && m_storage.DeviceId > high_limit) return;
            sender.Iam(m_storage.DeviceId, BacnetSegmentations.SEGMENTATION_BOTH);
        }

        /*****************************************************************************************************/
        static void handler_OnReadPropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property, BacnetMaxSegments max_segments)
        {
            lock (m_storage)
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

    }
}
