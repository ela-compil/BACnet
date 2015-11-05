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

//
// This class could replace FileGPIO, much more faster but only for Raspberry (not Edison, Beagle, ...)
// Same usage as FileGPIO : bool v=MemGPIO.Input(4); MemGPIO.OutputPin(7,true);
//
// One clean way for usage :
// static class MyInputsOutputs
// {
//      static public bool Button { get { return MemGPIO.InputPin(14); } }
//      static public void Led { set { MemGPIO.OutputPin(7, value); }}
// }
//
// another much complex/complete solution could be found at 
// https://github.com/raspberry-sharp/raspberry-sharp-io
//

namespace GPIO
{
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

        // DataSheet BCM 2835 ARM peripherals, page 6
        // https://www.raspberrypi.org/wp-content/uploads/2012/02/BCM2835-ARM-Peripherals.pdf
        const uint BCM2835_PERI_BASE = 0x20000000;
        const uint BCM2835_BLOCK_SIZE = (4 * 1024);

        // DataSheet BCM 2835 ARM peripherals, page 90
        const uint BCM2835_GPIO_BASE = (BCM2835_PERI_BASE + 0x200000);
        const int BCM2835_GPFSEL0 = 0x0000;    // 6 configuration registers
        const int BCM2835_GPSET0 = 0x001c;     // 2 output on registers
        const int BCM2835_GPCLR0 = 0x0028;     // 2 output off registers
        const int BCM2835_GPLEV0 = 0x0034;     // 2 input level registers

        /**********************************************************************************************/
        static MemGPIO()
        {
            if (Environment.OSVersion.Platform != System.PlatformID.Unix) return;

            int mem_fd;
            mem_fd = open("/dev/mem", 10002);
            gpioAddress = mmap(IntPtr.Zero, new UIntPtr(BCM2835_BLOCK_SIZE), 2, 1, mem_fd, new UIntPtr(BCM2835_GPIO_BASE));
            if (gpioAddress.ToInt32() == -1)
                System.Diagnostics.Trace.TraceError("Error opening ARM peripherals communication channel, GPIO fail (not sudo mode ?)");
            close(mem_fd);
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
                // write corresponding bit to 1 in BCM2835_GPSET0 or BCM2835_GPSET1 to set the output to 1
                // write corresponding bit to 1 in BCM2835_GPCLR0 or BCM2835_GPCLR1 to set the output to 0
                IntPtr GPSET_CLR_RegisterAddress;
                if (value)
                    GPSET_CLR_RegisterAddress = gpioAddress + BCM2835_GPSET0 + 4 * ((int)pin / 32);
                else
                    GPSET_CLR_RegisterAddress = gpioAddress + BCM2835_GPCLR0 + 4 * ((int)pin / 32);

                Marshal.WriteInt32(GPSET_CLR_RegisterAddress, 1 << ((int)pin % 32));
            }
        }
        /**********************************************************************************************/
        public static bool InputPin(uint pin)
        {
            if (gpioAddress.ToInt32() == -1) return false;

            lock (lockObj)
            {
                if (_In.Contains(pin))    // configuration already done in this mode ?
                {
                    SetupPin(pin, 0);
                    _In.Add(pin); _Out.Remove(pin);
                }

                // DataSheet BCM 2835 ARM peripherals, page 96     
                // read corresponding bit in BCM2835_GPLEV0 or BCM2835_GPLEV1 to get the input
                IntPtr GPLEV_RegisterAddress = gpioAddress + BCM2835_GPLEV0 + 4*((int)pin / 32);

                int value = Marshal.ReadInt32(GPLEV_RegisterAddress);
                return (value & (1 << ((int)pin % 32))) != 0;
            }
        }
        /**********************************************************************************************/
        private static void SetupPin(uint pin, int mode)
        {
            // DataSheet BCM 2835 ARM peripherals, page 92
            // BCM2835_GPFSEL0 : GPIO 0 to 9 configuration
            // BCM2835_GPFSEL1=BCM2835_GPFSEL0+4 : GPIO 10 to 19, 
            // ... up to BCM2835_GPFSEL5=BCM2835_GPFSEL0+20
            IntPtr GPFSEL_RegisterAddress = gpioAddress + BCM2835_GPFSEL0 + 4 * ((int)pin / 10);

            // BCM2835_GPFSEL0.0, BCM2835_GPFSEL0.1, BCM2835_GPFSEL0.2
            // ... get 001b for GPIO0 in input, 000b in output
            // BCM2835_GPFSEL0.3, BCM2835_GPFSEL0.4, BCM2835_GPFSEL0.5
            // ... get 001b for GPIO1 in input, 000b in output
            // up to BCM2835_GPFSEL5.11
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
