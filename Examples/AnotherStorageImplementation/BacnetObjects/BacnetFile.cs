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
using System.IO;

namespace BaCSharp
{
    public class BacnetFile:BaCSharpObject
    {
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING)]
        public virtual String PROP_FILE_TYPE
        {
            get { return "Binary"; }
        }
        public bool m_PROP_READ_ONLY;
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
                try
                {
                    FileInfo fi = new FileInfo(FilePath);
                    return (uint)fi.Length;
                }
                catch { return 0; } // no way to return -1 or other
            }
        }

        public String FilePath;

        public BacnetFile(int ObjId, String ObjName, String Description, String FilePath, bool ReadOnly)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, (uint)ObjId), ObjName,  Description)
        {
            m_PROP_READ_ONLY = ReadOnly;
            this.FilePath = FilePath;
        }
        public BacnetFile() { }

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
