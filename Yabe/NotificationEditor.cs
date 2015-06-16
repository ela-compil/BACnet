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

namespace Yabe
{
    public partial class NotificationEditor : Form
    {
        BacnetClient comm; BacnetAddress adr; BacnetObjectId object_id;

        public NotificationEditor(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {            
            this.comm=comm;
            this.adr=adr;
            this.object_id=object_id;

            InitializeComponent();

            LoadProperties();

            labelRecipient.Text = "Recipient List : " + object_id.ToString().Substring(7) ;
        }

        private void LoadProperties()
        {

            RecipientsTab.Controls.Clear();

            // Two properties
            List<BacnetPropertyReference> props = new List<BacnetPropertyReference>();
            props.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_RECIPIENT_LIST, ASN1.BACNET_ARRAY_ALL));
            props.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRIORITY, ASN1.BACNET_ARRAY_ALL));
            IList<BacnetReadAccessResult> PropertiesValues;
            comm.ReadPropertyMultipleRequest(adr, object_id, props, out PropertiesValues);

            foreach (BacnetPropertyValue aProp in PropertiesValues[0].values)
            {
                // priorities are common to all recipients
                if (aProp.property.propertyIdentifier == (uint)BacnetPropertyIds.PROP_PRIORITY)
                {
                    P_Off.Text = ((uint)aProp.value[0].Value).ToString();
                    P_Fault.Text = ((uint)aProp.value[1].Value).ToString();
                    P_Normal.Text = ((uint)aProp.value[2].Value).ToString();
                }
                else
                {
                    // a TabPage in the TabControl for each recipient
                    for (int i = 0; i < aProp.value.Count / 7; i++)
                    {
                        // convert the List<BacnetValue> into a DeviceReportingRecipient
                        DeviceReportingRecipient recipient=new DeviceReportingRecipient(aProp.value[i * 7], aProp.value[i * 7 + 1], aProp.value[i * 7 + 2], aProp.value[i * 7 + 3], aProp.value[i * 7 + 4], aProp.value[i * 7 + 5], aProp.value[i * 7 + 6]);

                        TabPage NewTab = new System.Windows.Forms.TabPage();
                        NewTab.Text = NewTab.Name = i.ToString();

                        // Create a Usercontrol and put it into the TabPage
                        RecipientUserCtrl content = new RecipientUserCtrl(NewTab, recipient);
                        content.DeviceAddrOK += new Action<RecipientUserCtrl, bool>(content_DeviceAddrOK);

                        NewTab.Controls.Add(content);
                        RecipientsTab.Controls.Add(NewTab);
                    }

                }
            }
        }

        private void WriteProperties()
        {
            try // Write Priorities
            {
                List<BacnetValue> PropVal=new List<BacnetValue>();

                PropVal.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, Convert.ToUInt32(P_Off.Text)));
                PropVal.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, Convert.ToUInt32(P_Fault.Text)));
                PropVal.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, Convert.ToUInt32(P_Normal.Text)));
                
                comm.WritePropertyRequest(adr, object_id, BacnetPropertyIds.PROP_PRIORITY, PropVal);
            }
            catch { }
            try  // Write recipient List
            {
                List<BacnetValue> PropVal = new List<BacnetValue>();
                foreach (TabPage t in RecipientsTab.Controls)
                {
                    if (t.Name != "Not Set") // Entry is OK ?
                    {
                        RecipientUserCtrl r=(RecipientUserCtrl)t.Controls[0];
                        DeviceReportingRecipient newrp;
                        if (r.adr!=null) // recipient is an IP address
                            newrp=new DeviceReportingRecipient(r.WeekOfDay,r.fromTime.Value,r.toTime.Value,r.adr,Convert.ToUInt16(r.ProcessId.Text),r.AckRequired.Checked,r.EventType);
                        else // recipient is a deviceId
                            newrp = new DeviceReportingRecipient(r.WeekOfDay, r.fromTime.Value, r.toTime.Value,new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE,r.deviceid), Convert.ToUInt16(r.ProcessId.Text), r.AckRequired.Checked, r.EventType);

                        PropVal.Add(new BacnetValue(newrp));
                    }
                }

                comm.WritePropertyRequest(adr, object_id, BacnetPropertyIds.PROP_RECIPIENT_LIST, PropVal);
            }
            catch { }
        }

        void content_DeviceAddrOK(RecipientUserCtrl Sender, bool AddrOk) // Handler called on each changement in recipient address TextBox
        {
            if (AddrOk == false)
                Sender.myTab.Text = Sender.myTab.Name = "Not Set";
            else
                Sender.myTab.Text = Sender.myTab.Name = ""; 

            // Re-numbering of each TabPage
            int i = 1;
            foreach (TabPage t in RecipientsTab.Controls)
                if (t.Name != "Not Set")
                {
                    t.Text = t.Name = i.ToString();
                    i++;
                }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) // delete a TabPage
        {
            try
            {
                RecipientsTab.Controls.RemoveAt(RecipientsTab.SelectedIndex);
            }
            catch { }

            int i=1;
            foreach (TabPage t in RecipientsTab.Controls) // Re-numbering of each TabPage
            {
                t.Text = t.Name = i.ToString();
                i++;
            }
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e) // add a TabPage
        {
            foreach (TabPage t in RecipientsTab.Controls)
                if (t.Name == "Not Set") return;

            TabPage NewTab = new System.Windows.Forms.TabPage();
            NewTab.Text = NewTab.Name = "Not Set";

            RecipientUserCtrl content = new RecipientUserCtrl(NewTab);
            content.DeviceAddrOK += new Action<RecipientUserCtrl, bool>(content_DeviceAddrOK);

            NewTab.Controls.Add(content);
            RecipientsTab.Controls.Add(NewTab);

            RecipientsTab.SelectTab("Not Set");
        }

        private void btReadWrite_Click(object sender, EventArgs e)
        {
            WriteProperties();
            int idx=RecipientsTab.SelectedIndex;
            LoadProperties();
            try { RecipientsTab.SelectedIndex = idx; } catch { }
        }
    }

    // UserControl for Tab content
    public partial class RecipientUserCtrl : UserControl
    {
        public TabPage myTab;

        public BacnetBitString EventType; // Updated by the related 3 Chekcbox
        public BacnetBitString WeekOfDay; // Updated by the related 7 Chekcbox
        public BacnetAddress adr;
        public uint deviceid;

        public RecipientUserCtrl(TabPage myTab, DeviceReportingRecipient? recipient = null)
        {
            InitializeComponent();
            this.myTab = myTab;
            
            if (recipient == null) return;

            if (recipient.Value.adr != null)
            {
                adr = recipient.Value.adr;
                Device.Text = adr.ToString();
            }
            else
            {
                deviceid = recipient.Value.Id.instance;
                Device.Text = deviceid.ToString();
            }

            AckRequired.Checked = recipient.Value.Ack_Required;
            ProcessId.Text = recipient.Value.processIdentifier.ToString();

            WeekOfDay = recipient.Value.WeekofDay;
            // dispatch the days
            Monday.Checked = ((WeekOfDay.value[0] & 1) == 1);
            Tuesday.Checked = ((WeekOfDay.value[0] & 2) == 2);
            Wedesnday.Checked = ((WeekOfDay.value[0] & 4) == 4);
            Thursday.Checked = ((WeekOfDay.value[0] & 8) == 8);
            Friday.Checked = ((WeekOfDay.value[0] & 16) == 16);
            Saturday.Checked = ((WeekOfDay.value[0] & 32) == 32);
            Sunday.Checked = ((WeekOfDay.value[0] & 64) == 64);

            EventType = recipient.Value.evenType;
            // dispatch the event types
            To_OffNormal.Checked = ((EventType.value[0] & 1) == 1);
            To_Fault.Checked = ((EventType.value[0] & 2) == 2);
            To_Normal.Checked = ((EventType.value[0] & 4) == 4);

            // Some problems when date is 0 in the original values (readed for the device)  
            // so toTime.Value=toTime cannot be done
            toTime.Value = new DateTime(2000, 1, 1, recipient.Value.toTime.Hour, recipient.Value.toTime.Minute, recipient.Value.toTime.Second);
            fromTime.Value = new DateTime(2000, 1, 1, recipient.Value.fromTime.Hour, recipient.Value.fromTime.Minute, recipient.Value.fromTime.Second);
        }

        public event Action<RecipientUserCtrl, bool> DeviceAddrOK;

        private void Device_TextChanged(object sender, EventArgs e)
        {
            adr = null;

            try
            {
                deviceid = Convert.ToUInt16(Device.Text);
                if (DeviceAddrOK != null) DeviceAddrOK(this, true); // it's a deviceId
            }
            catch
            {
                try
                {
                    adr = new BacnetAddress(BacnetAddressTypes.IP, Device.Text);
                    if (DeviceAddrOK != null) DeviceAddrOK(this, true); // it's a xxx.xxx.xxx.xxx:xxx
                }
                catch
                {
                    if (DeviceAddrOK != null) DeviceAddrOK(this, false);
                }
            }
        }

        private void Day_CheckStateChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            int num = Convert.ToInt32(cb.Tag) - 1;
            WeekOfDay.SetBit((byte)num, cb.Checked);
        }

        private void EventType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            int num = Convert.ToInt32(cb.Tag) - 1;
            EventType.SetBit((byte)num, cb.Checked);
        }

        private void RecipientUserCtrl_Load(object sender, EventArgs e)
        {

        }
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        private void InitializeComponent()
        {
            this.AckRequired = new System.Windows.Forms.CheckBox();
            this.ProcessId = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.EventGrp = new System.Windows.Forms.GroupBox();
            this.To_Normal = new System.Windows.Forms.CheckBox();
            this.To_Fault = new System.Windows.Forms.CheckBox();
            this.To_OffNormal = new System.Windows.Forms.CheckBox();
            this.Validity = new System.Windows.Forms.GroupBox();
            this.toTime = new System.Windows.Forms.DateTimePicker();
            this.fromTime = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Sunday = new System.Windows.Forms.CheckBox();
            this.Saturday = new System.Windows.Forms.CheckBox();
            this.Friday = new System.Windows.Forms.CheckBox();
            this.Thursday = new System.Windows.Forms.CheckBox();
            this.Wedesnday = new System.Windows.Forms.CheckBox();
            this.Tuesday = new System.Windows.Forms.CheckBox();
            this.Monday = new System.Windows.Forms.CheckBox();
            this.Receiver = new System.Windows.Forms.GroupBox();
            this.Device = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.EventGrp.SuspendLayout();
            this.Validity.SuspendLayout();
            this.Receiver.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // AckRequired
            // 
            this.AckRequired.AutoSize = true;
            this.AckRequired.Location = new System.Drawing.Point(26, 18);
            this.AckRequired.Name = "AckRequired";
            this.AckRequired.Size = new System.Drawing.Size(91, 17);
            this.AckRequired.TabIndex = 0;
            this.AckRequired.Text = "Ack Required";
            this.AckRequired.UseVisualStyleBackColor = true;
            // 
            // ProcessId
            // 
            this.ProcessId.Location = new System.Drawing.Point(285, 15);
            this.ProcessId.Name = "ProcessId";
            this.ProcessId.Size = new System.Drawing.Size(40, 20);
            this.ProcessId.TabIndex = 1;
            this.ProcessId.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(205, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Process Id";
            // 
            // EventGrp
            // 
            this.EventGrp.Controls.Add(this.To_Normal);
            this.EventGrp.Controls.Add(this.To_Fault);
            this.EventGrp.Controls.Add(this.To_OffNormal);
            this.EventGrp.Location = new System.Drawing.Point(26, 73);
            this.EventGrp.Name = "EventGrp";
            this.EventGrp.Size = new System.Drawing.Size(356, 70);
            this.EventGrp.TabIndex = 3;
            this.EventGrp.TabStop = false;
            this.EventGrp.Text = "EventType";
            // 
            // To_Normal
            // 
            this.To_Normal.AutoSize = true;
            this.To_Normal.Checked = true;
            this.To_Normal.CheckState = System.Windows.Forms.CheckState.Checked;
            this.To_Normal.Location = new System.Drawing.Point(260, 29);
            this.To_Normal.Name = "To_Normal";
            this.To_Normal.Size = new System.Drawing.Size(78, 17);
            this.To_Normal.TabIndex = 2;
            this.To_Normal.Tag = "3";
            this.To_Normal.Text = "To_Normal";
            this.To_Normal.UseVisualStyleBackColor = true;
            this.To_Normal.CheckedChanged += new System.EventHandler(this.EventType_CheckedChanged);
            // 
            // To_Fault
            // 
            this.To_Fault.AutoSize = true;
            this.To_Fault.Checked = true;
            this.To_Fault.CheckState = System.Windows.Forms.CheckState.Checked;
            this.To_Fault.Location = new System.Drawing.Point(135, 29);
            this.To_Fault.Name = "To_Fault";
            this.To_Fault.Size = new System.Drawing.Size(68, 17);
            this.To_Fault.TabIndex = 1;
            this.To_Fault.Tag = "2";
            this.To_Fault.Text = "To_Fault";
            this.To_Fault.UseVisualStyleBackColor = true;
            this.To_Fault.CheckedChanged += new System.EventHandler(this.EventType_CheckedChanged);
            // 
            // To_OffNormal
            // 
            this.To_OffNormal.AutoSize = true;
            this.To_OffNormal.Checked = true;
            this.To_OffNormal.CheckState = System.Windows.Forms.CheckState.Checked;
            this.To_OffNormal.Location = new System.Drawing.Point(11, 29);
            this.To_OffNormal.Name = "To_OffNormal";
            this.To_OffNormal.Size = new System.Drawing.Size(92, 17);
            this.To_OffNormal.TabIndex = 0;
            this.To_OffNormal.Tag = "1";
            this.To_OffNormal.Text = "To_OffNormal";
            this.To_OffNormal.UseVisualStyleBackColor = true;
            this.To_OffNormal.CheckedChanged += new System.EventHandler(this.EventType_CheckedChanged);
            // 
            // Validity
            // 
            this.Validity.Controls.Add(this.toTime);
            this.Validity.Controls.Add(this.fromTime);
            this.Validity.Controls.Add(this.label3);
            this.Validity.Controls.Add(this.label2);
            this.Validity.Controls.Add(this.Sunday);
            this.Validity.Controls.Add(this.Saturday);
            this.Validity.Controls.Add(this.Friday);
            this.Validity.Controls.Add(this.Thursday);
            this.Validity.Controls.Add(this.Wedesnday);
            this.Validity.Controls.Add(this.Tuesday);
            this.Validity.Controls.Add(this.Monday);
            this.Validity.Location = new System.Drawing.Point(25, 156);
            this.Validity.Name = "Validity";
            this.Validity.Size = new System.Drawing.Size(357, 111);
            this.Validity.TabIndex = 4;
            this.Validity.TabStop = false;
            this.Validity.Text = "Validity";
            // 
            // toTime
            // 
            this.toTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.toTime.Location = new System.Drawing.Point(269, 81);
            this.toTime.Name = "toTime";
            this.toTime.ShowUpDown = true;
            this.toTime.Size = new System.Drawing.Size(68, 20);
            this.toTime.TabIndex = 12;
            this.toTime.Value = new System.DateTime(2015, 6, 15, 23, 59, 59, 0);
            // 
            // fromTime
            // 
            this.fromTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.fromTime.Location = new System.Drawing.Point(65, 81);
            this.fromTime.Name = "fromTime";
            this.fromTime.ShowUpDown = true;
            this.fromTime.Size = new System.Drawing.Size(68, 20);
            this.fromTime.TabIndex = 11;
            this.fromTime.Value = new System.DateTime(2015, 6, 15, 0, 0, 0, 0);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(219, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "toTime";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "fromTime";
            // 
            // Sunday
            // 
            this.Sunday.AutoSize = true;
            this.Sunday.Checked = true;
            this.Sunday.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Sunday.Location = new System.Drawing.Point(180, 48);
            this.Sunday.Name = "Sunday";
            this.Sunday.Size = new System.Drawing.Size(62, 17);
            this.Sunday.TabIndex = 6;
            this.Sunday.Tag = "7";
            this.Sunday.Text = "Sunday";
            this.Sunday.UseVisualStyleBackColor = true;
            this.Sunday.CheckStateChanged += new System.EventHandler(this.Day_CheckStateChanged);
            // 
            // Saturday
            // 
            this.Saturday.AutoSize = true;
            this.Saturday.Checked = true;
            this.Saturday.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Saturday.Location = new System.Drawing.Point(94, 48);
            this.Saturday.Name = "Saturday";
            this.Saturday.Size = new System.Drawing.Size(68, 17);
            this.Saturday.TabIndex = 5;
            this.Saturday.Tag = "6";
            this.Saturday.Text = "Saturday";
            this.Saturday.UseVisualStyleBackColor = true;
            this.Saturday.CheckStateChanged += new System.EventHandler(this.Day_CheckStateChanged);
            // 
            // Friday
            // 
            this.Friday.AutoSize = true;
            this.Friday.Checked = true;
            this.Friday.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Friday.Location = new System.Drawing.Point(12, 48);
            this.Friday.Name = "Friday";
            this.Friday.Size = new System.Drawing.Size(54, 17);
            this.Friday.TabIndex = 4;
            this.Friday.Tag = "5";
            this.Friday.Text = "Friday";
            this.Friday.UseVisualStyleBackColor = true;
            this.Friday.CheckStateChanged += new System.EventHandler(this.Day_CheckStateChanged);
            // 
            // Thursday
            // 
            this.Thursday.AutoSize = true;
            this.Thursday.Checked = true;
            this.Thursday.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Thursday.Location = new System.Drawing.Point(269, 25);
            this.Thursday.Name = "Thursday";
            this.Thursday.Size = new System.Drawing.Size(70, 17);
            this.Thursday.TabIndex = 3;
            this.Thursday.Tag = "4";
            this.Thursday.Text = "Thursday";
            this.Thursday.UseVisualStyleBackColor = true;
            this.Thursday.CheckStateChanged += new System.EventHandler(this.Day_CheckStateChanged);
            // 
            // Wedesnday
            // 
            this.Wedesnday.AutoSize = true;
            this.Wedesnday.Checked = true;
            this.Wedesnday.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Wedesnday.Location = new System.Drawing.Point(180, 25);
            this.Wedesnday.Name = "Wedesnday";
            this.Wedesnday.Size = new System.Drawing.Size(83, 17);
            this.Wedesnday.TabIndex = 2;
            this.Wedesnday.Tag = "3";
            this.Wedesnday.Text = "Wedesnday";
            this.Wedesnday.UseVisualStyleBackColor = true;
            this.Wedesnday.CheckStateChanged += new System.EventHandler(this.Day_CheckStateChanged);
            // 
            // Tuesday
            // 
            this.Tuesday.AutoSize = true;
            this.Tuesday.Checked = true;
            this.Tuesday.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Tuesday.Location = new System.Drawing.Point(94, 25);
            this.Tuesday.Name = "Tuesday";
            this.Tuesday.Size = new System.Drawing.Size(67, 17);
            this.Tuesday.TabIndex = 1;
            this.Tuesday.Tag = "2";
            this.Tuesday.Text = "Tuesday";
            this.Tuesday.UseVisualStyleBackColor = true;
            this.Tuesday.CheckStateChanged += new System.EventHandler(this.Day_CheckStateChanged);
            // 
            // Monday
            // 
            this.Monday.AutoSize = true;
            this.Monday.Checked = true;
            this.Monday.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Monday.Location = new System.Drawing.Point(12, 25);
            this.Monday.Name = "Monday";
            this.Monday.Size = new System.Drawing.Size(64, 17);
            this.Monday.TabIndex = 0;
            this.Monday.Tag = "1";
            this.Monday.Text = "Monday";
            this.Monday.UseVisualStyleBackColor = true;
            this.Monday.CheckStateChanged += new System.EventHandler(this.Day_CheckStateChanged);
            // 
            // Receiver
            // 
            this.Receiver.Controls.Add(this.Device);
            this.Receiver.Location = new System.Drawing.Point(26, 291);
            this.Receiver.Name = "Receiver";
            this.Receiver.Size = new System.Drawing.Size(352, 79);
            this.Receiver.TabIndex = 5;
            this.Receiver.TabStop = false;
            this.Receiver.Text = "Receiver :  deviceId or IP:Port (like 4000 or 192.168.0.1:47808)";
            // 
            // Device
            // 
            this.Device.Location = new System.Drawing.Point(93, 30);
            this.Device.Name = "Device";
            this.Device.Size = new System.Drawing.Size(164, 20);
            this.Device.TabIndex = 0;
            this.Device.TextChanged += new System.EventHandler(this.Device_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.AckRequired);
            this.groupBox1.Controls.Add(this.ProcessId);
            this.groupBox1.Location = new System.Drawing.Point(26, 15);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(356, 49);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            // 
            // RecipientUserCtrl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Receiver);
            this.Controls.Add(this.Validity);
            this.Controls.Add(this.EventGrp);
            this.Name = "RecipientUserCtrl";
            this.Size = new System.Drawing.Size(404, 391);
            this.Load += new System.EventHandler(this.RecipientUserCtrl_Load);
            this.EventGrp.ResumeLayout(false);
            this.EventGrp.PerformLayout();
            this.Validity.ResumeLayout(false);
            this.Validity.PerformLayout();
            this.Receiver.ResumeLayout(false);
            this.Receiver.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.CheckBox AckRequired;
        public System.Windows.Forms.TextBox ProcessId;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox EventGrp;
        private System.Windows.Forms.CheckBox To_Normal;
        private System.Windows.Forms.CheckBox To_Fault;
        private System.Windows.Forms.CheckBox To_OffNormal;
        private System.Windows.Forms.GroupBox Validity;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox Sunday;
        private System.Windows.Forms.CheckBox Saturday;
        private System.Windows.Forms.CheckBox Friday;
        private System.Windows.Forms.CheckBox Thursday;
        private System.Windows.Forms.CheckBox Wedesnday;
        private System.Windows.Forms.CheckBox Tuesday;
        private System.Windows.Forms.CheckBox Monday;
        private System.Windows.Forms.GroupBox Receiver;
        public System.Windows.Forms.TextBox Device;
        private System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.DateTimePicker fromTime;
        public System.Windows.Forms.DateTimePicker toTime;
    }

}
