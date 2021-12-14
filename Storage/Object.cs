namespace System.IO.BACnet.Storage;

[Serializable]
public class Object
{
    [XmlAttribute]
    public BacnetObjectTypes Type { get; set; }

    [XmlAttribute]
    public uint Instance { get; set; }

    public Property[] Properties { get; set; }

    public Object()
    {
        Properties = new Property[0];
    }
}
