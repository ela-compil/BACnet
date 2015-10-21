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
using System.IO.BACnet;
using System.IO.BACnet.Serialize;
using System.Diagnostics;

namespace Yabe
{
    public partial class AlarmSummary : Form
    {
        BacnetClient comm; BacnetAddress adr;

        public AlarmSummary(ImageList img_List, BacnetClient comm, BacnetAddress adr, uint device_id)
        {
            InitializeComponent();
            this.Text = "Active Alarms on Device Id " + device_id.ToString();
            this.comm = comm;
            this.adr = adr;            

            IList<BacnetGetEventInformationData> Alarms;
            bool MoreEvent;
            TAlarmList.ImageList = img_List;

            // get the Alarm summary
            // Addentum 135-2012av-1 : Deprecate Execution of GetAlarmSummary, GetEVentInformation instead
            // -> parameter 2 in the method call
             if (comm.GetAlarmSummaryOrEventRequest(adr, Properties.Settings.Default.AlarmByGetEventInformation, out Alarms, out MoreEvent) == true)
            {
                LblInfo.Visible = false;
                AckText.Enabled = AckBt.Enabled = true;
                
                // fill the Treenode
                foreach (BacnetGetEventInformationData alarm in Alarms)
                {
                    int icon = MainDialog.GetIconNum(alarm.objectIdentifier.type);
                    TreeNode tn=new TreeNode(alarm.objectIdentifier.ToString(),icon,icon);
                    tn.Tag = alarm;
                    TAlarmList.Nodes.Add(tn);

                    icon = img_List.Images.Count; // out bound
                    tn.Nodes.Add(new TreeNode("Alarm state : "+GetEventNiceName(alarm.eventState.ToString()), icon, icon));
                    tn.Nodes.Add(new TreeNode(alarm.acknowledgedTransitions.ToString(), icon, icon));
                }

                if (Alarms.Count == 0)
                {
                    LblInfo.Visible = true;
                    LblInfo.Text = "Empty event list ... all is OK";
                }
            }
            if (MoreEvent == true)
                PartialLabel.Visible = true;
        }
        private static string GetEventNiceName(String name)
        {
            name = name.Substring(12);
            name = name.Replace('_', ' ');
            name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
            return name;
        }

        private void AckBt_Click(object sender, EventArgs e)
        {            
            TreeNode tn = TAlarmList.SelectedNode;
            if (tn == null) return;
            while (tn.Parent!=null) tn=tn.Parent;  // go up
   
            BacnetGetEventInformationData alarm = (BacnetGetEventInformationData)tn.Tag; // the alam content

            // Read the TO_OFF_NORMAL event time stamp
            IList<BacnetValue> values;
            if (comm.ReadPropertyRequest(adr, alarm.objectIdentifier, BacnetPropertyIds.PROP_EVENT_TIME_STAMPS, out values, 0, 1)==false)
            {
                Trace.TraceWarning("Error reading PROP_EVENT_TIME_STAMPS");
                return;
            }

            String s1 = ((BacnetValue[])(values[0].Value))[0].ToString(); // Date & 00:00:00 for Hour
            String s2 = ((BacnetValue[])(values[0].Value))[1].ToString(); // 00:00:00 & Time

            DateTime dt = Convert.ToDateTime(s1.Split(' ')[0] + " " + s2.Split(' ')[1]);
            BacnetGenericTime bgt = new BacnetGenericTime(dt, BacnetTimestampTags.TIME_STAMP_DATETIME);

            EncodeBuffer b = comm.GetEncodeBuffer(BVLC.BVLC_HEADER_LENGTH);
            NPDU.Encode(b, BacnetNpduControls.PriorityNormalMessage, adr.RoutedSource, null, 0, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            APDU.EncodeConfirmedServiceRequest(b, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, 0, BacnetMaxAdpu.MAX_APDU1476, 0, 0, 0);
            Services.EncodeAlarmAcknowledge(b, 57, alarm.objectIdentifier, (uint)alarm.eventState, AckText.Text, bgt,  new BacnetGenericTime(DateTime.Now, BacnetTimestampTags.TIME_STAMP_DATETIME));

            BacnetAsyncResult ret = new BacnetAsyncResult(comm, adr, 0, b.buffer, b.offset - BVLC.BVLC_HEADER_LENGTH, false, 0);
            ret.Resend();
            
        }

        private void TAlarmList_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode tn=e.Node;
            while (tn.Parent != null) tn = tn.Parent;

            if (tn.ToolTipText == "")
            {
                BacnetGetEventInformationData alarm = (BacnetGetEventInformationData)tn.Tag;
                IList<BacnetValue> name;

                comm.ReadPropertyRequest(adr, alarm.objectIdentifier, BacnetPropertyIds.PROP_OBJECT_NAME, out name);

                tn.ToolTipText = tn.Text;
                tn.Text = name[0].Value.ToString();

            }

        }

    }
}
