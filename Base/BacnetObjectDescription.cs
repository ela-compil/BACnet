namespace System.IO.BACnet;

public struct BacnetObjectDescription
{
    public BacnetObjectTypes typeId;
    public List<BacnetPropertyIds> propsId;
}
