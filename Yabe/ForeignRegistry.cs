using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO.BACnet;
using System.Threading;
using System.Net;
using System.Linq;
using System.Net.Sockets;

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
            try
            {
                IPAddress[] IPs = Dns.GetHostAddresses(BBMD_IP.Text);

                IPAddress IP;

                if (client.Transport is BacnetIpUdpProtocolTransport)
                    IP = IPs.First<IPAddress>(o => o.AddressFamily == AddressFamily.InterNetwork);
                else
                    IP = IPs.First<IPAddress>(o => o.AddressFamily == AddressFamily.InterNetworkV6);

                client.RegisterAsForeignDevice(IP.ToString(), 30, PortNumber());
                Thread.Sleep(50);
                client.RemoteWhoIs(IP.ToString(), PortNumber());
                SendWhois.Enabled = true;
            }
            catch { }
        }

        private void SendWhois_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress[] IPs = Dns.GetHostAddresses(BBMD_IP.Text);

                IPAddress IP;

                if (client.Transport is BacnetIpUdpProtocolTransport)
                    IP = IPs.First<IPAddress>(o => o.AddressFamily == AddressFamily.InterNetwork);
                else
                    IP = IPs.First<IPAddress>(o => o.AddressFamily == AddressFamily.InterNetworkV6);

                client.RemoteWhoIs(IP.ToString(), PortNumber());
            }
            catch { }
        }

        private void BBMD_IP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                sendFDR_Click(null, null);
        }
    }
}
