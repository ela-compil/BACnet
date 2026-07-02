/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2020 Frederic Chaxel <fchaxel@free.fr>
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
using BaCSharp;
using System.IO.BACnet;
using System.Threading;

namespace MultipleDevices
{
    class Program
    {
        static void Main(string[] args)
        {

            // The First Device with 1 Device Object and 1 AnalogInput
            DeviceObject Dev1 = new DeviceObject(1234, "Device 1", "The First Device", false);
            AnalogInput<float> Temp = new AnalogInput<float>
            (
                0,
                "Temperature",
                "Temperature",
                0,
                BacnetUnitsId.UNITS_DEGREES_CELSIUS
            );
            Dev1.AddBacnetObject(Temp);

            Temp.internal_PROP_PRESENT_VALUE = 22.6f;

            // The Second Device with 1 Device Object with 2 AnalogInput (first one is shared)
            DeviceObject Dev2 = new DeviceObject(5678, "Device 2", "The Second Device", false);
            AnalogInput<float>  Windspeed = new AnalogInput<float>
            (
                1,
                "Windspeed",
                "Wind speed",
                0,
                BacnetUnitsId.UNITS_KILOMETERS_PER_HOUR
            );
            Dev2.AddBacnetObject(Temp);
            Dev2.AddBacnetObject(Windspeed);

            Windspeed.internal_PROP_PRESENT_VALUE = 0.2f;

            // Force the JIT compiler to make some job before network access
            Dev2.Cli2Native();

            // Start the activity for both device
            BacnetActivity BacAc1 = new BacnetActivity();
            BacAc1.StartActivity(Dev1);

            BacnetActivity BacAc2 = new BacnetActivity();
            BacAc2.StartActivity(Dev2);

            // Some animation !
            for (; ; )
            {
                Thread.Sleep(1000);
                //  a Ramp on an object
                if (Temp.internal_PROP_PRESENT_VALUE < 30)
                    Temp.internal_PROP_PRESENT_VALUE = Temp.internal_PROP_PRESENT_VALUE + 1;
                else
                    Temp.internal_PROP_PRESENT_VALUE = 10f;

            }

        }
    }
}
