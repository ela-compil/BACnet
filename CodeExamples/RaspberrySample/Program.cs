/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr> 
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
using System.Linq;
using System.Text;
using System.IO.BACnet;
using System.Threading;
using System.IO.BACnet.Storage;
using System.Diagnostics;
using GPIO;

namespace BasicServer
{
    //
    // Raspberry GPIO Server based on Yabe code
    //
    class Program
    {
        static BacnetClient bacnet_client;
        static DeviceStorage m_storage;

        static List<BacnetObjectId> Input;
        static List<BacnetObjectId> Output;

        /*****************************************************************************************************/
        static void Main(string[] args)
        {

            Input = new List<BacnetObjectId>();
            Output = new List<BacnetObjectId>();

            try
            {
                StartActivity();
                Console.WriteLine("Running");

                for (;;)
                {
                    Thread.Sleep(50);
                    // Refresh all input & output
                    
                    foreach (BacnetObjectId o in Input)
                    {
                        IList<BacnetValue> valtowrite = new BacnetValue[1] { new BacnetValue(Convert.ToUInt16(FileGPIO.InputPin(o.instance))) };
                        lock (m_storage)
                            m_storage.WriteProperty(o, BacnetPropertyIds.PROP_PRESENT_VALUE, 0, valtowrite, true);
                    }
                    
                    foreach (BacnetObjectId o in Output)
                    {
                        IList<BacnetValue> valtoread;
                        // index 0 : number of values in the array
                        // index 1 : first value
                        lock (m_storage)
                            m_storage.ReadProperty(o, BacnetPropertyIds.PROP_PRESENT_VALUE, 1, out valtoread);
                        // Get the first ... and here the only element
                        bool val = Convert.ToBoolean(valtoread[0].Value);
                        FileGPIO.OutputPin(o.instance, val);
                    }
                }
            }
            catch { }
        }
        /*****************************************************************************************************/
        static void RaspberryGpioConfig()
        {
            FileGPIO.CleanUpAllPins();
            try
            {
                foreach (System.IO.BACnet.Storage.Object o in m_storage.Objects)
                {
                    if (o.Type == BacnetObjectTypes.OBJECT_BINARY_INPUT)
                    {
                        FileGPIO.InputPin(o.Instance);
                        Input.Add(new BacnetObjectId(BacnetObjectTypes.OBJECT_BINARY_INPUT, o.Instance));
                        Console.WriteLine("GPIO" + o.Instance.ToString() + " as Input in OBJECT_BINARY_INPUT:" + o.Instance.ToString());
                    }
                    if (o.Type == BacnetObjectTypes.OBJECT_BINARY_OUTPUT)
                    {
                        FileGPIO.OutputPin(o.Instance, true);
                        Output.Add(new BacnetObjectId(BacnetObjectTypes.OBJECT_BINARY_OUTPUT, o.Instance));
                        Console.WriteLine("GPIO" + o.Instance.ToString() + " as Output in OBJECT_BINARY_OUTPUT:" + o.Instance.ToString());
                    }
                }
            }
            catch { }
        }

        /*****************************************************************************************************/
        static void StartActivity()
        {
            // Load the device descriptor from the external file
            m_storage = DeviceStorage.Load("DeviceDescriptor.xml");

            RaspberryGpioConfig();

            // Bacnet on UDP/IP/Ethernet
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false)); 

            bacnet_client.OnWhoIs += new BacnetClient.WhoIsHandler(handler_OnWhoIs);
            bacnet_client.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(handler_OnReadPropertyRequest);
            bacnet_client.OnReadPropertyMultipleRequest += new BacnetClient.ReadPropertyMultipleRequestHandler(handler_OnReadPropertyMultipleRequest);
            bacnet_client.OnWritePropertyRequest += new BacnetClient.WritePropertyRequestHandler(handler_OnWritePropertyRequest);

            bacnet_client.Start();    // go
            // Send Iam
            bacnet_client.Iam(m_storage.DeviceId, new BacnetSegmentations());

        }

        /*****************************************************************************************************/
        static void handler_OnWritePropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyValue value, BacnetMaxSegments max_segments)
        {
            // only OBJECT_BINARY_OUTPUT:x.PROP_PRESENT_VALUE could be write in this sample code
            if ((object_id.type != BacnetObjectTypes.OBJECT_BINARY_OUTPUT) || ((BacnetPropertyIds)value.property.propertyIdentifier != BacnetPropertyIds.PROP_PRESENT_VALUE))
            {
                sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_WRITE_ACCESS_DENIED);
                return;
            }

            lock (m_storage)
            {
                try
                {
                    DeviceStorage.ErrorCodes code = m_storage.WriteCommandableProperty(object_id, (BacnetPropertyIds)value.property.propertyIdentifier, value.value[0], value.priority);
                    if (code == DeviceStorage.ErrorCodes.NotForMe)
                        code = m_storage.WriteProperty(object_id, (BacnetPropertyIds)value.property.propertyIdentifier, value.property.propertyArrayIndex, value.value);

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
        /*****************************************************************************************************/
        static void handler_OnWhoIs(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            if (low_limit != -1 && m_storage.DeviceId < low_limit) return;
            else if (high_limit != -1 && m_storage.DeviceId > high_limit) return;
            sender.Iam(m_storage.DeviceId, new BacnetSegmentations());
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

        /*****************************************************************************************************/
        static void handler_OnReadPropertyMultipleRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments max_segments)
        {
            lock (m_storage)
            {
                try
                {
                    IList<BacnetPropertyValue> value;
                    List<BacnetReadAccessResult> values = new List<BacnetReadAccessResult>();
                    foreach (BacnetReadAccessSpecification p in properties)
                    {
                        if (p.propertyReferences.Count == 1 && p.propertyReferences[0].propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL)
                        {
                            if (!m_storage.ReadPropertyAll(p.objectIdentifier, out value))
                            {
                                sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
                                return;
                            }
                        }
                        else
                            m_storage.ReadPropertyMultiple(p.objectIdentifier, p.propertyReferences, out value);
                        values.Add(new BacnetReadAccessResult(p.objectIdentifier, value));
                    }

                    sender.ReadPropertyMultipleResponse(adr, invoke_id, sender.GetSegmentBuffer(max_segments), values);

                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

    }
}
