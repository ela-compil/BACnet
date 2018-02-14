using System.IO.BACnet.Serialize;

namespace System.IO.BACnet
{
    public class BacnetPrescale : ASN1.IEncode, ASN1.IDecode
    {
        public uint Multiplier
        {
            get { return multiplier; }
            set { multiplier = value; }
        }

        public uint Modulo
        {
            get { return modulo; }
            set { modulo = value; }
        }

        public uint multiplier;
        public uint modulo;

        public BacnetPrescale(uint multiplier, uint modulo)
        {
            this.multiplier = multiplier;
            this.modulo = modulo;
        }

        public void Encode(EncodeBuffer buffer)
        {
            ASN1.encode_context_unsigned(buffer, 0, multiplier);
            ASN1.encode_context_unsigned(buffer, 1, modulo);
        }

        public int Decode(byte[] buffer, int offset, uint count)
        {
            var len = 0;

            var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out var tagNumber, out var lenValueType);
            if (tagNumber != 0)
            {
                return -1;
            }
            len += tagLen;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out multiplier);

            tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out tagNumber, out lenValueType);
            if (tagNumber != 1)
            {
                return -1;
            }
            len += tagLen;
            len += ASN1.decode_unsigned(buffer, offset + len, lenValueType, out modulo);
            return len;
        }
    }
}