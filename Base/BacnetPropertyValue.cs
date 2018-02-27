using System.Collections.Generic;

namespace System.IO.BACnet
{
    // TODO you have been flagged for refactoring due to un-C#-iness
    public struct BacnetPropertyValue
    {
        public BacnetPropertyReference property;
        public IList<BacnetValue> value;
        public byte priority;

        public override string ToString()
        {
            return property.ToString();
        }
    }
}