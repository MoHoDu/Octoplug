using System.Linq;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.RoomGeneration;
using UnityEngine;

namespace Octoplug.Tests.Editor.Power
{
    public sealed class RuntimeWorldRegistryTests
    {
        [Test]
        public void StagedProduct_IsHiddenUntilFinalizedWithRoomOwnership()
        {
            var gameObject = new GameObject("Runtime Product");
            ApplianceSource product;
            using (RuntimeSpawnGate.Begin())
            {
                product = gameObject.AddComponent<ApplianceSource>();
            }

            try
            {
                Assert.That(RuntimeWorldRegistry.GetProducts().Contains(product), Is.False);

                var roomId = new RoomId("runtime-room");
                RuntimeWorldRegistry.FinalizeRuntimeObject(product, roomId);

                Assert.That(RuntimeWorldRegistry.GetProducts().Contains(product), Is.True);
                Assert.That(
                    RuntimeWorldRegistry.TryGetRoomOwner(product, out var owner),
                    Is.True);
                Assert.That(owner, Is.EqualTo(roomId));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }

            Assert.That(RuntimeWorldRegistry.GetProducts().Contains(product), Is.False);
        }
    }
}
