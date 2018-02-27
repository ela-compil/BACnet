using System.Reflection;

namespace System.IO.BACnet.Helpers
{
    public static class FactoryHelper
    {
        public static TReturn CreateReflected<TFactory, TReturn>(object value) where TReturn : class
        {
            var mi = typeof(TFactory).GetMethod(
                "Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy, null, new[] { value.GetType() }, null);

            if (mi == null)
                throw new InvalidOperationException("factory method not found");

            return mi.Invoke(null, new[] { value }) as TReturn;
        }
    }
}
