using System;
using System.IO.BACnet;

namespace ObjectBrowseSample
{
    internal class Program
    {
        private static void Main()
        {
            var transport = new BacnetIpUdpProtocolTransport(0xBAC0, true);
            var client = new BacnetClient(transport);
            client.OnIam += OnIAm;
            client.Start();
            client.WhoIs();
            Console.ReadLine();
        }

        private static async void OnIAm(BacnetClient sender, BacnetAddress adr,
            uint deviceid, uint maxapdu, BacnetSegmentations segmentation, ushort vendorid)
        {
            Console.WriteLine($"Detected device {deviceid} at {adr}");

            // In theory each bacnet device should have object of type OBJECT_DEVICE with property PROP_OBJECT_LIST
            // This property is a list of all bacnet objects (ids) of that device

            var deviceObjId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceid);
            var objectIdList = await sender.ReadPropertyAsync(adr, deviceObjId, BacnetPropertyIds.PROP_OBJECT_LIST);

            foreach (var objId in objectIdList)
                Console.WriteLine($"{objId}");
        }
    }
}
