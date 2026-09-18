using System;
using NUnit.Framework;
using Octoplug.CameraFraming;
using Octoplug.RoomGeneration;

namespace CameraFraming.Core.Tests
{
    public class PanDeltaMapperTests
    {
        [Test]
        public void Map_MovesCameraOppositePointerDelta()
        {
            var result = PanDeltaMapper.Map(
                new Point2D(192f, 108f),
                orthographicSize: 5f,
                aspect: 16f / 9f,
                viewportWidthPixels: 1920f,
                viewportHeightPixels: 1080f,
                sensitivity: 1f);

            Assert.That(result.X, Is.EqualTo(-16f / 9f).Within(0.0001f));
            Assert.That(result.Y, Is.EqualTo(-1f).Within(0.0001f));
        }

        [Test]
        public void Map_ScalesWithOrthographicSizeAndSensitivity()
        {
            var baseline = PanDeltaMapper.Map(new Point2D(100f, 100f), 5f, 1f, 1000f, 1000f, 1f);
            var scaled = PanDeltaMapper.Map(new Point2D(100f, 100f), 10f, 1f, 1000f, 1000f, 0.5f);

            Assert.That(scaled, Is.EqualTo(baseline));
        }

        [Test]
        public void Map_RejectsInvalidViewport()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanDeltaMapper.Map(new Point2D(1f, 1f), 5f, 1f, 0f, 100f, 1f));
        }
    }
}
