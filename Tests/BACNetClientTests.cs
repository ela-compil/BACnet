using NUnit.Framework;

namespace System.IO.BACnet.Tests
{
    [TestFixture]
    public class BacNetClientTests
    {
        [Test]
        public void should_send_and_raise_iam()
        {
            // arrange
            const int sendingDeviceId = 123;
            var iAmCount = 0;
            var lastReceivedId = -1;
            var (client1, client2) = Helper.CreateConnectedClients();

            client2.OnIam += (sender, address, id, apdu, segmentation, vendorId) =>
            {
                iAmCount++;
                lastReceivedId = (int)id;
            };

            // act
            client1.Iam(sendingDeviceId);

            // assert
            Assert.That(iAmCount, Is.EqualTo(1));
            Assert.That(lastReceivedId, Is.EqualTo(sendingDeviceId));
        }
    }
}
