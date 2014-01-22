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

        public int Minimum { get { return m_progessbar.Minimum; } set { m_progessbar.Minimum = value; } }
        public int Maximum { get { return m_progessbar.Maximum; } set { m_progessbar.Maximum = value; } }

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
    }
}
