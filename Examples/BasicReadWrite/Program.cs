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
using System.Diagnostics;

namespace BasicReadWrite
{
    //
    // A very simple read/write client code based on Yabe code
    //
    class Program
    {
        static BacnetClient bacnet_client;

        // All the present Bacnet Device List
        static List<BacNode> DevicesList = new List<BacNode>();

        /*****************************************************************************************************/
        static void Main(string[] args)
        {

            Trace.Listeners.Add(new ConsoleTraceListener());

            try
            {
                StartActivity();
                Console.WriteLine("Started");

                Thread.Sleep(1000); // Wait a fiew time for WhoIs responses (managed in handler_OnIam)

                ReadWriteExample();
            }
            catch { }

            Console.ReadKey();            
        }
        /*****************************************************************************************************/
        static void StartActivity()
        {
            // Bacnet on UDP/IP/Ethernet
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false));
            // or Bacnet Mstp on COM4 à 38400 bps, own master id 8
            // m_bacnet_client = new BacnetClient(new BacnetMstpProtocolTransport("COM4", 38400, 8);
            // Or Bacnet Ethernet
            // bacnet_client = new BacnetClient(new BacnetEthernetProtocolTransport("Connexion au réseau local"));          
            // Or Bacnet on IPV6
            // bacnet_client = new BacnetClient(new BacnetIpV6UdpProtocolTransport(0xBAC0));

            bacnet_client.Start();    // go

            // Send WhoIs in order to get back all the Iam responses :  
            bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);            
            
            bacnet_client.WhoIs();

            /* Optional Remote Registration as A Foreign Device on a BBMD at @192.168.1.1 on the default 0xBAC0 port
                           
            bacnet_client.RegisterAsForeignDevice("192.168.1.1", 60);
            Thread.Sleep(20);
            bacnet_client.RemoteWhoIs("192.168.1.1");
            */
        }

        /*****************************************************************************************************/
        static void ReadWriteExample()
        {

            BacnetValue Value;
            bool ret;
            // Read Present_Value property on the object ANALOG_INPUT:0 provided by the device 12345
            // Scalar value only
            ret = ReadScalarValue(12345, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 0), BacnetPropertyIds.PROP_PRESENT_VALUE, out Value);

            if (ret == true)
            {
                Console.WriteLine("Read value : " + Value.Value.ToString());

                // Write Present_Value property on the object ANALOG_OUTPUT:0 provided by the device 4000
                BacnetValue newValue = new BacnetValue(Convert.ToSingle(Value.Value));   // expect it's a float
                ret = WriteScalarValue(4000, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, 0), BacnetPropertyIds.PROP_PRESENT_VALUE, newValue);

                Console.WriteLine("Write feedback : " + ret.ToString());
            }
            else
                Console.WriteLine("Error somewhere !");
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
        static bool ReadScalarValue(int device_id, BacnetObjectId BacnetObjet, BacnetPropertyIds Propriete, out BacnetValue Value)
        {
            BacnetAddress adr;
            IList<BacnetValue> NoScalarValue;

            Value = new BacnetValue(null);

            // Looking for the device
            adr=DeviceAddr((uint)device_id);
            if (adr == null) return false;  // not found

            // Property Read
            if (bacnet_client.ReadPropertyRequest(adr, BacnetObjet, Propriete, out NoScalarValue)==false)
                return false;

            Value = NoScalarValue[0];
            return true;
        }

        /*****************************************************************************************************/
        static bool WriteScalarValue(int device_id, BacnetObjectId BacnetObjet, BacnetPropertyIds Propriete, BacnetValue Value)
        {
            BacnetAddress adr;

            // Looking for the device
            adr = DeviceAddr((uint)device_id);
            if (adr == null) return false;  // not found

            // Property Write
            BacnetValue[] NoScalarValue = { Value };
            if (bacnet_client.WritePropertyRequest(adr, BacnetObjet, Propriete, NoScalarValue) == false)
                return false;

            return true;
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
