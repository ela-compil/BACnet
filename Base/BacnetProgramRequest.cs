﻿namespace System.IO.BACnet.Base
{
    public enum BacnetProgramRequest
    {
        PROGRAM_REQUEST_READY = 0,
        PROGRAM_REQUEST_LOAD = 1,
        PROGRAM_REQUEST_RUN = 2,
        PROGRAM_REQUEST_HALT = 3,
        PROGRAM_REQUEST_RESTART = 4,
        PROGRAM_REQUEST_UNLOAD = 5
    }
}
