/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr>
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
using System.Linq;
using System.Text;
using System.IO.BACnet;
using System.Threading;
using System.Diagnostics;

//
// This code shows a way to map bacnet objects in C# objects
// and how C# methods&properties in these classes could be used 
// as bacnet properties
//
// To understand, start with DeviceObject code then after have a look to 
// AnalogObject and AnalogInput for instance and after with AnalogOutput
// and close with BacnetObject last.
// The link between C# properties and Bacnet properties is made with the 
// properties names. The Bacnet type mapping of C# properties is made with the 
// mark applied to the properties [BaCSharpType ....]. If not set an automatic
// process is done (not all time OK).
// When required (sometimes) elementary C# properties could be 'override' with 
// two methods set2_xxx and get2_xxx which are used in priority if the two 
// solutions are present (property and set2 ...)
//

namespace AnotherStorageImplementation
{   

    class Program
    {
        static BacnetClient bacnet_client;

        static DeviceObject device;
        static uint deviceId = 1234;

        static void Main(string[] args)
        {

            // create the device object
            device = new DeviceObject(deviceId, "Device test");
            
            // ANALOG_INPUT:0 uint
            // initial value 0           
            AnalogInput<uint> ana0 = new AnalogInput<uint>
                (
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT,0), 
                0, 
                "Ana0 Int", 
                BacnetUnitsId.UNITS_AMPERES
                );
            device.AddBacnetObject(ana0);

            BacnetObject b;

            // ANALOG_VALUE:0 double without Priority Array
            // It seems that for AnalogOutput Priority Array is required
            // and not for AnalogValue where is it optional
            b = new AnalogOutput<double>
                (
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 0),
                5465.23,
                "Ana0 Double",
                BacnetUnitsId.UNITS_BARS,
                false
                );
            device.AddBacnetObject(b);

            b.OnWriteNotify += new BacnetObject.WriteNotificationCallbackHandler(handler_OnWriteNotify);           

            // ANALOG_OUTPUT:1 float with Priority Array on Present Value
            b = new AnalogOutput<float>
                (
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, 1),
                (float)56.8,
                "Ana1 Float",
                BacnetUnitsId.UNITS_DEGREES_CELSIUS,
                true
                );
            device.AddBacnetObject(b);

            b.OnWriteNotify += new BacnetObject.WriteNotificationCallbackHandler(handler_OnWriteNotify);

            // MULTI_STATE_VALUE:4 float with Priority Array on Present Value
            // could be MULTI_STATE_OUTPUT
            MultiStateOutput m = new MultiStateOutput
                (
                new BacnetObjectId(BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE, 4),
                (uint)1,
                (uint)6,
                "MultiState",
                true
                );
            m.m_PROP_STATE_TEXT[0] = new BacnetValue("Text Level 1");
            device.AddBacnetObject(m);

            StartActivity();

            Console.WriteLine("Running ...");

            // A simple activity
            for (; ; )
            {
                Thread.Sleep(1000);

                lock (device)
                {
                    // A direct write into the attribut value could be made
                    // if status change for protected to public
                    // but this one force the COV management if needed
                    ana0.internal_PROP_PRESENT_VALUE++;
                }
            }
        }
        /*****************************************************************************************************/
        static void handler_OnWriteNotify(BacnetObject sender, BacnetPropertyIds propId)
        {
            Console.WriteLine("Write success into object : " + sender.m_PROP_OBJECT_IDENTIFIER.ToString());
        }

        /*****************************************************************************************************/
        static void StartActivity()
        {
            // Bacnet on UDP/IP/Ethernet
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false));

            bacnet_client.OnWhoIs += new BacnetClient.WhoIsHandler(handler_OnWhoIs);
            bacnet_client.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(handler_OnReadPropertyRequest);
            bacnet_client.OnReadPropertyMultipleRequest += new BacnetClient.ReadPropertyMultipleRequestHandler(handler_OnReadPropertyMultipleRequest);
            bacnet_client.OnWritePropertyRequest += new BacnetClient.WritePropertyRequestHandler(handler_OnWritePropertyRequest);
            bacnet_client.OnSubscribeCOV += new BacnetClient.SubscribeCOVRequestHandler(handler_OnSubscribeCOV);
            bacnet_client.OnSubscribeCOVProperty += new BacnetClient.SubscribeCOVPropertyRequestHandler(handler_OnSubscribeCOVProperty);

            BacnetObject.OnCOVNotify += new BacnetObject.WriteNotificationCallbackHandler(handler_OnCOVManagementNotify);

            bacnet_client.Start();    // go
            // Send Iam
            bacnet_client.Iam(deviceId, new BacnetSegmentations());
        }
        /*****************************************************************************************************/
        static void handler_OnSubscribeCOV(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, BacnetMaxSegments max_segments)
        {
            lock (device)
            {
                BacnetObject bacobj = device.FindBacnetObject(monitoredObjectIdentifier);
                if (bacobj != null)
                {
                    //create 
                    Subscription sub = SubscriptionManager.HandleSubscriptionRequest(sender, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, (uint)BacnetPropertyIds.PROP_ALL, cancellationRequest, issueConfirmedNotifications, lifetime, 0);

                    //send confirm
                    sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invoke_id);

                    //also send first values
                    if (!cancellationRequest)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                        {
                            IList<BacnetPropertyValue> values;
                            if (bacobj.ReadPropertyAll(out values))
                                sender.Notify(adr, sub.subscriberProcessIdentifier, deviceId, sub.monitoredObjectIdentifier, (uint)sub.GetTimeRemaining(), sub.issueConfirmedNotifications, values);

                        }, null);
                    }
                }
                else
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
            }
        }
        /*****************************************************************************************************/
        static void handler_OnSubscribeCOVProperty(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference monitoredProperty, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement, BacnetMaxSegments max_segments)
        {
            lock (device)
            {
                BacnetObject bacobj = device.FindBacnetObject(monitoredObjectIdentifier);
                if (bacobj != null)
                {
                    //create 
                    Subscription sub = SubscriptionManager.HandleSubscriptionRequest(sender, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, monitoredProperty.propertyIdentifier, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement);

                    //send confirm
                    sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, invoke_id);
                    
                    //also send first values
                    if (!cancellationRequest)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                        {
                            IList<BacnetValue> _values;
                            bacobj.ReadPropertyValue(monitoredProperty, out _values);

                            List<BacnetPropertyValue> values = new List<BacnetPropertyValue>();
                            BacnetPropertyValue tmp = new BacnetPropertyValue();
                            tmp.property = sub.monitoredProperty;
                            tmp.value = _values;
                            values.Add(tmp);

                            sender.Notify(adr, sub.subscriberProcessIdentifier, deviceId, sub.monitoredObjectIdentifier, (uint)sub.GetTimeRemaining(), sub.issueConfirmedNotifications, values);
                        }, null);
                    }
                }
                else
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
            } 
        }
        /*****************************************************************************************************/
        static void handler_OnCOVManagementNotify(BacnetObject sender, BacnetPropertyIds propId)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                lock (device)
                {
                    //remove old leftovers
                    SubscriptionManager.RemoveOldSubscriptions();

                    //find subscription
                    List<Subscription> subs = SubscriptionManager.GetSubscriptionsForObject(sender.PROP_OBJECT_IDENTIFIER);

                    if (subs==null) return; // nobody

                    //Read the property
                    IList<BacnetValue> value;
                    BacnetPropertyReference br = new BacnetPropertyReference((uint)propId, (uint)System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    ErrorCodes error = sender.ReadPropertyValue(br, out value);

                    List<BacnetPropertyValue> values = new List<BacnetPropertyValue>();
                    BacnetPropertyValue tmp = new BacnetPropertyValue();
                    tmp.value = value;
                    tmp.property = br;
                    values.Add(tmp);

                    //send to all
                    foreach (Subscription sub in subs)
                    {
                        if (sub.monitoredProperty.propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL || sub.monitoredProperty.propertyIdentifier == (uint)propId)
                        {
                            tmp.property = sub.monitoredProperty;
                            if (!sub.reciever.Notify(sub.reciever_address, sub.subscriberProcessIdentifier, deviceId, sub.monitoredObjectIdentifier, (uint)sub.GetTimeRemaining(), sub.issueConfirmedNotifications, values))
                                SubscriptionManager.RemoveReceiver(sub.reciever_address);
                        }
                    }
                }
            }, null);
        }
        /*****************************************************************************************************/
        static void handler_OnWritePropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyValue value, BacnetMaxSegments max_segments)
        {
            lock (device)
            {
                BacnetObject bacobj = device.FindBacnetObject(object_id);
                if (bacobj != null)
                {
                    ErrorCodes error = bacobj.WritePropertyValue(value, true);
                    if (error == ErrorCodes.Good)
                        sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id);
                    else
                    {
                        BacnetErrorCodes bacEr=BacnetErrorCodes.ERROR_CODE_OTHER;
                        if (error == ErrorCodes.WriteAccessDenied)
                            bacEr = BacnetErrorCodes.ERROR_CODE_WRITE_ACCESS_DENIED;
                        if (error == ErrorCodes.OutOfRange)
                            bacEr = BacnetErrorCodes.ERROR_CODE_VALUE_OUT_OF_RANGE;

                        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, bacEr);
                    }
                }
                else
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);      
            }
        }

        /*****************************************************************************************************/
        static void handler_OnReadPropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property, BacnetMaxSegments max_segments)
        {
            lock (device)
            {
                BacnetObject bacobj=device.FindBacnetObject(object_id);

                if (bacobj != null)
                {
                    IList<BacnetValue> value;
                    ErrorCodes error= bacobj.ReadPropertyValue(property, out value);
                    if (error == ErrorCodes.Good)
                        sender.ReadPropertyResponse(adr, invoke_id, sender.GetSegmentBuffer(max_segments), object_id, property, value);
                    else
                        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY);
                }
                else
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
            }
        }

        /*****************************************************************************************************/
        static void handler_OnReadPropertyMultipleRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments max_segments)
        {
            lock (device)
            {
                try
                {
                    IList<BacnetPropertyValue> value;
                    List<BacnetReadAccessResult> values = new List<BacnetReadAccessResult>();
                    foreach (BacnetReadAccessSpecification p in properties)
                    {
                        if (p.propertyReferences.Count == 1 && p.propertyReferences[0].propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL)
                        {                            
                            BacnetObject bacobj=device.FindBacnetObject(p.objectIdentifier);
                            if (!bacobj.ReadPropertyAll(out value))
                            {
                                sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
                                return;
                            }
                        }
                        else
                        {
                            BacnetObject bacobj=device.FindBacnetObject(p.objectIdentifier);
                            bacobj.ReadPropertyMultiple(p.propertyReferences, out value);
                        }
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

        /*****************************************************************************************************/
        static void handler_OnWhoIs(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            if (low_limit != -1 && deviceId < low_limit) return;
            else if (high_limit != -1 && deviceId > high_limit) return;
            sender.Iam(deviceId, new BacnetSegmentations());
        }
    }
}
