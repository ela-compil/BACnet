/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
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
using System.Net;
using System.IO;
using System.Diagnostics;

// See BacnetActivity arround line 83 to swith form IPV4 to IPV6

namespace DemoBBMD
{
    class Program
    {
        static void SetBBMDList()
        {
            StringBuilder BBMDList = new StringBuilder();

            // Read the BBMD Peer List from the Config.txt file
            try
            {
                StreamReader sr = new StreamReader("Config.txt");
                while (!sr.EndOfStream)
                {
                    String l = sr.ReadLine();

                    if ((l.Length != 0) && (l[0] != '/'))
                    {
                        String[] Param = l.Split(';');

                        BacnetActivity.AddPeerBBMD(new IPEndPoint(IPAddress.Parse(Param[0]), Convert.ToInt32(Param[2])), IPAddress.Parse(Param[1]));
                        
                        BBMDList.Append(Param[0] + ":" + Param[2] + ";");

                        Console.WriteLine("\tWorking with peer BBMD : {0}:{1}", Param[0], Param[2]);
                    }
                }
                sr.Close();
            }
            catch { }

            // Update the first CHARACTERSTRING OBJECT Present Value
            BacnetObjectId b = new BacnetObjectId(BacnetObjectTypes.OBJECT_CHARACTERSTRING_VALUE, 0);
            BacnetActivity.SetBacObjectPresentValue(b, new BacnetValue(BBMDList.ToString()));
        }

        static void Main(string[] args)
        {

            Console.WriteLine("BBMD Demo Application started on 0xBAC0 Udp port\n\nAll foreign devices accepted (no filtering)\n");
            
            // start the FD acceptation at least if BBMD list is empty or corrupted
            // and set up the device, see BacnetActivity static constructor
            BacnetActivity.AddPeerBBMD(null, null);

            if (BacnetActivity.OpenError == true)
            {
                Console.WriteLine("\t Error, certainly due to the Udp Port already in use");
                return;
            }

            // Set BBMD peers
            SetBBMDList();

            // Update each 10s the second CHARACTERSTRING OBJECT Present Value
            BacnetObjectId b = new BacnetObjectId(BacnetObjectTypes.OBJECT_CHARACTERSTRING_VALUE, 1);
            for ( ; ; )
            {
                Thread.Sleep(10000);
                BacnetActivity.SetBacObjectPresentValue(b, new BacnetValue(BacnetActivity.GetFDList()));
            }
        }
    }
}
