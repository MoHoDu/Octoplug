using System;
using NUnit.Framework;
using Octoplug.CameraFraming;

namespace CameraFraming.Core.Tests
{
    public class ZoomInputMapperTests
    {
        [Test]
        public void MapScrollDelta_PositiveScrollZoomsIn()
        {
            var delta = ZoomInputMapper.MapScrollDelta(scrollY: 3f, sensitivity: 2f);
            Assert.That(delta, Is.EqualTo(-6f));
        }

        [Test]
        public void MapScrollDelta_NegativeScrollZoomsOut()
        {
            var delta = ZoomInputMapper.MapScrollDelta(scrollY: -3f, sensitivity: 2f);
            Assert.That(delta, Is.EqualTo(6f));
        }

        [Test]
        public void MapPinchDelta_FingersSpreadingApartZoomsIn()
        {
            var delta = ZoomInputMapper.MapPinchDelta(previousDistance: 100f, currentDistance: 140f, sensitivity: 0.1f);
            Assert.That(delta, Is.EqualTo(-4f).Within(0.0001f));
        }

        [Test]
        public void MapPinchDelta_FingersComingTogetherZoomsOut()
        {
            var delta = ZoomInputMapper.MapPinchDelta(previousDistance: 140f, currentDistance: 100f, sensitivity: 0.1f);
            Assert.That(delta, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void MapScrollDelta_RejectsNegativeSensitivity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZoomInputMapper.MapScrollDelta(1f, -1f));
        }

        [Test]
        public void MapPinchDelta_RejectsNegativeDistance()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZoomInputMapper.MapPinchDelta(-1f, 10f, 1f));
        }
    }
}
