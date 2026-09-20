using NUnit.Framework;
using Octoplug.Power;
using UnityEngine;

namespace Octoplug.Tests.Editor.Power
{
    public sealed class ApplianceSourcePoweredChangedTests
    {
        private GameObject _productObject;
        private ApplianceSource _appliance;

        [SetUp]
        public void SetUp()
        {
            _productObject = new GameObject("Product");
            _appliance = _productObject.AddComponent<ApplianceSource>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_productObject);
        }

        [Test]
        public void SetPowered_EmitsOnlyForActualStateChanges()
        {
            var calls = 0;
            var lastValue = false;
            _appliance.PoweredChanged += (source, powered) =>
            {
                Assert.That(source, Is.SameAs(_appliance));
                calls++;
                lastValue = powered;
            };

            _appliance.SetPowered(false);
            _appliance.SetPowered(true);
            _appliance.SetPowered(true);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(lastValue, Is.True);
        }
    }
}
