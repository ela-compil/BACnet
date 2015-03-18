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
        }

        private void sendFDR_Click(object sender, EventArgs e)
        {
            client.RegisterAsForeignDevice(BBMD_IP.Text, 30);
            Thread.Sleep(50);
            client.RemoteWhoIs(BBMD_IP.Text);
            SendWhois.Enabled = true;
        }

        private void SendWhois_Click(object sender, EventArgs e)
        {
            client.RemoteWhoIs(BBMD_IP.Text);
        }

        private void BBMD_IP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                sendFDR_Click(null, null);
        }
    }
}
