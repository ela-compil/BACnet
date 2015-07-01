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
using System.Windows.Forms.Calendar;
using System.Globalization;

namespace Yabe
{
    public partial class CalendarEditor : Form
    {
        BacnetClient comm; BacnetAddress adr; BacnetObjectId object_id;
        // dates in the bacnetobject
        BACnetCalendarEntry calendarEntries;

        DateTime CalendarStartRequested;

        public CalendarEditor(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id)
        {
            InitializeComponent();

            this.comm = comm;
            this.adr = adr;
            this.object_id = object_id;

            LoadCalendar();
        }

        private void LoadCalendar()
        {
            byte[] buf = null;
            comm.RawEncodedDecodedPropertyConfirmedRequest(adr, object_id, BacnetPropertyIds.PROP_DATE_LIST, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, ref buf);
            calendarEntries = new BACnetCalendarEntry();
            calendarEntries.ASN1decode(buf, 1, (uint)buf.Length);

            dateSelect.SelectionRange = new SelectionRange(DateTime.Now, DateTime.Now);

            listEntries.Items.Clear();
            foreach (object e in calendarEntries.Entries)
                listEntries.Items.Add(e);

            SetCalendarDisplayDate(DateTime.Now);
        }

        private void WriteCalendar()
        {
            try
            {
                // provisoire pour ne pas faire n'importe quoi
                // BacnetObjectId object_id = new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 23);

                List<BacnetValue> v = new List<BacnetValue>();
                v.Add(new BacnetValue(calendarEntries));
                comm.WritePropertyRequest(adr, object_id, BacnetPropertyIds.PROP_DATE_LIST, v);
            }
            catch { }
        }

        private void btReadWrite_Click(object sender, EventArgs e)
        {
            WriteCalendar();
            LoadCalendar();
        }

        private void SetCalendarDisplayDate (DateTime d)
        {
            DateTime start = new DateTime(d.Year, d.Month, 1);
            DateTime stop = start.AddMonths(1).AddHours(-1);

            CalendarStartRequested = start;

            calendarView.SetViewRange(start, stop);
        }

        private void dateSelect_DateChanged(object sender, DateRangeEventArgs e)
        {
            SetCalendarDisplayDate(e.Start);
        }

        private void listEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            object o = listEntries.Items[listEntries.SelectedIndex];
            if (o is BacnetDateRange)
            {
                BacnetDateRange bdr = (BacnetDateRange)o;

                if (bdr.startDate.year != 255)
                    SetCalendarDisplayDate(bdr.startDate.toDateTime());
                else if (bdr.endDate.year != 255)
                    SetCalendarDisplayDate(bdr.endDate.toDateTime().AddDays(-10));
            }

        }

        private void AddCalendarEntry(DateTime _start, DateTime _end,  Color color, String Text, object tag)
        {
            DateTime start, end;
            start = new DateTime(_start.Year, _start.Month, _start.Day, 0, 0, 0);
            end = new DateTime(_end.Year, _end.Month, _end.Day, 23, 59, 59);
            CalendarItem ci = new CalendarItem(calendarView, start, end, Text);
            ci.ApplyColor(color);
            ci.Tag = tag;

            if (start <= calendarView.Days[calendarView.Days.Length-1].Date && calendarView.Days[0] .Date <= end)
                calendarView.Items.Add(ci);
             
        }

        private void PlaceItemsInCalendarView()
        {
            foreach (object e in calendarEntries.Entries)
            {
                if (e is BacnetDate)
                {
                    BacnetDate bd = (BacnetDate)e;
                    if (bd.IsPeriodic)
                    {
                        foreach (CalendarDay dt in calendarView.Days)
                            if (bd.IsAFittingDate(dt.Date))
                                AddCalendarEntry(dt.Date,dt.Date, Color.Blue,"Periodic",bd);
                    }
                    else
                        AddCalendarEntry(bd.toDateTime(), bd.toDateTime(), Color.Green, "", bd);
                }
                else if (e is BacnetDateRange) 
                {
                    BacnetDateRange bdr = (BacnetDateRange)e;
                    DateTime start,end;

                    if (bdr.startDate.year != 255)
                        start = new DateTime(bdr.startDate.year, bdr.startDate.month, bdr.startDate.day, 0, 0, 0);
                    else
                        start = DateTime.MinValue;
                    if (bdr.endDate.year != 255)
                        end = new DateTime(bdr.endDate.year, bdr.endDate.month, bdr.endDate.day, 23, 59, 59);
                    else
                        end = DateTime.MaxValue;
                    CalendarItem ci = new CalendarItem(calendarView, start, end, "");
                    ci.ApplyColor(Color.Yellow);
                    ci.Tag = bdr;

                    if (start <= calendarView.Days[calendarView.Days.Length - 1].Date && calendarView.Days[0].Date <= end)
                        calendarView.Items.Add(ci);
                }
                else
                {
                    BacnetweekNDay bwnd = (BacnetweekNDay)e;
                    foreach (CalendarDay dt in calendarView.Days)
                        if (bwnd.IsAFittingDate(dt.Date))
                            AddCalendarEntry(dt.Date, dt.Date, Color.Red, "Periodic", bwnd);
                }
            }
        }

        // Called to renew all the data inside the control
        private void calendarView_LoadItems(object sender, CalendarLoadEventArgs e)
        {
            PlaceItemsInCalendarView();

        }

        private void calendarView_ItemDeleted(object sender, CalendarItemEventArgs e)
        {
            calendarEntries.Entries.Remove(e.Item.Tag);
            listEntries.Items.Remove(e.Item.Tag);
            SetCalendarDisplayDate(CalendarStartRequested);
        }

        private void calendarView_ItemCreated(object sender, CalendarItemCancelEventArgs e)
        {
            if ((e.Item.StartDate.Year == e.Item.EndDate.Year) && (e.Item.StartDate.Month == e.Item.EndDate.Month) && (e.Item.StartDate.Day == e.Item.EndDate.Day))
            {
                BacnetDate newbd = new BacnetDate((ushort)e.Item.StartDate.Year, (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                listEntries.Items.Add(newbd);
                calendarEntries.Entries.Add(newbd);
            }
            else
            {
                BacnetDateRange newbdr = new BacnetDateRange();
                newbdr.startDate = new BacnetDate((ushort)e.Item.StartDate.Year, (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                newbdr.endDate = new BacnetDate((ushort)e.Item.EndDate.Year, (byte)e.Item.EndDate.Month, (byte)e.Item.EndDate.Day);
                listEntries.Items.Add(newbdr);
                calendarEntries.Entries.Add(newbdr);
            }
            SetCalendarDisplayDate(CalendarStartRequested);
        }

        private void calendarView_ItemDatesChanged(object sender, CalendarItemEventArgs e)
        {
            object o = e.Item.Tag;

            if (((o is BacnetDate)&&(((BacnetDate)o).IsPeriodic))||( o is BacnetweekNDay))
            {
                MessageBox.Show("Cannot do that with perodic element\r\nEdit it with the Date entries list popup menu", "Yabe", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            //if ((o is BacnetDate) || (o is BacnetDateRange))
            {
                calendarEntries.Entries.Remove(o);
                int Idx = listEntries.Items.IndexOf(o);

                listEntries.Items.Remove(o);

                if ((e.Item.StartDate.Year == e.Item.EndDate.Year)&&(e.Item.StartDate.Month == e.Item.EndDate.Month)&&(e.Item.StartDate.Day == e.Item.EndDate.Day))
                {
                    BacnetDate newbd = new BacnetDate((ushort)e.Item.StartDate.Year, (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                    listEntries.Items.Insert(Idx, newbd);
                    calendarEntries.Entries.Add(newbd);
                }
                else
                {
                    BacnetDateRange newbdr = new BacnetDateRange();
                    newbdr.startDate = new BacnetDate((ushort)e.Item.StartDate.Year, (byte)e.Item.StartDate.Month, (byte)e.Item.StartDate.Day);
                    newbdr.endDate = new BacnetDate((ushort)e.Item.EndDate.Year, (byte)e.Item.EndDate.Month, (byte)e.Item.EndDate.Day);
                    listEntries.Items.Insert(Idx, newbdr);
                    calendarEntries.Entries.Add(newbdr);
                }
            }

            SetCalendarDisplayDate(CalendarStartRequested);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                calendarEntries.Entries.Remove(listEntries.SelectedItem);
                listEntries.Items.RemoveAt(listEntries.SelectedIndex);
            }
            catch { }
            SetCalendarDisplayDate(CalendarStartRequested);
        }

    }

    #region Calendar entries structures   

    // Some help from http://sourceforge.net/p/bacnet/mailman/message/1258810/

    /* Tag 0 in CalendarEntry */
    struct BacnetDate : ASN1.IASN1encode
    {
        public UInt16 year;     /* 255 any */
        public byte month;      /* 1=Jan; 255 any, 13 Odd, 14 Even */
        public byte day;        /* 1..31; 32 last day of the month; 255 any */
        public byte wday;       /* 1=Monday-7=Sunday, 255 any */

        public BacnetDate(UInt16 year, byte month, byte day, byte wday = 255)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.wday = wday;
        }

        public void ASN1encode(EncodeBuffer buffer)
        {
            ASN1.encode_tag(buffer, 0, true, 4);
            if (year != 255)
                buffer.Add((byte)(year - 1900)); // bacnet is ready for the year 2156 bug
            else
                buffer.Add(255);
            buffer.Add((byte)month);
            buffer.Add((byte)day);
            buffer.Add((byte)wday);
        }

        public int ASN1decode(byte[] buffer, int offset, uint len_value)
        {
            if (buffer[offset] != 255)
                year = (ushort)(buffer[offset] + 1900);
            else
                year = 255;

            month = buffer[offset + 1];
            day = buffer[offset + 2];
            wday = buffer[offset + 3];

            return 4;
        }
        public bool IsPeriodic
        {
            get { return (year == 255) || (month == 255) || (day == 255); }        
        }

        public bool IsAFittingDate (DateTime date)
        {
            if ((date.Year!=year)&&(year!=255))
                return false;

            if ((date.Month != month) && (month != 255) && (month != 13) && (month != 14))
                return false;
            if ((month == 13) && ((date.Month & 1) != 1))
                return false;
            if ((month == 14) && ((date.Month & 1) == 1))
                return false;

            if ((date.Day != day) && (day != 255))
                return false;
            // day 32 todo

            if (wday==255)
                return true;
            if ((wday==7)&&(date.DayOfWeek==0))  // Sunday 7 for Bacnet, 0 for .NET
                return true;
            if (wday==(int)date.DayOfWeek)
                return true;

            return false;
        }

        public DateTime toDateTime() // Not every time possible, too much complex (any month, any year ...)
        {
            try
            {
                if (day == 255)
                    return new DateTime(1, 1, 1);
                else
                    return new DateTime(year, month, day);
            }
            catch { }

            return DateTime.Now;
        }

        string GetDayName(int day)
        {
            if (day == 7) day = 0;
            return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[day];
        }

        public override string ToString()
        {
            String ret;

            if (wday != 255)
                ret = GetDayName(wday)+" ";
            else
                ret = "";

            if (day != 255)
                ret = ret + day.ToString() + "/";
            else
                ret = ret + "**/";

            switch (month)
            {
                case 13:
                    ret = ret + "odd/";
                    break;
                case 14:
                    ret = ret + "even/";
                    break;
                case 255:
                    ret = ret + "**/";
                    break;
                default:
                    ret = ret + month.ToString() + "/";
                    break;
            }


            if (year != 255)
                ret = ret +year.ToString();
            else
                ret = ret + "****";

            return ret;
        }
    }

    /* Tag 1 in CalendarEntry */
    struct BacnetDateRange : ASN1.IASN1encode
    {
        public BacnetDate startDate;
        public BacnetDate endDate;
        public void ASN1encode(EncodeBuffer buffer)
        {
            ASN1.encode_opening_tag(buffer, 1);
            startDate.ASN1encode(buffer);
            endDate.ASN1encode(buffer);
            ASN1.encode_closing_tag(buffer, 1);
        }
        public int ASN1decode(byte[] buffer, int offset, uint len_value)
        {
            int len = 1; // opening tag
            len += startDate.ASN1decode(buffer, offset + len, len_value);
            len++;
            len += endDate.ASN1decode(buffer, offset + len, len_value);
            return len;
        }

        public override string ToString()
        {
            string ret;

            if (startDate.day != 255)
                ret = "From " + startDate.ToString();
            else
                ret = "From **/**/**";

            if (endDate.day != 255)
                ret = ret + " to " + endDate.ToString();
            else
                ret = ret + " to **/**/**";

            return ret;
        }
    };
    /* Tag 2 in CalendarEntry */
    struct BacnetweekNDay : ASN1.IASN1encode
    {
        public byte month;  /* 1 January, 13 Odd, 14 Even, 255 Any */
        public byte week;   /* Don't realy understand */
        public byte wday;   /* 1=Monday-7=Sunday, 255 any */
        public void ASN1encode(EncodeBuffer buffer)
        {
            ASN1.encode_tag(buffer, 2, true, 3);
            buffer.Add(month);
            buffer.Add(week);
            buffer.Add(wday);
        }

        public int ASN1decode(byte[] buffer, int offset, uint len_value)
        {
            month = buffer[offset++];
            week = buffer[offset++];
            wday = buffer[offset];

            return 3;
        }

        string GetDayName(int day)
        {
            if (day == 7) day = 0;
            return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[day];
        }

        public override string ToString()
        {
            string ret;

            if (wday != 255)
                ret=  GetDayName(wday);
            else
                ret= "Every days";

            if (month!=255)
                ret=ret+"/"+CultureInfo.InvariantCulture.DateTimeFormat.MonthNames[month-1];
            else
                ret =ret+"/Every month";

            return ret;
        }

        public bool IsAFittingDate(DateTime date)
        {
            if ((date.Month != month) && (month != 255) && (month!=13) && (month!=14))
                return false;
            if ((month == 13) && ((date.Month & 1) != 1))
                return false;
            if ((month == 14) && ((date.Month & 1) == 1))
                return false;

            // What about week !

            if (wday == 255)
                return true;
            if ((wday == 7) && (date.DayOfWeek == 0))  // Sunday 7 for Bacnet, 0 for .NET
                return true;
            if (wday == (int)date.DayOfWeek)
                return true;

            return false;
        }
    }

    struct BACnetCalendarEntry : ASN1.IASN1encode
    {
        public List<object> Entries; // BacnetDate or BacnetDateRange or BacnetweekNDay

        public void ASN1encode(EncodeBuffer buffer)
        {
            if (Entries != null)
                foreach (ASN1.IASN1encode entry in Entries)
                    entry.ASN1encode(buffer);
        }


        public int ASN1decode(byte[] buffer, int offset, uint len_value)
        {
            int len = 0;
            byte tag_number;

            Entries = new List<object>();

            for (; ; )
            {

                byte b = buffer[offset + len];
                len += ASN1.decode_tag_number(buffer, offset + len, out tag_number);

                switch (tag_number)
                {
                    case 0:
                        BacnetDate bdt = new BacnetDate();
                        len += bdt.ASN1decode(buffer, offset + len, len_value);
                        Entries.Add(bdt);
                        break;
                    case 1:
                        BacnetDateRange bdr = new BacnetDateRange();
                        len += bdr.ASN1decode(buffer, offset + len, len_value);
                        Entries.Add(bdr);
                        len++; // closing tag
                        break;
                    case 2:
                        BacnetweekNDay bwd = new BacnetweekNDay();
                        len += bwd.ASN1decode(buffer, offset + len, len_value);
                        Entries.Add(bwd);
                        break;
                    default:
                        return len;
                }
            }

        }
    }

    #endregion
}
