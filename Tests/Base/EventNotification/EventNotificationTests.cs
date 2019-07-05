using System.IO.BACnet.Base;
using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using System.IO.BACnet.Helpers;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Base.EventNotification
{
    [TestFixture]
    public class EventNotificationTests
    {
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
            var instance = new StateTransition<ChangeOfValue<float>>(ChangeOfValueFactory.Create(123.456f));

            Assert.That(instance.ToString(), Is.Not.EqualTo(instance.GetType().ToString()));
        }

        [Test]
        public void should_override_tostring_in_changeofvalue_bool()
        {
            var instance = new StateTransition<ChangeOfState<bool>>(ChangeOfStateFactory.Create(true));

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
                Deadband = 17.01f,
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

            var sentData =
                new StateTransition<ChangeOfState<bool>>(ChangeOfStateFactory.Create(value)
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
                    TimeStamp = new BacnetGenericTime(new DateTime(2018, 2, 22, 16, 14, 15),
                        BacnetTimestampTags.TIME_STAMP_DATETIME),
                    ProcessIdentifier = 1,
                    ToState = BacnetEventStates.EVENT_STATE_NORMAL
                };

            // act
            client1.SendUnconfirmedEventNotification(Helper.DummyAddress, sentData);

            // assert
            Assert.That(receivedData, Is.Not.SameAs(sentData));
            Helper.AssertPropertiesAndFieldsAreEqual(sentData, receivedData);
        }

        [TestCase(default(BacnetBinaryPv))]
        [TestCase(default(BacnetEventTypes))]
        [TestCase(default(BacnetPolarity))]
        [TestCase(default(BacnetProgramRequest))]
        [TestCase(default(BacnetProgramState))]
        [TestCase(default(BacnetProgramError))]
        [TestCase(default(BacnetReliability))]
        [TestCase(default(BacnetEventStates))]
        [TestCase(default(BacnetDeviceStatus))]
        [TestCase(default(BacnetUnitsId))]
        [TestCase(default(uint))]
        [TestCase(default(BacnetLifeSafetyModes))]
        [TestCase(default(BacnetLifeSafetyStates))]
        public void should_raise_oneventnotify_when_sending_changeofstate_data(object value)
        {
            // arrange
            var (client1, client2) = Helper.CreateConnectedClients();
            StateTransition receivedData = null;
            client2.OnEventNotify += (sender, address, id, data, confirm) => receivedData = data as StateTransition;

            var t = value.GetType();
            var cosType = typeof(ChangeOfState<>).MakeGenericType(t);
            var stType = typeof(StateTransition<>).MakeGenericType(cosType);

            var sentData = Activator.CreateInstance(stType,
                    FactoryHelper.CreateReflected<ChangeOfStateFactory, ChangeOfState>(value).SetStatusFlags(
                        BacnetBitString.Parse("010")))
                as StateTransition;

            Assert.That(sentData, Is.Not.Null);

            sentData.AckRequired = false;
            sentData.EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_ZONE, 123);
            sentData.FromState = BacnetEventStates.EVENT_STATE_NORMAL;
            sentData.InitiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1);
            sentData.MessageText = "Dummy Operation";
            sentData.NotificationClass = 10;
            sentData.NotifyType = BacnetNotifyTypes.NOTIFY_EVENT;
            sentData.Priority = 1;
            sentData.TimeStamp = new BacnetGenericTime(new DateTime(2018, 2, 22, 16, 14, 15),
                BacnetTimestampTags.TIME_STAMP_DATETIME);
            sentData.ProcessIdentifier = 1;
            sentData.ToState = BacnetEventStates.EVENT_STATE_NORMAL;

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
                    .Create(123.456f)
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
                    .Create(BacnetBitString.Parse("101"))
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
                ErrorLimit = 12.34f,
                ReferenceValue = 56.78f,
                SetPointValue = 91.011f,
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
