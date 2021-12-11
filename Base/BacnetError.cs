namespace System.IO.BACnet;

public struct BacnetError
{
    public BacnetErrorClasses error_class;
    public BacnetErrorCodes error_code;

    public BacnetError(BacnetErrorClasses errorClass, BacnetErrorCodes errorCode)
    {
        error_class = errorClass;
        error_code = errorCode;
    }
    public BacnetError(uint errorClass, uint errorCode)
    {
        error_class = (BacnetErrorClasses)errorClass;
        error_code = (BacnetErrorCodes)errorCode;
    }
    public override string ToString()
    {
        return $"{error_class}: {error_code}";
    }
}
