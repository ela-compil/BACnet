using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.TestData;
using System.Linq;
using NUnit.Framework;
using static System.IO.BACnet.Tests.Helper;

namespace System.IO.BACnet.Tests.Serialize
{
    [TestFixture]
    class ObjectAccessServicesTests
    {
        [Test]
        public void should_encode_addlistelementrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = new List<BacnetValue>
            {
                new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE,
                    new BacnetObjectPropertyReference(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 15),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_RELIABILITY)))
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.1
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x01, 0x08, 0x0C, 0x02, 0xC0, 0x00, 0x03, 0x19, 0x35, 0x3E, 0x0C, 0x00, 0x00, 0x00, 0x0F,
                0x1E, 0x09, 0x55, 0x09, 0x67, 0x1F, 0x3F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ADD_LIST_ELEMENT, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 1);

            ObjectAccessServices.EncodeAddOrRemoveListElement(buffer,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_GROUP, 3),
                (uint) BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS, ASN1.BACNET_ARRAY_ALL, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_addlistelementrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.1
            var expectedBytes = new byte[] { 0x20, 0x01, 0x08 };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_ADD_LIST_ELEMENT, 1);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_removelistelementrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = new List<BacnetValue>
            {
                new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE,
                    new BacnetObjectPropertyReference(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 12),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_RELIABILITY),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_DESCRIPTION))),

                new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE,
                    new BacnetObjectPropertyReference(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 13),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_RELIABILITY),
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_DESCRIPTION)))
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.2
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x34, 0x09, 0x0C, 0x02, 0xC0, 0x00, 0x03, 0x19, 0x35, 0x3E, 0x0C, 0x00, 0x00, 0x00, 0x0C,
                0x1E, 0x09, 0x55, 0x09, 0x67, 0x09, 0x1C, 0x1F, 0x0C, 0x00, 0x00, 0x00, 0x0D, 0x1E, 0x09, 0x55, 0x09,
                0x67, 0x09, 0x1C, 0x1F, 0x3F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 52);

            ObjectAccessServices.EncodeAddOrRemoveListElement(buffer,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_GROUP, 3),
                (uint)BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS, ASN1.BACNET_ARRAY_ALL, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_removelistelementrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.2
            var expectedBytes = new byte[] { 0x20, 0x34, 0x09 };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT, 52);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_createobjectrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_3();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.3
            var expectedBytes = new byte[]
            {
                0x00, 0x04, 0x56, 0x0A, 0x0E, 0x09, 0x0A, 0x0F, 0x1E, 0x09, 0x4D, 0x2E, 0x75, 0x08, 0x00, 0x54, 0x72,
                0x65, 0x6E, 0x64, 0x20, 0x31, 0x2F, 0x09, 0x29, 0x2E, 0x91, 0x00, 0x2F, 0x1F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU1024, 86);

            ObjectAccessServices.EncodeCreateObject(buffer, input.ObjectType, input.ValueList);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EqualTo(expectedBytes));
        }

        [Test]
        public void should_decode_createobjectrequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_3();

            // act
            ObjectAccessServices.EncodeCreateObject(buffer, input.ObjectType, input.ValueList);
            ObjectAccessServices.DecodeCreateObject(default(BacnetAddress), buffer.buffer, 0, buffer.GetLength(),
                out var objectId, out var valuesRefs);

            // assert
            Assert.That(objectId, Is.EqualTo(input.ObjectType));
            Assert.That(valuesRefs, Is.EquivalentTo(input.ValueList));
        }

        [Test]
        public void should_encode_createobjectrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.3
            var expectedBytes = new byte[] { 0x30, 0x56, 0x0A, 0xC4, 0x02, 0x80, 0x00, 0x0D };

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, 86);

            ObjectAccessServices.EncodeCreateObjectAcknowledge(buffer,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 13));

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_deleteobjectrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.4
            var expectedBytes = new byte[] {0x00, 0x04, 0x57, 0x0B, 0xC4, 0x02, 0xC0, 0x00, 0x06};

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU1024, 87);

            ObjectAccessServices.EncodeDeleteObject(buffer, ASHRAE.F_3_4());

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_deleteobjectrequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_4();

            ObjectAccessServices.EncodeDeleteObject(buffer, input);
            ObjectAccessServices.DecodeDeleteObject(buffer.buffer, 0, buffer.GetLength(), out var objectId);

            // assert
            Assert.That(objectId, Is.EqualTo(input));
        }

        [Test]
        public void should_encode_deleteobjectrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.4
            var expectedBytes = new byte[] { 0x20, 0x57, 0x0B };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT, 87);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_deleteobjectrequest_fail_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.4
            var expectedBytes = new byte[] {0x50, 0x58, 0x0B, 0x91, 0x01, 0x91, 0x17};

            // act
            APDU.EncodeError(buffer, BacnetPduTypes.PDU_TYPE_ERROR,
                BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT, 88);
            ASN1.EncodeError(buffer, BacnetErrorClasses.ERROR_CLASS_OBJECT,
                BacnetErrorCodes.ERROR_CODE_OBJECT_DELETION_NOT_PERMITTED);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_readpropertyrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_3_5();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.5
            var expectedBytes = new byte[] {0x00, 0x00, 0x01, 0x0C, 0x0C, 0x00, 0x00, 0x00, 0x05, 0x19, 0x55};

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU50, 1);

            ObjectAccessServices.EncodeReadProperty(buffer, data.ObjectId, data.PropertyId, data.ArrayIndex);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_readpropertyrequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_5();

            ObjectAccessServices.EncodeReadProperty(buffer, input.ObjectId, input.PropertyId, input.ArrayIndex);
            ObjectAccessServices.DecodeReadProperty(buffer.buffer, 0, buffer.GetLength(), out var objectId,
                out var propertyReference);

            // assert
            Assert.That(objectId, Is.EqualTo(input.ObjectId));
            Assert.That(propertyReference.propertyIdentifier, Is.EqualTo((uint)input.PropertyId));
            Assert.That(propertyReference.propertyArrayIndex, Is.EqualTo(input.ArrayIndex));
        }

        [Test]
        public void should_encode_readpropertyrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = ASHRAE.F_3_5_Ack();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.5
            var expectedBytes = new byte[]
                {0x30, 0x01, 0x0C, 0x0C, 0x00, 0x00, 0x00, 0x05, 0x19, 0x55, 0x3E, 0x44, 0x42, 0x90, 0x99, 0x9A, 0x3F};

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, 1);
            ObjectAccessServices.EncodeReadPropertyAcknowledge(buffer, data.ObjectId, data.PropertyId, data.ValueList,
                data.ArrayIndex);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_readpropertyrequest_ack_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var input = ASHRAE.F_3_5_Ack();

            // act
            ObjectAccessServices.EncodeReadPropertyAcknowledge(buffer, input.ObjectId, input.PropertyId,
                input.ValueList, input.ArrayIndex);
            ObjectAccessServices.DecodeReadPropertyAcknowledge(DummyAddress, buffer.buffer, 0, buffer.GetLength(),
                out var objectId, out var propertyReference, out var valueList);


            // assert
            Assert.That(objectId, Is.EqualTo(input.ObjectId));
            Assert.That(propertyReference.propertyIdentifier, Is.EqualTo((uint)input.PropertyId));
            Assert.That(propertyReference.propertyArrayIndex, Is.EqualTo(input.ArrayIndex));
            Assert.That(valueList, Is.EquivalentTo(input.ValueList));
        }

        [Test]
        public void should_encode_readpropertymultiplerequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = ASHRAE.F_3_7();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.7
            var expectedBytes = new byte[]
                {0x00, 0x04, 0xF1, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x10, 0x1E, 0x09, 0x55, 0x09, 0x67, 0x1F};

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU1024, 241);

            ObjectAccessServices.EncodeReadPropertyMultiple(buffer, data.ObjectId, data.Properties);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_readpropertymultiplerequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var input = ASHRAE.F_3_7();

            // act
            ObjectAccessServices.EncodeReadPropertyMultiple(buffer, input.ObjectId, input.Properties);
            ObjectAccessServices.DecodeReadPropertyMultiple(buffer.buffer, 0, buffer.GetLength(), out var properties);

            // assert
            Assert.That(properties.Count, Is.EqualTo(1));
            Assert.That(properties[0].objectIdentifier, Is.EqualTo(input.ObjectId));
            Assert.That(properties[0].propertyReferences, Is.EquivalentTo(input.Properties));
        }

        [Test]
        public void should_encode_readpropertymultiplerequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = ASHRAE.F_3_7_Ack();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.7
            var expectedBytes = new byte[]
            {
                0x30, 0xF1, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x10, 0x1E, 0x29, 0x55, 0x4E, 0x44, 0x42, 0x90, 0x99, 0x9A,
                0x4F, 0x29, 0x67, 0x4E, 0x91, 0x00, 0x4F, 0x1F
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, 241);
            ObjectAccessServices.EncodeReadPropertyMultipleAcknowledge(buffer, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_readpropertymultiplerequest_ack_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var input = ASHRAE.F_3_7_Ack();

            // act
            ObjectAccessServices.EncodeReadPropertyMultipleAcknowledge(buffer, input);
            ObjectAccessServices.DecodeReadPropertyMultipleAcknowledge(DummyAddress, buffer.buffer, 0,
                buffer.GetLength(), out var values);

            // assert
            Assert.That(values, Is.EquivalentTo(input));
        }

        [Test]
        public void should_encode_readpropertymultiplerequest_formultipleobjects_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = new List<BacnetReadAccessSpecification>
            {
                new BacnetReadAccessSpecification(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 33),
                    new List<BacnetPropertyReference>
                    {
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE)
                    }),

                new BacnetReadAccessSpecification(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 50),
                    new List<BacnetPropertyReference>
                    {
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE)
                    }),

                new BacnetReadAccessSpecification(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 35),
                    new List<BacnetPropertyReference>
                    {
                        new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE)
                    }),
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.7
            var expectedBytes = new byte[]
            {
                0x00, 0x04, 0x02, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x21, 0x1E, 0x09, 0x55, 0x1F, 0x0C, 0x00, 0x00, 0x00,
                0x32, 0x1E, 0x09, 0x55, 0x1F, 0x0C, 0x00, 0x00, 0x00, 0x23, 0x1E, 0x09, 0x55, 0x1F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU1024, 2);

            ObjectAccessServices.EncodeReadPropertyMultiple(buffer, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_readpropertymultiplerequest_formultipleobjects_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = new List<BacnetReadAccessResult>
            {
                new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 33),
                    new List<BacnetPropertyValue>
                    {
                        new BacnetPropertyValue
                        {
                            property = new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                            value = new List<BacnetValue> {new BacnetValue(42.3f)},
                        }
                    }),

                new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 50),
                    new List<BacnetPropertyValue>
                    {
                        new BacnetPropertyValue
                        {
                            property = new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                            value = new List<BacnetValue>
                            {
                                new BacnetValue(new BacnetError(BacnetErrorClasses.ERROR_CLASS_OBJECT,
                                    BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT))
                            },
                        }
                    }),

                new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 35),
                    new List<BacnetPropertyValue>
                    {
                        new BacnetPropertyValue
                        {
                            property = new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                            value = new List<BacnetValue> {new BacnetValue(435.7f)},
                        }
                    })
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.7
            var expectedBytes = new byte[]
            {
                0x30, 0x02, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x21, 0x1E, 0x29, 0x55, 0x4E, 0x44, 0x42, 0x29, 0x33, 0x33,
                0x4F, 0x1F, 0x0C, 0x00, 0x00, 0x00, 0x32, 0x1E, 0x29, 0x55, 0x5E, 0x91, 0x01, 0x91, 0x1F, 0x5F, 0x1F,
                0x0C, 0x00, 0x00, 0x00, 0x23, 0x1E, 0x29, 0x55, 0x4E, 0x44, 0x43, 0xD9, 0xD9, 0x9A, 0x4F, 0x1F
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, 2);
            ObjectAccessServices.EncodeReadPropertyMultipleAcknowledge(buffer, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_readrangerequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_3_8();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.8
            var expectedBytes = new byte[]
            {
                0x02, 0x02, 0x01, 0x1A, 0x0C, 0x05, 0x00, 0x00, 0x01, 0x19, 0x83, 0x7E, 0xA4, 0x62, 0x03, 0x17, 0x01,
                0xB4, 0x13, 0x34, 0x22, 0x00, 0x31, 0x04, 0x7F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 1,
                type: BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED);

            ObjectAccessServices.EncodeReadRange(buffer, data.ObjectId, data.PropertyId, data.RequestType,
                data.Position, data.Time, data.Count, data.ArrayIndex);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_readrangerequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_8();

            // act
            ObjectAccessServices.EncodeReadRange(buffer, input.ObjectId, input.PropertyId, input.RequestType,
                input.Position, input.Time, input.Count, input.ArrayIndex);
            ObjectAccessServices.DecodeReadRange(buffer.buffer, 0, buffer.GetLength(), out var objectId,
                out var property, out var requestType, out var position, out var time, out var count);

            // assert
            Assert.That(objectId, Is.EqualTo(input.ObjectId));
            Assert.That(property.propertyIdentifier, Is.EqualTo((uint)input.PropertyId));
            Assert.That(property.propertyArrayIndex, Is.EqualTo(input.ArrayIndex));
            Assert.That(requestType, Is.EqualTo(input.RequestType));
            Assert.That(position, Is.EqualTo(input.Position));
            Assert.That(time, Is.EqualTo(input.Time));
            Assert.That(count, Is.EqualTo(input.Count));
        }

        [Test]
        public void should_encode_readrangerequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_3_8_Ack();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.8
            var expectedBytes = new byte[]
            {
                0x30, 0x01, 0x1A, 0x0C, 0x05, 0x00, 0x00, 0x01, 0x19, 0x83, 0x3A, 0x05, 0xC0, 0x49, 0x02, 0x5E, 0x0E,
                0xA4, 0x62, 0x03, 0x17, 0x01, 0xB4, 0x13, 0x36, 0x1B, 0x00, 0x0F, 0x1E, 0x2C, 0x41, 0x90, 0x00, 0x00,
                0x1F, 0x2A, 0x04, 0x00, 0x0E, 0xA4, 0x62, 0x03, 0x17, 0x01, 0xB4, 0x13, 0x38, 0x1B, 0x00, 0x0F, 0x1E,
                0x2C, 0x41, 0x90, 0xCC, 0xCD, 0x1F, 0x2A, 0x04, 0x00, 0x5F, 0x6B, 0x01, 0x35, 0x61
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, 1);

            var applicationData = new EncodeBuffer();
            ObjectAccessServices.EncodeLogRecord(applicationData, data.Record1);
            ObjectAccessServices.EncodeLogRecord(applicationData, data.Record2);

            ObjectAccessServices.EncodeReadRangeAcknowledge(buffer, data.ObjectId, data.PropertyId, data.Flags,
                data.ItemCount, applicationData.ToArray(), data.RequestType, data.FirstSequence);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        /*
         * TODO: DecodeReadRangeAcknowledge should return the properties it decodes, and we should compare them to the input
         */
        public void should_decode_readrangerequest_ack_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_8_Ack();

            // act
            var applicationData = new EncodeBuffer();
            ObjectAccessServices.EncodeLogRecord(applicationData, input.Record1);
            ObjectAccessServices.EncodeLogRecord(applicationData, input.Record2);

            ObjectAccessServices.EncodeReadRangeAcknowledge(buffer, input.ObjectId, input.PropertyId, input.Flags,
                input.ItemCount, applicationData.ToArray(), input.RequestType, input.FirstSequence);

            ObjectAccessServices.DecodeReadRangeAcknowledge(buffer.buffer, 0, buffer.GetLength(), out var rangeBuffer);

            // assert
            Assert.That(rangeBuffer, Is.EquivalentTo(applicationData.ToArray()));
        }

        [Test]
        public void should_decode_multiple_logrecords()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_3_8_Ack();

            // act
            ObjectAccessServices.EncodeLogRecord(buffer, data.Record1);
            ObjectAccessServices.EncodeLogRecord(buffer, data.Record2);
            ObjectAccessServices.DecodeLogRecord(buffer.buffer, 0, buffer.GetLength(), (int)data.ItemCount, out var decodedRecords);

            /*
             * Debug - write packet to network to analyze in WireShark
             *

            var client = new BacnetClient(new BacnetIpUdpProtocolTransport(47808, true));
            client.Start();
            client.ReadRangeResponse(new BacnetAddress(BacnetAddressTypes.IP, "192.168.1.99"), 42, null,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 1),
                new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL),
                BacnetResultFlags.LAST_ITEM, 2, buffer.ToArray(), BacnetReadRangeRequestTypes.RR_BY_TIME, 79201);
            */

            // assert
            Assert.That(decodedRecords.Length, Is.EqualTo(2));
            Assert.That(data.Record1, Is.Not.SameAs(decodedRecords[0]));
            Assert.That(data.Record2, Is.Not.SameAs(decodedRecords[1]));
            AssertPropertiesAndFieldsAreEqual(data.Record1, decodedRecords[0]);
            AssertPropertiesAndFieldsAreEqual(data.Record2, decodedRecords[1]);
        }

        [Test]
        public void should_encode_writepropertyrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_3_9();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.9
            var expectedBytes = new byte[]
            {
                0x00, 0x04, 0x59, 0x0F, 0x0C, 0x00, 0x80, 0x00, 0x01, 0x19, 0x55, 0x3E, 0x44, 0x43, 0x34, 0x00, 0x00,
                0x3F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY,
                BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 89);

            ObjectAccessServices.EncodeWriteProperty(buffer, data.ObjectId, data.PropertyId, data.ValueList,
                data.ArrayIndex, data.Priority);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_writepropertyrequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_9();

            ObjectAccessServices.EncodeWriteProperty(buffer, input.ObjectId, input.PropertyId, input.ValueList,
                input.ArrayIndex, input.Priority);
            ObjectAccessServices.DecodeWriteProperty(DummyAddress, buffer.buffer, 0, buffer.GetLength(),
                out var objectId, out var value);

            // assert
            Assert.That(objectId, Is.EqualTo(input.ObjectId));
            Assert.That(value.property.propertyIdentifier, Is.EqualTo((uint)input.PropertyId));
            Assert.That(value.property.propertyArrayIndex, Is.EqualTo(input.ArrayIndex));
            Assert.That(value.value, Is.EquivalentTo(input.ValueList));
            Assert.That(value.priority, Is.EqualTo(input.Priority == ASN1.BACNET_NO_PRIORITY ? 16 : input.Priority));
        }

        [Test]
        public void should_encode_writepropertyrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.9
            var expectedBytes = new byte[] { 0x20, 0x59, 0x0F };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, 89);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_writepropertymultiplerequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_3_10();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.10
            var expectedBytes = new byte[]
            {
                0x00, 0x04, 0x01, 0x10, 0x0C, 0x00, 0x80, 0x00, 0x05, 0x1E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x86, 0x00,
                0x00, 0x2F, 0x1F, 0x0C, 0x00, 0x80, 0x00, 0x06, 0x1E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x86, 0x00, 0x00,
                0x2F, 0x1F, 0x0C, 0x00, 0x80, 0x00, 0x07, 0x1E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x90, 0x00, 0x00, 0x2F,
                0x1F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE,
                BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 1);

            ObjectAccessServices.EncodeWritePropertyMultiple(buffer, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_writepropertymultiplerequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_3_10();

            // act
            ObjectAccessServices.EncodeWritePropertyMultiple(buffer, input);
            ObjectAccessServices.DecodeWritePropertyMultiple(DummyAddress, buffer.buffer, 0, buffer.GetLength(),
                out var output);

            // assert
            Assert.That(output, Is.EquivalentTo(input));
        }

        [Test]
        public void should_encode_writepropertymultiplerequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.3.10
            var expectedBytes = new byte[] { 0x20, 0x01, 0x10 };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, 1);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }
    }
}
