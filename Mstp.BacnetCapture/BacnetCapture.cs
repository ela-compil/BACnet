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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using Wireshark;
using System.Collections;
using System.IO.BACnet;
using System.Runtime.InteropServices;

// Thank's to http://icons8.com/ for shark icon

namespace Mstp.BacnetCapture
{
    public partial class BacnetCapture : Form
    {
        // In Wireshark the pipe fullname is \\.\pipe\bacnet
        // it could be launch with
        // "C:\Program Files\Wireshark\Wireshark.exe" -ni \\.\pipe\bacnet
        // or something equivalent on Linux ... but for Mono BufferedTreeView class must be removed

        WiresharkSender Wireshark = new WiresharkSender("bacnet",165);  // 165 = bacnet mstp
        BacnetMstpProtocolTransport Serial;
        List<BacnetNode> NoeudsMstp = new List<BacnetNode>();
        String Master = "Node ";

        bool[] FiltreMessages=new bool[8];

        public BacnetCapture()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();

            // Get the serial ports list
            foreach (string s in ports)
                comboPort.Items.Add(s);
            
            // select the last one which is certainly an Usb to Rs485 adapter 
            if (ports != null)
                if (ports.Length!=0)
                    comboPort.Text = ports[ports.Length-1];

            // Get also Morten's Pipe transport
            // for test purpose : can see Pool for Master activity with DemoServer
            ports = BacnetPipeTransport.AvailablePorts;
            foreach (string str in ports)
                if (str.StartsWith("com", StringComparison.InvariantCultureIgnoreCase))
                {
                    comboPort.Items.Add(str);;
                }

            System.Windows.Forms.ToolTip ToolTip1 = new System.Windows.Forms.ToolTip();
            ToolTip1.SetToolTip(this, "© Frederic Chaxel, Morten Kvistgaard 2015\r\n MIT License\r\n Thanks to http://icons8.com/");

            toFrench();
            treeView.TreeViewNodeSorter = new TreeNodeSorter(); // Specific TreeView sorter

            Form1_Resize(null, null);

        }

        // change language
        // too much simple for using ressources class for that.
        private void toFrench()
        {

            if (Application.CurrentCulture.TwoLetterISOLanguageName == "fr")
            {

                label1.Text = "Wireshark doit être à l'écoute sur le Pipeline :";
                label2.Text = "Port Série :";
                label3.Text = "Vitesse :";
                label4.Text = "Filtre de suppression";
                label5.Text = "Statistiques d\'envois";
                Master = "Noeud ";
            }
        }

        // Serial Transport Listener start
        private void buttonGo_Click(object sender, EventArgs e)
        {

            try
            {
                int com_number = 0;
                if (comboPort.Text.Length >= 3) int.TryParse(comboPort.Text.Substring(3), out com_number);
                if (com_number >= 1000)      // these are Morten's special "pipe" com ports 
                    Serial = new BacnetMstpProtocolTransport(new BacnetPipeTransport(comboPort.Text), -1);
                else
                    Serial = new BacnetMstpProtocolTransport(comboPort.Text, Convert.ToInt32(comboSpeed.Text), -1);

                Serial.RawMessageRecieved += new BacnetMstpProtocolTransport.RawMessageReceivedHandler(Serial_RawMessageRecieved);

                Serial.Start_SpyMode();

                buttonGo.Enabled = false;
            }
            catch { }
        }

        // clean frame types name
        private static string GetNiceName(SnifferDisplayBacnetMstpFrameTypes frameType)
        {
            string name = frameType.ToString();
            name = name.Replace('_', ' ');
            name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
            return name;
        }

        // A frame is received
        void Serial_RawMessageRecieved(byte[] buffer, int offset, int lenght)
        {
            byte source_address = buffer[4];
            byte frame_type = buffer[2];

            // If filter not set, send it to Wireshark
            if (((int)frame_type > 7) || (FiltreMessages[(int)frame_type] == false))
                Wireshark.SendToWireshark(buffer, offset, lenght);

            // Update HMI and counters 
            BeginInvoke(new Action<byte, byte>(MessageReceivedIndication), new object[] { frame_type, source_address });
        }

        // A new node on Mstp, add it to the list and into the treeview
        BacnetNode MakeNewNode(byte source_address)
        {
            BacnetNode noeud = new BacnetNode(source_address);
            NoeudsMstp.Add(noeud);

            // New TreeView node : Key and value are the address, Tag also but in int format
            TreeNode T = treeView.Nodes.Add(source_address.ToString(), Master + source_address.ToString(), 0, 0);
            T.Tag = (int)source_address; // used to sort the treeview by source address criteria

            treeView.Sort();

            TreeNode T2 = T.Nodes.Add("Global", "Global", 1, 1);
            TreeNode T3 = T2.Nodes.Add("", "", 3, 3);
            T3.Tag = 0;
            T3 = T2.Nodes.Add("", "", 3, 3);
            T3.Tag = 1;

            TreeNode T4 = T.Nodes.Add("Detail", "Detail", 2, 2);
            T4.Tag = 1;
            // No need to put a real content, it will be overwrite just after
            for (int i = 0; i < 10; i++)
            {
                TreeNode T5 = T4.Nodes.Add(i.ToString(), "", 3, 3);
                T5.Tag = i;
            }

            return noeud;

        }

        // Update HMI and counters 
        void MessageReceivedIndication(byte frame_type, byte source_address)
        {
            treeView.BeginUpdate();

            // Find the node
            BacnetNode noeud = NoeudsMstp.Find(item => item.Num == source_address);

            if (noeud == null)  // Not here, creation
                noeud = MakeNewNode(source_address);

            // Update node data
            noeud.NewFrameSend((int)frame_type);

            // TreeView update
            TreeNode[] Tn = treeView.Nodes.Find(source_address.ToString(), false);

            for (int i = 0; i < 10; i++)
            {
                Tn[0].Nodes[1].Nodes[i].Text = noeud.FrameTypeStatistic[i].ToString() + " - " + GetNiceName((SnifferDisplayBacnetMstpFrameTypes)i);
            }
            Tn[0].Nodes[0].Nodes[0].Text = noeud.TotalFrames.ToString() + " Total";
            Tn[0].Nodes[0].Nodes[1].Text = noeud.MeanTimeTokenRotation.ToString() + " ms MTTR";

            treeView.EndUpdate();
        }
       
        // Frame filter for Wireshark
        private void checkedFiltresValue_Changed(object sender, EventArgs e)
        {
            // boolean affectation is thread safe, no need to do more
            FiltreMessages[0] = checkToken.Checked;
            FiltreMessages[1] = FiltreMessages[2] = checkMaster.Checked;
            FiltreMessages[3] = FiltreMessages[4] = checkTest.Checked;
            FiltreMessages[5] = FiltreMessages[6] = FiltreMessages[7] = checkData.Checked;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            treeView.Height = this.Height-235;
            checksbox.Height = this.Height - 235;
            this.Width=420;
        }
    
    }

    // treeview comparer base on the (int)Tag associated to the TreeNode
    public class TreeNodeSorter : IComparer
    {
        public int Compare(object x, object y)
        {
            TreeNode tx = x as TreeNode;
            TreeNode ty = y as TreeNode;

            int i = Convert.ToInt32(tx.Tag);
            int j = Convert.ToInt32(ty.Tag);

            return i.CompareTo(j); 
        }
    }

    // Thanks to http://stackoverflow.com/questions/10362988/treeview-flickering
    // avoid flickering ... quite all others solutions found on the web are not OK for treeview
    // come from previous post http://www.codeproject.com/Articles/37253/Double-buffered-Tree-and-Listviews
    class BufferedTreeView : TreeView
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            
            SendMessage(this.Handle, TVM_SETEXTENDEDSTYLE, (IntPtr)TVS_EX_DOUBLEBUFFER, (IntPtr)TVS_EX_DOUBLEBUFFER);
            base.OnHandleCreated(e);

        }
        // Pinvoke:
        private const int TVM_SETEXTENDEDSTYLE = 0x1100 + 44;
        private const int TVM_GETEXTENDEDSTYLE = 0x1100 + 45;
        private const int TVS_EX_DOUBLEBUFFER = 0x0004;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    }

    public enum SnifferDisplayBacnetMstpFrameTypes : byte
    {
        TOKEN = 0,
        POLL_FOR_MASTER = 1,
        REPLY_TO_POLL_FOR_MASTER = 2,
        TEST_REQUEST = 3,
        TEST_RESPONSE = 4,
        DATA_EXPECTING_REPLY = 5,
        DATA_NOT_EXPECTING_REPLY = 6,
        REPLY_POSTPONED = 7,
        // These two types are not the real one, see BacnetMstpFrameTypes enum
        UNKNOW_FRAME_TYPE = 8,
        PROPRIETARY_FRAME = 9,
    };
}
