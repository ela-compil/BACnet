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
using BaCSharp;
using System.Xml.Serialization;
using System.IO.BACnet.Serialize;
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

// Remove the reference to the NewtonSoft.Json dll if data persistence is not required

namespace AnotherStorageImplementation
{   

    class Program
    {
        static uint deviceId = 1234;

        static DeviceObject device;
        static AnalogInput<double> ana0;
        static TrendLog trend0;

        static void Main(string[] args)
        {
            InitDeviceObjects();

            SaveAndRestoreBackSample();

            BacnetActivity.StartActivity(device);

            Console.WriteLine("Running ...");

            // A simple activity
            int i = 0;
            for (; ; )
            {
                Thread.Sleep(500);

                lock (device)   // required for all change
                {
                    // A direct write into the attribut value could be made
                    // if status change for protected to public
                    // but this one force the COV management if needed
                    ana0.internal_PROP_PRESENT_VALUE = 100 * Math.Sin(0.1*(i++));
                }
            }
        }

        /*****************************************************************************************************/
        static void handler_OnWriteNotify(BaCSharpObject sender, BacnetPropertyIds propId)
        {
            // External Write ... could be internaly made !!! for instance by Schedule Object
            Console.WriteLine("Write success into object : " + sender.ToString());
        }
        /*****************************************************************************************************/
        static void InitDeviceObjects()
        {

            // create the device object with StructuredView acceptation
            device = new DeviceObject(deviceId, "Device test","A test Device", true);

            // ANALOG_INPUT:0 uint
            // initial value 0           
            ana0 = new AnalogInput<double>
                (
                0,
                "Ana0 Sin double",
                "Ana0 Sin double",
                0,
                BacnetUnitsId.UNITS_AMPERES
                );
            ana0.m_PROP_HIGH_LIMIT = 50;
            ana0.m_PROP_LOW_LIMIT = -50;
            ana0.m_PROP_DEADBAND = 5;
            ana0.Enable_Reporting(true, 0);
            
            device.AddBacnetObject(ana0);   // don't forget to do this
            
            // Create A StructuredView
            StructuredView s = new StructuredView(0, "Content","A View");
            // register it
            device.AddBacnetObject(s);  // don't forget to do this
  
            BaCSharpObject b;
            // ANALOG_VALUE:0 double with Priority Array
            //
            b = new AnalogValue<double>
                (
                0,
                "Ana0 Double",
                "Ana0 Double",
                5465.23,
                BacnetUnitsId.UNITS_BARS,
                true
                );
            s.AddBacnetObject(b); // Put it in the view

            b.OnWriteNotify += new BaCSharpObject.WriteNotificationCallbackHandler(handler_OnWriteNotify);

            // ANALOG_OUTPUT:1 int with Priority Array on Present Value
            b = new AnalogOutput<int>
                (
                1,
                "Ana1 int",
                "Ana1 int",
                (int)56,
                BacnetUnitsId.UNITS_DEGREES_CELSIUS
                );
            s.AddBacnetObject(b); // Put it in the view

            b.OnWriteNotify += new BaCSharpObject.WriteNotificationCallbackHandler(handler_OnWriteNotify);

            // MULTI_STATE_OUTPUT:4 with 6 states
            MultiStateOutput m = new MultiStateOutput
                (
                4,
                "MultiStates",
                "MultiStates",
                1,
                6
                );
            for (int i = 1; i < 7; i++) m.m_PROP_STATE_TEXT[i-1] = new BacnetValue("Text Level " + i.ToString());

            s.AddBacnetObject(m); // in the view

            StructuredView s2 = new StructuredView(1, "Complex objects", "Complex objects");
            s.AddBacnetObject(s2);

            // TREND_LOG:0 with int values
            trend0 = new TrendLog(0, "Trend signed int", "Trend signed int", 200, BacnetTrendLogValueType.TL_TYPE_SIGN);
            s2.AddBacnetObject(trend0); // in the second level view
            // fill Log with more values than the size
            for (int i = 0; i < 300; i++)
            {
                DateTime current = DateTime.Now.AddSeconds(-300+i);
                if ((i>200)&&(i<210))   // simulate some errors in the trend
                    trend0.AddValue(new BacnetError(), current, 0, BacnetTrendLogValueType.TL_TYPE_ERROR);
                else
                    trend0.AddValue((int)(i* Math.Sin((float)i / 0.01)), current, 0);
            }

            trend0.AddValue(new BacnetError(), DateTime.Now, 0, BacnetTrendLogValueType.TL_TYPE_ERROR);

            // BACFILE:0
            // File access right me be allowed to the current user
            // for read and for write if any
            b = new BacnetFile
                (
                0,
                "A file",
                "File description",
                "c:\\RemoteObject.xml",
                false
                );
            s2.AddBacnetObject(b); // in the second level view
            
            NotificationClass nc = new NotificationClass
                (
                0, 
                "An alarm sender",
                "Alarm description",
                device.PROP_OBJECT_IDENTIFIER
                );

            device.AddBacnetObject(nc); 

            // Put two elements into the NC recipient List

            // Valid Day
            BacnetBitString week = new BacnetBitString();
            for (int i = 0; i < 7; i++) week.SetBit((byte)i, true); // Monday to Sunday
            // transition
            BacnetBitString transition = new BacnetBitString();
            transition.SetBit(0, true); // To OffNormal
            transition.SetBit(1, true); // To Fault
            transition.SetBit(2, true); // To Normal

            DeviceReportingRecipient r = new DeviceReportingRecipient
            (
                week, // week days
                DateTime.MinValue.AddDays(10),  // fromTime
                DateTime.MaxValue,              // toTime
                new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 4000),
                (uint)4,    // processid
                true,       // Ack required
                transition  // transition
            );
            nc.AddReportingRecipient(r);
            
            r= new DeviceReportingRecipient
            (
                week,
                DateTime.MinValue.AddDays(10),
                DateTime.MaxValue,
                new BacnetAddress(BacnetAddressTypes.IP,0,new Byte[6]{255,255,255,255,0xBA,0xC0}),
                (uint)4,
                true,
                transition
            );

            nc.AddReportingRecipient(r);
            
            // Create a Schedule
            Schedule sch = new Schedule(0, "Schedule", "Schedule");
            // MUST be added to the device list before modification
            device.AddBacnetObject(sch);

            // a link to the internal analog output
            sch.AddPropertyReference(new BacnetDeviceObjectPropertyReference
                     (new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, 1),
                     BacnetPropertyIds.PROP_PRESENT_VALUE));
            // a link to analog output through the network : could be on another device than itself
            sch.AddPropertyReference(new BacnetDeviceObjectPropertyReference
            (
                  new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, 1),
                  BacnetPropertyIds.PROP_PRESENT_VALUE,
                  new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1234)));
            
            sch.PROP_SCHEDULE_DEFAULT = (int)452;

            // Schedule a change today in 60 seconds
            sch.AddSchedule
                ( 
                DateTime.Now.DayOfWeek == 0 ? 6 : (int)DateTime.Now.DayOfWeek - 1, // Monday=0, Sunday=6                 
                DateTime.Now.AddSeconds(10), (int)900                
                );    
            sch.PROP_OUT_OF_SERVICE = false;    // needed after all initialization to start the service

            // One empty Calendar, could be fullfill with yabe

            Calendar cal = new Calendar(0, "Test Calendar", "A Yabe calendar");
            device.AddBacnetObject(cal);

        }

        // This shows how presistence could be achieved
        // only partial tests since today, complex objects not really operational
        // but all basic one seems OK
        // 
        // Leave a reference to the NewtonSoft.Json dll
        // also have a look to http://www.newtonsoft.com/json

        static void SaveAndRestoreBackSample()
        {
            /*
             
            // Serialization
            Newtonsoft.Json.JsonSerializerSettings v = new Newtonsoft.Json.JsonSerializerSettings();
            v.TypeNameHandling=Newtonsoft.Json.TypeNameHandling.All;
            String s=Newtonsoft.Json.JsonConvert.SerializeObject(device, v);
            // s could be put on a file

            // Deserialization
            DeviceObject o = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceObject>(s, v);
            o.Post_NewtonSoft_Json_Deserialization();

            // Change the oldest by the newest
            device = o;
            // retreive the newest copies of differents objects
            ana0=(AnalogInput<double>)device.FindBacnetObject(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT,0));
            trend0 = (TrendLog)device.FindBacnetObject(new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 0));

           */
        }
    }
}
