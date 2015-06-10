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
        static uint deviceId = 1234;

        static DeviceObject device;
        static AnalogInput<uint> ana0;
        static TrendLog trend0;

        static void Main(string[] args)
        {
            InitDeviceObjects();

            BacnetActivity.StartActivity(device);

            Console.WriteLine("Running ...");

            // A simple activity
            for (; ; )
            {
                Thread.Sleep(1000);

                lock (device)   // required for all change
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
        static void InitDeviceObjects()
        {

            // create the device object with StructuredView acceptation
            // for Yabe this means that all others objects are put into a view

            device = new DeviceObject(deviceId, "Device test", true);

            // Create the View
            StructuredView s = new StructuredView(0, "Content", device);
            // register it
            device.AddBacnetObject(s);

            // ANALOG_INPUT:0 uint
            // initial value 0           
            ana0 = new AnalogInput<uint>
                (
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 0),
                0,
                "Ana0 Int",
                BacnetUnitsId.UNITS_AMPERES
                );
            s.AddBacnetObject(ana0);

            BacnetObject b;
            // ANALOG_VALUE:0 double without Priority Array
            // It seems that for AnalogOutput Priority Array is required
            // and not for AnalogValue where is it optional
            b = new AnalogValue<double>
                (
                0,
                5465.23,
                "Ana0 Double",
                BacnetUnitsId.UNITS_BARS,
                true
                );
            s.AddBacnetObject(b);

            b.OnWriteNotify += new BacnetObject.WriteNotificationCallbackHandler(handler_OnWriteNotify);

            // ANALOG_OUTPUT:1 float with Priority Array on Present Value
            b = new AnalogOutput<float>
                (
                1,
                (float)56.8,
                "Ana1 Float",
                BacnetUnitsId.UNITS_DEGREES_CELSIUS
                );
            s.AddBacnetObject(b);

            b.OnWriteNotify += new BacnetObject.WriteNotificationCallbackHandler(handler_OnWriteNotify);

            // MULTI_STATE_VALUE:4 float with Priority Array on Present Value
            // could be MULTI_STATE_OUTPUT
            MultiStateOutput m = new MultiStateOutput
                (
                4,
                1,
                6,
                "MultiState"
                );
            for (int i = 1; i < 7; i++) m.m_PROP_STATE_TEXT[0] = new BacnetValue("Text Level " + i.ToString());

            s.AddBacnetObject(m);

            StructuredView s2 = new StructuredView(1, "Complex objects", device);
            s.AddBacnetObject(s2);

            // TREND_LOG:0 with int values
            trend0 = new TrendLog (0, "Trend int",200);
            s2.AddBacnetObject(trend0);
            // fill 1/2 Log
            for (int i = 0; i < 100; i++)
            {
                DateTime current = DateTime.Now.AddSeconds(-i);
                trend0.AddValue(BacnetTrendLogValueType.TL_TYPE_SIGN, (int)(50 * Math.Sin((float)i / 0.01)), current, 0);
            }

            // BACFILE:0
            // File access right me be allowed to the current user
            // for read and for write if any
            b = new BacnetFile
                (
                0,
                "A file",
                "c:\\RemoteObject.xml",
                false
                );
            s2.AddBacnetObject(b);

        }
    }
}
