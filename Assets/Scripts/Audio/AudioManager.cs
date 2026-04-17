using UnityEngine;
using System.Collections;

namespace CardBattle.Audio
{
    /// <summary>
    /// Main audio system controller. Singleton, survives scene loads.
    /// Three-bus architecture: BGM (dual-source crossfade), SFX (8-channel pool), UI (4-channel pool).
    /// Volume persisted via PlayerPrefs.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Data")]
        [SerializeField] private AudioData _audioData;

        private BGMController _bgm;
        private SFXPool _sfxPool;
        private SFXPool _uiPool;
        private AudioSource _ambientSource;

        private float _masterVol, _bgmVol, _sfxVol, _uiVol;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create sub-controllers
            var bgmGo = new GameObject("BGM");
            bgmGo.transform.SetParent(transform);
            _bgm = bgmGo.AddComponent<BGMController>();

            var sfxGo = new GameObject("SFXPool");
            sfxGo.transform.SetParent(transform);
            _sfxPool = sfxGo.AddComponent<SFXPool>();

            var uiGo = new GameObject("UIPool");
            uiGo.transform.SetParent(transform);
            _uiPool = uiGo.AddComponent<SFXPool>();
            // UI pool is smaller
            // (SFXPool defaults to 8, UI uses 4 — set via serialized field or leave as 8 for simplicity)

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;
            _ambientSource.spatialBlend = 0;

            LoadVolumeSettings();
        }

        // === BGM ===

        public void PlayBGM(string clipId, float crossfadeDuration = 2f)
        {
            var entry = _audioData?.GetEntry(clipId);
            if (entry?.clip == null) { Debug.Log($"[Audio] BGM placeholder: {clipId}"); return; }
            _bgm.Play(entry.clip, entry.loop, entry.loopStart, entry.loopEnd, crossfadeDuration);
        }

        public void PlayBGMHardCut(string clipId)
        {
            var entry = _audioData?.GetEntry(clipId);
            if (entry?.clip == null) { Debug.Log($"[Audio] BGM placeholder: {clipId}"); return; }
            _bgm.HardCut(entry.clip, entry.loop);
        }

        public void StopBGM(float fadeDuration = 1f) => _bgm.Stop(fadeDuration);
        public void StopBGMImmediate() => _bgm.StopImmediate();
        public string CurrentBGM => _bgm.CurrentClipName;

        // === SFX ===

        public void PlaySFX(string clipId, float pitch = 1f)
        {
            var entry = _audioData?.GetEntry(clipId);
            if (entry?.clip == null) { Debug.Log($"[Audio] SFX placeholder: {clipId}"); return; }
            _sfxPool.Play(entry.clip, entry.volume * _sfxVol, pitch);
        }

        public void PlayUI(string clipId)
        {
            var entry = _audioData?.GetEntry(clipId);
            if (entry?.clip == null) { Debug.Log($"[Audio] UI placeholder: {clipId}"); return; }
            _uiPool.Play(entry.clip, entry.volume * _uiVol);
        }

        public void StopAllSFX()
        {
            _sfxPool.StopAll();
            _ambientSource.Stop();
        }

        public void StopAll()
        {
            _bgm.StopImmediate();
            _sfxPool.StopAll();
            _uiPool.StopAll();
            _ambientSource.Stop();
        }

        // === Ambient ===

        public void PlayAmbient(string clipId, bool loop = true)
        {
            var entry = _audioData?.GetEntry(clipId);
            if (entry?.clip == null) return;
            _ambientSource.clip = entry.clip;
            _ambientSource.loop = loop;
            _ambientSource.volume = entry.volume;
            _ambientSource.Play();
        }

        public void StopAmbient(float fadeDuration = 1f) => StartCoroutine(FadeOutSource(_ambientSource, fadeDuration));
        public void SetAmbientPitch(float pitch) => _ambientSource.pitch = pitch;

        // === Volume ===

        public float MasterVolume { get => _masterVol; set { _masterVol = Mathf.Clamp01(value); ApplyVolumes(); SaveVolumeSettings(); } }
        public float BGMVolume { get => _bgmVol; set { _bgmVol = Mathf.Clamp01(value); ApplyVolumes(); SaveVolumeSettings(); } }
        public float SFXVolume { get => _sfxVol; set { _sfxVol = Mathf.Clamp01(value); ApplyVolumes(); SaveVolumeSettings(); } }
        public float UIVolume { get => _uiVol; set { _uiVol = Mathf.Clamp01(value); ApplyVolumes(); SaveVolumeSettings(); } }

        private void ApplyVolumes()
        {
            _bgm.SetVolume(_masterVol * _bgmVol);
            _ambientSource.volume = _masterVol * _bgmVol * 0.5f;
        }

        private void LoadVolumeSettings()
        {
            _masterVol = PlayerPrefs.GetFloat("audio_master", 1f);
            _bgmVol = PlayerPrefs.GetFloat("audio_bgm", 0.8f);
            _sfxVol = PlayerPrefs.GetFloat("audio_sfx", 1f);
            _uiVol = PlayerPrefs.GetFloat("audio_ui", 0.8f);
            ApplyVolumes();
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("audio_master", _masterVol);
            PlayerPrefs.SetFloat("audio_bgm", _bgmVol);
            PlayerPrefs.SetFloat("audio_sfx", _sfxVol);
            PlayerPrefs.SetFloat("audio_ui", _uiVol);
            PlayerPrefs.Save();
        }

        // === Dynamic BGM ===

        public void CheckBattleBGM(int myLP, int oppLP)
        {
            if (CurrentBGM == AudioConstants.BGM_VICTORY || CurrentBGM == AudioConstants.BGM_DEFEAT) return;
            if (CurrentBGM == AudioConstants.BGM_BATTLE_BOSS) return;

            if (myLP <= 2000 || oppLP <= 2000)
                PlayBGM(AudioConstants.BGM_BATTLE_TENSE);
            else
                PlayBGM(AudioConstants.BGM_BATTLE_NORMAL);
        }

        public void TriggerBossBGM(int myLP, int oppLP)
        {
            PlayBGM(AudioConstants.BGM_BATTLE_BOSS);
            StartCoroutine(RevertBossAfterDelay(myLP, oppLP));
        }

        private IEnumerator RevertBossAfterDelay(int myLP, int oppLP)
        {
            yield return new WaitForSeconds(AudioConstants.BOSS_MIN_DURATION);
            CheckBattleBGM(myLP, oppLP);
        }

        public void PlayVictorySequence()
        {
            StopAll();
            PlaySFX(AudioConstants.SFX_VICTORY);
            StartCoroutine(DelayedBGM(AudioConstants.BGM_VICTORY, AudioConstants.RESULT_SFX_DELAY));
        }

        public void PlayDefeatSequence()
        {
            StopAll();
            PlaySFX(AudioConstants.SFX_DEFEAT);
            StartCoroutine(DelayedBGM(AudioConstants.BGM_DEFEAT, AudioConstants.RESULT_SFX_DELAY));
        }

        private IEnumerator DelayedBGM(string clipId, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayBGMHardCut(clipId);
        }

        private IEnumerator FadeOutSource(AudioSource source, float duration)
        {
            float start = source.volume;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(start, 0, t / duration);
                yield return null;
            }
            source.Stop();
            source.volume = start;
        }
    }
}
