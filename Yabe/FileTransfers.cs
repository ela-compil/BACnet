/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
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
using System.Text;
using System.IO.BACnet;

namespace Yabe
{
    class FileTransfers
    {
        public bool Cancel { get; set; }

        public static int ReadFileSize(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            IList<BacnetValue> value;
            try
            {
                if (!comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_FILE_SIZE, out value))
                    return -1;
                if (value == null || value.Count == 0)
                    return -1;
                return (int)Convert.ChangeType(value[0].Value, typeof(int));
            }
            catch
            {
                return -1;
            }
        }

        public void DownloadFileByBlocking(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, string filename, Action<int> progress_action)
        {
            Cancel = false;

            //open file
            System.IO.FileStream fs = null;
            try
            {
                fs = System.IO.File.OpenWrite(filename);
            }
            catch (Exception ex)
            {
                throw new System.IO.IOException("Couldn't open file", ex);
            }

            int position = 0;
            uint count = (uint)comm.GetFileBufferMaxSize();
            bool end_of_file = false;
            byte[] buffer;
            int buffer_offset;
            try
            {
                while (!end_of_file && !Cancel)
                {
                    //read from device
                    if (!comm.ReadFileRequest(adr, object_id, ref position, ref count, out end_of_file, out buffer, out buffer_offset))
                        throw new System.IO.IOException("Couldn't read file");
                    position += (int)count;

                    //write to file
                    if (count > 0)
                    {
                        fs.Write(buffer, buffer_offset, (int)count);
                        if (progress_action != null) progress_action(position);
                    }
                }
            }
            finally
            {
                fs.Close();
            }
        }

        public void DownloadFileBySegmentation(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, string filename, Action<int> progress_action)
        {
            Cancel = false;

            //open file
            System.IO.FileStream fs = null;
            try
            {
                fs = System.IO.File.OpenWrite(filename);
            }
            catch (Exception ex)
            {
                throw new System.IO.IOException("Couldn't open file", ex);
            }

            BacnetMaxSegments old_segments = comm.MaxSegments;
            comm.MaxSegments = BacnetMaxSegments.MAX_SEG65;     //send as many segments as needed
            comm.ProposedWindowSize = Properties.Settings.Default.Segments_ProposedWindowSize;      //set by options
            comm.ForceWindowSize = true;
            try
            {
                int position = 0;
                //uint count = (uint)comm.GetFileBufferMaxSize() * 20;     //this is more realistic
                uint count = 50000;                                        //this is more difficult
                bool end_of_file = false;
                byte[] buffer;
                int buffer_offset;
                while (!end_of_file && !Cancel)
                {
                    //read from device
                    if (!comm.ReadFileRequest(adr, object_id, ref position, ref count, out end_of_file, out buffer, out buffer_offset))
                        throw new System.IO.IOException("Couldn't read file");
                    position += (int)count;

                    //write to file
                    if (count > 0)
                    {
                        fs.Write(buffer, buffer_offset, (int)count);
                        if (progress_action != null) progress_action(position);
                    }
                }
            }
            finally
            {
                fs.Close();
                comm.MaxSegments = old_segments;
                comm.ForceWindowSize = false;
            }
        }

        /// <summary>
        /// This method is based upon increasing the MaxInfoFrames in the MSTP.
        /// In Bacnet/IP this will have bad effect due to the retries
        /// </summary>
        public void DownloadFileByAsync(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, string filename, Action<int> progress_action)
        {
            Cancel = false;

            //open file
            System.IO.FileStream fs = null;
            try
            {
                fs = System.IO.File.OpenWrite(filename);
            }
            catch (Exception ex)
            {
                throw new System.IO.IOException("Couldn't open file", ex);
            }

            uint max_count = (uint)comm.GetFileBufferMaxSize();
            BacnetAsyncResult[] transfers = new BacnetAsyncResult[50];
            byte old_max_info_frames = comm.Transport.MaxInfoFrames;
            comm.Transport.MaxInfoFrames = 50;      //increase max_info_frames so that we can occupy line more. This might be against 'standard'

            try
            {
                int position = 0;
                bool eof = false;

                while (!eof && !Cancel)
                {
                    //start many async transfers
                    for (int i = 0; i < transfers.Length; i++)
                        transfers[i] = (BacnetAsyncResult)comm.BeginReadFileRequest(adr, object_id, position + i * (int)max_count, max_count, false);

                    //wait for all transfers to finish
                    int current = 0;
                    int retries = comm.Retries;
                    while (current < transfers.Length)
                    {
                        if (!transfers[current].WaitForDone(comm.Timeout))
                        {
                            if (--retries > 0)
                            {
                                transfers[current].Resend();
                                continue;
                            }
                            else
                                throw new System.IO.IOException("Couldn't read file");
                        }
                        retries = comm.Retries;

                        uint count;
                        byte[] file_buffer;
                        int file_buffer_offset;
                        Exception ex;
                        comm.EndReadFileRequest(transfers[current], out count, out position, out eof, out file_buffer, out file_buffer_offset, out ex);
                        transfers[current] = null;
                        if (ex != null) throw ex;

                        if (count > 0)
                        {
                            //write to file
                            fs.Position = position;
                            fs.Write(file_buffer, file_buffer_offset, (int)count);
                            position += (int)count;
                            if (progress_action != null) progress_action(position);
                        }
                        current++;
                    }
                }
            }
            finally
            {
                fs.Close();
                comm.Transport.MaxInfoFrames = old_max_info_frames;
            }
        }

        public void UploadFileByBlocking(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, string filename, Action<int> progress_action)
        {
            Cancel = false;

            //open file
            System.IO.FileStream fs = null;
            try
            {
                fs = System.IO.File.OpenRead(filename);
            }
            catch (Exception ex)
            {
                throw new System.IO.IOException("Couldn't open file", ex);
            }

            try
            {
                int position = 0;
                int count = comm.GetFileBufferMaxSize();
                byte[] buffer = new byte[count];
                while (count > 0 && !Cancel)
                {
                    //read from disk
                    count = fs.Read(buffer, 0, count);
                    if (count < 0)
                        throw new System.IO.IOException("Couldn't read file");
                    else if (count == 0)
                        continue;

                    //write to device
                    if (!comm.WriteFileRequest(adr, object_id, ref position, count, buffer))
                        throw new System.IO.IOException("Couldn't write file");

                    //progress
                    if (count > 0)
                    {
                        position += count;
                        if (progress_action != null) progress_action(position);
                    }
                }
            }
            finally
            {
                fs.Close();
            }
        }
    }
}
