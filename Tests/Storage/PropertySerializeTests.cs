using System.Globalization;
using System.IO.BACnet.Storage;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Property.SerializeValue for numeric application tags. Regression tests for #143:
/// double values must keep full precision, and values boxed as a different numeric
/// type than their tag must convert instead of throwing InvalidCastException.
/// </summary>
public class PropertySerializeTests
{
    [Fact]
    public void Double_keeps_full_precision()
    {
        const double value = 3.141592653589793d; // more precision than a float can hold
        var bv = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE, value);

        var s = Property.SerializeValue(bv, BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE);

        Assert.Equal(value, double.Parse(s, CultureInfo.InvariantCulture)); // no double->float truncation
    }

    [Fact]
    public void Real_serializes_invariant()
    {
        var bv = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, 65.5f);

        var s = Property.SerializeValue(bv, BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL);

        Assert.Equal("65.5", s);
    }

    [Fact]
    public void UnsignedInt_serializes()
    {
        var bv = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, 4194303u);

        var s = Property.SerializeValue(bv, BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT);

        Assert.Equal("4194303", s);
    }

    [Fact]
    public void Real_from_value_boxed_as_double_does_not_throw()
    {
        // The #143 scenario: a double boxed under a REAL tag. The old (float)value.Value
        // cast threw InvalidCastException; Convert.ToSingle handles it.
        var bv = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, 42.5d);

        var s = Property.SerializeValue(bv, BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL);

        Assert.Equal("42.5", s);
    }
}
