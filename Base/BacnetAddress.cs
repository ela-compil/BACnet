namespace System.IO.BACnet;

public class BacnetAddress : ASN1.IEncode
{
    public ushort net;
    public byte[] adr;
    public byte[] VMac = new byte[3]; // for IP V6, could be integrated also as 3 additional bytes in adr
    public BacnetAddressTypes type;

    // Modif FC
    public BacnetAddress RoutedSource = null;

    // DAL
    public BacnetAddress RoutedDestination = null;

    public BacnetAddress(BacnetAddressTypes addressType, ushort network = 0, byte[] address = null)
    {
        type = addressType;
        net = network;
        adr = address;
    }

    public BacnetAddress(BacnetAddressTypes addressType, string address = null, ushort network = 0)
        : this(addressType, network)
    {
        if (address == null)
            return;

        switch (type)
        {
            case BacnetAddressTypes.IP:
                adr = new byte[6];
                var addressParts = address.Split(':');
                var addressBytes = IPAddress.Parse(addressParts[0]).GetAddressBytes();
                Array.Copy(addressBytes, adr, addressBytes.Length);

                var portBytes = BitConverter.GetBytes(addressParts.Length > 1
                    ? ushort.Parse(addressParts[1])
                    : (ushort)0xBAC0);

                if (BitConverter.IsLittleEndian)
                    portBytes = portBytes.Reverse().ToArray();

                Array.Copy(portBytes, 0, adr, addressBytes.Length, portBytes.Length);
                break;

            case BacnetAddressTypes.Ethernet:
                adr = PhysicalAddress.Parse(address).GetAddressBytes();
                break;

            default:
                throw new NotSupportedException("String format is not supported for address type " + type);
        }
    }

    public override int GetHashCode()
    {
        // DAL this was originally broken...
        var str = Convert.ToBase64String(adr);
        return str.GetHashCode();
    }

    public override string ToString()
    {
        return ToString(type);
    }

    public string ToString(BacnetAddressTypes addressType)
    {
        while (true)
        {
            switch (addressType)
            {
                case BacnetAddressTypes.IP:
                    return adr != null && adr.Length >= 6
                        ? $"{adr[0]}.{adr[1]}.{adr[2]}.{adr[3]}:{(adr[4] << 8) | adr[5]}"
                        : "0.0.0.0";

                case BacnetAddressTypes.MSTP:
                    return adr != null && adr.Length >= 1
                        ? $"{adr[0]}"
                        : "-1";

                case BacnetAddressTypes.PTP:
                    return "x";

                case BacnetAddressTypes.Ethernet:
                    return $"{new PhysicalAddress(adr)}";

                case BacnetAddressTypes.IPV6:
                    return adr != null && adr.Length == 18
                        ? $"{new IPAddress(adr.Take(16).ToArray())}:{(adr[16] << 8) | adr[17]}"
                        : "[::]";

                default: // Routed @ are always like this, NPDU do not contains the MAC type, only the lenght
                    if (adr == null || adr.Length == 0)
                        return "?";

                    switch (adr.Length)
                    {
                        case 6: // certainly IP, but not sure (Newron System send it for internal usage with 4*0 bytes)
                            addressType = BacnetAddressTypes.IP;
                            continue;

                        case 18: // Not sure it could appears, since NPDU may contains Vmac ?
                            addressType = BacnetAddressTypes.IPV6;
                            continue;

                        case 3:
                            return $"IPv6 VMac : {adr[0] << 16 | (adr[1] << 8) | adr[2]}";

                        default:
                            return string.Join(" ", adr);
                    }
            }
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

    public bool HasAddress(IPAddress ipAddress)
    {
        if (type != BacnetAddressTypes.IP || adr == null || ipAddress == null)
            return false;

        return adr.Take(4).SequenceEqual(ipAddress.GetAddressBytes());
    }

    public override bool Equals(object obj)
    {
        if (obj is not BacnetAddress) return false;
        var d = (BacnetAddress)obj;
        if (adr == null && d.adr == null) return true;
        if (adr == null || d.adr == null) return false;
        if (adr.Length != d.adr.Length) return false;
        if (adr.Where((t, i) => t != d.adr[i]).Any())
            return false;

        // Modif FC
        if (RoutedSource == null && d.RoutedSource != null)
            return false;

        // DAL
        if (RoutedDestination == null && d.RoutedDestination != null)
            return false;

        if (d.RoutedSource == null && RoutedSource == null &&
            d.RoutedDestination == null && RoutedDestination == null)
            return true;

        bool rv = RoutedSource?.Equals(d.RoutedSource) ?? false;
        rv |= RoutedDestination?.Equals(d.RoutedDestination) ?? false;
        return rv;
    }

    // checked if device is routed by curent equipement
    public bool IsMyRouter(BacnetAddress device)
    {
        if (device.RoutedSource == null || RoutedSource != null)
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

        if (RoutedDestination != null)
            hash += $":{RoutedDestination.FullHashString()}";

        return hash;
    }
}
