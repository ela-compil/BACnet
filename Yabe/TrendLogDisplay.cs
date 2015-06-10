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
using System.Threading;
using ZedGraph;
using System.IO;
using System.Diagnostics;

namespace Yabe
{
    public partial class TrendLogDisplay : Form
    {
        int Logsize;
        PointPairList[] Pointslists;
        BacnetClient comm; BacnetAddress adr; BacnetObjectId object_id;
        // Number of records read per ReadRange request
        // 60 is a good value for quite full Udp/Ipv4/Ethernet packets
        // otherwise it could be rejected, or fragmented by Ip, or ...
        int NbRecordsByStep = 60;
        int CurvesNumber = 1;

        bool StopDownload = false;

        public TrendLogDisplay(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {

            InitializeComponent();

            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(m_progressBar, "Click here to stop download");            
            
            m_zedGraphCtl.GraphPane.XAxis.Type = AxisType.Date;
            m_zedGraphCtl.GraphPane.XAxis.Title.Text = "Date/Time";
            m_zedGraphCtl.GraphPane.YAxis.Title.Text = "Values";
            m_zedGraphCtl.GraphPane.XAxis.MajorGrid.IsVisible = true;
            m_zedGraphCtl.GraphPane.YAxis.MajorGrid.IsVisible = true;
            m_zedGraphCtl.GraphPane.XAxis.MajorGrid.Color = Color.Gray;
            m_zedGraphCtl.GraphPane.YAxis.MajorGrid.Color = Color.Gray;
            m_zedGraphCtl.IsAntiAlias = true;
            // Zedgraph patch made to use the false IsAntiAlias default value for textRendering :
            // antiaslias is clean for curves but not for the text
            // to set antialias for fonts ZedGraph.FontSpec.Default.IsAntiAlias=true can be used

            Logsize = ReadRangeSize(comm, adr, object_id);
            if (Logsize < 0) Logsize = 0;
            m_progresslabel.Text = "Downloads of " + Logsize + " records in progress (0%)";
            m_progressBar.Maximum = Logsize;

            m_zedGraphCtl.GraphPane.Title.Text = ReadCurveName(comm, adr, object_id);

            // get the number of Trend in the Log, 1 for basic TrendLog
            if (object_id.type == BacnetObjectTypes.OBJECT_TREND_LOG_MULTIPLE)
                CurvesNumber = ReadNumberofCurves(comm, adr, object_id);

            m_zedGraphCtl.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(m_zedGraphCtl_ContextMenuBuilder);
            m_zedGraphCtl.PointValueEvent += new ZedGraphControl.PointValueHandler(m_zedGraphCtl_PointValueEvent);

            if ((Logsize != 0) && (CurvesNumber != 0))
            {
                Pointslists = new PointPairList[CurvesNumber];
                for (int i = 0; i < CurvesNumber; i++)
                    Pointslists[i] = new PointPairList();

                NbRecordsByStep = NbRecordsByStep - 5 * CurvesNumber;

                this.UseWaitCursor = true;

                // Start downloads in thread
                this.comm = comm;
                this.adr = adr;
                this.object_id = object_id;
                Thread th = new Thread(DownloadFullTrendLog);
                th.IsBackground = true;
                th.Start();
            }
            else
            {
                m_progressBar.Visible = false;
                m_progresslabel.Text = "The trendlog is empty, nothing to display";
            }

            m_list.Visible=false; // to avoid flicker during download
        }

        // Stop download
        private void m_progressBar_Click(object sender, EventArgs e)
        {
            StopDownload = true;
        }

        private void UpdateProgress(int ValueAdd)
        {
            this.UseWaitCursor = false; // no more needed, progress bar is moving
            m_progressBar.Value += ValueAdd;
            int Percent = m_progressBar.Value * 100 / Logsize;

            m_progresslabel.Text = "Downloads of " + Logsize + " records in progress (" + Percent.ToString() + "%)";
        }

        private void UpdateEnd(bool state)
        {
            ColorSymbolRotator color = new ColorSymbolRotator();

            // Even if an error occurs during downloads, maybe some values are OK
            for (int i = 0; i < CurvesNumber; i++)
            {
                LineItem l = m_zedGraphCtl.GraphPane.AddCurve("", Pointslists[i], color.NextColor, SymbolType.None);
                l.Line.Width = 2;
            }
            m_zedGraphCtl.GraphPane.Chart.Fill = new Fill(Color.White, Color.FromArgb(255, 255, 190), 45F);

            m_zedGraphCtl.RestoreScale(m_zedGraphCtl.GraphPane);

            this.UseWaitCursor = false;
            m_progressBar.Visible = false;

            if (state == false)
                m_progresslabel.Text = "Error loading trend";
            else
                m_progresslabel.Visible = false;

            m_list.Visible=true;
        }

        // Get the numbers of records in the Log
        private int ReadRangeSize(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            IList<BacnetValue> value;
            try
            {
                if (!comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_RECORD_COUNT, out value))
                    return -1;
                if (value == null || value.Count == 0)
                    return -1;
                return (int)Convert.ChangeType(value[0].Value, typeof(int));
            }
            catch
            {
                Trace.TraceError("Couldn't load PROP_RECORD_COUNT from TendLog object");
                return -1;
            }
        }

        // PROP_OBJECT_NAME is used, could be PROP_OBJECT_DESCRIPTION maybe
        private string ReadCurveName(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            IList<BacnetValue> value;
            try
            {
                if (!comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_OBJECT_NAME, out value))
                    return "";
                if (value == null || value.Count == 0)
                    return "";
                return value[0].Value.ToString();
            }
            catch
            {
                return "";
            }
        }
        // Only for MULTIPLE TREND LOG
        private int ReadNumberofCurves(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            IList<BacnetValue> value;
            try
            {
                if (!comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_LOG_DEVICE_OBJECT_PROPERTY, out value))
                    return 0;
                return (value == null ? 0 : value.Count);

            }
            catch
            {
                return 0;
            }
        }

        //
        // Download the TrendLog in many blocks of NbRecordByStep values
        // it could be a (ligh) problem with sliding windows logs with high speed 
        // modification : it will lost some values and duplicate some others.
        //
        private void DownloadFullTrendLog()
        {   
            uint ItemCount;
            int Idx;

            try
            {
                // First index is 1
                Idx = 1;

                do
                {
                    byte[] TrendBuffer;
                    ItemCount = (uint)Math.Min(NbRecordsByStep, Logsize - Idx + 1);
                    if (Idx == 951)
                        Idx = 951;

                    //read
                    if ((comm.ReadRangeRequest(adr, object_id, (uint)Idx, ref ItemCount, out TrendBuffer) == false) || (ItemCount <= 0))
                    {
                        Trace.TraceError("Couldn't load log data");
                        BeginInvoke(new Action<bool>(UpdateEnd), false);
                        return;
                    }

                    int len = 0;
                    for (int itm = 0; itm < ItemCount; itm++)
                    {
                        //decode
                        BacnetLogRecord[] records;
                        int l;
                        if ((l = System.IO.BACnet.Serialize.Services.DecodeLogRecord(TrendBuffer, len, TrendBuffer.Length-len, CurvesNumber, out records)) < 0)
                        {
                            Trace.TraceError("Couldn't decode log data");
                            BeginInvoke(new Action<bool>(UpdateEnd), false);
                            return;
                        }
                        len += l;

                        //update interface
                        for (int i = 0; i < records.Length; i++, Idx++)
                        {
                            if(records[i].type == BacnetTrendLogValueType.TL_TYPE_UNSIGN || records[i].type == BacnetTrendLogValueType.TL_TYPE_SIGN || records[i].type == BacnetTrendLogValueType.TL_TYPE_REAL)
                                Pointslists[i].Add(new XDate(records[i].timestamp), (double)Convert.ChangeType(records[i].Value, typeof(double)));
                            else
                                Pointslists[i].Add(new XDate(records[i].timestamp), double.NaN);

                            AddToList(Idx, records[i].timestamp, records[i].type, records[i].Value, records[i].statusFlags.ConvertToInt());
                        }
                    }

                    // Update progress bar
                    BeginInvoke(new Action<int>(UpdateProgress), (int)ItemCount);

                } while (((Idx + 1) < Logsize)&&(StopDownload==false));

                BeginInvoke(new Action<bool>(UpdateEnd), true);
            }
            catch(Exception ex)
            {
                Trace.TraceError("Error during log data: " + ex.Message);
                try
                {
                    // Exception if Form is closed
                    BeginInvoke(new Action<bool>(UpdateEnd), false);
                }
                catch { }
            }

        }

        // With the original Zedgraph.dll it's blinking. Default tooltip also.
        // Well known problem with existing patch.
        // http://sourceforge.net/p/zedgraph/patches/20/ Patch applied in the attached zedgraph library
        string m_zedGraphCtl_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            PointPair pt = curve[iPt];
            XDate d = new XDate(pt.X);
            DateTime dt = d;

            // auto adjustable digit precision, quite a copyright here :=)
            String ValStr = "0";
            if (pt.Y != 0)
            {
                int resolution = (int)Math.Max(0, Math.Ceiling(4 - Math.Log10(Math.Abs(pt.Y))));
                ValStr = Math.Round(pt.Y, resolution).ToString();
            }
            return "Date : "+dt.ToString() + "\nValue : " + ValStr; 
        }

        #region ZedGraphCSVExport

        // CSV Export for ZedGraph
        // thank's to http://www.smallguru.com/2009/06/zedgraph-csharp-graph-data-export-to-cs/
        void m_zedGraphCtl_ContextMenuBuilder(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            ToolStripMenuItem _item = new ToolStripMenuItem();
            // This is the text that will show up in the menu
            _item.Text = "Export Data as CSV";
            _item.Click += new EventHandler(ShowSaveAsForExportCSV);
            // Add the menu item to the menu,as 3rd Item
            menuStrip.Items.Insert(2, _item);
        }
        private void ShowSaveAsForExportCSV(object sender, System.EventArgs e)
        {
            try
            {
                //show saveAs CmdDlg
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "CSV files (*.csv)|*.csv";
                saveFileDialog1.ShowDialog();
                StreamWriter CSVWriter = new StreamWriter(saveFileDialog1.FileName);
                WriteCSVToStream(CSVWriter);
                CSVWriter.Close();
            }
            catch (Exception)
            {
            }
        }
        // Only the first trend is used for X axis and number of values
        private void WriteCSVToStream(StreamWriter CSVWriter)
        {
            //First line is for Headers, X and Y Axis
            CSVWriter.Write("Date/Time");
            for (int i = 0; i < Pointslists.Length; i++)
                CSVWriter.Write(";Values");
            CSVWriter.WriteLine();

            //subsequent lines are having data
            for (int i = 0; i < Pointslists[0].Count; i++)
            {
                XDate d = new XDate(Pointslists[0][i].X);
                DateTime dt = d;    // to get the second
                CSVWriter.Write(dt.ToString());
                for (int cv = 0; cv < Pointslists.Length; cv++)
                {
                    double val = Pointslists[cv][i].Y;
                    if (d != double.NaN)
                        CSVWriter.Write(";" + Pointslists[cv][i].Y);
                    else
                        CSVWriter.Write(";");
                }
                CSVWriter.WriteLine();
            }

        }
        #endregion

        private void AddToList(int sequence_no, DateTime dt, BacnetTrendLogValueType type, object value, uint status)
        {
            ListViewItem itm = new ListViewItem();
            itm.Text = sequence_no.ToString();
            itm.SubItems.Add(dt.ToString());
            itm.SubItems.Add(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(type.ToString().ToLower().Substring(8)));
            itm.SubItems.Add(value != null ? value.ToString() : "NULL");
            itm.SubItems.Add(status.ToString());
            this.Invoke((MethodInvoker)delegate { m_list.Items.Add(itm); });
        }
    }
}
