namespace System.IO.BACnet.Serialize;

[Flags]
public enum EncodeResult
{
    Good = 0,
    NotEnoughBuffer = 1
}
