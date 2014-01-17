using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Yabe
{
    public partial class SettingsDialog : Form
    {
        public SettingsDialog()
        {
            InitializeComponent();
        }

        public object SelectedObject { get { return m_SettingsGrid.SelectedObject; } set { m_SettingsGrid.SelectedObject = value; } }

        private void SettingsDialog_Load(object sender, EventArgs e)
        {

        }
    }
}
