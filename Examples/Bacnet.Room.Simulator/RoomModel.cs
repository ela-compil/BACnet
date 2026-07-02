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

namespace Bacnet.Room.Simulator
{
    // A room with energy losts proportional to internal Temp minus external Temp
    // an gains proportional to insuflated Temp minus internal Temp
    // and random effects
    // and some delays
    class RoomModel
    {

        double Tempint;
        double RollingCounter = 0;
        Random random = new Random();

        const int NBDelay = 10;
        double[] EnergieDelay = new double[NBDelay];
        int IdxDelay=0;

        public RoomModel(double Tini)
        {
            Tempint= Tini;

        }

        double Delay(double newVal)
        {
            double RetVal;

            EnergieDelay[IdxDelay] = newVal;

            RetVal = EnergieDelay[(IdxDelay + NBDelay - 1) % NBDelay];

            IdxDelay++;
            IdxDelay = IdxDelay % NBDelay;

            return RetVal;
        }

        public double GetNextTemp(double TempAirSoufle, double TempExt, int PSoufflage)
        {

            double Perte = 0.03 * (Tempint - TempExt);
            double Gain = 0.015 * Delay(PSoufflage * (TempAirSoufle - Tempint)) ;

            double Perturbation = 0.0005 * Math.Cos(RollingCounter / 10.0) ;
            RollingCounter += 2 * random.NextDouble();

            Tempint = Tempint - Perte + Gain  + Perturbation;

            return Tempint;

        }
    }
}
