using System.IO.BACnet.Serialize;
using System.Linq;

namespace System.IO.BACnet
{
    public struct BacnetBitString : IEquatable<BacnetBitString>
    {
        public byte BitsUsed { get; }
        public byte[] Value { get; }

        public byte Length => BitsUsed;
        public bool this[byte bitNumber] => GetBit(bitNumber);

        public BacnetBitString(byte[] value, byte bitsUsed)
        {
            BitsUsed = bitsUsed;
            Value = value;

            if (Value.Length > ASN1.MAX_BITSTRING_BYTES)
                throw new ArgumentException($"max length is {ASN1.MAX_BITSTRING_BYTES}");
        }

        public override string ToString()
        {
            var ret = "";
            for (var i = 0; i < BitsUsed; i++)
            {
                ret = ret + ((Value[i / 8] & (1 << (i % 8))) > 0 ? "1" : "0");
            }

            return ret;
        }

        public BacnetBitString SetBit(byte bitNumber, bool v)
        {
            var byteNumber = (byte)(bitNumber / 8);
            byte bitMask = 1;

            var newValue = Value?.Clone() as byte[] ?? new byte[ASN1.MAX_BITSTRING_BYTES];

            byte newBitsUsed = 0;
            if (byteNumber < ASN1.MAX_BITSTRING_BYTES)
            {
                /* set max bits used */
                newBitsUsed = BitsUsed < bitNumber + 1 ? (byte)(bitNumber + 1) : BitsUsed;
                bitMask = (byte)(bitMask << (bitNumber - byteNumber * 8));
                if (v)
                    newValue[byteNumber] |= bitMask;
                else
                    newValue[byteNumber] &= (byte)~bitMask;
            }

            return new BacnetBitString(newValue, newBitsUsed);
        }

        public bool GetBit(byte bitNumber)
        {
            var byteNumber = (byte)(bitNumber / 8);

            if (byteNumber >= ASN1.MAX_BITSTRING_BYTES || bitNumber >= BitsUsed)
                throw new ArgumentOutOfRangeException(nameof(bitNumber));

            if (Value == null)
                return false;

            var bitMask = (byte)(1 << (bitNumber - byteNumber * 8));
            return (Value[byteNumber] & bitMask) > 0;
        }

        public static BacnetBitString Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return new BacnetBitString(new byte[ASN1.MAX_BITSTRING_BYTES], 0);

            var newBitsUsed = (byte)str.Length;
            var newValue = new byte[ASN1.MAX_BITSTRING_BYTES];

            for (var i = 0; i < newBitsUsed; i++)
            {
                var isSet = str[i] == '1';
                if (isSet) newValue[i / 8] |= (byte)(1 << (i % 8));
            }

            return new BacnetBitString(newValue, newBitsUsed);
        }

        public uint ConvertToInt()
        {
            return Value != null
                ? BitConverter.ToUInt32(Value, 0)
                : 0;
        }

        public static BacnetBitString ConvertFromInt(uint value, byte? bitsUsed = null)
            => new BacnetBitString(BitConverter.GetBytes(value), bitsUsed ?? (byte)(Math.Log(value, 2) + 1));

        public bool Equals(BacnetBitString other)
        {
            return BitsUsed == other.BitsUsed && Value.SequenceEqual(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is BacnetBitString s && Equals(s);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (BitsUsed.GetHashCode() * 397) ^ (Value != null ? Value.Aggregate(397, (i, b) => i ^ b) : 0);
            }
        }
    }
}