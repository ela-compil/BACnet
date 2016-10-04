namespace System.IO.BACnet
{
    public struct BacnetPropetyState
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

        public BacnetPropertyStateTypes tag;
        public uint state;

        public override string ToString()
        {
            return $"{tag}:{state}";
        }
    }
}