using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO.BACnet
{
    public struct BacnetTimeValue
    {
        public BacnetGenericTime Time;
        public BacnetValue Value;

        public BacnetTimeValue(BacnetGenericTime time, BacnetValue value)
        {
            Time = time;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Time} = {Value}";
        }
    }
}
