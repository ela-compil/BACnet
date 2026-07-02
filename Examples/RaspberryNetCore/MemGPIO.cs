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
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

//
// This class could replace FileGPIO, much more faster but only for Raspberry (not Edison, Beagle, ...)
// Same usage as FileGPIO : bool v=MemGPIO.Input(4); MemGPIO.OutputPin(7,true);
//
// One clean way for usage :
// static class MyInputsOutputs
// {
//      static public bool Button { get { return MemGPIO.InputPin(14); } }
//      static public bool Led { set { MemGPIO.OutputPin(7, value); }}
// }
//
// another much complex/complete solution could be found at 
// https://github.com/raspberry-sharp/raspberry-sharp-io
//

namespace GPIO
{
    public enum Event_Detect_Type {Rising_Edge=0x4C, Falling_Edge=0x58, High_Level=0x64, Low_Level=0x70, Asynch_Rising_Edge=0x7c, Asynch_Falling_Edge=0x88};

    public static class MemGPIO
    {
        /**********************************************************************************************/
        [DllImport("libc")]
        private static extern IntPtr mmap(IntPtr address, UIntPtr size, int protect, int flags, int file, UIntPtr offset);
        [DllImport("libc")]
        private static extern int open(string filename, int mode);
        [DllImport("libc")]
        private static extern int close(int fileid);
        /**********************************************************************************************/

        static IntPtr gpioAddress = new IntPtr(-1);
        static object lockObj = new object();

        //contains list of pins OUT direction
        static List<uint> _Out= new List<uint>();
        //contains list of pins IN direction
        static List<uint> _In = new List<uint>();

        /**********************************************************************************************/

        // DataSheet BCM 2835 ARM peripherals, page 90
        // https://www.raspberrypi.org/wp-content/uploads/2012/02/BCM283x-ARM-Peripherals.pdf
        const int BCM283x_GPFSEL0 = 0x00;    // 6 configuration registers
        const int BCM283x_GPSET0 = 0x1c;     // 2 output on registers
        const int BCM283x_GPCLR0 = 0x28;     // 2 output off registers
        const int BCM283x_GPLEV0 = 0x34;     // 2 input level registers
        const int BCM283x_GPEDS0 = 0x40;    // 2 event detect status registers
        const int BCM283x_GPPUD = 0x94;      // Pull Command
        const int BCM283x_GPPUDCLK0 = 0x98;  // 2 Pull Clock

        /**********************************************************************************************/
        static MemGPIO()
        {
            if (Environment.OSVersion.Platform != System.PlatformID.Unix) return;

            try
            {
                // DataSheet BCM 2835 ARM peripherals, page 6
                uint BCM283x_BLOCK_SIZE = (4 * 1024);
                uint BCM283x_GPIO_BASE=0;
                
                // https://www.iot-programmer.com/index.php/books/22-raspberry-pi-and-the-iot-in-c/chapters-raspberry-pi-and-the-iot-in-c/59-raspberry-pi-and-the-iot-in-c-memory-mapped-gpio?showall=&start=1
                if (File.Exists("/proc/device-tree/soc/ranges"))
                {
                    byte[] buf = File.ReadAllBytes("/proc/device-tree/soc/ranges");                    
                    BCM283x_GPIO_BASE = (uint)((buf[4] << 24) |( buf[5] << 16) | (buf[6] << 8) | (buf[7] << 0) |  0x200000);                  
                }

                if (BCM283x_GPIO_BASE == 0) // old technic, already here : /proc/device-tree/soc/ranges seems not to be present on old kernel versions
                {
                    // http://www.raspberrypi-spy.co.uk/2012/09/checking-your-raspberry-pi-board-version/
                    string readValue = File.ReadAllText("/proc/cpuinfo");

                    if (readValue.Contains("BCM2708") || readValue.Contains("BCM2835"))  // BCM2708=BCM2835 for Pi up to B+, Pi2 it's BCM2709=BCM2836
                        BCM283x_GPIO_BASE = 0x20000000 + 0x200000;
                    else
                        BCM283x_GPIO_BASE = 0x3F000000 + 0x200000;
                }

                int mem_fd;
                mem_fd = open("/dev/mem", 10002);
                gpioAddress = mmap(IntPtr.Zero, new UIntPtr(BCM283x_BLOCK_SIZE), 2, 1, mem_fd, new UIntPtr(BCM283x_GPIO_BASE));
                if (gpioAddress.ToInt32() == -1)
                    System.Diagnostics.Trace.TraceError("Error opening ARM peripherals communication channel, GPIO fail (not sudo mode ?)");
                close(mem_fd);
            }
            catch { }
        }
        /**********************************************************************************************/
        public static void OutputPin(uint pin, bool value)
        {
            if (gpioAddress.ToInt32()==-1) return;

            lock (lockObj)
            {
                if (!_Out.Contains(pin))    // configuration already done in this mode ?
                {
                    SetupPin(pin, 1);
                    _Out.Add(pin); _In.Remove(pin);
                }

                // DataSheet BCM 2835 ARM peripherals, page 95
                // write corresponding bit to 1 in BCM283x_GPSET0 or BCM283x_GPSET1 to set the output to 1
                // write corresponding bit to 1 in BCM283x_GPCLR0 or BCM283x_GPCLR1 to set the output to 0
                IntPtr GPSET_CLR_RegisterAddress;
                if (value)
                    GPSET_CLR_RegisterAddress = gpioAddress + BCM283x_GPSET0 + 4 * ((int)pin / 32);
                else
                    GPSET_CLR_RegisterAddress = gpioAddress + BCM283x_GPCLR0 + 4 * ((int)pin / 32);

                Marshal.WriteInt32(GPSET_CLR_RegisterAddress, 1 << ((int)pin % 32));
            }
        }
        /**********************************************************************************************/
        public static bool InputPin(uint pin)
        {
            if (gpioAddress.ToInt32() == -1) return false;

            lock (lockObj)
            {
                if (!_In.Contains(pin))    // configuration already done in this mode ?
                {
                    SetupPin(pin, 0);
                    _In.Add(pin); _Out.Remove(pin);
                }

                // DataSheet BCM 2835 ARM peripherals, page 96     
                // read corresponding bit in BCM283x_GPLEV0 or BCM283x_GPLEV1 to get the input
                IntPtr GPLEV_RegisterAddress = gpioAddress + BCM283x_GPLEV0 + 4*((int)pin / 32);

                int value = Marshal.ReadInt32(GPLEV_RegisterAddress);
                return (value & (1 << ((int)pin % 32))) != 0;
            }
        }
        /**********************************************************************************************/
        public static void SetPullUpDown(uint pin, bool PullUp, bool PullDown)
        {
            if (gpioAddress.ToInt32() == -1) return;

            int Val = 0;

            lock (lockObj)
            {
                if (!_In.Contains(pin))    // configuration already done in input mode ?
                {
                    SetupPin(pin, 0);
                    _In.Add(pin); _Out.Remove(pin);
                }

                // DataSheet BCM 2835 ARM peripherals, pages 100-101

                if (PullDown) Val = 1;
                if (PullUp) Val = 2; // Value if both set

                Marshal.WriteInt32(gpioAddress + BCM283x_GPPUD, Val);
                Thread.Sleep(1);
                IntPtr GPSET_CLK_RegisterAddress = gpioAddress + BCM283x_GPPUDCLK0 + 4 * ((int)pin / 32);
                Marshal.WriteInt32(GPSET_CLK_RegisterAddress, 1 << ((int)pin % 32));
                Thread.Sleep(1);
                Marshal.WriteInt32(gpioAddress + BCM283x_GPPUD, 0);
                Marshal.WriteInt32(GPSET_CLK_RegisterAddress, 0);
            }
        }
        /**********************************************************************************************/
        public static void Enable_Event_Detection(uint pin, Event_Detect_Type EvType)
        {
            if (gpioAddress.ToInt32() == -1) return;

            lock (lockObj)
            {
                if (!_In.Contains(pin))    // configuration already done in input mode ?
                {
                    SetupPin(pin, 0);
                    _In.Add(pin); _Out.Remove(pin);
                }

                // DataSheet BCM 2835 ARM peripherals, page 97-100
                IntPtr RegisterAddress = gpioAddress + (int)EvType + 4 * ((int)pin / 32);
                int val = Marshal.ReadInt32(RegisterAddress);
                Marshal.WriteInt32(RegisterAddress, val | (1 << ((int)pin % 32)));
            }
        }
        /**********************************************************************************************/
        public static void Disable_Event_Detection(uint pin, Event_Detect_Type EvType)
        {
            if (gpioAddress.ToInt32() == -1) return;

            lock (lockObj)
            {
                // DataSheet BCM 2835 ARM peripherals, page 97-100
                IntPtr RegisterAddress = gpioAddress + (int)EvType + 4 * ((int)pin / 32);
                int val = Marshal.ReadInt32(RegisterAddress);
                Marshal.WriteInt32(RegisterAddress, val & ~(1 << ((int)pin % 32)));
            }
        }
        /**********************************************************************************************/
        public static bool IsEvent_Detected(uint pin)
        {
            if (gpioAddress.ToInt32() == -1) return false ;

            lock (lockObj)
            {
                // DataSheet BCM 2835 ARM peripherals, page 96
                IntPtr GPEDS_RegisterAddress = gpioAddress + BCM283x_GPEDS0 + 4 * ((int)pin / 32);

                int value = Marshal.ReadInt32(GPEDS_RegisterAddress);
                bool EventOK = (value & (1 << ((int)pin % 32))) != 0;

                if (EventOK)
                    Marshal.WriteInt32(GPEDS_RegisterAddress, (1 << ((int)pin % 32)));

                return EventOK;
            }
        }
        /**********************************************************************************************/
        private static void SetupPin(uint pin, int mode)
        {
            // DataSheet BCM 2835 ARM peripherals, page 92
            // BCM283x_GPFSEL0 : GPIO 0 to 9 configuration
            // BCM283x_GPFSEL1=BCM283x_GPFSEL0+4 : GPIO 10 to 19, 
            // ... up to BCM283x_GPFSEL5=BCM283x_GPFSEL0+20
            IntPtr GPFSEL_RegisterAddress = gpioAddress + BCM283x_GPFSEL0 + 4 * ((int)pin / 10);

            // BCM283x_GPFSEL0.0, BCM283x_GPFSEL0.1, BCM283x_GPFSEL0.2
            // ... get 001b for GPIO0 in input, 000b in output
            // BCM283x_GPFSEL0.3, BCM283x_GPFSEL0.4, BCM283x_GPFSEL0.5
            // ... get 001b for GPIO1 in input, 000b in output
            // up to BCM283x_GPFSEL5.11
            int shift = 3 * ((int)pin % 10);
            int mask = ~(7 << shift);
            int value = mode << shift;

            int v=Marshal.ReadInt32(GPFSEL_RegisterAddress);
            v = (v & mask) | value;
            Marshal.WriteInt32(GPFSEL_RegisterAddress, v);

        }
        /**********************************************************************************************/
    }
}
