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
using System.Diagnostics;
using System.Reflection;

namespace BaCSharp
{
    // Not serializable, quite complexe in a polymorphic context
    public class DeviceObject : BaCSharpObject
    {
        public List<BaCSharpObject> ObjectsList=new List<BaCSharpObject>();

        public List<BacnetValue> m_PROP_OBJECT_LIST = new List<BacnetValue>();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)]
        public virtual List<BacnetValue> PROP_OBJECT_LIST
        {
            get { return m_PROP_OBJECT_LIST; }
        }

        public List<BacnetValue> m_PROP_STRUCTURED_OBJECT_LIST = new List<BacnetValue>();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)]
        public virtual List<BacnetValue> PROP_STRUCTURED_OBJECT_LIST
        {
            get { return m_PROP_STRUCTURED_OBJECT_LIST; }
        }

        public BacnetBitString m_PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED
        {
            get { return m_PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED; }
        }

        public BacnetBitString m_PROP_PROTOCOL_SERVICES_SUPPORTED = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_PROTOCOL_SERVICES_SUPPORTED
        {
            get { return m_PROP_PROTOCOL_SERVICES_SUPPORTED; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING)]
        public virtual String PROP_VENDOR_NAME
        {
            get { return "F. Chaxel MIT licence, 2015"; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING)]
        public virtual String PROP_FIRMWARE_REVISION
        {
            get { return "Version Beta 1"; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_VENDOR_IDENTIFIER
        {
            get { return 61440; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_APPLICATION_SOFTWARE_VERSION
        {
            get { return 1; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_PROTOCOL_VERSION
        {
            get { return 1; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_PROTOCOL_REVISION
        {
            get { return 6; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_MAX_APDU_LENGTH_ACCEPTED
        {
            get { return 1476; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_APDU_TIMEOUT
        {
            get { return 3000; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_NUMBER_OF_APDU_RETRIES
        {
            get { return 3; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_DATABASE_REVISION
        {
            get { return 0; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_SEGMENTATION_SUPPORTED
        {
            get { return 3; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)]
        public virtual object PROP_DEVICE_ADDRESS_BINDING
        {
            get { return null; }
        }

        public bool UseStructuredView;

        public DeviceObject(uint Id, String DeviceName, String Description, bool UseStructuredView)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, Id), DeviceName, Description)
        {
            this.UseStructuredView = UseStructuredView;

            m_PROP_OBJECT_LIST.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, m_PROP_OBJECT_IDENTIFIER));
            m_PROP_STRUCTURED_OBJECT_LIST.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, m_PROP_OBJECT_IDENTIFIER));

            m_PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED.SetBit((byte)BacnetObjectTypes.OBJECT_DEVICE, true);

            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.MAX_BACNET_SERVICES_SUPPORTED, false); //set all false
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_I_AM, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_WHO_IS, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_READ_PROP_MULTIPLE, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_READ_PROPERTY, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_WRITE_PROPERTY, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_SUBSCRIBE_COV, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_SUBSCRIBE_COV_PROPERTY, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_READ_RANGE, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_ATOMIC_READ_FILE, true);
            m_PROP_PROTOCOL_SERVICES_SUPPORTED.SetBit((byte)BacnetServicesSupported.SERVICE_SUPPORTED_ATOMIC_WRITE_FILE, true);
        }

        public DeviceObject() { }
        // Each object provided by the server must be added one by one to the DeviceObject
        public virtual void AddBacnetObject(BaCSharpObject newObj)
        {
            ObjectsList.Add(newObj);
            newObj.deviceOwner=this;

            // Update OBJECT_TYPES_SUPPORTED
            m_PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED.SetBit((byte)newObj.PROP_OBJECT_IDENTIFIER.type, true);
            // Update OBJECT_LIST
            m_PROP_OBJECT_LIST.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, newObj.PROP_OBJECT_IDENTIFIER));
            
            // Update the structured object list
            // but only if the caller is not a view
            // check by caller method type class appartenance
            MethodBase m = new StackFrame(1).GetMethod();
            bool CallerIsView = (m.DeclaringType == typeof(StructuredView));

            if (!CallerIsView)
                m_PROP_STRUCTURED_OBJECT_LIST.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, newObj.PROP_OBJECT_IDENTIFIER));

        }

        // Can be use to find a particular object with it's bacnet id 
        public virtual BaCSharpObject FindBacnetObject(BacnetObjectId objId)
        {
            if (this.Equals(objId)) return this;

            foreach (BaCSharpObject b in ObjectsList)
                if (b.Equals(objId))
                    return b;
            return null;
        }

        public virtual bool FindBacnetObjectType(BacnetObjectTypes objType)
        {

            foreach (BaCSharpObject b in ObjectsList)
                if (b.PROP_OBJECT_TYPE==(uint)objType)
                    return true;
            return false;
        }

        protected override uint BacnetMethodNametoId(String Name)
        {
            if ((Name == "get_PROP_STRUCTURED_OBJECT_LIST") && (!this.UseStructuredView))  // Hide this property
                return (uint)((int)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID + 1);
            else
                return base.BacnetMethodNametoId(Name);
        }
    }
}
