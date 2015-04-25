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
        BacnetClient comm; BacnetAddress adr; BacnetObjectId object_id;
        // Default value type here if no values are already present
        // Could be choosen somewhere by the user
        BacnetApplicationTags ScheduleType = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;

        public ScheduleDisplay(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            InitializeComponent();
            this.comm = comm;
            this.adr = adr;
            this.object_id = object_id;

            ReadEffectivePeriod();
            ReadEffectiveWeeklySchedule();

            ToolTip t1=new ToolTip();
            t1.AutomaticDelay = 0;
            t1.SetToolTip(TxtStartDate, "A wrong value set this to Always");
            ToolTip t2 = new ToolTip();
            t2.AutomaticDelay = 0;
            t2.SetToolTip(TxtEndDate, "A wrong value set this to Always"); 
        }

        // Read start and stop dates validity for the schedule
        private void ReadEffectivePeriod()
        {
            IList<BacnetValue> value;
            try
            {
                if (comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, out value))
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
            comm.RawEncodedDecodedPropertyConfirmedRequest(adr, object_id, BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, ref InOutBuffer);

        }
        
        // no test here if buffer is to small
        private void ManualEncodeAndSend()
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
            comm.RawEncodedDecodedPropertyConfirmedRequest(adr, object_id, BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, ref InOutBuffer);

        }

        private void Update_Click(object sender, EventArgs e)
        {
            WriteEffectivePeriod();
            ManualEncodeAndSend();
            ReadEffectiveWeeklySchedule();
        }

       private void ReadEffectiveWeeklySchedule()
        {
            Schedule.BeginUpdate();

            Schedule.Nodes.Clear();

            byte[] InOutBuffer = null;

            try
            {
                if (comm.RawEncodedDecodedPropertyConfirmedRequest(adr, object_id, BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, ref InOutBuffer))
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

        TreeNode mySelectedNode;

        private void Schedule_MouseDown(object sender, MouseEventArgs e)
        {
            mySelectedNode = Schedule.GetNodeAt(e.X, e.Y);
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

        // do not modify a Day Node
        private void modifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((mySelectedNode != null) && (mySelectedNode.Parent != null))
            {

                if (!mySelectedNode.IsEditing)
                {
                    Schedule.LabelEdit = true;
                    mySelectedNode.BeginEdit();
                }
            }
            else
                Schedule.LabelEdit = false;
        }

        // Do not delete A day, only a time schedule entry
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((mySelectedNode != null) && (mySelectedNode.Parent != null))
            {
                Schedule.Nodes.Remove(mySelectedNode);
                mySelectedNode = null;
            }
        }

        // Add a new entry at the right place
        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mySelectedNode != null)
            {
                TreeNode T = new TreeNode("00:00:00 = 0",1,1);

                if (mySelectedNode.Parent == null)
                {
                    mySelectedNode.Nodes.Add(T);
                    mySelectedNode.Expand();    // sometimes neeeded
                }
                else
                    mySelectedNode.Parent.Nodes.Add(T);

                // Modify mode
                mySelectedNode = T;
                modifyToolStripMenuItem_Click(null, null);
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

        private void ScheduleDisplay_Load(object sender, EventArgs e)
        {

        }

    }
}
