/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Morten Kvistgaard <mk@pch-engineering.dk>
*                    Frederic Chaxel <fchaxel@free.fr 
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
using System.Text;
using System.Windows.Forms;
using System.IO.BACnet;

namespace BacnetToDatabase
{
    public partial class Main : Form
    {
        private BacnetClient bacnet_client;

        public Main()
        {
            InitializeComponent();

            // Bacnet on UDP/IP/Ethernet
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false));
            bacnet_client.OnIam += new BacnetClient.IamHandler(bacnet_client_OnIam);
            bacnet_client.Start();
        }

        private void bacnet_client_OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ListViewItem itm = m_list.Items.Add(adr.ToString());
                itm.Tag = new KeyValuePair<BacnetAddress, uint>(adr, device_id);
                itm.SubItems.Add("");

                //read name
                IList<BacnetValue> values;
                if (bacnet_client.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_NAME, out values))
                    itm.SubItems[1].Text = (string)values[0].Value;

            }, null);
        }

        private void m_delayedStart_Tick(object sender, EventArgs e)
        {
            m_delayedStart.Enabled = false;
            SendSearch();
        }

        private void SendSearch()
        {
            bacnet_client.WhoIs();
        }

        private void m_SearchButton_Click(object sender, EventArgs e)
        {
            SendSearch();
        }

        private void m_TransferButton_Click(object sender, EventArgs e)
        {
            //get Bacnet selection
            if (m_list.SelectedItems.Count <= 0)
            {
                MessageBox.Show(this, "Please select a device", "No device selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            KeyValuePair<BacnetAddress, uint> device = (KeyValuePair<BacnetAddress, uint>)m_list.SelectedItems[0].Tag;

            //open database connection
            System.Data.SqlServerCe.SqlCeConnection con = new System.Data.SqlServerCe.SqlCeConnection(@"Data Source=..\..\SampleDatabase.sdf");
            con.Open();

            //retrieve list of 'properties'
            IList<BacnetValue> value_list;
            bacnet_client.ReadPropertyRequest(device.Key, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.Value), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list);
            LinkedList<BacnetObjectId> object_list = new LinkedList<BacnetObjectId>();
            foreach (BacnetValue value in value_list)
            {
                if (Enum.IsDefined(typeof(BacnetObjectTypes), ((BacnetObjectId)value.Value).Type))
                    object_list.AddLast((BacnetObjectId)value.Value);
            }

            //go through all 'properties' and store their 'present data' into a SQL database
            foreach (BacnetObjectId object_id in object_list)
            {
                //read all properties
                IList<BacnetValue> values = null;
                try
                {
                    if (!bacnet_client.ReadPropertyRequest(device.Key, object_id, BacnetPropertyIds.PROP_PRESENT_VALUE, out values))
                    {
                        MessageBox.Show(this, "Couldn't fetch 'present value' for object: " + object_id.ToString());
                        continue;
                    }
                }
                catch (Exception)
                {
                    //perhaps the 'present value' is non existing - ignore
                    continue;
                }

                //store in DB
                using (System.Data.SqlServerCe.SqlCeCommand com = new System.Data.SqlServerCe.SqlCeCommand("INSERT INTO SampleTable VALUES(@ObjectName,@PropertyId,@Value)", con))
                {
                    com.Parameters.AddWithValue("@ObjectName", object_id.ToString());
                    com.Parameters.AddWithValue("@PropertyId", values[0].Tag.ToString());
                    com.Parameters.AddWithValue("@Value", values[0].Value.ToString());
                    com.ExecuteNonQuery();
                }
            }

            //close DB
            con.Close();

            //done
            MessageBox.Show(this, "Done!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
