namespace System.IO.BACnet;

public struct BACnetCalendarEntry : ASN1.IEncode, ASN1.IDecode
{
    public List<object> Entries; // BacnetDate or BacnetDateRange or BacnetweekNDay

    public void Encode(EncodeBuffer buffer)
    {
        if (Entries == null)
            return;

        foreach (ASN1.IEncode entry in Entries)
        {
            if (entry is BacnetDate)
            {
                ASN1.encode_tag(buffer, 0, true, 4);
                entry.Encode(buffer);
            }

            if (entry is BacnetDateRange)
            {
                ASN1.encode_opening_tag(buffer, 1);
                entry.Encode(buffer);
                ASN1.encode_closing_tag(buffer, 1);
            }

            if (entry is BacnetweekNDay)
            {
                ASN1.encode_tag(buffer, 2, true, 3);
                entry.Encode(buffer);
            }
        }
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        var len = 0;

        Entries = new List<object>();

        while (true)
        {
            len += ASN1.decode_tag_number(buffer, offset + len, out byte tagNumber);

            switch (tagNumber)
            {
                case 0:
                    var bdt = new BacnetDate();
                    len += bdt.Decode(buffer, offset + len, count);
                    Entries.Add(bdt);
                    break;
                case 1:
                    var bdr = new BacnetDateRange();
                    len += bdr.Decode(buffer, offset + len, count);
                    Entries.Add(bdr);
                    len++; // closing tag
                    break;
                case 2:
                    var bwd = new BacnetweekNDay();
                    len += bwd.Decode(buffer, offset + len, count);
                    Entries.Add(bwd);
                    break;
                default:
                    return len - 1; // closing Tag
            }
        }

    }
}
