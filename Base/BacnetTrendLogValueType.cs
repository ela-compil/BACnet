namespace System.IO.BACnet;

public enum BacnetTrendLogValueType : byte
{
    // Copyright (C) 2009 Peter Mc Shane in Steve Karg Stack, trendlog.h
    // Thank's to it's encoding sample, very usefull for this decoding work
    TL_TYPE_STATUS = 0,
    TL_TYPE_BOOL = 1,
    TL_TYPE_REAL = 2,
    TL_TYPE_ENUM = 3,
    TL_TYPE_UNSIGN = 4,
    TL_TYPE_SIGN = 5,
    TL_TYPE_BITS = 6,
    TL_TYPE_NULL = 7,
    TL_TYPE_ERROR = 8,
    TL_TYPE_DELTA = 9,
    TL_TYPE_ANY = 10
}
