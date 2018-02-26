using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Base.EventNotification
{
    [TestFixture]
    public class EventNotificationTests
    {
        [TestCase(typeof(StateTransition<ChangeOfState>))]
        [TestCase(typeof(StateTransition<ChangeOfBitString>))]
        [TestCase(typeof(StateTransition<UnsignedRange>))]
        [TestCase(typeof(StateTransition<BufferReady>))]
        [TestCase(typeof(StateTransition<OutOfRange>))]
        [TestCase(typeof(StateTransition<FloatingLimit>))]
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
        public void should_override_tostring_in_changeofvalue_float()
        {
            var instance = new StateTransition<ChangeOfValue<float>>(ChangeOfValueFactory.CreateNew((float)123.456));

            Assert.That(instance.ToString(), Is.Not.EqualTo(instance.GetType().ToString()));
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

        [Test]
        public void should_raise_oneventnotify_when_sending_changeofbitstring_data()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<ChangeOfBitString>(new ChangeOfBitString()
            {
                ReferencedBitString = BacnetBitString.Parse("101"),
                StatusFlags = BacnetBitString.Parse("010")
            })
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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

        [Test]
        public void should_raise_oneventnotify_when_sending_unsignedrange_data()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<UnsignedRange>(new UnsignedRange()
            {
                ExceededLimit = 100,
                ExceedingValue = 110,
                StatusFlags = BacnetBitString.Parse("010")
            })
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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

        [Test]
        public void should_raise_oneventnotify_when_sending_outofrange_data()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<OutOfRange>(new OutOfRange
            {
                ExceededLimit = float.MinValue,
                ExceedingValue = float.MaxValue,
                Deadband = (float)17.01,
                StatusFlags = BacnetBitString.Parse("010")
            })
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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

        [TestCase(true)]
        [TestCase(false)]
        public void should_raise_oneventnotify_when_sending_changeofstate_data_bool(bool value)
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<ChangeOfState>(new ChangeOfState()
            {
                NewState = new BacnetPropertyState
                {
                    tag = BacnetPropertyState.BacnetPropertyStateTypes.BOOLEAN_VALUE,
                    state = new BacnetPropertyState.State() { boolean_value = value }
                },
                StatusFlags = BacnetBitString.Parse("010")
            })
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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

        [Test]
        public void should_raise_oneventnotify_when_sending_changeofvalue_data_real()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<ChangeOfValue<float>>(
                ChangeOfValueFactory
                    .CreateNew((float)123.456)
                    .SetStatusFlags(BacnetBitString.Parse("010")))
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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

        [Test]
        public void should_raise_oneventnotify_when_sending_changeofvalue_data_bits()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<ChangeOfValue<BacnetBitString>>(
                ChangeOfValueFactory
                    .CreateNew(BacnetBitString.Parse("101"))
                    .SetStatusFlags(BacnetBitString.Parse("010")))
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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

        [Test]
        public void should_raise_oneventnotify_when_sending_bufferready_data()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<BufferReady>(new BufferReady
            {
                BufferProperty = new BacnetDeviceObjectPropertyReference(
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                    BacnetPropertyIds.PROP_PRESENT_VALUE,
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1)),
                CurrentNotification = 5,
                PreviousNotification = 4
            })
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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

        [Test]
        public void should_raise_oneventnotify_when_sending_floatinglimit_data()
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var sentData = new StateTransition<FloatingLimit>(new FloatingLimit()
            {
                ErrorLimit = (float)12.34,
                ReferenceValue = (float)56.78,
                SetPointValue = (float)91.011,
                StatusFlags = BacnetBitString.Parse("010")
            })
            {
                AckRequired = false,
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123),
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
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
