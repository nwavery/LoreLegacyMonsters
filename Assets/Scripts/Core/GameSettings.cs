using UnityEngine;
using UnityEngine.InputSystem;

namespace LoreLegacyMonsters.Core
{
    public static class GameSettings
    {
        const string KeyMasterVolume = "settings.masterVolume";
        const string KeyMusicVolume = "settings.musicVolume";
        const string KeySfxVolume = "settings.sfxVolume";
        const string KeyVsync = "settings.vsync";
        const string KeyFrameCap = "settings.frameCap";
        const string KeyFullscreenMode = "settings.fullscreenMode";
        const string KeyMoveUp = "settings.key.moveUp";
        const string KeyMoveDown = "settings.key.moveDown";
        const string KeyMoveLeft = "settings.key.moveLeft";
        const string KeyMoveRight = "settings.key.moveRight";
        const string KeyInteract = "settings.key.interact";
        const string KeyPause = "settings.key.pause";

        public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMasterVolume, 1f));
        public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusicVolume, 0.8f));
        public static float SfxVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfxVolume, 0.85f));
        public static bool Vsync => PlayerPrefs.GetInt(KeyVsync, 1) != 0;
        public static int FrameCap => Mathf.Clamp(PlayerPrefs.GetInt(KeyFrameCap, 120), 30, 240);

        public static FullScreenMode ScreenMode =>
            (FullScreenMode)Mathf.Clamp(PlayerPrefs.GetInt(KeyFullscreenMode, (int)FullScreenMode.FullScreenWindow), 0, 3);

        public static Key MoveUp => (Key)PlayerPrefs.GetInt(KeyMoveUp, (int)Key.W);
        public static Key MoveDown => (Key)PlayerPrefs.GetInt(KeyMoveDown, (int)Key.S);
        public static Key MoveLeft => (Key)PlayerPrefs.GetInt(KeyMoveLeft, (int)Key.A);
        public static Key MoveRight => (Key)PlayerPrefs.GetInt(KeyMoveRight, (int)Key.D);
        public static Key Interact => (Key)PlayerPrefs.GetInt(KeyInteract, (int)Key.E);
        public static Key Pause => (Key)PlayerPrefs.GetInt(KeyPause, (int)Key.P);

        public static void SetMasterVolume(float value) => PlayerPrefs.SetFloat(KeyMasterVolume, Mathf.Clamp01(value));
        public static void SetMusicVolume(float value) => PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value));
        public static void SetSfxVolume(float value) => PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value));
        public static void SetVsync(bool value) => PlayerPrefs.SetInt(KeyVsync, value ? 1 : 0);
        public static void SetFrameCap(int value) => PlayerPrefs.SetInt(KeyFrameCap, Mathf.Clamp(value, 30, 240));
        public static void SetScreenMode(FullScreenMode mode) => PlayerPrefs.SetInt(KeyFullscreenMode, (int)mode);
        public static void SetMoveKeys(Key up, Key down, Key left, Key right)
        {
            PlayerPrefs.SetInt(KeyMoveUp, (int)up);
            PlayerPrefs.SetInt(KeyMoveDown, (int)down);
            PlayerPrefs.SetInt(KeyMoveLeft, (int)left);
            PlayerPrefs.SetInt(KeyMoveRight, (int)right);
        }

        public static void SetInteractKey(Key key) => PlayerPrefs.SetInt(KeyInteract, (int)key);
        public static void SetPauseKey(Key key) => PlayerPrefs.SetInt(KeyPause, (int)key);

        public static void ApplyDisplay()
        {
            QualitySettings.vSyncCount = Vsync ? 1 : 0;
            Application.targetFrameRate = Vsync ? -1 : FrameCap;
            Screen.fullScreenMode = ScreenMode;
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
