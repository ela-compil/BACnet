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

            Logsize = ReadRangeSize(comm, adr, object_id);
            m_progresslabel.Text = "Downloads of " + Logsize + " records in progress (0%)";
            m_progressBar.Maximum = Logsize;

            ReadCurveName(comm, adr, object_id);
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
            // I'm a rookie with lambda expressions, but I like it !
            // It's the way data as to be given to me by the TrendLogDecoder
            Action<int, DateTime, double> DataStorage = (idx, timestamp, val) => Pointslists[idx].Add(new XDate(timestamp), val);
            
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

                    if ((comm.ReadRangeRequest(adr, object_id, (uint)Idx, ref ItemCount, out TrendBuffer) == false) || (ItemCount == 0))
                    {
                        BeginInvoke(new Action<bool>(UpdateEnd), false);
                        return;
                    }

                    if (TrendLogDecoder.DecodeTrendBuffer(TrendBuffer, ItemCount, CurvesNumber, DataStorage) != ItemCount)
                    {
                        BeginInvoke(new Action<bool>(UpdateEnd), false);
                        return;
                    }

                    // Update progress bar
                    BeginInvoke(new Action<int>(UpdateProgress), (int)ItemCount);

                    Idx += (int)ItemCount;

                } while (((Idx + 1) < Logsize)&&(StopDownload==false));

                BeginInvoke(new Action<bool>(UpdateEnd), true);
            }
            catch
            {
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
            catch (Exception ex)
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

    }

    public enum BacnetTrendLogValueType : byte
    {
        // Copyright (C) 2009 Peter Mc Shane in Steve Karg Stack, trendlog.h
        // Thank's to it's encoding sample, very usefull for this decoding work
        TL_TYPE_STATUS = 0,
        TL_TYPE_BOOL = 1,
        TL_TYPE_REAL = 2,
        TL_TYPE_ENUM = 3,
        TL_TYPE_UNSIGN = 4,
        TL_TYPE_SIGN = 5,
        TL_TYPE_BITS = 6,
        TL_TYPE_NULL = 7,
        TL_TYPE_ERROR = 8,
        TL_TYPE_DELTA = 9,
        TL_TYPE_ANY = 10
    }

    public static class TrendLogDecoder
    {
        //
        // Decode the TrendLog buffer 
        // and fill the ZedGraph PointList to draw the curve(s)
        // It's not linked to Zedgraph. Give another Action<int, DateTime, double> 
        // like a call to Add to an array of List<KeyValuePair<DateTime, double>>
        // or other to get back the values. If a value is null or in error, double.NaN is provided
        //
        public static uint DecodeTrendBuffer(byte[] buffer, uint ItemCount, int NbCurves, Action<int, DateTime, double> StoreValue)
        {
            int offset = 0;
            byte tagnumber = 0;

            for (int i = 0; i < (int)ItemCount; i++)
            {
                DateTime date;
                DateTime time;

                offset += ASN1.decode_tag_number(buffer, offset, out tagnumber);

                if (tagnumber != 0)
                    return (uint)0;

                // Date and Time in Tag 0
                offset += ASN1.decode_application_date(buffer, offset, out date);
                offset += ASN1.decode_application_time(buffer, offset, out time);

                DateTime dt = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

                if (!(ASN1.decode_is_closing_tag(buffer, offset)))
                    return (uint)0;
                offset++;

                // Value or error in Tag 1
                offset += ASN1.decode_tag_number(buffer, offset, out tagnumber);
                if (tagnumber != 1)
                    return (uint)0;

                byte ContextTagType = 0;
                uint TagLenght = 0;

                // Not test for TrendLogMultiple
                // Seems to be encoded like this somewhere in an Ashrae document
                for (int CurveNumber = 0; CurveNumber < NbCurves; CurveNumber++)
                {
                    offset += ASN1.decode_tag_number_and_value(buffer, offset, out ContextTagType, out TagLenght);

                    switch ((BacnetTrendLogValueType)ContextTagType)
                    {
                        case BacnetTrendLogValueType.TL_TYPE_STATUS:
                            BacnetBitString StatusFlags;
                            offset += ASN1.decode_bitstring(buffer, offset, TagLenght, out StatusFlags);
                            StoreValue(CurveNumber, dt, double.NaN); // this is a discontinuity in the curve
                            break;  
                        case BacnetTrendLogValueType.TL_TYPE_BOOL:
                            StoreValue(CurveNumber, dt, buffer[offset++]);
                            break;
                        case BacnetTrendLogValueType.TL_TYPE_REAL:
                        case BacnetTrendLogValueType.TL_TYPE_DELTA:
                            if (TagLenght == 4)
                            {
                                float ValR;
                                offset += ASN1.decode_real(buffer, offset, out ValR);
                                StoreValue(CurveNumber, dt, ValR); // here it's equivalent to Pointslist.Add(new XDate(dt), ValR);
                            }
                            else  // not sure double can exist here
                            {
                                double ValD;
                                offset += ASN1.decode_double(buffer, offset, out ValD);
                                StoreValue(CurveNumber, dt, ValD);
                            }
                            break;
                        case BacnetTrendLogValueType.TL_TYPE_ENUM:
                            uint ValEnum;
                            offset += ASN1.decode_enumerated(buffer, offset, TagLenght, out ValEnum);
                            StoreValue(CurveNumber, dt, ValEnum);
                            break;
                        case BacnetTrendLogValueType.TL_TYPE_SIGN:
                            int ValS;
                            offset += ASN1.decode_signed(buffer, offset, TagLenght, out ValS);
                            StoreValue(CurveNumber, dt, ValS);
                            break;
                        case BacnetTrendLogValueType.TL_TYPE_UNSIGN:
                            uint ValU;
                            offset += ASN1.decode_unsigned(buffer, offset, TagLenght, out ValU);
                            StoreValue(CurveNumber, dt, ValU);
                            break;
                        case BacnetTrendLogValueType.TL_TYPE_ERROR:
                            uint Errcode;
                            offset += ASN1.decode_enumerated(buffer, offset, (uint)2, out Errcode);
                            offset += ASN1.decode_enumerated(buffer, offset, (uint)2, out Errcode);
                            byte b = buffer[offset];
                            offset++;
                            StoreValue(CurveNumber, dt, double.NaN); // this is a discontinuity in the curve
                            break;  
                        case BacnetTrendLogValueType.TL_TYPE_NULL:
                            offset++;
                            StoreValue(CurveNumber, dt, double.NaN); // this is a discontinuity in the curve
                            break; 
                        // No way to handle these data types
                        case BacnetTrendLogValueType.TL_TYPE_ANY:
                        case BacnetTrendLogValueType.TL_TYPE_BITS:
                        default:
                            return 0;
                    }
                }

                if (!(ASN1.decode_is_closing_tag(buffer, offset)))
                    return 0;
                offset++;

                // Optional Tag 2
                if (offset < buffer.Length)
                {
                    ASN1.decode_tag_number(buffer, offset, out tagnumber);
                    if (tagnumber == 2)
                    {
                        offset++;
                        BacnetBitString StatusFlags;
                        offset += ASN1.decode_bitstring(buffer, offset, 2, out StatusFlags);
                    }
                }
            }
            return ItemCount;
        }

    }
}
