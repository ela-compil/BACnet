using System.IO.BACnet.EventNotification.EventValues;

namespace System.IO.BACnet
{
    public interface IHasStatusFlags
    {
        BacnetBitString StatusFlags { get; set; }
    }

    public static class Extensions
    {
        public static T SetStatusFlags<T>(this T obj, BacnetBitString statusFlags) where T:IHasStatusFlags
        {
            obj.StatusFlags = statusFlags;
            return obj;
        }
    }
}
