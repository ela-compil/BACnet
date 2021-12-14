namespace System.IO.BACnet;

public struct BacnetPropertyState
{
    public enum BacnetPropertyStateTypes
    {
        BOOLEAN_VALUE,
        BINARY_VALUE,
        EVENT_TYPE,
        POLARITY,
        PROGRAM_CHANGE,
        PROGRAM_STATE,
        REASON_FOR_HALT,
        RELIABILITY,
        STATE,
        SYSTEM_STATUS,
        UNITS,
        UNSIGNED_VALUE,
        LIFE_SAFETY_MODE,
        LIFE_SAFETY_STATE
    }

    public struct State
    {
        public bool boolean_value;
        public BacnetBinaryPv binaryValue;
        public BacnetEventTypes eventType;
        public BacnetPolarity polarity;
        public BacnetProgramRequest programChange;
        public BacnetProgramState programState;
        public BacnetProgramError programError;
        public BacnetReliability reliability;
        public BacnetEventStates state;
        public BacnetDeviceStatus systemStatus;
        public BacnetUnitsId units;
        public uint unsignedValue;
        public BacnetLifeSafetyModes lifeSafetyMode;
        public BacnetLifeSafetyStates lifeSafetyState;
    }

    public BacnetPropertyStateTypes tag;
    public State state;

    public override string ToString()
    {
        return $"{tag}:{state}";
    }
}
