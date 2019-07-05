namespace System.IO.BACnet
{
    // TODO you have been flagged for refactoring due to un-C#-iness
    public struct BacnetGenericTime
    {
        public BacnetTimestampTags Tag;
        public DateTime Time;
        public ushort Sequence;

        public BacnetGenericTime(DateTime time, BacnetTimestampTags tag = BacnetTimestampTags.TIME_STAMP_NONE, ushort sequence = 0)
        {
            Time = time;
            Tag = tag;
            Sequence = sequence;
        }

        public override string ToString()
        {
            return $"{Time}";
        }
    }
}