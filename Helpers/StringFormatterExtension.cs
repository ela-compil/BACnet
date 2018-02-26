using System.Collections.Generic;
using System.Linq;

namespace System.IO.BACnet.Helpers
{
    public static class StringFormatterExtension
    {
        public static IEnumerable<string> PropertiesWithValues<TType>(this TType obj, params string[] except)
            where TType : class
        {
            if (obj == null)
                return new string[0];

            return obj.GetType().GetProperties()
                .Where(p => !except.Contains(p.Name))
                .Select(p =>
                {
                    var propertyName = char.ToLower(p.Name[0]) + p.Name.Substring(1);
                    return $"{propertyName}: {p.GetValue(obj, null)}";
                });
        }
    }
}