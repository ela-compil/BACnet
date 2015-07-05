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
using System.IO.BACnet.Storage;
using System.Diagnostics;

namespace Yabe
{
    public partial class ScheduleDisplay : Form
    { 
        BacnetClient comm; BacnetAddress adr; BacnetObjectId schedule_id;
        // Default value type here if no values are already present
        // Could be choosen somewhere by the user
        BacnetApplicationTags ScheduleType = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;

        TreeNode mySelectedScheduleNode;

        public ScheduleDisplay(ImageList img_List, BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            InitializeComponent();
            this.comm = comm;
            this.adr = adr;
            this.schedule_id = object_id;

            // Yes it could be done in one time but
            // decoding is sometimes made by the stack, sometimes no
            // ... so no way !
            ReadEffectivePeriod();
            ReadEffectiveWeeklySchedule();
            ReadObjectsPropertiesReferences();

            ToolTip t1=new ToolTip();
            t1.AutomaticDelay = 0;
            t1.SetToolTip(TxtStartDate, "A wrong value set this to Always");
            ToolTip t2 = new ToolTip();
            t2.AutomaticDelay = 0;
            t2.SetToolTip(TxtEndDate, "A wrong value set this to Always");

            // get the ImageList from MainDialog
            listReferences.SmallImageList = img_List; 
        }

        // Read start and stop dates validity for the schedule
        private void ReadEffectivePeriod()
        {
            IList<BacnetValue> value;
            try
            {
                if (comm.ReadPropertyRequest(adr, schedule_id, BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, out value))
                {
                    DateTime dt=(DateTime)value[0].Value;
                    if (dt.Ticks != 0)  // it's the way always date (encoded FF-FF-FF-FF) is put into a DateTime struct
                        TxtStartDate.Text = dt.ToString("d");
                    else
                        TxtStartDate.Text = "Always";

                    dt = (DateTime)value[1].Value;
                    if (dt.Ticks != 0)
                        TxtEndDate.Text = dt.ToString("d");
                    else
                        TxtEndDate.Text = "Always";
                }
            }
            catch
            {
            }
        }

        private void WriteEffectivePeriod()
        {
            // Manual ASN.1/BER encoding
            EncodeBuffer b = comm.GetEncodeBuffer(0);
            ASN1.encode_opening_tag(b, 3);

            DateTime dt;

            if (TxtStartDate.Text != "Always")
                dt = Convert.ToDateTime(TxtStartDate.Text);
            else
                dt=new DateTime(0);
            ASN1.encode_application_date(b, dt);

            if (TxtEndDate.Text != "Always")
                dt = Convert.ToDateTime(TxtEndDate.Text);
            else
                dt = new DateTime(0);
            ASN1.encode_application_date(b, dt);

            ASN1.encode_closing_tag(b, 3);

            Array.Resize<byte>(ref b.buffer, b.offset);
            byte[] InOutBuffer = b.buffer;
            comm.RawEncodedDecodedPropertyConfirmedRequest(adr, schedule_id, BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, ref InOutBuffer);

        }

        private void AddPropertyRefentry(BacnetDeviceObjectPropertyReference bopr, int IdxRemove)
        {
            String newText;

            if (bopr.deviceIndentifier.type != BacnetObjectTypes.OBJECT_DEVICE)
                newText = bopr.objectIdentifier.ToString().Substring(7) + " - " + ((BacnetPropertyIds)bopr.propertyIdentifier).ToString().Substring(5) + " on localDevice";
            else
                newText = bopr.objectIdentifier.ToString().Substring(7) + " - " + ((BacnetPropertyIds)bopr.propertyIdentifier).ToString().Substring(5) + " on DEVICE:" + bopr.deviceIndentifier.instance.ToString();

            if (IdxRemove != -1)
                listReferences.Items.RemoveAt(IdxRemove); // remove an old entry

            ListViewItem lvi=new ListViewItem();
            // add a new one
            lvi.Text = newText;
            lvi.Tag = bopr;
            lvi.ImageIndex = MainDialog.GetIconNum(bopr.objectIdentifier.type);
            listReferences.Items.Add(lvi);
        }

        private void ReadObjectsPropertiesReferences()
        {
            listReferences.BeginUpdate();
            listReferences.Items.Clear();
            try
            {
                IList<BacnetValue> value;
                if (comm.ReadPropertyRequest(adr, schedule_id, BacnetPropertyIds.PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES, out value))
                {
                    foreach (BacnetValue bv in value)
                    {
                        BacnetDeviceObjectPropertyReference bopr = (BacnetDeviceObjectPropertyReference)bv.Value;
                        AddPropertyRefentry(bopr, -1);
                    }
                }

            }
            catch
            {

            }
             listReferences.EndUpdate();
        }

        private void WriteObjectsPropertiesReferences()
        {
            List<BacnetValue> values=new List<BacnetValue>();

            if (listReferences.Items.Count != 0)
            {
                values=new List<BacnetValue>();

                foreach (ListViewItem lvi in listReferences.Items)
                {
                    BacnetDeviceObjectPropertyReference b = (BacnetDeviceObjectPropertyReference)lvi.Tag;
                    values.Add(new BacnetValue(b));
                }
            }

            comm.WritePropertyRequest(adr, schedule_id, BacnetPropertyIds.PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES, values);
            
        }

        // no test here if buffer is to small
        private void WriteEffectiveWeeklySchedule()
        {
            // Manual ASN.1/BER encoding
            EncodeBuffer b = comm.GetEncodeBuffer(0);
            ASN1.encode_opening_tag(b, 3);

            // Monday
            //  Time
            //  Value
            //  Time    
            //  Value
            // Thusday
            //  ....
            for (int i = 0; i < 7; i++)
            {
                ASN1.encode_opening_tag(b, 0);
                TreeNode T = Schedule.Nodes[i];

                foreach (TreeNode entry in T.Nodes)
                {
                    String[] s = entry.Text.Split('=');

                    BacnetValue bdt = Property.DeserializeValue(s[0], BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME);
                    BacnetValue bval;
                    if (s[1].ToLower().Contains("null"))
                        bval = new BacnetValue(null);
                    else
                        bval = Property.DeserializeValue(s[1], ScheduleType);

                    ASN1.bacapp_encode_application_data(b, bdt);
                    ASN1.bacapp_encode_application_data(b, bval);
                }

                ASN1.encode_closing_tag(b, 0);

            }
            ASN1.encode_closing_tag(b, 3);

            Array.Resize<byte>(ref b.buffer, b.offset);
            byte[] InOutBuffer = b.buffer;
            comm.RawEncodedDecodedPropertyConfirmedRequest(adr, schedule_id, BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, ref InOutBuffer);

        }

       private void ReadEffectiveWeeklySchedule()
        {
            Schedule.BeginUpdate();
            Schedule.Nodes.Clear();

            byte[] InOutBuffer = null;

            try
            {
                if (comm.RawEncodedDecodedPropertyConfirmedRequest(adr, schedule_id, BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, ref InOutBuffer))
                {
                    int offset = 0;
                    byte tag_number;
                    uint len_value_type;

                    // Tag 3
                    offset += ASN1.decode_tag_number(InOutBuffer, offset, out tag_number);
                    if (tag_number != 3) return;

                    for (int i = 1; i < 8; i++)
                    {
                        TreeNode tday = null;

                        tday = new TreeNode("[" + (i-1).ToString() + "] : " + System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.DayNames[i % 7],0,0);
 
                        Schedule.Nodes.Add(tday);

                        // Tag 0
                        offset += ASN1.decode_tag_number(InOutBuffer, offset, out tag_number);
                        while (!ASN1.IS_CLOSING_TAG(InOutBuffer[offset]))
                        {
                            BacnetValue value;
                            String s;

                            // Time
                            offset += ASN1.decode_tag_number_and_value(InOutBuffer, offset, out tag_number, out len_value_type);
                            offset += ASN1.bacapp_decode_data(InOutBuffer, offset, InOutBuffer.Length, (BacnetApplicationTags)tag_number, len_value_type, out value);
                            DateTime dt = (DateTime)value.Value;                            

                            // Value
                            offset += ASN1.decode_tag_number_and_value(InOutBuffer, offset, out tag_number, out len_value_type);
                            offset += ASN1.bacapp_decode_data(InOutBuffer, offset, InOutBuffer.Length, (BacnetApplicationTags)tag_number, len_value_type, out value);

                            if (value.Tag != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                            {
                                s = dt.ToString("T") + " = " + Property.SerializeValue(value, value.Tag); // Second value is the ... value (Bool, Int, Uint, Float, double or null)
                                ScheduleType = value.Tag;               // all type must be the same for a valid schedule (maybe, not sure !), so remember it
                            }
                            else
                                s = dt.ToString("T") + " = null";

                            tday.Nodes.Add(new TreeNode(s, 1, 1));
                        }
                        offset++;

                    }
                    offset += ASN1.decode_tag_number(InOutBuffer, offset, out tag_number);
                    if (tag_number != 3)
                        Schedule.Nodes.Clear();
                }
            }
            catch { }
            finally
            {
                Schedule.EndUpdate();

                Schedule.Sort(); // Time entries are not necesserary sorted, so do it (that's also why days are assign to [0], .. [6])
                Schedule.ExpandAll();
                Schedule.LabelEdit = true;

            }

        }

        private void Update_Click(object sender, EventArgs e)
        {
            WriteEffectivePeriod();
            WriteEffectiveWeeklySchedule();
            WriteObjectsPropertiesReferences();

            ReadEffectivePeriod();
            ReadEffectiveWeeklySchedule();
            ReadObjectsPropertiesReferences();
        }

        private void Schedule_MouseDown(object sender, MouseEventArgs e)
        {
            mySelectedScheduleNode = Schedule.GetNodeAt(e.X, e.Y);
        }

        // Verify if Time is OK and if Value is in the right format
        private bool Valid_Entry(string e)
        {

            String[] s = e.Split('=');
            if (s.Length != 2) return false;

            try
            {
                DateTime dt = Convert.ToDateTime("01/01/2001 " + s[0]);
                if (s[1].ToLower().Contains("null")) return true;
                BacnetValue bv = Property.DeserializeValue(s[1], ScheduleType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Schedule_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label != null) 
            {
                if ((e.Label.Length > 0)&&(Valid_Entry(e.Label)))
                {                    
                    e.Node.EndEdit(false);
                }
                else
                {
                    e.CancelEdit = true;
                    e.Node.BeginEdit();
                    MessageBox.Show("Wrong Format : hh:mm:ss = Val or null","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                }
            }
            else
            {
                e.CancelEdit = true;
            }

        }

        private void modifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveControl == Schedule)   // In the Schedule List
            {
                // do not modify a Day Node
                if ((mySelectedScheduleNode != null) && (mySelectedScheduleNode.Parent != null))
                {

                    if (!mySelectedScheduleNode.IsEditing)
                    {
                        Schedule.LabelEdit = true;
                        mySelectedScheduleNode.BeginEdit();
                    }
                }
                else
                    Schedule.LabelEdit = false;
            }
            else
            {
                try
                {
                    EditPropertyObjectReference form = new EditPropertyObjectReference((BacnetDeviceObjectPropertyReference)listReferences.SelectedItems[0].Tag);
                    form.ShowDialog();
                    listReferences.SelectedItems[0].Tag = form.ObjRef;
                    int idx=listReferences.SelectedItems[0].Index;

                    if (form.RefModified == true)
                        AddPropertyRefentry(form.ObjRef, idx);
                }
                catch { }

            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveControl == Schedule)   // In the Schedule List
            {
                // Do not delete A day entry, only a time schedule entry
                if ((mySelectedScheduleNode != null) && (mySelectedScheduleNode.Parent != null))
                {
                    Schedule.Nodes.Remove(mySelectedScheduleNode);
                    mySelectedScheduleNode = null;
                }
            }
            else
            {
                foreach (ListViewItem item in listReferences.SelectedItems)
                    listReferences.Items.Remove(item);
            }
        }

        // Add a new entry at the right place
        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveControl == Schedule)   // In the Schedule List
            {
                if (mySelectedScheduleNode != null)
                {
                    TreeNode T = new TreeNode("00:00:00 = 0", 1, 1);

                    if (mySelectedScheduleNode.Parent == null)
                    {
                        mySelectedScheduleNode.Nodes.Add(T);
                        mySelectedScheduleNode.Expand();    // sometimes neeeded
                    }
                    else
                        mySelectedScheduleNode.Parent.Nodes.Add(T);

                    // Modify mode
                    mySelectedScheduleNode = T;
                    modifyToolStripMenuItem_Click(null, null);
                }
            }
            else
            {
                BacnetDeviceObjectPropertyReference newobj = new BacnetDeviceObjectPropertyReference(new BacnetObjectId(), BacnetPropertyIds.PROP_PRESENT_VALUE);

                EditPropertyObjectReference form = new EditPropertyObjectReference(newobj);
                form.ShowDialog();


                if (form.OutOK == true)
                    AddPropertyRefentry(form.ObjRef, -1);
            }
        }

        Form FormDatePicker;
        private void DateTimePicker(ref string datestr)
        {
            MonthCalendar cal = new MonthCalendar();
            cal.KeyPress += new KeyPressEventHandler(cal_KeyPress);

            FormDatePicker = new Form();
            FormDatePicker.Text = "Select and close";
            FormDatePicker.Icon = this.Icon;
            FormDatePicker.Controls.Add(cal);
            FormDatePicker.AutoSize = true;
            FormDatePicker.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cal.Location = new Point(10, 10);   // best effect

            try
            {
                DateTime dt = Convert.ToDateTime(datestr);
                cal.SetDate(dt);   
            }
            catch { }

            DialogResult d=FormDatePicker.ShowDialog();

            if (d != DialogResult.Abort)
                datestr = cal.SelectionRange.Start.ToString("d");
        }

        void cal_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)    // Abort the DatetimePicker Form with ESC
            {
                FormDatePicker.DialogResult = DialogResult.Abort;
                FormDatePicker.Close();
            }
        }

        private void StartDatePicker_Click(object sender, EventArgs e)
        {
            String s = TxtStartDate.Text;
            DateTimePicker(ref s);
            TxtStartDate.Text = s;       
        }

        private void EndDatePicker_Click(object sender, EventArgs e)
        {
            String s = TxtEndDate.Text;
            DateTimePicker(ref s);
            TxtEndDate.Text = s;  
        }

        // only a valid date or no value : Always
        private void TxtDate_Validated(object sender, EventArgs e)
        {
            TextBox _sender = (TextBox)sender;
            if (_sender.Text == "Always") return;
            try
            {
                Convert.ToDateTime(_sender.Text);
            }
            catch
            {
                _sender.Text = "Always";
            }
        }
        private void TxtDate_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                TxtDate_Validated(sender, null);
        }

    }

    /*******************************************************/

    public class EditPropertyObjectReference : Form
    {
        public BacnetDeviceObjectPropertyReference ObjRef;
        public bool OutOK = false;
        public bool RefModified;

        public EditPropertyObjectReference(BacnetDeviceObjectPropertyReference ObjRef)
        {
            this.ObjRef = ObjRef;
            InitializeComponent();

            foreach (BacnetObjectTypes bot in Enum.GetValues(typeof(BacnetObjectTypes)))
                Reference_ObjType.Items.Add(new Enumcombo(bot.ToString().Substring(7), (uint)bot));

            for (int i = 0; i < Reference_ObjType.Items.Count; i++)
                if ((Reference_ObjType.Items[i] as Enumcombo).enumValue == (uint)ObjRef.objectIdentifier.type)
                {
                    Reference_ObjType.SelectedIndex = i;
                    break;
                }

            foreach (BacnetPropertyIds bpi in Enum.GetValues(typeof(BacnetPropertyIds)))
                Reference_Prop.Items.Add(new Enumcombo(bpi.ToString().Substring(5), (uint)bpi));

            for (int i = 0; i < Reference_Prop.Items.Count; i++)
                if ((Reference_Prop.Items[i] as Enumcombo).enumValue == (uint)ObjRef.propertyIdentifier)
                {
                    Reference_Prop.SelectedIndex = i;
                    break;
                }

            Reference_ObjId.Text = ObjRef.objectIdentifier.instance.ToString();

            if (ObjRef.deviceIndentifier.type == BacnetObjectTypes.OBJECT_DEVICE)
                Reference_Device.Text = ObjRef.deviceIndentifier.instance.ToString();
            if (ObjRef.arrayIndex != ASN1.BACNET_ARRAY_ALL)
                Reference_Array.Text = ObjRef.arrayIndex.ToString();
        }

        private void EditPropertyObjectReference_Load(object sender, System.EventArgs e)
        {
            this.SetDesktopLocation(Cursor.Position.X-this.Width/2, Cursor.Position.Y-this.Height/2);
        }

        private void OK_Click(object sender, EventArgs e)
        {

            try
            {
                BacnetObjectId? device = null;
                uint ArrayIdx = ASN1.BACNET_ARRAY_ALL;

                if (Reference_Device.Text != "")
                    device = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, Convert.ToUInt16(Reference_Device.Text));
                if (Reference_Array.Text != "")
                    ArrayIdx = Convert.ToUInt16(Reference_Array.Text);


                BacnetDeviceObjectPropertyReference newref = new BacnetDeviceObjectPropertyReference(
                    new BacnetObjectId((BacnetObjectTypes)(Reference_ObjType.SelectedItem as Enumcombo).enumValue, Convert.ToUInt16(Reference_ObjId.Text)),
                    (BacnetPropertyIds)(Reference_Prop.SelectedItem as Enumcombo).enumValue, device, ArrayIdx);

                if (!ObjRef.Equals(newref))
                {
                    ObjRef = newref;
                    RefModified = true;
                }
                OutOK = true;
                Close();
            }
            catch
            {
                Close();
            }
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

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Reference_ObjType = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.Reference_Prop = new System.Windows.Forms.ComboBox();
            this.Reference_Array = new System.Windows.Forms.TextBox();
            this.Reference_Device = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.Reference_ObjId = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            this.Text = "Object Property Reference";
            this.Load += new System.EventHandler(this.EditPropertyObjectReference_Load);
 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(131, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Device Id";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Array Index (1..*)";
            // 
            // Reference_ObjType
            // 
            this.Reference_ObjType.FormattingEnabled = true;
            this.Reference_ObjType.Location = new System.Drawing.Point(29, 29);
            this.Reference_ObjType.Name = "Reference_ObjType";
            this.Reference_ObjType.Size = new System.Drawing.Size(154, 21);
            this.Reference_ObjType.Sorted = true;
            this.Reference_ObjType.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(29, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Object reference";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(29, 64);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(46, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Property";
            // 
            // Reference_Prop
            // 
            this.Reference_Prop.FormattingEnabled = true;
            this.Reference_Prop.Location = new System.Drawing.Point(29, 80);
            this.Reference_Prop.Name = "Reference_Prop";
            this.Reference_Prop.Size = new System.Drawing.Size(210, 21);
            this.Reference_Prop.TabIndex = 5;
            // 
            // Reference_Array
            // 
            this.Reference_Array.Location = new System.Drawing.Point(33, 44);
            this.Reference_Array.Name = "Reference_Array";
            this.Reference_Array.Size = new System.Drawing.Size(47, 20);
            this.Reference_Array.TabIndex = 6;
            // 
            // Reference_Device
            // 
            this.Reference_Device.Location = new System.Drawing.Point(134, 44);
            this.Reference_Device.Name = "Reference_Device";
            this.Reference_Device.Size = new System.Drawing.Size(42, 20);
            this.Reference_Device.TabIndex = 7;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(78, 227);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(121, 31);
            this.button1.TabIndex = 8;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.OK_Click);
            // 
            // Reference_ObjId
            // 
            this.Reference_ObjId.Location = new System.Drawing.Point(205, 29);
            this.Reference_ObjId.Name = "Reference_ObjId";
            this.Reference_ObjId.Size = new System.Drawing.Size(34, 20);
            this.Reference_ObjId.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(189, 32);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(10, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = ":";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Reference_Device);
            this.groupBox1.Controls.Add(this.Reference_Array);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(29, 125);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(210, 84);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Optional";
            // 
            // EditObjectReference
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(267, 271);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.Reference_ObjId);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Reference_Prop);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.Reference_ObjType);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditObjectReference";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox Reference_ObjType;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox Reference_Prop;
        private System.Windows.Forms.TextBox Reference_Array;
        private System.Windows.Forms.TextBox Reference_Device;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox Reference_ObjId;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox1;
    }

    public class Enumcombo
    {
        public String Name;
        public uint enumValue;
        public Enumcombo(String Name, uint enumValue)
        {
            this.Name = Name;
            this.enumValue = enumValue;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
