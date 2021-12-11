namespace System.IO.BACnet;

public enum BacnetMaxSegments : byte
{
    MAX_SEG0 = 0,
    MAX_SEG2 = 0x10,
    MAX_SEG4 = 0x20,
    MAX_SEG8 = 0x30,
    MAX_SEG16 = 0x40,
    MAX_SEG32 = 0x50,
    MAX_SEG64 = 0x60,
    MAX_SEG65 = 0x70
}
