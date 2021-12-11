namespace System.IO.BACnet.Helpers;

public static class BacnetValuesExtensions
{
    public static bool Has(this IList<BacnetPropertyValue> propertyValues, BacnetPropertyIds propertyId)
    {
        if (propertyValues.All(v => v.property.GetPropertyId() != propertyId))
            return false;

        return propertyValues
            .Where(v => v.property.GetPropertyId() == propertyId)
            .Any(v => !v.value.HasError());
    }

    public static bool HasError(this IList<BacnetPropertyValue> propertyValues, BacnetErrorCodes error)
    {
        return propertyValues
            .SelectMany(p => p.value)
            .Where(v => v.Tag == BacnetApplicationTags.BACNET_APPLICATION_TAG_ERROR)
            .Any(v => v.As<BacnetError>().error_code == error);
    }

    public static bool HasError(this IList<BacnetPropertyValue> propertyValues)
    {
        return propertyValues.Any(p => p.value.HasError());
    }

    public static bool HasError(this IList<BacnetValue> values)
    {
        return values.Any(v => v.Tag == BacnetApplicationTags.BACNET_APPLICATION_TAG_ERROR);
    }

    public static object Get(this IList<BacnetPropertyValue> propertyValues, BacnetPropertyIds propertyId)
    {
        return Get<object>(propertyValues, propertyId);
    }

    public static T Get<T>(this IList<BacnetPropertyValue> propertyValues, BacnetPropertyIds propertyId)
    {
        return GetMany<T>(propertyValues, propertyId).FirstOrDefault();
    }

    public static T[] GetMany<T>(this IList<BacnetPropertyValue> propertyValues, BacnetPropertyIds propertyId)
    {
        if (!propertyValues.Has(propertyId))
            return new T[0];

        var property = propertyValues.First(v => v.property.GetPropertyId() == propertyId);

        return property.property.propertyArrayIndex == ASN1.BACNET_ARRAY_ALL
            ? property.value.GetMany<T>()
            : new[] { property.value[(int)property.property.propertyArrayIndex].As<T>() };
    }

    public static T[] GetMany<T>(this IList<BacnetValue> values)
    {
        return values.Select(v => v.As<T>()).ToArray();
    }

    public static T Get<T>(this IList<BacnetValue> values)
    {
        return GetMany<T>(values).FirstOrDefault();
    }
}
