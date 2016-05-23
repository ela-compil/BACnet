using System.IO.BACnet.Serialize;
using System.Linq;
using System.Net;
using System.Text;

namespace System.IO.BACnet
{
    public class BacnetAddress : ASN1.IEncode
    {
        public ushort net;
        public byte[] adr;
        public byte[] VMac=new byte[3]; // for IP V6, could be integrated also as 3 additional bytes in adr
        public BacnetAddressTypes type;

        // Modif FC
        public BacnetAddress RoutedSource=null;

        public BacnetAddress(BacnetAddressTypes type, ushort net, byte[] adr)
        {
            this.type = type;
            this.net = net;
            this.adr = adr ?? new byte[0];
        }

        public BacnetAddress(BacnetAddressTypes type, string s)
        {
            this.type = type;
            switch (type)
            {
                case BacnetAddressTypes.IP:
                    try
                    {
                        var ipStrCut = s.Split(':');
                        IPAddress ip;
                        var isIp = IPAddress.TryParse(ipStrCut[0], out ip);
                        uint port = Convert.ToUInt16(ipStrCut[1]);
                        if (isIp)
                        {
                            var cut = ipStrCut[0].Split('.');
                            adr=new byte[6];
                            for (var i=0;i<4;i++)
                                adr[i]=Convert.ToByte(cut[i]); 
                            adr[4] = (byte)((port & 0xff00) >> 8);
                            adr[5] = (byte)(port & 0xff);
                        }
                        else
                            throw new Exception();
                    }
                    catch { throw new Exception(); }
                    break;
                case BacnetAddressTypes.Ethernet:
                    try
                    {
                        var ethStrCut = s.Split('-');
                        adr=new byte[6];
                        for (var i = 0; i < 6; i++)
                            adr[i] = Convert.ToByte(ethStrCut[i], 16);
                    }
                    catch { throw new Exception(); }
                    break;
            }
        }

        public BacnetAddress()
        {
            type = BacnetAddressTypes.None;
        }

        public override int GetHashCode()
        {
            return adr.GetHashCode();
        }

        public override string ToString()
        {
            return ToString(type);
        }

        public string ToString(BacnetAddressTypes type)
        {
            switch (type)
            {
                case BacnetAddressTypes.IP:
                    if(adr == null || adr.Length < 6) return "0.0.0.0";
                    return adr[0] + "." + adr[1] + "." + adr[2] + "." + adr[3] + ":" + ((adr[4] << 8) | (adr[5] << 0));
                case BacnetAddressTypes.MSTP:
                    if(adr == null || adr.Length < 1) return "-1";
                    return adr[0].ToString();
                case BacnetAddressTypes.PTP:
                    return "x";
                case BacnetAddressTypes.Ethernet:
                    var sb1 = new StringBuilder();
                    for (var i = 0; i < 6; i++)
                    {
                        sb1.Append(adr[i].ToString("X2"));
                        if (i != 5) sb1.Append('-');
                    }

                    return sb1.ToString();
                case BacnetAddressTypes.IPV6:
                    if (adr == null || adr.Length != 18) return "[::]";
                    var port = (ushort)((adr[16] << 8) | (adr[17] << 0));
                    var ipv6 = new byte[16];
                    Array.Copy(adr, ipv6, 16);
                    var ep = new IPEndPoint(new IPAddress(ipv6), (int)port);
                    return ep.ToString();

                default: // Routed @ are always like this, NPDU do not contains the MAC type, only the lenght
                    if (adr == null) return "?";

                    if (adr.Length == 6) // certainly IP, but not sure (Newron System send it for internal usage with 4*0 bytes)
                        return ToString(BacnetAddressTypes.IP);                   

                    if (adr.Length == 18)   // Not sure it could appears, since NPDU may contains Vmac ?
                        return ToString(BacnetAddressTypes.IPV6); 

                    if (adr.Length==3)
                        return "IPv6 VMac : "+((int)(adr[0] << 16) | (adr[1] << 8) | adr[2]);

                    return string.Join(" ", adr);
            }
        }

        public string ToString(bool sourceOnly)
        {
            if (RoutedSource == null) 
                return ToString();

            return sourceOnly
                ? RoutedSource.ToString()
                : $"{RoutedSource} via {ToString()}";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BacnetAddress)) return false;
            var d = (BacnetAddress)obj;
            if (adr == null && d.adr == null) return true;
            if (adr == null || d.adr == null) return false;
            if (adr.Length != d.adr.Length) return false;
            if (adr.Where((t, i) => t != d.adr[i]).Any())
                return false;

            // Modif FC
            if ((RoutedSource == null) && (d.RoutedSource != null))
                return false;                  
            if ((d.RoutedSource==null)&&(RoutedSource == null)) return true;
            return RoutedSource?.Equals(d.RoutedSource) ?? false;
        }

        // checked if device is routed by curent equipement
        public bool IsMyRouter(BacnetAddress device)
        {
            if ((device.RoutedSource == null)||(RoutedSource!=null))
                return false;
            if (adr.Length != device.adr.Length)
                return false;

            return !adr.Where((t, i) => t != device.adr[i]).Any();
        }

        public void Encode(EncodeBuffer buffer)
        {
            ASN1.encode_opening_tag(buffer, 1);
            ASN1.encode_application_unsigned(buffer, net);
            ASN1.encode_application_octet_string(buffer, adr, 0, adr.Length);
            ASN1.encode_closing_tag(buffer, 1);
        }

        public string FullHashString()
        {
            var hash = $"{(uint)type}.{net}.{string.Concat(adr.Select(a => a.ToString("X2")))}";

            if (RoutedSource != null)
                hash += $":{RoutedSource.FullHashString()}";

            return hash;
        }
    }
}