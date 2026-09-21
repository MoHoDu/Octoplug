using NUnit.Framework;
using Octoplug.Audio;
using UnityEngine;

namespace Octoplug.Tests.Editor.Audio
{
    public sealed class GameplaySfxCatalogTests
    {
        [Test]
        public void ResourcesCatalog_ReferencesAllApprovedClips()
        {
            var catalog = Resources.Load<GameplaySfxCatalog>(
                "Audio/GameplaySfxCatalog");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.NeedFail, Is.Not.Null);
            Assert.That(catalog.NeedFail.name, Is.EqualTo("need_fail"));
            Assert.That(catalog.NeedSpawn, Is.Not.Null);
            Assert.That(catalog.NeedSpawn.name, Is.EqualTo("need_spawn"));
            Assert.That(catalog.PlugConnect, Is.Not.Null);
            Assert.That(catalog.PlugConnect.name, Is.EqualTo("plug_connect"));
            Assert.That(catalog.PlugDisconnect, Is.Not.Null);
            Assert.That(catalog.PlugDisconnect.name, Is.EqualTo("plug_disconnect"));
        }
    }
}
