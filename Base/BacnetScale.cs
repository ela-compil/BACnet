using System.IO.BACnet.Serialize;

namespace System.IO.BACnet
{
    public class BacnetScale : ASN1.IEncode, ASN1.IDecode
    {
        public enum ValueType : byte
        {
            FLOAT = 0,
            INTEGER = 1,
        }

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public object value;

        public BacnetScale(object value)
        {
            this.value = value;
        }

        public void Encode(EncodeBuffer buffer)
        {
            if (value is float floatVal)
            {
                ASN1.encode_context_real(buffer, (byte)ValueType.FLOAT, floatVal);
            }
            else if (value is uint uintVal)
            {
                ASN1.encode_context_unsigned(buffer, (byte)ValueType.INTEGER, uintVal);
            }
        }

        public int Decode(byte[] buffer, int offset, uint count)
        {
            var len = 0;

            var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber == (byte)ValueType.FLOAT)
            {
                float oval;
                len += tagLen;
                len += ASN1.decode_real(buffer, offset + len, out oval);

                value = oval;
            }
            else if (tagNumber == (byte)ValueType.INTEGER)
            {
                len += tagLen;
                len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out var oval);

                value = oval;
            }
            else
                return -1;

            return len;
        }
    }
}