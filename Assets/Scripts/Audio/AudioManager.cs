using LoreLegacyMonsters.Core;
using UnityEngine;

namespace LoreLegacyMonsters.Audio
{
    /// <summary>
    /// UI SFX indices (<see cref="PlayUiSfx"/>): 0 = light menu tick / navigation; 1 = gear equip; 2 = gear unequip.
    /// Wire clips in the prefab or scene AudioManager component.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioClip[] townMusic;
        [SerializeField] AudioClip[] routeMusic;
        [SerializeField] AudioClip[] forestMusic;
        [SerializeField] AudioClip[] combatMusic;
        [SerializeField] AudioClip[] uiSfx;

        AudioSource musicSource;
        AudioSource sfxSource;

        public static AudioManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            ApplyVolumeSettings();
        }

        public static AudioManager EnsureExists()
        {
            if (Instance != null)
                return Instance;
            var go = new GameObject("AudioManager");
            return go.AddComponent<AudioManager>();
        }

        public void ApplyVolumeSettings()
        {
            AudioListener.volume = GameSettings.MasterVolume;
            if (musicSource != null) musicSource.volume = GameSettings.MusicVolume;
            if (sfxSource != null) sfxSource.volume = GameSettings.SfxVolume;
        }

        public void PlayUiSfx(int index)
        {
            if (uiSfx == null || uiSfx.Length == 0 || index < 0 || index >= uiSfx.Length || uiSfx[index] == null)
                return;
            sfxSource?.PlayOneShot(uiSfx[index], GameSettings.SfxVolume);
        }

        public void PlayMusicForArea(string areaId)
        {
            var next = PickByArea(areaId);
            if (next == null || musicSource == null || musicSource.clip == next)
                return;
            musicSource.clip = next;
            musicSource.Play();
        }

        public void PlayCombatMusic()
        {
            var next = PickRandom(combatMusic);
            if (next == null || musicSource == null)
                return;
            if (musicSource.clip != next)
                musicSource.clip = next;
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        AudioClip PickByArea(string areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
                return PickRandom(townMusic);
            areaId = areaId.ToLowerInvariant();
            if (areaId.Contains("forest") || areaId.Contains("grove"))
                return PickRandom(forestMusic);
            if (areaId.Contains("route") || areaId.Contains("road") || areaId.Contains("ridge"))
                return PickRandom(routeMusic);
            return PickRandom(townMusic);
        }

        static AudioClip PickRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            var idx = Random.Range(0, clips.Length);
            return clips[idx];
        }
    }
}
