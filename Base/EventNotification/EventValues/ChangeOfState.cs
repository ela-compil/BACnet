using System.Diagnostics.CodeAnalysis;
using System.IO.BACnet.Base;
using System.IO.BACnet.Helpers;
using System.Reflection;

namespace System.IO.BACnet.EventNotification.EventValues
{
    public abstract class ChangeOfState : EventValuesBase, IHasStatusFlags
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_CHANGE_OF_STATE;

        public BacnetBitString StatusFlags { get; set; }
    }

    public class ChangeOfState<T> : ChangeOfState
    {

        public T NewState { get; set; }

        protected ChangeOfState(T value)
        {
            NewState = value;
        }

        public static ChangeOfState<bool> Create(bool value)
            => new ChangeOfState<bool>(value);

        public static ChangeOfState<BacnetBinaryPv> Create(BacnetBinaryPv value)
            => new ChangeOfState<BacnetBinaryPv>(value);

        public static ChangeOfState<BacnetEventTypes> Create(BacnetEventTypes value)
            => new ChangeOfState<BacnetEventTypes>(value);

        public static ChangeOfState<BacnetPolarity> Create(BacnetPolarity value)
            => new ChangeOfState<BacnetPolarity>(value);

        public static ChangeOfState<BacnetProgramRequest> Create(BacnetProgramRequest value)
            => new ChangeOfState<BacnetProgramRequest>(value);

        public static ChangeOfState<BacnetProgramState> Create(BacnetProgramState value)
            => new ChangeOfState<BacnetProgramState>(value);

        public static ChangeOfState<BacnetProgramError> Create(BacnetProgramError value)
            => new ChangeOfState<BacnetProgramError>(value);

        public static ChangeOfState<BacnetReliability> Create(BacnetReliability value)
            => new ChangeOfState<BacnetReliability>(value);

        public static ChangeOfState<BacnetEventStates> Create(BacnetEventStates value)
            => new ChangeOfState<BacnetEventStates>(value);

        public static ChangeOfState<BacnetDeviceStatus> Create(BacnetDeviceStatus value)
            => new ChangeOfState<BacnetDeviceStatus>(value);

        public static ChangeOfState<BacnetUnitsId> Create(BacnetUnitsId value)
            => new ChangeOfState<BacnetUnitsId>(value);

        public static ChangeOfState<uint> Create(uint value)
            => new ChangeOfState<uint>(value);

        public static ChangeOfState<BacnetLifeSafetyModes> Create(BacnetLifeSafetyModes value)
            => new ChangeOfState<BacnetLifeSafetyModes>(value);

        public static ChangeOfState<BacnetLifeSafetyStates> Create(BacnetLifeSafetyStates value)
            => new ChangeOfState<BacnetLifeSafetyStates>(value);
    }

    [ExcludeFromCodeCoverage]
    public abstract class ChangeOfStateFactory : ChangeOfState<object>
    {
        protected ChangeOfStateFactory(object value) : base(value)
        {
            throw new InvalidOperationException("this is a dummy class to avoid static call with generic type parameter");
        }
    }
}