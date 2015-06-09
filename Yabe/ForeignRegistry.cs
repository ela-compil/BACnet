using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO.BACnet;
using System.Threading;

namespace Yabe
{
    public partial class ForeignRegistry : Form
    {
        BacnetClient client;

        public ForeignRegistry(BacnetClient client)
        {
            this.client = client;
            InitializeComponent();
            BBMD_IP.Text = Properties.Settings.Default.DefaultBBMD;
        }

        private int PortNumber()
        {
            int Port;
            Int32.TryParse(BBMD_Port.Text, out Port);
            return Port==0 ? 47808 : Port;

        }
        private void sendFDR_Click(object sender, EventArgs e)
        {
            client.RegisterAsForeignDevice(BBMD_IP.Text, 30, PortNumber());
            Thread.Sleep(50);
            client.RemoteWhoIs(BBMD_IP.Text, PortNumber());
            SendWhois.Enabled = true;
        }

        private void SendWhois_Click(object sender, EventArgs e)
        {
            client.RemoteWhoIs(BBMD_IP.Text, PortNumber());
        }

        private void BBMD_IP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                sendFDR_Click(null, null);
        }
    }
}
