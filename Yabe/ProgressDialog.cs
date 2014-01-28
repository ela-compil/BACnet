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

namespace Yabe
{
    public partial class ProgressDialog : Form
    {
        public int Minimum { get { return m_progessbar.Minimum; } set { m_progessbar.Minimum = value; } }
        public int Maximum { get { return m_progessbar.Maximum; } set { m_progessbar.Maximum = value; } }

        public event EventHandler Cancel;

        public ProgressDialog()
        {
            InitializeComponent();
        }

        public int Value
        {
            get
            {
                return m_progessbar.Value;
            }
            set
            {
                m_progessbar.Value = value;
                Application.DoEvents();
            }
        }

        public void Increment(int value)
        {
            m_progessbar.Increment(value);
            Application.DoEvents();
        }

        public string Label
        {
            get
            {
                return m_Label.Text;
            }
            set
            {
                m_Label.Text = value;
                Application.DoEvents();
            }
        }

        public bool IsCancelled 
        {
            get
            {
                if (this.IsDisposed || this.DialogResult == System.Windows.Forms.DialogResult.Cancel) return true;
                else return false;
            }
        }

        private void m_CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ProgressDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Cancel != null) Cancel(this, null);
        }
    }
}
