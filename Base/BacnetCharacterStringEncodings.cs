namespace System.IO.BACnet;

public enum BacnetCharacterStringEncodings
{
    CHARACTER_ANSI_X34 = 0,  /* deprecated : Addendum 135-2008k  */
    CHARACTER_UTF8 = 0,
    CHARACTER_MS_DBCS = 1,
    CHARACTER_JISC_6226 = 2, /* deprecated : Addendum 135-2008k  */
    CHARACTER_JISX_0208 = 2,
    CHARACTER_UCS4 = 3,
    CHARACTER_UCS2 = 4,
    CHARACTER_ISO8859 = 5
}
