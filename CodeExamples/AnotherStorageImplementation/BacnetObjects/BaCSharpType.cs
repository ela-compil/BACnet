using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.BACnet;

namespace AnotherStorageImplementation
{
    class BaCSharpTypeAttribute : Attribute
    {
        public BacnetApplicationTags SerializeType;

        public BaCSharpTypeAttribute(BacnetApplicationTags SerializeType)
        {
            this.SerializeType = SerializeType;
        }
    }
}
