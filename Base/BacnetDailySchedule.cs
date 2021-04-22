using System;
using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.Linq;
using System.Text;

namespace System.IO.BACnet
{
    public struct BacnetDailySchedule : ASN1.IEncode, ASN1.IDecode
    {
        public List<BacnetTimeValue> DaySchedule;

        

        public int Decode(byte[] buffer, int offset, uint count)
        {
            int len = 0;
            DaySchedule = new List<BacnetTimeValue>();
            //begin of daily sched
            if (ASN1.IS_OPENING_TAG(buffer[offset + len]))
            {
                len++;
                //end of daily sched
                while (!ASN1.IS_CLOSING_TAG(buffer[offset + len]) )
                {
                    len++; //ignore apptag time ?
                    len += ASN1.decode_bacnet_time(buffer, offset + len, out DateTime time);


                    var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out BacnetApplicationTags tagNumber, out uint lenValueType);
                    BacnetValue value;
                    if (tagLen > 0)
                    {
                        len += tagLen;
                        var decodeLen = ASN1.bacapp_decode_data(buffer, offset + len, offset + len + 1, tagNumber, lenValueType, out value);
                        len += decodeLen;
                    }
                    else
                    {
                        value = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, null);
                    }


                    DaySchedule.Add(new BacnetTimeValue(new BacnetGenericTime(time, BacnetTimestampTags.TIME_STAMP_TIME), value));

                }
                //closing tag
                len++;
            }
            
            return len;
        }

        public void Encode(EncodeBuffer buffer)
        {
            ASN1.encode_opening_tag(buffer, 0);

            if (DaySchedule != null)
            {
                foreach (var dayItem in DaySchedule)
                {

                    ASN1.encode_tag(buffer, (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, false, 4);
                    ASN1.encode_bacnet_time(buffer, dayItem.Time.Time);

                    ASN1.bacapp_encode_application_data(buffer, dayItem.Value);
                }
            }
            ASN1.encode_closing_tag(buffer, 0);
        }

        public override string ToString()
        {
            return $"DaySchedule Len: {DaySchedule?.Count()}";
        }

    }
}
