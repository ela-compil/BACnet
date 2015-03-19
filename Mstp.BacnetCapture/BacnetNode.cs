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
using System.Diagnostics;

namespace Mstp.BacnetCapture
{
    class BacnetNode
    {
        public int[] FrameTypeStatistic = new int[10];
        public byte Num;
        public int TotalFrames;

        static Stopwatch St = new Stopwatch();
        
        long mTTR;

        long LastTick;

        static BacnetNode()
        {
            St.Start();
        }

        public BacnetNode(byte Num)
        {
            this.Num = Num;
        }

        public void NewFrameSend(int type_frame)
        {
            if (type_frame < 8)
                FrameTypeStatistic[type_frame]++;
            else
                if (type_frame < 128)
                    FrameTypeStatistic[8]++;
                else
                    FrameTypeStatistic[9]++;

            TotalFrames++;

            if (type_frame == 0)
                TokenRotationTimeUpdate();

        }
        
        public int MeanTimeTokenRotation
        {
            get { return (int)((1000*mTTR)/Stopwatch.Frequency); }
        }

        private void TokenRotationTimeUpdate()
        {
            long tick;

            if (LastTick == 0)
            {
                LastTick = St.ElapsedTicks;
                return;
            }

            tick = St.ElapsedTicks;
            // smoothing
            mTTR = ((tick - LastTick) + 100 * mTTR) / 101;
            LastTick = tick;

        }

    }
}
