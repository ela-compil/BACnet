using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO.BACnet
{
    public class BacnetException : Exception
    {
        public BacnetException()
        {
        }

        public BacnetException(string message)
        : base(message)
        {
        }

        public BacnetException(string message, Exception inner)
        : base(message, inner)
        {
        }
    }

    public class BacnetApduTimeoutException : BacnetException
    {
        public BacnetApduTimeoutException(string message)
        : base(message)
        {
        }
    }

    public class BacnetErrorException : BacnetException
    {
        public BacnetErrorClasses ErrorClass { get; }
        public BacnetErrorCodes ErrorCode { get; }

        public BacnetErrorException(BacnetErrorClasses errorClass, BacnetErrorCodes errorCode)
         : base($"Error from device: {errorClass} - {errorCode}")
        {
            ErrorClass = errorClass;
            ErrorCode = errorCode;
        }
    }

    public class BacnetAbortException : BacnetException
    {
        public BacnetAbortReason Reason { get; }
        public BacnetErrorCodes ErrorCode { get; }

        public BacnetAbortException(BacnetAbortReason reason)
        : base($"Abort from device, reason: {reason}")
        {
            Reason = reason;
        }
    }

    public class BacnetRejectException : BacnetException
    {
        public BacnetRejectReason Reason { get; }
        public BacnetErrorCodes ErrorCode { get; }

        public BacnetRejectException(BacnetRejectReason reason)
        : base($"Reject from device, reason: {reason}")
        {
            Reason = reason;
        }
    }
}
