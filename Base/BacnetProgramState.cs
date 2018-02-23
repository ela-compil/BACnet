﻿namespace System.IO.BACnet.Base
{
    public enum BacnetProgramState
    {
        PROGRAM_STATE_IDLE = 0,
        PROGRAM_STATE_LOADING = 1,
        PROGRAM_STATE_RUNNING = 2,
        PROGRAM_STATE_WAITING = 3,
        PROGRAM_STATE_HALTED = 4,
        PROGRAM_STATE_UNLOADING = 5
    }
}
