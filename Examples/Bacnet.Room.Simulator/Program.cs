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
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Management;

namespace Bacnet.Room.Simulator
{

    static class Program
    {
        public static int Count;
        public static int DeviceId=-1;
        public static string IPAddress = "Default";

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            if ((args != null) && (args.Length >= 1))
            {
                if (Int32.TryParse(args[0], out DeviceId) == false)
                    DeviceId = -1;
            }
            if ((args != null) && (args.Length == 2))
                IPAddress = args[1];

            // Le semaphore sert a donner un id unqiue au noeud Bacnet si DeviceId=-1
            Semaphore s = new Semaphore(63, 63, "Bacnet.Room{FAED-FAED}");
            if (s.WaitOne() == true)
            {
                Count = 64-s.Release();
                s.WaitOne();
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new BacForm());
            }
            catch
            {
                MessageBox.Show("Fatal Error", "Bacnet.Room.Simulator", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            s.Release();
        }

    }
}
