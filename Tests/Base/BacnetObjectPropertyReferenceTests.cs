using NUnit.Framework;

namespace System.IO.BACnet.Tests.Base
{
    [TestFixture]
    public class BacnetObjectPropertyReferenceTests
    {
        [Test]
        public void should_throw_argumentoutofrangeexception_when_no_propertyreferences()
        {
            Assert.That(
                () => new BacnetObjectPropertyReference(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 1)),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
