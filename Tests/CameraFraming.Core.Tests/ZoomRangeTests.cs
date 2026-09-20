using System;
using NUnit.Framework;
using Octoplug.CameraFraming;

namespace CameraFraming.Core.Tests
{
    public class ZoomRangeTests
    {
        [Test]
        public void Clamp_KeepsValueWithinRange()
        {
            var range = new ZoomRange(5f, 20f);
            Assert.That(range.Clamp(1f), Is.EqualTo(5f));
            Assert.That(range.Clamp(12f), Is.EqualTo(12f));
            Assert.That(range.Clamp(999f), Is.EqualTo(20f));
        }

        [Test]
        public void Constructor_RejectsNonPositiveMinimum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoomRange(0f, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoomRange(-1f, 10f));
        }

        [Test]
        public void Constructor_RejectsMaximumBelowMinimum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoomRange(10f, 5f));
        }

        [Test]
        public void Constructor_AllowsEqualMinAndMax()
        {
            var range = new ZoomRange(5f, 5f);
            Assert.That(range.Clamp(100f), Is.EqualTo(5f));
        }
    }
}
