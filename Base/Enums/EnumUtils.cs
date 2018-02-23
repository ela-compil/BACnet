/*
 * This is a hack, and should go away!
 *
 * discovered here: https://stackoverflow.com/a/28527552
 */

using System.IO.BACnet.Serialize;

namespace System.IO.BACnet
{
    public abstract class EnumClassUtils<TClass> where TClass : class
    {
        public static int DecodeEnumerated<TEnum>(byte[] buffer, int offset, uint lenValue, out TEnum value) where TEnum: struct, TClass
        {
            var len = ASN1.decode_unsigned(buffer, offset, lenValue, out var rawValue);
            value = (TEnum)(dynamic)rawValue;
            return len;
        }

        public static int DecodeContextEnumerated<TEnum>(byte[] buffer, int offset, byte tagNumber, out TEnum value) where TEnum : struct, TClass
        {
            if (!ASN1.decode_is_context_tag(buffer, offset, tagNumber) || ASN1.decode_is_closing_tag(buffer, offset))
            {
                value = default;
                return -1;
            }

            var len = ASN1.decode_tag_number_and_value(buffer, offset, out _, out var lenValue);
            return len + DecodeEnumerated(buffer, offset + len, lenValue, out value);
        }
    }

    public class EnumUtils : EnumClassUtils<Enum>
    {
    }
}
