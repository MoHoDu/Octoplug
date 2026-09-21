using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Octoplug.Audio
{
    public static class GameplaySfxPlayer
    {
        private const string CatalogPath = "Audio/GameplaySfxCatalog";
        private static GameplaySfxCatalog catalog;
        private static AudioSource source;

        private static Action<GameplaySfxCue> playbackOverride;

#if UNITY_EDITOR
        public static IDisposable OverridePlaybackForVerification(
            Action<GameplaySfxCue> playback)
        {
            var previous = playbackOverride;
            playbackOverride = playback;
            return new PlaybackOverrideScope(previous);
        }
#endif

        public static void Play(GameplaySfxCue cue)
        {
            if (playbackOverride != null)
            {
                playbackOverride(cue);
                return;
            }

            catalog ??= Resources.Load<GameplaySfxCatalog>(CatalogPath);
            var clip = catalog != null ? catalog.GetClip(cue) : null;
            if (clip == null)
            {
                return;
            }

            EnsureSource();
            source.PlayOneShot(clip);
        }

        private static void EnsureSource()
        {
            if (source != null)
            {
                return;
            }

            var owner = new GameObject("Gameplay SFX");
            Object.DontDestroyOnLoad(owner);
            source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.pitch = 1f;
        }

#if UNITY_EDITOR
        private sealed class PlaybackOverrideScope : IDisposable
        {
            private readonly Action<GameplaySfxCue> previous;
            private bool disposed;

            public PlaybackOverrideScope(Action<GameplaySfxCue> previous)
            {
                this.previous = previous;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                playbackOverride = previous;
                disposed = true;
            }
        }
#endif
    }
}
