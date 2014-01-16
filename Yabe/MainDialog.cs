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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO.BACnet;

namespace BACNetExplorer
{
    public partial class MainDialog : Form
    {
        private Dictionary<BacnetClient, BacnetLine> m_devices = new Dictionary<BacnetClient, BacnetLine>();
        private Dictionary<string, ListViewItem> m_subscription_index = new Dictionary<string, ListViewItem>();
        private uint m_next_subscription_id = 0;

        private class BacnetLine
        {
            public BacnetClient Line;
            public List<KeyValuePair<BACNET_ADDRESS, uint>> Devices = new List<KeyValuePair<BACNET_ADDRESS,uint>>();
            public HashSet<byte> mstp_sources_seen = new HashSet<byte>();
            public HashSet<byte> mstp_pfm_destinations_seen = new HashSet<byte>();
            public BacnetLine(BacnetClient comm)
            {
                Line = comm;
            }
        }

        public MainDialog()
        {
            InitializeComponent();
            Trace.Listeners.Add(new MyTraceListener(this));
            m_DeviceTree.ExpandAll();

            //load splitter setup
            try
            {
                if (Properties.Settings.Default.FormSize != new Size(0, 0))
                    this.Size = Properties.Settings.Default.FormSize;
                FormWindowState state = (FormWindowState)Enum.Parse(typeof(FormWindowState), Properties.Settings.Default.FormState);
                if (state != FormWindowState.Minimized)
                    this.WindowState = state;
                if (Properties.Settings.Default.SplitterButtom != -1)
                    m_SplitContainerButtom.SplitterDistance = Properties.Settings.Default.SplitterButtom;
                if (Properties.Settings.Default.SplitterLeft != -1)
                    m_SplitContainerLeft.SplitterDistance = Properties.Settings.Default.SplitterLeft;
                if (Properties.Settings.Default.SplitterRight != -1)
                    m_SplitContainerRight.SplitterDistance = Properties.Settings.Default.SplitterRight;
            }
            catch
            {
                //ignore
            }
        }

        private static string ConvertToText(IList<BACNET_VALUE> values)
        {
            if (values == null)
                return "[null]";
            else if (values.Count == 0)
                return "";
            else if (values.Count == 1)
                return values[0].Value.ToString();
            else
            {
                string ret = "{";
                foreach (BACNET_VALUE value in values)
                    ret += value.Value.ToString() + ",";
                ret = ret.Substring(0, ret.Length - 1);
                ret += "}";
                return ret;
            }
        }

        private void m_client_OnCOVNotification(BacnetClient sender, BACNET_ADDRESS adr, uint subscriberProcessIdentifier, BACNET_OBJECT_ID initiatingDeviceIdentifier, BACNET_OBJECT_ID monitoredObjectIdentifier, uint timeRemaining, byte invoke_id, bool need_confirm, ICollection<BACNET_PROPERTY_VALUE> values)
        {
            string sub_key = adr.ToString() + ":" + initiatingDeviceIdentifier.instance + ":" + subscriberProcessIdentifier;
            if (m_subscription_index.ContainsKey(sub_key))
            {
                this.BeginInvoke((MethodInvoker)delegate{
                try
                {
                    ListViewItem itm = m_subscription_index[sub_key];
                    foreach (BACNET_PROPERTY_VALUE value in values)
                    {
                        switch ((BACNET_PROPERTY_ID)value.property.propertyIdentifier)
                        {
                            case BACNET_PROPERTY_ID.PROP_PRESENT_VALUE:
                                itm.SubItems[3].Text = ConvertToText(value.value);
                                itm.SubItems[4].Text = DateTime.Now.ToString();
                                if (itm.SubItems[5].Text == "Not started") itm.SubItems[5].Text = "OK";
                                break;
                            case BACNET_PROPERTY_ID.PROP_STATUS_FLAGS:
                                if (value.value != null && value.value.Count > 0)
                                {
                                    BACNET_STATUS_FLAGS status = (BACNET_STATUS_FLAGS)((BACNET_BIT_STRING)value.value[0].Value).ConvertToInt();
                                    string status_text = "";
                                    if ((status & BACNET_STATUS_FLAGS.STATUS_FLAG_FAULT) == BACNET_STATUS_FLAGS.STATUS_FLAG_FAULT)
                                        status_text += "FAULT,";
                                    else if ((status & BACNET_STATUS_FLAGS.STATUS_FLAG_IN_ALARM) == BACNET_STATUS_FLAGS.STATUS_FLAG_IN_ALARM)
                                        status_text += "ALARM,";
                                    else if ((status & BACNET_STATUS_FLAGS.STATUS_FLAG_OUT_OF_SERVICE) == BACNET_STATUS_FLAGS.STATUS_FLAG_OUT_OF_SERVICE)
                                        status_text += "OOS,";
                                    else if ((status & BACNET_STATUS_FLAGS.STATUS_FLAG_OVERRIDDEN) == BACNET_STATUS_FLAGS.STATUS_FLAG_OVERRIDDEN)
                                        status_text += "OR,";
                                    if (status_text != "")
                                    {
                                        status_text = status_text.Substring(0, status_text.Length - 1);
                                        itm.SubItems[5].Text = status_text;
                                    }
                                    else
                                        itm.SubItems[5].Text = "OK";
                                }
                                break;
                            default:
                                Trace.TraceInformation("Got " + ((BACNET_PROPERTY_ID)value.property.propertyIdentifier).ToString() + " from device");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception in subcribed value: " + ex.Message);
                }
                });
            }

            //send ack
            if (need_confirm)
            {
                sender.SimpleAckResponse(adr, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_COV_NOTIFICATION, invoke_id);
            }
        }

        #region " Trace Listner "
        private class MyTraceListener : TraceListener
        {
            private MainDialog m_form;

            public MyTraceListener(MainDialog form)
                : base("MyListener")
            {
                m_form = form;
            }

            public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
            {
                if ((this.Filter != null) && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null)) return;

                ConsoleColor color;
                switch (eventType)
                {
                    case TraceEventType.Error:
                        color = ConsoleColor.Red;
                        break;
                    case TraceEventType.Warning:
                        color = ConsoleColor.Yellow;
                        break;
                    case TraceEventType.Information:
                        color = ConsoleColor.DarkGreen;
                        break;
                    default:
                        color = ConsoleColor.Gray;
                        break;
                }

                WriteColor(message + Environment.NewLine, color);
            }

            public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
            {
                if ((this.Filter != null) && !this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null)) return;

                ConsoleColor color;
                switch (eventType)
                {
                    case TraceEventType.Error:
                        color = ConsoleColor.Red;
                        break;
                    case TraceEventType.Warning:
                        color = ConsoleColor.Yellow;
                        break;
                    case TraceEventType.Information:
                        color = ConsoleColor.DarkGreen;
                        break;
                    default:
                        color = ConsoleColor.Gray;
                        break;
                }

                WriteColor(string.Format(format, args) + Environment.NewLine, color);
            }

            public override void Write(string message)
            {
                WriteColor(message, ConsoleColor.Gray);
            }
            public override void WriteLine(string message)
            {
                WriteColor(message + Environment.NewLine, ConsoleColor.Gray);
            }

            private void WriteColor(string message, ConsoleColor color)
            {
                if (!m_form.IsHandleCreated) return;

                m_form.m_LogText.BeginInvoke((MethodInvoker)delegate { m_form.m_LogText.AppendText(message); });
            }
        }
        #endregion

        private void MainDialog_Load(object sender, EventArgs e)
        {

        }

        private TreeNode FindCommTreeNode(BacnetClient comm)
        {
            foreach (TreeNode node in m_DeviceTree.Nodes[0].Nodes)
            {
                BacnetClient c = node.Tag as BacnetClient;
                if(c != null && c.Equals(comm)) return node;
            }
            return null;
        }

        private TreeNode FindCommTreeNode(IBacnetTransport transport)
        {
            foreach (TreeNode node in m_DeviceTree.Nodes[0].Nodes)
            {
                BacnetClient c = node.Tag as BacnetClient;
                if (c != null && c.Transport.Equals(transport)) return node;
            }
            return null;
        }

        void m_client_OnIam(BacnetClient sender, BACNET_ADDRESS adr, uint device_id, uint max_apdu, BACNET_SEGMENTATION segmentation, ushort vendor_id)
        {
            KeyValuePair<BACNET_ADDRESS, uint> new_entry = new KeyValuePair<BACNET_ADDRESS, uint>(adr, device_id);
            if (!m_devices.ContainsKey(sender)) return;
            if (!m_devices[sender].Devices.Contains(new_entry))
                m_devices[sender].Devices.Add(new_entry);
            else
                return;

            //update GUI
            this.BeginInvoke((MethodInvoker)delegate
            {
                TreeNode parent = FindCommTreeNode(sender);
                if (parent == null) return;

                //update existing (this can happen in MSTP)
                foreach (TreeNode s in parent.Nodes)
                {
                    KeyValuePair<BACNET_ADDRESS, uint>? entry = s.Tag as KeyValuePair<BACNET_ADDRESS, uint>?;
                    if(entry != null && entry.Value.Key.Equals(adr))
                    {
                        s.Text = new_entry.Key + " - " + new_entry.Value;
                        s.Tag = new_entry;
                        return;
                    }
                }

                //add
                TreeNode node = parent.Nodes.Add(new_entry.Key + " - " + new_entry.Value);
                node.ImageIndex = 2;
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = new_entry;
                m_DeviceTree.ExpandAll();
            });
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Yet Another Bacnet Explorer - Yabe\nVersion 1.0\nBy Morten Kvistgaard - Copyright 2014\n" +
                "\nReference: http://bacnet.sourceforge.net/" + 
                "\nReference: http://www.unified-automation.com/products/development-tools/uaexpert.html" +
                "\nReference: http://www.famfamfam.com/", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void addDevicesearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SearchDialog dlg = new SearchDialog();
            if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                BacnetClient comm = dlg.Result;
                m_devices.Add(comm, new BacnetLine(comm));

                //add to tree
                TreeNode node = m_DeviceTree.Nodes[0].Nodes.Add(comm.ToString());
                node.Tag = comm;
                switch (comm.Transport.Type)
                {
                    case AddressTypes.IP:
                        node.ImageIndex = 3;
                        break;
                    case AddressTypes.MSTP:
                        node.ImageIndex = 1;
                        break;
                    default:
                        node.ImageIndex = 8;
                        break;
                }
                node.SelectedImageIndex = node.ImageIndex;
                m_DeviceTree.ExpandAll();

                try
                {
                    //start BACnet
                    comm.OnIam += new BacnetClient.IamHandler(m_client_OnIam);
                    comm.OnCOVNotification += new BacnetClient.COVNotificationHandler(m_client_OnCOVNotification);
                    comm.Start();

                    //start search
                    if (comm.Transport.Type == AddressTypes.IP || (comm.Transport is BacnetMstpProtocolTransport && ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress != -1))
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                        {
                            for (int i = 0; i < comm.Retries; i++)
                            {
                                comm.WhoIs();
                                System.Threading.Thread.Sleep(comm.Timeout);
                            }
                        }, null);
                    }

                    //special MSTP auto discovery
                    if (comm.Transport is BacnetMstpProtocolTransport)
                    {
                        ((BacnetMstpProtocolTransport)comm.Transport).FrameRecieved += new BacnetMstpProtocolTransport.FrameRecievedHandler(MSTP_FrameRecieved);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Couldn't start Bacnet communication: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MSTP_FrameRecieved(BacnetMstpProtocolTransport sender, MSTP_FRAME_TYPE frame_type, byte destination_address, byte source_address, int msg_length)
        {
            BacnetLine device_line = null;
            foreach (BacnetLine l in m_devices.Values)
            {
                if (l.Line.Transport == sender)
                {
                    device_line = l;
                    break;
                }
            }
            if (device_line == null) return;
            lock (device_line.mstp_sources_seen)
            {
                if (!device_line.mstp_sources_seen.Contains(source_address))
                {
                    device_line.mstp_sources_seen.Add(source_address);

                    //find parent node
                    TreeNode parent = FindCommTreeNode(sender);

                    //find "free" node. The "free" node might have been added
                    TreeNode free_node = null;
                    foreach (TreeNode n in parent.Nodes)
                    {
                        if (n.Text == "free" + source_address)
                        {
                            free_node = n;
                            break;
                        }
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        TreeNode node = parent.Nodes.Add("device" + source_address);
                        node.ImageIndex = 2;
                        node.SelectedImageIndex = node.ImageIndex;
                        node.Tag = new KeyValuePair<BACNET_ADDRESS, uint>(new BACNET_ADDRESS(AddressTypes.MSTP, 0, new byte[]{source_address}), 0xFFFFFFFF);
                        if (free_node != null) free_node.Remove();
                        m_DeviceTree.ExpandAll();
                    });
                }
                if (frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_POLL_FOR_MASTER && !device_line.mstp_pfm_destinations_seen.Contains(destination_address) && sender.SourceAddress != destination_address)
                {
                    device_line.mstp_pfm_destinations_seen.Add(destination_address);
                    if (!device_line.mstp_sources_seen.Contains(destination_address))
                    {
                        TreeNode parent = FindCommTreeNode(sender);
                        this.Invoke((MethodInvoker)delegate
                        {
                            TreeNode node = parent.Nodes.Add("free" + destination_address);
                            node.ImageIndex = 9;
                            node.SelectedImageIndex = node.ImageIndex;
                            m_DeviceTree.ExpandAll();
                        });
                    }
                }
            }
        }

        private void m_SearchToolButton_Click(object sender, EventArgs e)
        {
            addDevicesearchToolStripMenuItem_Click(this, null);
        }

        private void RemoveSubscriptions(KeyValuePair<BACNET_ADDRESS, uint> device)
        {
            LinkedList<string> deletes = new LinkedList<string>();
            foreach (KeyValuePair<string, ListViewItem> entry in m_subscription_index)
            {
                SubscribtionIndex sub = (SubscribtionIndex)entry.Value.Tag;
                if (sub.adr == device.Key)
                {
                    m_SubscriptionView.Items.Remove(entry.Value);
                    deletes.AddLast(sub.sub_key);
                }
            }
            foreach (string sub_key in deletes)
                m_subscription_index.Remove(sub_key);
        }

        private void RemoveSubscriptions(BacnetClient comm)
        {
            LinkedList<string> deletes = new LinkedList<string>();
            foreach (KeyValuePair<string, ListViewItem> entry in m_subscription_index)
            {
                SubscribtionIndex sub = (SubscribtionIndex)entry.Value.Tag;
                if (sub.comm == comm)
                {
                    m_SubscriptionView.Items.Remove(entry.Value);
                    deletes.AddLast(sub.sub_key);
                }
            }
            foreach (string sub_key in deletes)
                m_subscription_index.Remove(sub_key);
        }

        private void removeDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;
            KeyValuePair<BACNET_ADDRESS, uint>? device_entry = m_DeviceTree.SelectedNode.Tag as KeyValuePair<BACNET_ADDRESS, uint>?;
            BacnetClient comm_entry = m_DeviceTree.SelectedNode.Tag as BacnetClient;
            if (device_entry != null)
            {
                if (MessageBox.Show(this, "Delete this device?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    m_devices[(BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag].Devices.Remove((KeyValuePair<BACNET_ADDRESS, uint>)device_entry);
                    if (m_devices[(BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag].Devices.Count == 0)
                        m_devices.Remove((BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag);
                    m_DeviceTree.Nodes.Remove(m_DeviceTree.SelectedNode);
                    RemoveSubscriptions((KeyValuePair<BACNET_ADDRESS, uint>)device_entry);
                }
            }
            else if (comm_entry != null)
            {
                if (MessageBox.Show(this, "Delete this transport?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    m_devices.Remove(comm_entry);
                    m_DeviceTree.Nodes.Remove(m_DeviceTree.SelectedNode);
                    RemoveSubscriptions(comm_entry);
                    comm_entry.Dispose();
                }
            }
        }

        private void m_RemoveToolButton_Click(object sender, EventArgs e)
        {
            removeDeviceToolStripMenuItem_Click(this, null);
        }

        private void m_DeviceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            KeyValuePair<BACNET_ADDRESS, uint>? entry = e.Node.Tag as KeyValuePair<BACNET_ADDRESS, uint>?;
            if (entry != null)
            {
                m_AddressSpaceTree.Nodes.Clear();   //clear

                BacnetClient comm = (BacnetClient)e.Node.Parent.Tag;
                BACNET_ADDRESS adr = entry.Value.Key;
                LinkedList<BACNET_VALUE> value_list;
                uint device_id = entry.Value.Value;

                //unconfigured MSTP?
                if (comm.Transport is BacnetMstpProtocolTransport && ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress == -1)
                {
                    if (MessageBox.Show("The MSTP transport is not yet configured. Would you like to set source_address now?", "Set Source Address", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No) return;

                    //find suggested address
                    byte address = 0;
                    BacnetLine line = m_devices[comm];
                    lock (line.mstp_sources_seen)
                    {
                        foreach (byte s in line.mstp_pfm_destinations_seen)
                        {
                            if (!line.mstp_sources_seen.Contains(s))
                            {
                                address = s;
                                break;
                            }
                        }
                    }

                    //display choice
                    SourceAddressDialog dlg = new SourceAddressDialog();
                    dlg.SourceAddress = address;
                    if( dlg.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
                    ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress = dlg.SourceAddress;
                }

                //update "address space"?
                try
                {
                    if (!comm.ReadPropertyRequest(adr, new BACNET_OBJECT_ID(BACNET_OBJECT_TYPE.OBJECT_DEVICE, device_id), BACNET_PROPERTY_ID.PROP_OBJECT_LIST, out value_list))
                    {
                        MessageBox.Show(this, "Couldn't fetch objects", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during read: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (BACNET_VALUE value in value_list)
                {
                    TreeNode node = m_AddressSpaceTree.Nodes.Add(((BACNET_OBJECT_ID)value.Value).type + ": " + ((BACNET_OBJECT_ID)value.Value).instance);
                    node.Tag = value;
                    BACNET_OBJECT_ID? id = value.Value as BACNET_OBJECT_ID?;
                    if (id != null)
                    {
                        switch (id.Value.type)
                        {
                            case BACNET_OBJECT_TYPE.OBJECT_DEVICE:
                                node.ImageIndex = 2;
                                break;
                            case BACNET_OBJECT_TYPE.OBJECT_FILE:
                                node.ImageIndex = 5;
                                break;
                            case BACNET_OBJECT_TYPE.OBJECT_ANALOG_INPUT:
                            case BACNET_OBJECT_TYPE.OBJECT_ANALOG_OUTPUT:
                            case BACNET_OBJECT_TYPE.OBJECT_ANALOG_VALUE:
                                node.ImageIndex = 6;
                                break;
                            case BACNET_OBJECT_TYPE.OBJECT_BINARY_INPUT:
                            case BACNET_OBJECT_TYPE.OBJECT_BINARY_OUTPUT:
                            case BACNET_OBJECT_TYPE.OBJECT_BINARY_VALUE:
                                node.ImageIndex = 7;
                                break;
                            default:
                                node.ImageIndex = 4;
                                break;
                        }
                    }
                    else
                        node.ImageIndex = 4;
                    node.SelectedImageIndex = node.ImageIndex;
                }
            }
        }

        private void addDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addDevicesearchToolStripMenuItem_Click(this, null);
        }

        private void removeDeviceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            removeDeviceToolStripMenuItem_Click(this, null);
        }

        private void UpdateGrid(TreeNode selected_node)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BACNET_ADDRESS, uint>)) return;
                KeyValuePair<BACNET_ADDRESS, uint> entry = (KeyValuePair<BACNET_ADDRESS, uint>)m_DeviceTree.SelectedNode.Tag;
                BACNET_ADDRESS adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                if (selected_node.Tag is BACNET_VALUE && ((BACNET_VALUE)selected_node.Tag).Value is BACNET_OBJECT_ID)
                {
                    m_DataGrid.SelectedObject = null;   //clear

                    //fetch properties. This might not be supported by ReadMultiple. (Too bad)
                    BACNET_OBJECT_ID object_id = (BACNET_OBJECT_ID)((BACNET_VALUE)selected_node.Tag).Value;
                    BACNET_PROPERTY_REFERENCE[] properties = new BACNET_PROPERTY_REFERENCE[] { new BACNET_PROPERTY_REFERENCE((uint)BACNET_PROPERTY_ID.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
                    ICollection<BACNET_PROPERTY_VALUE> multi_value_list;
                    try
                    {
                        if (!comm.ReadPropertyMultipleRequest(adr, object_id, properties, out multi_value_list))
                        {
                            MessageBox.Show(this, "Couldn't fetch properties", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Error during read: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    //update grid
                    Utilities.DynamicPropertyGridContainer bag = new Utilities.DynamicPropertyGridContainer();
                    foreach (BACNET_PROPERTY_VALUE p_value in multi_value_list)
                    {
                        BACNET_VALUE[] b_values = new BACNET_VALUE[p_value.value.Count];
                        p_value.value.CopyTo(b_values, 0);
                        object value = null;
                        if (b_values.Length > 1)
                        {
                            object[] arr = new object[b_values.Length];
                            for (int j = 0; j < arr.Length; j++)
                                arr[j] = b_values[j].Value;
                            value = arr;
                        }
                        else if (b_values.Length == 1)
                            value = b_values[0].Value;
                        bag.Add(new Utilities.CustomProperty(((BACNET_PROPERTY_ID)p_value.property.propertyIdentifier).ToString(), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag.ToString() : "", null, p_value.property));
                    }
                    m_DataGrid.SelectedObject = bag;
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void m_AddressSpaceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateGrid(e.Node);
        }

        private void m_DataGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BACNET_ADDRESS, uint>)) return;
                KeyValuePair<BACNET_ADDRESS, uint> entry = (KeyValuePair<BACNET_ADDRESS, uint>)m_DeviceTree.SelectedNode.Tag;
                BACNET_ADDRESS adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                //fetch object_id
                if (m_AddressSpaceTree.SelectedNode == null) return;
                else if (m_AddressSpaceTree.SelectedNode.Tag == null) return;
                else if (!(m_AddressSpaceTree.SelectedNode.Tag is BACNET_VALUE) || !(((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value is BACNET_OBJECT_ID)) return;
                BACNET_OBJECT_ID object_id = (BACNET_OBJECT_ID)((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value;

                //fetch property
                Utilities.CustomPropertyDescriptor c = (Utilities.CustomPropertyDescriptor)e.ChangedItem.PropertyDescriptor;
                BACNET_PROPERTY_REFERENCE property = (BACNET_PROPERTY_REFERENCE)c.CustomProperty.Tag;

                //new value
                object new_value = e.ChangedItem.Value;

                //convert to bacnet
                BACNET_VALUE[] b_value = null;
                try
                {
                    if (new_value != null && new_value.GetType().IsArray)
                    {
                        Array arr = (Array)new_value;
                        b_value = new BACNET_VALUE[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                            b_value[i] = new BACNET_VALUE(arr.GetValue(i));
                    }
                    else if (new_value != null)
                    {
                        b_value = new BACNET_VALUE[1];
                        b_value[0] = new BACNET_VALUE(new_value);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Couldn't convert property: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //write
                try
                {
                    if (!comm.WritePropertyRequest(adr, object_id, (BACNET_PROPERTY_ID)property.propertyIdentifier, b_value))
                    {
                        MessageBox.Show(this, "Couldn't write property", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during write: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //reload
                UpdateGrid(m_AddressSpaceTree.SelectedNode);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void downloadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BACNET_ADDRESS, uint>)) return;
                KeyValuePair<BACNET_ADDRESS, uint> entry = (KeyValuePair<BACNET_ADDRESS, uint>)m_DeviceTree.SelectedNode.Tag;
                BACNET_ADDRESS adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                //fetch object_id
                if (
                    m_AddressSpaceTree.SelectedNode == null ||
                    !(m_AddressSpaceTree.SelectedNode.Tag is BACNET_VALUE) ||
                    !(((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value is BACNET_OBJECT_ID) ||
                    !(((BACNET_OBJECT_ID)((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value).type == BACNET_OBJECT_TYPE.OBJECT_FILE))
                {
                    MessageBox.Show(this, "The marked object is not a file", "Not a file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                BACNET_OBJECT_ID object_id = (BACNET_OBJECT_ID)((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value;

                //where to store file?
                SaveFileDialog dlg = new SaveFileDialog();
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                string filename = dlg.FileName;

                //open file
                System.IO.FileStream fs = null;
                try
                {
                    fs = System.IO.File.OpenWrite(filename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Couldn't open file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int position = 0;
                uint count = (uint)comm.Transport.GetMaxBufferLength() - 7;
                bool end_of_file = false;
                byte[] buffer = new byte[count];
                try
                {
                    while (!end_of_file)
                    {
                        //read from device
                        if (!comm.ReadFileRequest(adr, object_id, ref position, ref count, out end_of_file, buffer, 0))
                        {
                            MessageBox.Show(this, "Couldn't read file", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        position += (int)count;

                        //write to file
                        fs.Write(buffer, 0, (int)count);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during read file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    fs.Close();
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            //information
            MessageBox.Show(this, "Done", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void uploadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BACNET_ADDRESS, uint>)) return;
                KeyValuePair<BACNET_ADDRESS, uint> entry = (KeyValuePair<BACNET_ADDRESS, uint>)m_DeviceTree.SelectedNode.Tag;
                BACNET_ADDRESS adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                //fetch object_id
                if (
                    m_AddressSpaceTree.SelectedNode == null ||
                    !(m_AddressSpaceTree.SelectedNode.Tag is BACNET_VALUE) ||
                    !(((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value is BACNET_OBJECT_ID) ||
                    !(((BACNET_OBJECT_ID)((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value).type == BACNET_OBJECT_TYPE.OBJECT_FILE))
                {
                    MessageBox.Show(this, "The marked object is not a file", "Not a file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                BACNET_OBJECT_ID object_id = (BACNET_OBJECT_ID)((BACNET_VALUE)m_AddressSpaceTree.SelectedNode.Tag).Value;

                //which file to upload?
                OpenFileDialog dlg = new OpenFileDialog();
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                string filename = dlg.FileName;

                //open file
                System.IO.FileStream fs = null;
                try
                {
                    fs = System.IO.File.OpenRead(filename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Couldn't open file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    int position = 0;
                    int count = (int)comm.Transport.GetMaxBufferLength() - 7;
                    byte[] buffer = new byte[count];
                    while (count > 0)
                    {
                        count = fs.Read(buffer, position, count);
                        if (count <= 0) continue;

                        if (!comm.WriteFileRequest(adr, object_id, ref position, count, buffer))
                        {
                            MessageBox.Show(this, "Couldn't write file", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        position += count;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during write file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    fs.Close();
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            //information
            MessageBox.Show(this, "Done", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void m_AddressSpaceTree_ItemDrag(object sender, ItemDragEventArgs e)
        {
            m_AddressSpaceTree.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void m_SubscriptionView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private string GetObjectName(BacnetClient comm, BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                LinkedList<BACNET_VALUE> value;
                if (!comm.ReadPropertyRequest(adr, object_id, BACNET_PROPERTY_ID.PROP_OBJECT_NAME, out value))
                    return "[Timed out]";
                return value.First.Value.Value.ToString();
            }
            catch (Exception ex)
            {
                return "[Error: " + ex.Message + " ]";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private class SubscribtionIndex
        {
            public BacnetClient comm;
            public BACNET_ADDRESS adr;
            public BACNET_OBJECT_ID object_id;
            public string sub_key;
            public uint subscribe_id;
            public SubscribtionIndex(BacnetClient comm, BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, string sub_key, uint subscribe_id)
            {
                this.comm = comm;
                this.adr = adr;
                this.object_id = object_id;
                this.sub_key = sub_key;
                this.subscribe_id = subscribe_id;
            }
        }

        private void m_SubscriptionView_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                //fetch end point
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BACNET_ADDRESS, uint>)) return;
                KeyValuePair<BACNET_ADDRESS, uint> entry = (KeyValuePair<BACNET_ADDRESS, uint>)m_DeviceTree.SelectedNode.Tag;
                BACNET_ADDRESS adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                //fetch object_id
                TreeNode node = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (node.Tag == null || !(node.Tag is BACNET_VALUE) || !(((BACNET_VALUE)node.Tag).Value is BACNET_OBJECT_ID)) return;
                BACNET_OBJECT_ID object_id = (BACNET_OBJECT_ID)((BACNET_VALUE)node.Tag).Value;

                //add to list
                ListViewItem itm = m_SubscriptionView.Items.Add(entry.Key + " - " + entry.Value);
                itm.SubItems.Add(object_id.ToString());
                itm.SubItems.Add(GetObjectName(comm, adr, object_id));   //name
                itm.SubItems.Add("");   //value
                itm.SubItems.Add("");   //time
                itm.SubItems.Add("Not started");   //status

                //add to index
                m_next_subscription_id++;
                string sub_key = adr.ToString() + ":" + entry.Value + ":" + m_next_subscription_id;
                itm.Tag = new SubscribtionIndex(comm, adr, object_id, sub_key, m_next_subscription_id);
                m_subscription_index.Add(sub_key, itm);

                //add to device
                try
                {
                    if (!comm.SubscribeCOVRequest(adr, object_id, m_next_subscription_id, false, false))      //should we use confirmed or unconfirmed events?
                    {
                        MessageBox.Show(this, "Couldn't subscribe", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during subscribe: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void MainDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //commit setup
                Properties.Settings.Default.SplitterButtom = m_SplitContainerButtom.SplitterDistance;
                Properties.Settings.Default.SplitterLeft = m_SplitContainerLeft.SplitterDistance;
                Properties.Settings.Default.SplitterRight = m_SplitContainerRight.SplitterDistance;
                Properties.Settings.Default.FormSize = this.Size;
                Properties.Settings.Default.FormState = this.WindowState.ToString();

                //save
                Properties.Settings.Default.Save();
            }
            catch
            {
                //ignore
            }
        }

        private void m_SubscriptionView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            if (m_SubscriptionView.SelectedItems.Count == 1)
            {
                ListViewItem itm = m_SubscriptionView.SelectedItems[0];
                SubscribtionIndex sub = (SubscribtionIndex)itm.Tag;
                if (m_subscription_index.ContainsKey(sub.sub_key))
                {
                    //remove from device
                    try
                    {
                        if (!sub.comm.SubscribeCOVRequest(sub.adr, sub.object_id, sub.subscribe_id, true, false))
                        {
                            MessageBox.Show(this, "Couldn't unsubscribe", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Couldn't delete subscribtion: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    //remove from interface
                    m_SubscriptionView.Items.Remove(itm);
                    m_subscription_index.Remove(sub.sub_key);
                }
            }
        }

        private void sendWhoIsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;
            else if (!(m_DeviceTree.SelectedNode.Tag is BacnetClient)) return;
            BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;

            //send
            comm.WhoIs();
        }
    }
}
