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
using System.IO;

namespace Yabe
{
    public partial class MainDialog : Form
    {
        private Dictionary<BacnetClient, BacnetDeviceLine> m_devices = new Dictionary<BacnetClient, BacnetDeviceLine>();
        private Dictionary<string, ListViewItem> m_subscription_index = new Dictionary<string, ListViewItem>();
        private uint m_next_subscription_id = 0;

        private class BacnetDeviceLine
        {
            public BacnetClient Line;
            public List<KeyValuePair<BacnetAddress, uint>> Devices = new List<KeyValuePair<BacnetAddress, uint>>();
            public HashSet<byte> mstp_sources_seen = new HashSet<byte>();
            public HashSet<byte> mstp_pfm_destinations_seen = new HashSet<byte>();
            public BacnetDeviceLine(BacnetClient comm)
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
                if (Properties.Settings.Default.GUI_FormSize != new Size(0, 0))
                    this.Size = Properties.Settings.Default.GUI_FormSize;
                FormWindowState state = (FormWindowState)Enum.Parse(typeof(FormWindowState), Properties.Settings.Default.GUI_FormState);
                if (state != FormWindowState.Minimized)
                    this.WindowState = state;
                if (Properties.Settings.Default.GUI_SplitterButtom != -1)
                    m_SplitContainerButtom.SplitterDistance = Properties.Settings.Default.GUI_SplitterButtom;
                if (Properties.Settings.Default.GUI_SplitterLeft != -1)
                    m_SplitContainerLeft.SplitterDistance = Properties.Settings.Default.GUI_SplitterLeft;
                if (Properties.Settings.Default.GUI_SplitterRight != -1)
                    m_SplitContainerRight.SplitterDistance = Properties.Settings.Default.GUI_SplitterRight;
            }
            catch
            {
                //ignore
            }
        }

        private static string ConvertToText(IList<BacnetValue> values)
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
                foreach (BacnetValue value in values)
                    ret += value.Value.ToString() + ",";
                ret = ret.Substring(0, ret.Length - 1);
                ret += "}";
                return ret;
            }
        }

        private void SetSubscriptionStatus(ListViewItem itm, string status)
        {
            if (itm.SubItems[5].Text == status) return;
            itm.SubItems[5].Text = status;
            itm.SubItems[4].Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void OnCOVNotification(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool need_confirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments)
        {
            string sub_key = adr.ToString() + ":" + initiatingDeviceIdentifier.instance + ":" + subscriberProcessIdentifier;
            if (m_subscription_index.ContainsKey(sub_key))
            {
                this.BeginInvoke((MethodInvoker)delegate{
                try
                {
                    ListViewItem itm = m_subscription_index[sub_key];
                    foreach (BacnetPropertyValue value in values)
                    {
                        switch ((BacnetPropertyIds)value.property.propertyIdentifier)
                        {
                            case BacnetPropertyIds.PROP_PRESENT_VALUE:
                                itm.SubItems[3].Text = ConvertToText(value.value);
                                itm.SubItems[4].Text = DateTime.Now.ToString("HH:mm:ss");
                                if (itm.SubItems[5].Text == "Not started") itm.SubItems[5].Text = "OK";
                                break;
                            case BacnetPropertyIds.PROP_STATUS_FLAGS:
                                if (value.value != null && value.value.Count > 0)
                                {
                                    BacnetStatusFlags status = (BacnetStatusFlags)((BacnetBitString)value.value[0].Value).ConvertToInt();
                                    string status_text = "";
                                    if ((status & BacnetStatusFlags.STATUS_FLAG_FAULT) == BacnetStatusFlags.STATUS_FLAG_FAULT)
                                        status_text += "FAULT,";
                                    else if ((status & BacnetStatusFlags.STATUS_FLAG_IN_ALARM) == BacnetStatusFlags.STATUS_FLAG_IN_ALARM)
                                        status_text += "ALARM,";
                                    else if ((status & BacnetStatusFlags.STATUS_FLAG_OUT_OF_SERVICE) == BacnetStatusFlags.STATUS_FLAG_OUT_OF_SERVICE)
                                        status_text += "OOS,";
                                    else if ((status & BacnetStatusFlags.STATUS_FLAG_OVERRIDDEN) == BacnetStatusFlags.STATUS_FLAG_OVERRIDDEN)
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
                                //got something else? ignore it
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
                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, invoke_id);
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
            //start renew timer at half lifetime
            int lifetime = (int)Properties.Settings.Default.Subscriptions_Lifetime;
            if (lifetime > 0)
            {
                m_subscriptionRenewTimer.Interval = (lifetime / 2) * 1000;
                m_subscriptionRenewTimer.Enabled = true;
            }

            //display nice floats in propertygrid
            Utilities.CustomSingleConverter.DontDisplayExactFloats = true;
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

        void OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            KeyValuePair<BacnetAddress, uint> new_entry = new KeyValuePair<BacnetAddress, uint>(adr, device_id);
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
                    KeyValuePair<BacnetAddress, uint>? entry = s.Tag as KeyValuePair<BacnetAddress, uint>?;
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
            MessageBox.Show(this, "Yet Another Bacnet Explorer - Yabe\nVersion " + this.GetType().Assembly.GetName().Version + "\nBy Morten Kvistgaard - Copyright 2014\n" +
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
                m_devices.Add(comm, new BacnetDeviceLine(comm));

                //add to tree
                TreeNode node = m_DeviceTree.Nodes[0].Nodes.Add(comm.ToString());
                node.Tag = comm;
                switch (comm.Transport.Type)
                {
                    case BacnetAddressTypes.IP:
                        node.ImageIndex = 3;
                        break;
                    case BacnetAddressTypes.MSTP:
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
                    comm.ProposedWindowSize = Properties.Settings.Default.Segments_ProposedWindowSize;
                    comm.Retries = (int)Properties.Settings.Default.DefaultRetries;
                    comm.Timeout = (int)Properties.Settings.Default.DefaultTimeout;
                    comm.MaxSegments = BacnetClient.GetSegmentsCount(Properties.Settings.Default.Segments_Max);
                    comm.OnIam += new BacnetClient.IamHandler(OnIam);
                    comm.OnCOVNotification += new BacnetClient.COVNotificationHandler(OnCOVNotification);
                    comm.Start();

                    //start search
                    if (comm.Transport.Type == BacnetAddressTypes.IP || (comm.Transport is BacnetMstpProtocolTransport && ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress != -1) || comm.Transport.Type == BacnetAddressTypes.PTP)
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
                    m_devices.Remove(comm);
                    node.Remove();
                    MessageBox.Show(this, "Couldn't start Bacnet communication: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MSTP_FrameRecieved(BacnetMstpProtocolTransport sender, BacnetMstpFrameTypes frame_type, byte destination_address, byte source_address, int msg_length)
        {
            try
            {
                if (this.IsDisposed) return;
                BacnetDeviceLine device_line = null;
                foreach (BacnetDeviceLine l in m_devices.Values)
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

                        //update gui
                        this.Invoke((MethodInvoker)delegate
                        {
                            TreeNode node = parent.Nodes.Add("device" + source_address);
                            node.ImageIndex = 2;
                            node.SelectedImageIndex = node.ImageIndex;
                            node.Tag = new KeyValuePair<BacnetAddress, uint>(new BacnetAddress(BacnetAddressTypes.MSTP, 0, new byte[] { source_address }), 0xFFFFFFFF);
                            if (free_node != null) free_node.Remove();
                            m_DeviceTree.ExpandAll();
                        });

                        //detect collision
                        if (source_address == sender.SourceAddress)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                MessageBox.Show(this, "Selected source address seems to be occupied!", "Collision detected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            });
                        }
                    }
                    if (frame_type == BacnetMstpFrameTypes.FRAME_TYPE_POLL_FOR_MASTER && !device_line.mstp_pfm_destinations_seen.Contains(destination_address) && sender.SourceAddress != destination_address)
                    {
                        device_line.mstp_pfm_destinations_seen.Add(destination_address);
                        if (!device_line.mstp_sources_seen.Contains(destination_address) && Properties.Settings.Default.MSTP_DisplayFreeAddresses)
                        {
                            TreeNode parent = FindCommTreeNode(sender);
                            if (this.IsDisposed) return;
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
            catch (ObjectDisposedException)
            {
                //we're closing down ... ignore
            }
        }

        private void m_SearchToolButton_Click(object sender, EventArgs e)
        {
            addDevicesearchToolStripMenuItem_Click(this, null);
        }

        private void RemoveSubscriptions(KeyValuePair<BacnetAddress, uint> device)
        {
            LinkedList<string> deletes = new LinkedList<string>();
            foreach (KeyValuePair<string, ListViewItem> entry in m_subscription_index)
            {
                Subscribtion sub = (Subscribtion)entry.Value.Tag;
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
                Subscribtion sub = (Subscribtion)entry.Value.Tag;
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
            KeyValuePair<BacnetAddress, uint>? device_entry = m_DeviceTree.SelectedNode.Tag as KeyValuePair<BacnetAddress, uint>?;
            BacnetClient comm_entry = m_DeviceTree.SelectedNode.Tag as BacnetClient;
            if (device_entry != null)
            {
                if (MessageBox.Show(this, "Delete this device?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    m_devices[(BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag].Devices.Remove((KeyValuePair<BacnetAddress, uint>)device_entry);
                    if (m_devices[(BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag].Devices.Count == 0)
                        m_devices.Remove((BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag);
                    m_DeviceTree.Nodes.Remove(m_DeviceTree.SelectedNode);
                    RemoveSubscriptions((KeyValuePair<BacnetAddress, uint>)device_entry);
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

        private void SetNodeIcon(BacnetObjectTypes object_type, TreeNode node)
        {
            switch (object_type)
            {
                case BacnetObjectTypes.OBJECT_DEVICE:
                    node.ImageIndex = 2;
                    break;
                case BacnetObjectTypes.OBJECT_FILE:
                    node.ImageIndex = 5;
                    break;
                case BacnetObjectTypes.OBJECT_ANALOG_INPUT:
                case BacnetObjectTypes.OBJECT_ANALOG_OUTPUT:
                case BacnetObjectTypes.OBJECT_ANALOG_VALUE:
                    node.ImageIndex = 6;
                    break;
                case BacnetObjectTypes.OBJECT_BINARY_INPUT:
                case BacnetObjectTypes.OBJECT_BINARY_OUTPUT:
                case BacnetObjectTypes.OBJECT_BINARY_VALUE:
                    node.ImageIndex = 7;
                    break;
                case BacnetObjectTypes.OBJECT_GROUP:
                    node.ImageIndex = 10;
                    break;
                case BacnetObjectTypes.OBJECT_STRUCTURED_VIEW:
                    node.ImageIndex = 11;
                    break;
                default:
                    node.ImageIndex = 4;
                    break;
            }
            node.SelectedImageIndex = node.ImageIndex;
        }

        private void AddObjectEntry(BacnetClient comm, BacnetAddress adr, string name, BacnetObjectId object_id, TreeNodeCollection nodes)
        {
            if (string.IsNullOrEmpty(name)) name = object_id.ToString();
            TreeNode node = nodes.Add(name);
            node.Tag = object_id;

            //icon
            SetNodeIcon(object_id.type, node);

            //fetch sub properties
            if (object_id.type == BacnetObjectTypes.OBJECT_GROUP)
                FetchGroupProperties(comm, adr, object_id, node.Nodes);
            else if (object_id.type == BacnetObjectTypes.OBJECT_STRUCTURED_VIEW)
                FetchViewObjects(comm, adr, object_id, node.Nodes);
        }

        private IList<BacnetValue> FetchStructuredObjects(BacnetClient comm, BacnetAddress adr, uint device_id)
        {
            IList<BacnetValue> ret;
            int old_reties = comm.Retries;
            try
            {
                comm.Retries = 1;       //only do 1 retry
                if (!comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_STRUCTURED_OBJECT_LIST, out ret))
                {
                    Trace.TraceInformation("Didn't get response from 'Structured Object List'");
                    return null;
                }
                return ret == null || ret.Count == 0 ? null : ret;
            }
            catch (Exception)
            {
                Trace.TraceInformation("Got exception from 'Structured Object List'");
                return null;
            }
            finally
            {
                comm.Retries = old_reties;
            }
        }

        private void AddObjectListOneByOneAsync(BacnetClient comm, BacnetAddress adr, uint device_id, uint count)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                        {
                            IList<BacnetValue> value_list;
                            try
                            {
                                for (int i = 1; i <= count; i++)
                                {
                                    value_list = null;
                                    if (!comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list, 0, (uint)i))
                                    {
                                        MessageBox.Show("Couldn't fetch object list index", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return;
                                    }

                                    //add to tree
                                    foreach (BacnetValue value in value_list)
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            AddObjectEntry(comm, adr, null, (BacnetObjectId)value.Value, m_AddressSpaceTree.Nodes);
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error during read: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        });
        }

        private void m_DeviceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            KeyValuePair<BacnetAddress, uint>? entry = e.Node.Tag as KeyValuePair<BacnetAddress, uint>?;
            if (entry != null)
            {
                m_AddressSpaceTree.Nodes.Clear();   //clear

                BacnetClient comm = (BacnetClient)e.Node.Parent.Tag;
                BacnetAddress adr = entry.Value.Key;
                uint device_id = entry.Value.Value;

                //unconfigured MSTP?
                if (comm.Transport is BacnetMstpProtocolTransport && ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress == -1)
                {
                    if (MessageBox.Show("The MSTP transport is not yet configured. Would you like to set source_address now?", "Set Source Address", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No) return;

                    //find suggested address
                    byte address = 0xFF;
                    BacnetDeviceLine line = m_devices[comm];
                    lock (line.mstp_sources_seen)
                    {
                        foreach (byte s in line.mstp_pfm_destinations_seen)
                        {
                            if (s < address && !line.mstp_sources_seen.Contains(s))
                                address = s;
                        }
                    }

                    //display choice
                    SourceAddressDialog dlg = new SourceAddressDialog();
                    dlg.SourceAddress = address;
                    if( dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel) return;
                    ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress = dlg.SourceAddress;
                    Application.DoEvents();     //let the interface relax
                }

                //update "address space"?
                this.Cursor = Cursors.WaitCursor;
                Application.DoEvents();
                int old_timeout = comm.Timeout;
                IList<BacnetValue> value_list = null;
                try
                {
                    //fetch structured view if possible
                    if(Properties.Settings.Default.DefaultPreferStructuredView) 
                        value_list = FetchStructuredObjects(comm, adr, device_id);

                    //fetch normal list
                    if (value_list == null)
                    {
                        try
                        {
                            if (!comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list))
                            {
                                Trace.TraceWarning("Didn't get response from 'Object List'");
                                value_list = null;
                            }
                        }
                        catch (Exception)
                        {
                            Trace.TraceWarning("Got exception from 'Object List'");
                            value_list = null;
                        }
                    }

                    //fetch list one-by-one
                    if (value_list == null)
                    {
                        try
                        {
                            //fetch object list count
                            if (!comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list, 0, 0))
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

                        if (value_list != null && value_list.Count == 1 && value_list[0].Value is uint)
                        {
                            uint list_count = (uint)value_list[0].Value;
                            AddObjectListOneByOneAsync(comm, adr, device_id, list_count);
                            return;
                        }
                        else
                        {
                            MessageBox.Show(this, "Couldn't read 'Object List' count", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    //add to tree
                    foreach (BacnetValue value in value_list)
                    {
                        AddObjectEntry(comm, adr, null, (BacnetObjectId)value.Value, m_AddressSpaceTree.Nodes);
                    }
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void FetchViewObjects(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, TreeNodeCollection nodes)
        {
            try
            {
                IList<BacnetValue> values;
                if (comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_SUBORDINATE_LIST, out values))
                {
                    foreach (BacnetValue value in values)
                    {
                        if (value.Value is BacnetObjectId)
                        {
                            AddObjectEntry(comm, adr, null, (BacnetObjectId)value.Value, nodes);
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning("Couldn't fetch view members");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Couldn't fetch view members: " + ex.Message);
            }
        }

        private void FetchGroupProperties(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, TreeNodeCollection nodes)
        {
            try
            {
                IList<BacnetValue> values;
                if (comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS, out values))
                {
                    foreach (BacnetValue value in values)
                    {
                        if (value.Value is BacnetReadAccessSpecification)
                        {
                            BacnetReadAccessSpecification spec = (BacnetReadAccessSpecification)value.Value;
                            foreach (BacnetPropertyReference p in spec.propertyReferences)
                            {
                                AddObjectEntry(comm, adr, spec.objectIdentifier.ToString() + ":" + ((BacnetPropertyIds)p.propertyIdentifier).ToString(), spec.objectIdentifier, nodes);
                            }
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning("Couldn't fetch group members");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Couldn't fetch group members: " + ex.Message);
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

        private static string GetNiceName(BacnetPropertyIds property)
        {
            string name = property.ToString();
            if(name.StartsWith("PROP_")) name = name.Substring(5);
            name = name.Replace('_', ' ');
            name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
            return name;
        }

        private bool ReadProperty(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, ref IList<BacnetPropertyValue> values, uint array_index = System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
        {
            BacnetPropertyValue new_entry = new BacnetPropertyValue();
            new_entry.property = new BacnetPropertyReference((uint)property_id, array_index);
            IList<BacnetValue> value;
            try
            {
                if (!comm.ReadPropertyRequest(adr, object_id, property_id, out value, 0, array_index))
                    return false;     //ignore
            }
            catch
            {
                return false;         //ignore
            }
            new_entry.value = value;
            values.Add(new_entry);
            return true;
        }

        private bool ReadAllPropertiesBySingle(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, out IList<BacnetReadAccessResult> value_list)
        {
            value_list = null;

            IList<BacnetPropertyValue> values = new List<BacnetPropertyValue>();

            int old_retries = comm.Retries;
            comm.Retries = 1;       //we don't want to spend too much time on non existing properties
            try
            {
                if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE)
                {
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_NAME, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_TYPE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_SYSTEM_STATUS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_VENDOR_NAME, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_VENDOR_IDENTIFIER, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_MODEL_NAME, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_FIRMWARE_REVISION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_APPLICATION_SOFTWARE_VERSION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LOCATION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DESCRIPTION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROTOCOL_VERSION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROTOCOL_REVISION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROTOCOL_SERVICES_SUPPORTED, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED, ref values);
                    if (!ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_LIST, ref values))
                    {
                        //read object list count instead
                        ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_LIST, ref values, 0);
                    }
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_MAX_APDU_LENGTH_ACCEPTED, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_SEGMENTATION_SUPPORTED, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_MAX_SEGMENTS_ACCEPTED, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_VT_CLASSES_SUPPORTED, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_ACTIVE_VT_SESSIONS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LOCAL_DATE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LOCAL_DATE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_UTC_OFFSET, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DAYLIGHT_SAVINGS_STATUS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_APDU_SEGMENT_TIMEOUT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_APDU_TIMEOUT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_NUMBER_OF_APDU_RETRIES, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LIST_OF_SESSION_KEYS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_TIME_SYNCHRONIZATION_RECIPIENTS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_MAX_MASTER, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_MAX_INFO_FRAMES, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DEVICE_ADDRESS_BINDING, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DATABASE_REVISION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_CONFIGURATION_FILES, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LAST_RESTORE_TIME, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_BACKUP_FAILURE_TIMEOUT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_ACTIVE_COV_SUBSCRIPTIONS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_SLAVE_PROXY_ENABLE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_MANUAL_SLAVE_ADDRESS_BINDING, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_AUTO_SLAVE_DISCOVERY, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_SLAVE_ADDRESS_BINDING, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROFILE_NAME, ref values);
                }
                else if (object_id.type == BacnetObjectTypes.OBJECT_ANALOG_VALUE || object_id.type == BacnetObjectTypes.OBJECT_ANALOG_INPUT || object_id.type == BacnetObjectTypes.OBJECT_ANALOG_OUTPUT)
                {
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_NAME, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_TYPE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_SYSTEM_STATUS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DESCRIPTION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PRESENT_VALUE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_EVENT_STATE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_RELIABILITY, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OUT_OF_SERVICE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_UNITS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PRIORITY_ARRAY, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_RELINQUISH_DEFAULT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_COV_INCREMENT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_TIME_DELAY, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_NOTIFICATION_CLASS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_HIGH_LIMIT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LOW_LIMIT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DEADBAND, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LIMIT_ENABLE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_EVENT_ENABLE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_ACKED_TRANSITIONS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_NOTIFY_TYPE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_EVENT_TIME_STAMPS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROFILE_NAME, ref values);
                }
                else if (object_id.type == BacnetObjectTypes.OBJECT_OCTETSTRING_VALUE)
                {
                    /* I'm not these are these are the right ones */
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_NAME, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_TYPE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_SYSTEM_STATUS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DESCRIPTION, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PRESENT_VALUE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_EVENT_STATE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_RELIABILITY, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OUT_OF_SERVICE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_UNITS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PRIORITY_ARRAY, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_RELINQUISH_DEFAULT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_COV_INCREMENT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_TIME_DELAY, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_NOTIFICATION_CLASS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_HIGH_LIMIT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LOW_LIMIT, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_DEADBAND, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_LIMIT_ENABLE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_EVENT_ENABLE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_ACKED_TRANSITIONS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_NOTIFY_TYPE, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_EVENT_TIME_STAMPS, ref values);
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROFILE_NAME, ref values);
                }
                else
                    throw new NotImplementedException("Haven't got to this yet");
            }
            finally
            {
                comm.Retries = old_retries;
            }

            value_list = new BacnetReadAccessResult[] { new BacnetReadAccessResult(object_id, values) };
            return true;
        }

        private void UpdateGrid(TreeNode selected_node)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                BacnetAddress adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                if (selected_node.Tag is BacnetObjectId)
                {
                    m_DataGrid.SelectedObject = null;   //clear

                    BacnetObjectId object_id = (BacnetObjectId)selected_node.Tag;
                    BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
                    IList<BacnetReadAccessResult> multi_value_list;
                    try
                    {
                        //fetch properties. This might not be supported (ReadMultiple) or the response might be too long.
                        if (!comm.ReadPropertyMultipleRequest(adr, object_id, properties, out multi_value_list))
                        {
                            MessageBox.Show(this, "Couldn't fetch properties", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        Trace.TraceWarning("Couldn't perform ReadPropertyMultiple ... Trying ReadProperty instead");
                        Application.DoEvents();
                        try
                        {
                            //fetch properties with single calls
                            if (!ReadAllPropertiesBySingle(comm, adr, object_id, out multi_value_list))
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
                    }

                    //update grid
                    Utilities.DynamicPropertyGridContainer bag = new Utilities.DynamicPropertyGridContainer();
                    foreach (BacnetPropertyValue p_value in multi_value_list[0].values)
                    {
                        object value = null;
                        BacnetValue[] b_values = null;
                        if (p_value.value != null)
                        {
                            b_values = new BacnetValue[p_value.value.Count];
                            p_value.value.CopyTo(b_values, 0);
                            if (b_values.Length > 1)
                            {
                                object[] arr = new object[b_values.Length];
                                for (int j = 0; j < arr.Length; j++)
                                    arr[j] = b_values[j].Value;
                                value = arr;
                            }
                            else if (b_values.Length == 1)
                                value = b_values[0].Value;
                        }
                        else
                            b_values = new BacnetValue[0];

                        // Modif FC
                        switch ((BacnetPropertyIds)p_value.property.propertyIdentifier)
                        {
                            // PROP_RELINQUISH_DEFAULT can be write to null value
                            case BacnetPropertyIds.PROP_PRESENT_VALUE:
                                // change to the related nullable type
                                Type t = null;
                                try
                                {
                                    t = value.GetType();
                                    t = Type.GetType("System.Nullable`1[" + value.GetType().FullName + "]");
                                }
                                catch { }
                                bag.Add(new Utilities.CustomProperty(GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, t != null ? t : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                                break;
                            // PROP_UNITS : Unit nice name
                            case BacnetPropertyIds.PROP_UNITS:
                                string str = GetNiceUnitName((BacnetUnitsId)Convert.ToInt32(value));
                                bag.Add(new Utilities.CustomProperty(GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), str, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                                break;
                            default:
                                bag.Add(new Utilities.CustomProperty(GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                                break;
                        }

                    }
                    m_DataGrid.SelectedObject = bag;
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private static string GetNiceUnitName(BacnetUnitsId Unit)
        {
            string unitStr = Unit.ToString();
            if (unitStr.StartsWith("UNITS_")) unitStr = unitStr.Substring(6);
            unitStr = unitStr.Replace('_', ' ');
            unitStr = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(unitStr.ToLower());
            return unitStr;
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
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                BacnetAddress adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                //fetch object_id
                if (m_AddressSpaceTree.SelectedNode == null) return;
                else if (m_AddressSpaceTree.SelectedNode.Tag == null) return;
                else if (!(m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId)) return;
                BacnetObjectId object_id = (BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag;

                //fetch property
                Utilities.CustomPropertyDescriptor c = (Utilities.CustomPropertyDescriptor)e.ChangedItem.PropertyDescriptor;
                BacnetPropertyReference property = (BacnetPropertyReference)c.CustomProperty.Tag;

                //new value
                object new_value = e.ChangedItem.Value;

                //convert to bacnet
                BacnetValue[] b_value = null;
                try
                {
                    if (new_value != null && new_value.GetType().IsArray)
                    {
                        Array arr = (Array)new_value;
                        b_value = new BacnetValue[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                            b_value[i] = new BacnetValue(arr.GetValue(i));
                    }
                    else
                    {
                        {
                            // Modif FC
                            b_value = new BacnetValue[1];
                            b_value[0] = new BacnetValue((BacnetApplicationTags)c.CustomProperty.bacnetApplicationTags, new_value);
                        }
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
                    comm.WritePriority = Properties.Settings.Default.DefaultWritePriority;
                    if (!comm.WritePropertyRequest(adr, object_id, (BacnetPropertyIds)property.propertyIdentifier, b_value))
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
                BacnetClient comm = null;
                BacnetAddress adr;
                try
                {
                    if (m_DeviceTree.SelectedNode == null) return;
                    else if (m_DeviceTree.SelectedNode.Tag == null) return;
                    else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                    KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                    adr = entry.Key;
                    comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
                }
                finally
                {
                    if (comm == null) MessageBox.Show(this, "This is not a valid node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                //fetch object_id
                if (
                    m_AddressSpaceTree.SelectedNode == null ||
                    !(m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId) ||
                    !(((BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag).type == BacnetObjectTypes.OBJECT_FILE))
                {
                    MessageBox.Show(this, "The marked object is not a file", "Not a file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                BacnetObjectId object_id = (BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag;

                //where to store file?
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = Properties.Settings.Default.GUI_LastFilename;
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                string filename = dlg.FileName;
                Properties.Settings.Default.GUI_LastFilename = filename;

                //get file size
                int filesize = FileTransfers.ReadFileSize(comm, adr, object_id);
                if (filesize < 0)
                {
                    MessageBox.Show(this, "Couldn't read file size", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //display progress
                ProgressDialog progress = new ProgressDialog();
                progress.Text = "Downloading file ...";
                progress.Label = "0 of " + (filesize / 1024) + " kb ... (0.0 kb/s)";
                progress.Maximum = filesize;
                progress.Show(this);

                DateTime start = DateTime.Now;
                double kb_per_sec = 0;
                FileTransfers transfer = new FileTransfers();
                EventHandler cancel_handler = (s, a) => { transfer.Cancel = true; };
                progress.Cancel += cancel_handler;
                Action<int> update_progress = (position) =>
                {
                    kb_per_sec = (position / 1024) / (DateTime.Now - start).TotalSeconds;
                    progress.Value = position;
                    progress.Label = string.Format((position / 1024) + " of " + (filesize / 1024) + " kb ... ({0:F1} kb/s)", kb_per_sec);
                };
                Application.DoEvents();
                try
                {
                    if(Properties.Settings.Default.DefaultDownloadSpeed == 2)
                        transfer.DownloadFileBySegmentation(comm, adr, object_id, filename, update_progress);
                    else if(Properties.Settings.Default.DefaultDownloadSpeed == 1)
                        transfer.DownloadFileByAsync(comm, adr, object_id, filename, update_progress);
                    else
                        transfer.DownloadFileByBlocking(comm, adr, object_id, filename, update_progress);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during download file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    progress.Hide();
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            //information
            try
            {
                MessageBox.Show(this, "Done", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
            }
        }

        private void uploadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                BacnetClient comm = null;
                BacnetAddress adr;
                try
                {
                    if (m_DeviceTree.SelectedNode == null) return;
                    else if (m_DeviceTree.SelectedNode.Tag == null) return;
                    else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                    KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                    adr = entry.Key;
                    comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
                }
                finally
                {
                    if (comm == null) MessageBox.Show(this, "This is not a valid node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                //fetch object_id
                if (
                    m_AddressSpaceTree.SelectedNode == null ||
                    !(m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId) ||
                    !(((BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag).type == BacnetObjectTypes.OBJECT_FILE))
                {
                    MessageBox.Show(this, "The marked object is not a file", "Not a file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                BacnetObjectId object_id = (BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag;

                //which file to upload?
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.FileName = Properties.Settings.Default.GUI_LastFilename;
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                string filename = dlg.FileName;
                Properties.Settings.Default.GUI_LastFilename = filename;

                //display progress
                int filesize = (int)(new System.IO.FileInfo(filename)).Length;
                ProgressDialog progress = new ProgressDialog();
                progress.Text = "Uploading file ...";
                progress.Label = "0 of " + (filesize / 1024) + " kb ... (0.0 kb/s)";
                progress.Maximum = filesize;
                progress.Show(this);

                FileTransfers transfer = new FileTransfers();
                DateTime start = DateTime.Now;
                double kb_per_sec = 0;
                EventHandler cancel_handler = (s, a) => { transfer.Cancel = true; };
                progress.Cancel += cancel_handler;
                Action<int> update_progress = (position) =>
                {
                    kb_per_sec = (position / 1024) / (DateTime.Now - start).TotalSeconds;
                    progress.Value = position;
                    progress.Label = string.Format((position / 1024) + " of " + (filesize / 1024) + " kb ... ({0:F1} kb/s)", kb_per_sec);
                };
                try
                {
                    transfer.UploadFileByBlocking(comm, adr, object_id, filename, update_progress);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during upload file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    progress.Hide();
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            //information
            MessageBox.Show(this, "Done", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // en cours
        private void showTrendLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BacnetClient comm = null;
                BacnetAddress adr;
                try
                {
                    if (m_DeviceTree.SelectedNode == null) return;
                    else if (m_DeviceTree.SelectedNode.Tag == null) return;
                    else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                    KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                    adr = entry.Key;
                    comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
                }
                finally
                {
                    if (comm == null) MessageBox.Show(this, "This is not a valid node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                //fetch object_id
                if (
                    m_AddressSpaceTree.SelectedNode == null ||
                    !(m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId) ||
                    !((((BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag).type == BacnetObjectTypes.OBJECT_TREND_LOG_MULTIPLE) ||
                    (((BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag).type == BacnetObjectTypes.OBJECT_TRENDLOG)))
                {
                    MessageBox.Show(this, "The marked object is not a TrendLog", "Not a TrendLog", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                BacnetObjectId object_id = (BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag;

                new TrendLogDisplay(comm, adr, object_id).ShowDialog();

            }
            catch {}
        }

        private void m_AddressSpaceTree_ItemDrag(object sender, ItemDragEventArgs e)
        {
            m_AddressSpaceTree.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void m_SubscriptionView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private string GetObjectName(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                IList<BacnetValue> value;
                if (!comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_OBJECT_NAME, out value))
                    return "[Timed out]";
                if (value == null || value.Count == 0)
                    return "";
                else
                    return value[0].Value.ToString();
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

        private class Subscribtion
        {
            public BacnetClient comm;
            public BacnetAddress adr;
            public BacnetObjectId object_id;
            public string sub_key;
            public uint subscribe_id;
            public Subscribtion(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, string sub_key, uint subscribe_id)
            {
                this.comm = comm;
                this.adr = adr;
                this.object_id = object_id;
                this.sub_key = sub_key;
                this.subscribe_id = subscribe_id;
            }
        }

        private void CreateSubscription(BacnetClient comm, BacnetAddress adr, uint device_id, BacnetObjectId object_id)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch device_id if needed
                if (device_id >= System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE)
                {
                    device_id = FetchDeviceId(comm, adr);
                }

                //add to list
                ListViewItem itm = m_SubscriptionView.Items.Add(adr + " - " + device_id);
                itm.SubItems.Add(object_id.ToString());
                itm.SubItems.Add(GetObjectName(comm, adr, object_id));   //name
                itm.SubItems.Add("");   //value
                itm.SubItems.Add("");   //time
                itm.SubItems.Add("Not started");   //status

                //add to index
                m_next_subscription_id++;
                string sub_key = adr.ToString() + ":" + device_id + ":" + m_next_subscription_id;
                itm.Tag = new Subscribtion(comm, adr, object_id, sub_key, m_next_subscription_id);
                m_subscription_index.Add(sub_key, itm);

                //add to device
                try
                {
                    if (!comm.SubscribeCOVRequest(adr, object_id, m_next_subscription_id, false, Properties.Settings.Default.Subscriptions_IssueConfirmedNotifies, Properties.Settings.Default.Subscriptions_Lifetime))
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
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void m_SubscriptionView_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                //fetch end point
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                BacnetAddress adr = entry.Key;
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;

                //fetch object_id
                TreeNode node = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (node.Tag == null || !(node.Tag is BacnetObjectId)) return;
                BacnetObjectId object_id = (BacnetObjectId)node.Tag;

                //create
                CreateSubscription(comm, adr, entry.Value, object_id);
            }
        }

        private void MainDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //commit setup
                Properties.Settings.Default.GUI_SplitterButtom = m_SplitContainerButtom.SplitterDistance;
                Properties.Settings.Default.GUI_SplitterLeft = m_SplitContainerLeft.SplitterDistance;
                Properties.Settings.Default.GUI_SplitterRight = m_SplitContainerRight.SplitterDistance;
                Properties.Settings.Default.GUI_FormSize = this.Size;
                Properties.Settings.Default.GUI_FormState = this.WindowState.ToString();

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
                Subscribtion sub = (Subscribtion)itm.Tag;
                if (m_subscription_index.ContainsKey(sub.sub_key))
                {
                    //remove from device
                    try
                    {
                        if (!sub.comm.SubscribeCOVRequest(sub.adr, sub.object_id, sub.subscribe_id, true, false, 0))
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
            BacnetClient comm = null;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is BacnetClient)) return;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;
            }
            finally
            {
                if (comm == null) MessageBox.Show(this, "Please select a \"transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //send
            comm.WhoIs();
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string readme_path = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( typeof(MainDialog).Assembly.Location), "README.txt");
            System.Diagnostics.Process.Start(readme_path);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsDialog dlg = new SettingsDialog();
            dlg.SelectedObject = Properties.Settings.Default;
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// This will download all values from a given device and store it in a xml format, fit for the DemoServer
        /// This can be a good way to test serializing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exportDeviceDBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                adr = entry.Key;
                device_id = entry.Value;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
            }
            finally
            {
                if (comm == null) MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //select file to store
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "xml|*.xml";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                //get all objects
                System.IO.BACnet.Storage.DeviceStorage storage = new System.IO.BACnet.Storage.DeviceStorage();
                IList<BacnetValue> value_list;
                comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list);
                LinkedList<BacnetObjectId> object_list = new LinkedList<BacnetObjectId>();
                foreach (BacnetValue value in value_list)
                    object_list.AddLast((BacnetObjectId)value.Value);

                foreach (BacnetObjectId object_id in object_list)
                {
                    //read all properties
                    IList<BacnetReadAccessResult> multi_value_list;
                    BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
                    comm.ReadPropertyMultipleRequest(adr, object_id, properties, out multi_value_list);

                    //store
                    foreach (BacnetPropertyValue value in multi_value_list[0].values)
                    {
                        storage.WriteProperty(object_id, (BacnetPropertyIds)value.property.propertyIdentifier, value.property.propertyArrayIndex, value.value, true);
                    }
                }

                //save to disk
                storage.Save(dlg.FileName);

                //display
                MessageBox.Show(this, "Done", "Export done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error during export: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private uint FetchDeviceId(BacnetClient comm, BacnetAddress adr)
        {
            IList<BacnetValue> value;
            if (comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE), BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, out value))
            {
                if (value != null && value.Count > 0 && value[0].Value is BacnetObjectId)
                {
                    BacnetObjectId object_id = (BacnetObjectId)value[0].Value;
                    return object_id.instance;
                }
                else
                    return 0xFFFFFFFF;
            }
            else
                return 0xFFFFFFFF;
        }

        private void subscribeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;
            else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
            KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
            BacnetAddress adr = entry.Key;
            BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
            uint device_id = entry.Value;

            //fetch object_id
            if (
                m_AddressSpaceTree.SelectedNode == null ||
                !(m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId))
            {
                MessageBox.Show(this, "The marked object is not an object", "Not an object", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            BacnetObjectId object_id = (BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag;

            //create 
            CreateSubscription(comm, adr, device_id, object_id);
        }

        private void m_subscriptionRenewTimer_Tick(object sender, EventArgs e)
        {
            foreach (ListViewItem itm in m_subscription_index.Values)
            {
                try
                {
                    Subscribtion sub = (Subscribtion)itm.Tag;
                    if (!sub.comm.SubscribeCOVRequest(sub.adr, sub.object_id, sub.subscribe_id, false, Properties.Settings.Default.Subscriptions_IssueConfirmedNotifies, Properties.Settings.Default.Subscriptions_Lifetime))
                    {
                        SetSubscriptionStatus(itm, "Offline");
                        Trace.TraceWarning("Couldn't renew subscription " + sub.subscribe_id);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception during renew subscription: " + ex.Message);
                }
            }
        }

        private void sendWhoIsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            sendWhoIsToolStripMenuItem_Click(this, null);
        }

        private void exportDeviceDBToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            exportDeviceDBToolStripMenuItem_Click(this, null);
        }

        private void downloadFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            downloadFileToolStripMenuItem_Click(this, null);
        }

        private void showTrendLogToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            showTrendLogToolStripMenuItem_Click(null, null);
        }

        private void uploadFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            uploadFileToolStripMenuItem_Click(this, null);
        }

        private void subscribeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            subscribeToolStripMenuItem_Click(this, null);
        }

        private void timeSynchronizeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            timeSynchronizeToolStripMenuItem_Click(this, null);
        }

        private void timeSynchronizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                adr = entry.Key;
                device_id = entry.Value;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
            }
            finally
            {
                if (comm == null) MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //send
            if(Properties.Settings.Default.TimeSynchronize_UTC)
                comm.SynchronizeTime(adr, DateTime.Now.ToUniversalTime(), true);
            else
                comm.SynchronizeTime(adr, DateTime.Now, false);

            //done
            MessageBox.Show(this, "OK", "Time Synchronize", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void communicationControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                adr = entry.Key;
                device_id = entry.Value;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
            }
            finally
            {
                if (comm == null) MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //Options
            DeviceCommunicationControlDialog dlg = new DeviceCommunicationControlDialog();
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

            if (dlg.IsReinitialize)
            {
                //Reinitialize Device
                if (!comm.ReinitializeRequest(adr, dlg.ReinitializeState, dlg.Password))
                    MessageBox.Show(this, "Couldn't perform device communication control", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "OK", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //Device Communication Control
                if (!comm.DeviceCommunicationControlRequest(adr, dlg.Duration, dlg.DisableCommunication ? (uint)1 : (uint)0, dlg.Password))
                    MessageBox.Show(this, "Couldn't perform device communication control", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "OK", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void communicationControlToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            communicationControlToolStripMenuItem_Click(this, null);
        }
        // Modif FC
        // base on http://www.big-eu.org/fileadmin/downloads/EDE2_2_Templates.zip
        // This will download all values from a given device and store it in an EDE csv format,
        private void exportDeviceEDEFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                KeyValuePair<BacnetAddress, uint> entry = (KeyValuePair<BacnetAddress, uint>)m_DeviceTree.SelectedNode.Tag;
                adr = entry.Key;
                device_id = entry.Value;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
            }
            finally
            {
                if (comm == null) MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //select file to store
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "csv|*.csv";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                StreamWriter Sw = new StreamWriter(dlg.FileName);

                Sw.WriteLine("# Proposal_Engineering-Data-Exchange - B.I.G.-EU");
                Sw.WriteLine("PROJECT_NAME");
                Sw.WriteLine("VERSION_OF_REFERENCEFILE");
                Sw.WriteLine("TIMESTAMP_OF_LAST_CHANGE;" + DateTime.Now.ToShortDateString());
                Sw.WriteLine("AUTHOR_OF_LAST_CHANGE;YABE Yet Another Bacnet Explorer");
                Sw.WriteLine("VERSION_OF_LAYOUT;2.2");
                Sw.WriteLine("#mandatory;mandator;mandatory;mandatory;mandatory;optional;optional;optional;optional;optional;optional;optional;optional;optional;optional;optional");
                Sw.WriteLine("# keyname;device obj.-instance;object-name;object-type;object-instance;description;present-value-default;min-present-value;max-present-value;settable;supports COV;hi-limit;low-limit;state-text-reference;unit-code;vendor-specific-addres");
                //get all objects
                IList<BacnetValue> value_list;
                comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list);
                LinkedList<BacnetObjectId> object_list = new LinkedList<BacnetObjectId>();
                foreach (BacnetValue value in value_list)
                {
                    BacnetObjectId Bacobj = (BacnetObjectId)value.Value;

                    IList<BacnetReadAccessResult> multi_value_list;
                    BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
                    comm.ReadPropertyMultipleRequest(adr, (BacnetObjectId)value.Value, properties, out multi_value_list);

                    BacnetReadAccessResult br = multi_value_list[0];

                    string Identifier = "";
                    string Description = "";
                    string UnitCode = "";

                    foreach (BacnetPropertyValue pv in br.values)
                    {
                        if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_OBJECT_NAME)
                            Identifier = pv.value[0].Value.ToString();
                        if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_DESCRIPTION)
                            Description = pv.value[0].Value.ToString(); ;
                        if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_UNITS)
                            UnitCode = pv.value[0].Value.ToString(); ;
                    }

                    Sw.WriteLine(value.ToString() + ";" + device_id.ToString() + ";" + Identifier + ";" + ((int)Bacobj.type).ToString() + ";" + Bacobj.instance.ToString() + ";" + Description + ";;;;;;;;;" + UnitCode);

                }

                Sw.Close();

                //display
                MessageBox.Show(this, "Done", "Export done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error during export: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void foreignDeviceRegistrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is BacnetClient)) return;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;
            }
            finally
            {

                if (comm == null) MessageBox.Show(this, "Please select an \"IP transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Form F = new ForeignRegistry(comm);
            F.ShowDialog();
        }

    }
}
