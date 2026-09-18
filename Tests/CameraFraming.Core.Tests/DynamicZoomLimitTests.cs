using System;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.CameraFraming;
using Octoplug.RoomGeneration;

namespace CameraFraming.Core.Tests
{
    public class DynamicZoomLimitTests
    {
        [Test]
        public void CombineBounds_UnionsAllSuppliedRooms()
        {
            var bounds = new List<RoomBounds2D>
            {
                new RoomBounds2D(0f, 0f, 8f, 6f),
                new RoomBounds2D(-6f, 3f, 12f, 6f),
                new RoomBounds2D(4f, -9f, 8f, 9f)
            };

            var combined = DynamicZoomLimit.CombineBounds(bounds);

            Assert.That(combined.MinX, Is.EqualTo(-6f));
            Assert.That(combined.MinY, Is.EqualTo(-9f));
            Assert.That(combined.MaxX, Is.EqualTo(12f));
            Assert.That(combined.MaxY, Is.EqualTo(9f));
        }

        [Test]
        public void CombineBounds_RejectsEmptyList()
        {
            Assert.Throws<ArgumentException>(() => DynamicZoomLimit.CombineBounds(new List<RoomBounds2D>()));
        }

        [Test]
        public void ComputeMaxOrthographicSize_GrowsWithHouseSize()
        {
            var small = new RoomBounds2D(0f, 0f, 8f, 6f);
            var large = new RoomBounds2D(0f, 0f, 40f, 30f);

            var smallMax = DynamicZoomLimit.ComputeMaxOrthographicSize(small, aspect: 16f / 9f, margin: 1f, minOrthographicSize: 5f);
            var largeMax = DynamicZoomLimit.ComputeMaxOrthographicSize(large, aspect: 16f / 9f, margin: 1f, minOrthographicSize: 5f);

            Assert.That(largeMax, Is.GreaterThan(smallMax));
        }

        [Test]
        public void ComputeMaxOrthographicSize_NeverGoesBelowMinimum()
        {
            var tiny = new RoomBounds2D(0f, 0f, 1f, 1f);
            var max = DynamicZoomLimit.ComputeMaxOrthographicSize(tiny, aspect: 16f / 9f, margin: 0f, minOrthographicSize: 5f);
            Assert.That(max, Is.EqualTo(5f));
        }

        [Test]
        public void ComputeMaxOrthographicSize_UsesWidthWhenWiderThanTall()
        {
            var wide = new RoomBounds2D(0f, 0f, 40f, 2f);
            var aspect = 2f;
            var margin = 0f;
            var max = DynamicZoomLimit.ComputeMaxOrthographicSize(wide, aspect, margin, minOrthographicSize: 5f);

            // Width-driven: half width / aspect = 20 / 2 = 10, greater than half height (1).
            Assert.That(max, Is.EqualTo(10f));
        }

        [Test]
        public void ComputeMaxOrthographicSize_AtDesignReferenceAspect_NeverClipsWideHouse()
        {
            // A house much wider than the 16:9 design reference must still fit horizontally:
            // the width-driven requirement (half width / aspect) must dominate and be honored.
            var wideHouse = new RoomBounds2D(0f, 0f, 60f, 10f);
            var max = DynamicZoomLimit.ComputeMaxOrthographicSize(wideHouse, CameraDesignReference.DesignAspect, margin: 0f, minOrthographicSize: 5f);

            var expectedFromWidth = (60f * 0.5f) / CameraDesignReference.DesignAspect;
            Assert.That(max, Is.EqualTo(expectedFromWidth).Within(0.001f));
            Assert.That(max, Is.GreaterThanOrEqualTo(10f * 0.5f), "The result must never be smaller than the vertical requirement either.");
        }

        [Test]
        public void ComputeMaxOrthographicSize_AtDesignReferenceAspect_NeverClipsTallHouse()
        {
            var tallHouse = new RoomBounds2D(0f, 0f, 10f, 40f);
            var max = DynamicZoomLimit.ComputeMaxOrthographicSize(tallHouse, CameraDesignReference.DesignAspect, margin: 0f, minOrthographicSize: 5f);

            // Height-driven: half height (20) must dominate over half width / aspect (10 / (16/9) ~= 5.6).
            Assert.That(max, Is.EqualTo(20f).Within(0.001f));
        }
    }
}
