using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

// some light modifications from the original
// https://www.raspberrypi.org/forums/viewtopic.php?p=88063#p88063

//
// This class works also on Beaglebone, Intel Edison, and a lot of others plateforms
//

namespace GPIO
{
    public static class FileGPIO
    {
        //GPIO connector on the Pi (P1) (as found next to the yellow RCA video socket on the Rpi circuit board)
        //P1-01 = top left,    P1-02 = top right
        //P1-25 = bottom left, P1-26 = bottom right
        //pi connector P1 pin     = GPIOnum = slice of pi v1.0 board label
        //                  P1-07 = GPIO4   = GP7
        //                  P1-11 = GPIO17  = GP0
        //                  P1-12 = GPIO18  = GP1
        //                  P1-13 = GPIO21  = GP2
        //                  P1-15 = GPIO22  = GP3
        //                  P1-16 = GPIO23  = GP4
        //                  P1-18 = GPIO24  = GP5
        //                  P1-22 = GPIO25  = GP6
        //So to turn on Pin7 on the GPIO connector, pass in 4 as the pin parameter

        public enum enumDirection { IN, OUT };

        private const string GPIO_PATH ="/sys/class/gpio/";

        //contains list of pins exported with an OUT direction
        static List<uint> _OutExported = new List<uint>();

        //contains list of pins exported with an IN direction
        static List<uint> _InExported = new List<uint>();

        static FileGPIO()
        {
            CleanUpAllPins();
        }

        //this gets called automatically when we try and output to, or input from, a pin
        private static void SetupPin(uint pin, enumDirection direction)
        {
            try
            {
                //unexport if it we're using it already
                if (_OutExported.Contains(pin) || _InExported.Contains(pin)) UnexportPin(pin);

                //export
                File.WriteAllText(GPIO_PATH + "export", pin.ToString());

                // set i/o direction
                File.WriteAllText(GPIO_PATH +"gpio"+pin.ToString() + "/direction", direction.ToString().ToLower());

                //record the fact that we've setup that pin
                if (direction == enumDirection.OUT)
                    _OutExported.Add(pin);
                else
                    _InExported.Add(pin);
            }
            catch{}
        }

        //no need to setup pin this is done for you
        public static void OutputPin(uint pin, bool value) 
        {
            if (Environment.OSVersion.Platform != System.PlatformID.Unix) return;

            //if we havent used the pin before,  or if we used it as an input before, set it up
            if (!_OutExported.Contains(pin) || _InExported.Contains(pin)) SetupPin(pin, enumDirection.OUT);

            string writeValue = "0";
            if (value) writeValue = "1";
            File.WriteAllText(GPIO_PATH +"gpio"+ pin.ToString() + "/value", writeValue);

        }

        //no need to setup pin this is done for you
        public static bool InputPin(uint pin)
        {
            if (Environment.OSVersion.Platform != System.PlatformID.Unix) return false;

            bool returnValue = false;

            //if we havent used the pin before, or if we used it as an output before, set it up
            if (!_InExported.Contains(pin) || _OutExported.Contains(pin) ) SetupPin(pin, enumDirection.IN);
         
            string filename = GPIO_PATH +"gpio"+ pin.ToString() + "/value";
            if (File.Exists(filename))
            {
                string readValue = File.ReadAllText(filename);
                if (readValue != null && readValue.Length > 0 && readValue[0] == '1') returnValue = true;
            }
            else
                throw new Exception(string.Format("Cannot read from {0}. File does not exist", pin));

            return returnValue;
        }

        //if for any reason you want to unexport a particular pin use this, otherwise just call CleanUpAllPins when you're done
        private static void UnexportPin(uint pin)
        {
            bool found = false;
            if (_OutExported.Contains(pin))
            {
                found = true;
                _OutExported.Remove(pin);
            }
            if (_InExported.Contains(pin))
            {
                found = true;
                _InExported.Remove(pin);
            }

            if (found)
            {
                File.WriteAllText(GPIO_PATH + "unexport", pin.ToString());
            }
        }

        public static void CleanUpAllPins()
        {
            if (Environment.OSVersion.Platform != System.PlatformID.Unix) return;

            for (int i=0;i<30;i++)
                try
                {
                    File.WriteAllText(GPIO_PATH + "unexport", i.ToString());
                }
                catch { }
        }
       
    }
}
