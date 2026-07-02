/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Morten Kvistgaard <mk@pch-engineering.dk>
*                    Frederic Chaxel <fchaxel@free.fr 
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

namespace BasicAdviseCOV
{
    //
    // A very simple client code based on Yabe code
    //
    class Program
    {
        static BacnetClient bacnet_client;

        // All the present Bacnet Device List
        static List<BacNode> DevicesList = new List<BacNode>();

        /*****************************************************************************************************/
        static void Main(string[] args)
        {
            try
            {
                StartActivity();
                Console.WriteLine("Started");

                Thread.Sleep(1000); // Wait a fiew time for WhoIs responses (managed in handler_OnIam)

                bacnet_client.OnCOVNotification += new BacnetClient.COVNotificationHandler(handler_OnCOVNotification);
                // Advise for 60 secondes
                BasicAdviseCOV(60);

                Console.ReadKey();

                // unAdvise
                BasicAdviseCOV(0);

            }
            catch { }

        
        }

        /*****************************************************************************************************/
        static void handler_OnCOVNotification(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool need_confirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments)
        {

            Console.Write(monitoredObjectIdentifier.ToString() + " COV : ");
            foreach (BacnetPropertyValue value in values)
            {
                switch ((BacnetPropertyIds)value.property.propertyIdentifier)
                {
                    case BacnetPropertyIds.PROP_PRESENT_VALUE:
                        Console.WriteLine(value.value[0].ToString());
                        break;
                    case BacnetPropertyIds.PROP_STATUS_FLAGS:
                        string status_text = "";
                        if (value.value != null && value.value.Count > 0)
                        {
                            BacnetStatusFlags status = (BacnetStatusFlags)((BacnetBitString)value.value[0].Value).ConvertToInt();
                            if ((status & BacnetStatusFlags.STATUS_FLAG_FAULT) == BacnetStatusFlags.STATUS_FLAG_FAULT)
                                status_text += "FAULT,";
                            else if ((status & BacnetStatusFlags.STATUS_FLAG_IN_ALARM) == BacnetStatusFlags.STATUS_FLAG_IN_ALARM)
                                status_text += "ALARM,";
                            else if ((status & BacnetStatusFlags.STATUS_FLAG_OUT_OF_SERVICE) == BacnetStatusFlags.STATUS_FLAG_OUT_OF_SERVICE)
                                status_text += "OOS,";
                            else if ((status & BacnetStatusFlags.STATUS_FLAG_OVERRIDDEN) == BacnetStatusFlags.STATUS_FLAG_OVERRIDDEN)
                                status_text += "OR,";
                        }
                        if (status_text!="")
                            Console.WriteLine(status_text);
                        break;
                    default:
                        //got something else? ignore it
                        break;
                }
            }

            if (need_confirm)
            {
                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, invoke_id);
            }
        }
        /*****************************************************************************************************/
        static void StartActivity()
        {
            // Bacnet on UDP/IP/Ethernet
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false));
            // or Bacnet Mstp on COM4 à 38400 bps, own master id 8 : BacnetTransportSerial.cs must be added to this project
            // m_bacnet_client = new BacnetClient(new BacnetMstpProtocolTransport("COM4", 38400, 8);

            bacnet_client.Start();    // go

            // Send WhoIs in order to get back all the Iam responses :  
            bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);
            bacnet_client.WhoIs();
        }

        /*****************************************************************************************************/
        static bool BasicAdviseCOV(uint duration)
        {
            BacnetAddress adr;
            // Looking for the device 1026
            adr = DeviceAddr((uint)1026);
            if (adr == null) return false;  // not found

            bool cancel = false;
            if (duration == 0) cancel = true;

            // advise to OBJECT_ANALOG_INPUT:1 provided by the device 1026
            bacnet_client.SubscribeCOVRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 1), 0, cancel, false, duration);

            return true;
        }

        /*****************************************************************************************************/
        static void handler_OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            lock (DevicesList)
            {
                // Device already registred ?
                foreach (BacNode bn in DevicesList)
                    if (bn.getAdd(device_id) != null) return;   // Yes

                // Not already in the list
                DevicesList.Add(new BacNode(adr, device_id));   // add it
            }
        }


        /*****************************************************************************************************/
        static BacnetAddress DeviceAddr(uint device_id)
        {
            BacnetAddress ret;

            lock (DevicesList)
            {
                foreach (BacNode bn in DevicesList)
                {
                    ret = bn.getAdd(device_id);
                    if (ret != null) return ret;
                }
                // not in the list
                return null;
            }
        }
    }

    class BacNode
    {
        BacnetAddress adr;
        uint device_id;

        public BacNode(BacnetAddress adr, uint device_id)
        {
            this.adr = adr;
            this.device_id = device_id;
        }

        public BacnetAddress getAdd(uint device_id)
        {
            if (this.device_id == device_id)
                return adr;
            else
                return null;
        }
    }
}
