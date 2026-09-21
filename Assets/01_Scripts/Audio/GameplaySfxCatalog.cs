using UnityEngine;

namespace Octoplug.Audio
{
    [CreateAssetMenu(
        fileName = "GameplaySfxCatalog",
        menuName = "Octoplug/Audio/Gameplay SFX Catalog")]
    public sealed class GameplaySfxCatalog : ScriptableObject
    {
        [SerializeField] private AudioClip needFail;
        [SerializeField] private AudioClip needSpawn;
        [SerializeField] private AudioClip plugConnect;
        [SerializeField] private AudioClip plugDisconnect;

        public AudioClip NeedFail => needFail;
        public AudioClip NeedSpawn => needSpawn;
        public AudioClip PlugConnect => plugConnect;
        public AudioClip PlugDisconnect => plugDisconnect;

        public AudioClip GetClip(GameplaySfxCue cue)
        {
            return cue switch
            {
                GameplaySfxCue.NeedFail => needFail,
                GameplaySfxCue.NeedSpawn => needSpawn,
                GameplaySfxCue.PlugConnect => plugConnect,
                GameplaySfxCue.PlugDisconnect => plugDisconnect,
                _ => null
            };
        }
    }
}
