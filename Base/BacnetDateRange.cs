using System.IO.BACnet.Serialize;

namespace System.IO.BACnet
{
    public struct BacnetDateRange : ASN1.IEncode
    {
        public BacnetDate startDate;
        public BacnetDate endDate;

        public BacnetDateRange(BacnetDate start, BacnetDate end)
        {
            startDate = start;
            endDate = end;
        }

        public void Encode(EncodeBuffer buffer)
        {
            ASN1.encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, false, 4);
            startDate.Encode(buffer);
            ASN1.encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, false, 4);
            endDate.Encode(buffer);
        }

        public int ASN1decode(byte[] buffer, int offset, uint len_value)
        {
            var len = 1; // opening tag
            len += startDate.ASN1decode(buffer, offset + len, len_value);
            len++;
            len += endDate.ASN1decode(buffer, offset + len, len_value);
            return len;
        }

        public bool IsAFittingDate(DateTime date)
        {
            date = new DateTime(date.Year, date.Month, date.Day);
            return (date >= startDate.toDateTime()) && (date <= endDate.toDateTime());
        }

        public override string ToString()
        {
            string ret;

            if (startDate.day != 255)
                ret = "From " + startDate;
            else
                ret = "From **/**/**";

            if (endDate.day != 255)
                ret = ret + " to " + endDate;
            else
                ret = ret + " to **/**/**";

            return ret;
        }
    };
}