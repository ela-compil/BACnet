using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.BACnet;
using System.IO;

namespace AnotherStorageImplementation
{
    class BacnetFile:BacnetObject
    {
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING)]
        public virtual String PROP_FILE_TYPE
        {
            get { return "Binary"; }
        }
        protected bool m_PROP_READ_ONLY;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_READ_ONLY
        {
            get { return m_PROP_READ_ONLY; }
        }
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_ARCHIVE
        {
            get { return false; }
        }
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_FILE_ACCESS_METHOD
        {
            get { return 1; } // FILE_STREAM_ACCESS  
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_FILE_SIZE
        {
            get {
                FileInfo fi = new FileInfo(FilePath);
                return (uint)fi.Length; 
            }
        }

        protected String FilePath;

        public BacnetFile(int ObjId, String ObjName, String FilePath, bool ReadOnly)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, (uint)ObjId), ObjName)
        {
            m_PROP_READ_ONLY = ReadOnly;
            this.FilePath = FilePath;
        }

        public virtual byte[] ReadFileBlock(int position, int quantity)
        {
            try
            {
                byte[] b=new byte[quantity];
                FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                fs.Seek(position, SeekOrigin.Begin);
                fs.Read(b,0, quantity);
                fs.Close();
                return b;
            }
            catch{}

            return null;
        }

        public virtual bool WriteFileBlock(byte[] block, int position, int quantity)
        {
            try
            {
                FileStream fs;
                if (position==0)
                    fs= new FileStream(FilePath, FileMode.Create);
                else
                    fs = new FileStream(FilePath, FileMode.Append);

                fs.Write(block, 0, quantity);
                fs.Close();
                return true;
            }
            catch { }

            return false;
        }
    }
}
