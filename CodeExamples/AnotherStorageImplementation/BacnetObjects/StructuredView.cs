using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.BACnet;

namespace AnotherStorageImplementation
{
    class StructuredView : BacnetObject, IRegisterBacnetObject
    {
        List<BacnetValue> m_PROP_SUBORDINATE_LIST = new List<BacnetValue>();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)]
        public virtual List<BacnetValue> PROP_SUBORDINATE_LIST
        {
            get { return m_PROP_SUBORDINATE_LIST; }
        }

        IRegisterBacnetObject Container;

        public StructuredView(int ObjId, String ObjName, IRegisterBacnetObject Container)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_STRUCTURED_VIEW, (uint)ObjId), ObjName)
        {
            this.Container = Container;
        }

        public void AddBacnetObject(BacnetObject newObj)
        {
            m_PROP_SUBORDINATE_LIST.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, newObj.m_PROP_OBJECT_IDENTIFIER));
            Container.AddBacnetObject(newObj);
        }
    }
}
