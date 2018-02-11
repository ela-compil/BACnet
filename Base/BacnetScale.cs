using System.IO.BACnet.Serialize;

namespace System.IO.BACnet
{
    public class BacnetScale : ASN1.IEncode, ASN1.IDecode
    {
        public enum BacnetScaleTypes : byte
        {
            FLOAT = 0,
            INTEGER = 1,
        }

        public object Val
        {
            get { return val; }
            set { val = value; }
        }

        public object val;

        public BacnetScale(object val)
        {
            this.val = val;
        }

        public void Encode(EncodeBuffer buffer)
        {
            if (val is float)
            {
                ASN1.encode_context_real(buffer, (byte)BacnetScaleTypes.FLOAT, (float)val);
            }
            else if (val is uint)
            {
                ASN1.encode_context_unsigned(buffer, (byte)BacnetScaleTypes.INTEGER, (uint)val);
            }
        }

        public int Decode(byte[] buffer, int offset, uint count)
        {
            var len = 0;
            byte tagNumber;
            uint lenValueType;

            var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            if (tagNumber == (byte)BacnetScaleTypes.FLOAT)
            {
                float oval;
                len += tagLen;
                len += ASN1.decode_real(buffer, offset + len, out oval);

                val = oval;
            }
            else if (tagNumber == (byte)BacnetScaleTypes.INTEGER)
            {
                uint oval;

                len += tagLen;
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out oval);

                val = oval;
            }
            else
                return -1;

            return len;
        }
    }
}