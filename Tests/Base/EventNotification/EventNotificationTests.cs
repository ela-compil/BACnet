using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Base.EventNotification
{
    [TestFixture]
    public class EventNotificationTests
    {
        [TestCase(typeof(StateTransition<ChangeOfLifeSafety>))]
        [TestCase(typeof(NotificationData))]
        public void should_override_tostring(Type type)
        {
            var args = type.GetGenericArguments();

            var instance = args.Length > 0
                ? Activator.CreateInstance(type, Activator.CreateInstance(args[0]))
                : Activator.CreateInstance(type);

            Assert.That(instance.ToString(), Is.Not.EqualTo(type.ToString()));
        }

        [Test]
        public void should_raise_oneventnotify_when_sending_changeoflifesafety_data()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<ChangeOfLifeSafety>(new ChangeOfLifeSafety()
            {
                NewMode = BacnetLifeSafetyModes.LIFE_SAFETY_MODE_DEFAULT,
                NewState = BacnetLifeSafetyStates.LIFE_SAFETY_STATE_QUIET,
                OperationExpected = BacnetLifeSafetyOperations.LIFE_SAFETY_OP_NONE,
                StatusFlags = BacnetBitString.Parse("01")
            })
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                EventType = BacnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY,
                FromState = BacnetEventStates.EVENT_STATE_OFFNORMAL,
                InitiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1),
                MessageText = "Dummy Operation",
                NotificationClass = 10,
                NotifyType = BacnetNotifyTypes.NOTIFY_EVENT,
                Priority = 1,
                TimeStamp = new BacnetGenericTime(new DateTime(2018, 2, 22, 16, 14, 15), BacnetTimestampTags.TIME_STAMP_DATETIME),
                ProcessIdentifier = 1,
                ToState = BacnetEventStates.EVENT_STATE_NORMAL
            };

            // act
            client1.SendUnconfirmedEventNotification(Helper.DummyAddress, sentData);

            // assert
            Assert.That(receivedData, Is.Not.SameAs(sentData));
            Helper.AssertPropertiesAndFieldsAreEqual(sentData, receivedData);
        }
    }
}
