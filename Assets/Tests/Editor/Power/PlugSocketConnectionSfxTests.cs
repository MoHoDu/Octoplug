using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Audio;
using Octoplug.Power;
using Octoplug.Power.Connection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Octoplug.Tests.Editor.Power
{
    public sealed class PlugSocketConnectionSfxTests
    {
        private readonly List<GameObject> roots = new();
        private readonly List<GameplaySfxCue> played = new();
        private System.IDisposable playbackOverride;

        [SetUp]
        public void SetUp()
        {
            playbackOverride = GameplaySfxPlayer.OverridePlaybackForVerification(
                cue => played.Add(cue));
        }

        [TearDown]
        public void TearDown()
        {
            playbackOverride?.Dispose();
            for (var index = roots.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(roots[index]);
            }
        }

        [Test]
        public void PairingMutations_PlayOnlyCommittedConnectionSounds()
        {
            var plug = Create<PlugConnector>("Plug");
            var firstSocket = Create<SocketConnector>("First Socket");
            var secondSocket = Create<SocketConnector>("Second Socket");

            Assert.That(PlugSocketConnection.Connect(plug, firstSocket), Is.True);
            Assert.That(played, Is.EqualTo(new[] { GameplaySfxCue.PlugConnect }));

            Assert.That(PlugSocketConnection.Connect(plug, firstSocket), Is.True);
            PlugSocketConnection.NotifyTopologyChanged();
            Assert.That(played, Has.Count.EqualTo(1));

            Assert.That(PlugSocketConnection.Connect(plug, secondSocket), Is.True);
            Assert.That(
                played,
                Is.EqualTo(new[]
                {
                    GameplaySfxCue.PlugConnect,
                    GameplaySfxCue.PlugConnect
                }));

            PlugSocketConnection.Disconnect(plug);
            PlugSocketConnection.Disconnect(plug);
            Assert.That(
                played,
                Is.EqualTo(new[]
                {
                    GameplaySfxCue.PlugConnect,
                    GameplaySfxCue.PlugConnect,
                    GameplaySfxCue.PlugDisconnect
                }));
        }

        [Test]
        public void RejectedOccupiedSocket_DoesNotPlaySound()
        {
            var firstPlug = Create<PlugConnector>("First Plug");
            var secondPlug = Create<PlugConnector>("Second Plug");
            var socket = Create<SocketConnector>("Socket");
            Assert.That(PlugSocketConnection.Connect(firstPlug, socket), Is.True);
            played.Clear();

            Assert.That(PlugSocketConnection.Connect(secondPlug, socket), Is.False);

            Assert.That(played, Is.Empty);
        }

        private T Create<T>(string name) where T : Component
        {
            var root = new GameObject(name);
            roots.Add(root);
            return root.AddComponent<T>();
        }
    }
}
