using System.Globalization;
using System.IO.BACnet.Storage;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// The device storage XML is a portable, tool-neutral file: bacnet-stack and YABE both
/// read and write it. REAL, DOUBLE and UNSIGNED_INT were made culture-invariant in #143,
/// but SIGNED_INT still round-trips through the current culture on both sides.
/// Under ICU, sv-SE uses U+2212 (MINUS SIGN) rather than the ASCII hyphen.
/// </summary>
public class PropertyCultureTests
{
    private const string Culture = "sv-SE";

    [Fact]
    public void SignedInt_serializes_invariant()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(Culture);
            var bv = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT, -5);

            var s = Property.SerializeValue(bv, BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT);

            Assert.Equal("-5", s);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void SignedInt_deserializes_ascii_hyphen()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(Culture);

            var bv = Property.DeserializeValue("-5", BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT);

            Assert.Equal(-5, bv.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
