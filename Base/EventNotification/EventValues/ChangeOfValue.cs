using System.Diagnostics.CodeAnalysis;

namespace System.IO.BACnet.EventNotification.EventValues
{
    public abstract class ChangeOfValue : EventValuesBase, IHasStatusFlags
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_CHANGE_OF_VALUE;
        public BacnetCOVTypes Tag { get; protected set; }
        public BacnetBitString StatusFlags { get; set; }
    }

    public class ChangeOfValue<T> : ChangeOfValue
    {
        public T ChangedValue { get; }

        protected ChangeOfValue(T value, BacnetCOVTypes type)
        {
            ChangedValue = value;
            Tag = type;
        }

        public static ChangeOfValue<float> Create(float value)
            => new ChangeOfValue<float>(value, BacnetCOVTypes.CHANGE_OF_VALUE_REAL);

        public static ChangeOfValue<BacnetBitString> Create(BacnetBitString value)
            => new ChangeOfValue<BacnetBitString>(value, BacnetCOVTypes.CHANGE_OF_VALUE_BITS);
    }

    [ExcludeFromCodeCoverage]
    public abstract class ChangeOfValueFactory : ChangeOfValue<object>
    {
        protected ChangeOfValueFactory(object value, BacnetCOVTypes type) : base(value, type)
        {
            throw new InvalidOperationException("this is a dummy class to avoid static call with generic type parameter");
        }
    }
}