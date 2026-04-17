# Unity AudioManager C# 類別設計

## 類別架構

```
Assets/Scripts/Audio/
├── AudioManager.cs          ← Singleton 主控制器
├── BGMController.cs         ← BGM 播放 + CrossFade
├── SFXPool.cs               ← SFX Object Pool
├── AudioData.cs             ← ScriptableObject 音效資料
└── AudioConstants.cs        ← 常數定義
```

## AudioManager.cs

```csharp
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 遊戲音效系統主控制器。Singleton，跨場景存活。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer _mixer;

    [Header("Audio Data")]
    [SerializeField] private AudioData _audioData;

    [Header("Controllers")]
    [SerializeField] private BGMController _bgm;
    [SerializeField] private SFXPool _sfxPool;
    [SerializeField] private SFXPool _uiPool;
    [SerializeField] private AudioSource _ambientSource;

    // --- Lifecycle ---

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumeSettings();
    }

    // --- Public API: BGM ---

    /// <summary>播放 BGM（CrossFade 切換）</summary>
    public void PlayBGM(string clipId, float crossfadeDuration = 2f)
    {
        var clip = _audioData.GetClip(clipId);
        if (clip == null) return;
        var entry = _audioData.GetEntry(clipId);
        _bgm.Play(clip, entry.loop, entry.loopStart, entry.loopEnd, crossfadeDuration);
    }

    /// <summary>硬切播放 BGM（勝利/敗北用）</summary>
    public void PlayBGMHardCut(string clipId)
    {
        var clip = _audioData.GetClip(clipId);
        if (clip == null) return;
        var entry = _audioData.GetEntry(clipId);
        _bgm.HardCut(clip, entry.loop);
    }

    /// <summary>停止 BGM（淡出）</summary>
    public void StopBGM(float fadeDuration = 1f) => _bgm.Stop(fadeDuration);

    // --- Public API: SFX ---

    /// <summary>播放音效</summary>
    public void PlaySFX(string clipId, float pitch = 1f)
    {
        var clip = _audioData.GetClip(clipId);
        if (clip == null) return;
        var entry = _audioData.GetEntry(clipId);
        _sfxPool.Play(clip, entry.volume, pitch, entry.priority);
    }

    /// <summary>播放 UI 音效（獨立通道）</summary>
    public void PlayUI(string clipId)
    {
        var clip = _audioData.GetClip(clipId);
        if (clip == null) return;
        var entry = _audioData.GetEntry(clipId);
        _uiPool.Play(clip, entry.volume, 1f, entry.priority);
    }

    /// <summary>停止所有 SFX</summary>
    public void StopAllSFX()
    {
        _sfxPool.StopAll();
        _ambientSource.Stop();
    }

    /// <summary>停止所有音效（BGM + SFX + UI + Ambient）</summary>
    public void StopAll()
    {
        _bgm.StopImmediate();
        _sfxPool.StopAll();
        _uiPool.StopAll();
        _ambientSource.Stop();
    }

    // --- Public API: Ambient ---

    /// <summary>播放環境音（LP 心跳警告等）</summary>
    public void PlayAmbient(string clipId, bool loop = true)
    {
        var clip = _audioData.GetClip(clipId);
        if (clip == null) return;
        var entry = _audioData.GetEntry(clipId);
        _ambientSource.clip = clip;
        _ambientSource.loop = loop;
        _ambientSource.volume = entry.volume;
        _ambientSource.Play();
    }

    public void StopAmbient(float fadeDuration = 1f) =>
        StartCoroutine(FadeOut(_ambientSource, fadeDuration));

    public void SetAmbientPitch(float pitch) => _ambientSource.pitch = pitch;

    // --- Public API: Volume Control ---

    public void SetMasterVolume(float linear) =>
        SetMixerVolume("MasterVolume", linear);

    public void SetBGMVolume(float linear) =>
        SetMixerVolume("BGMVolume", linear);

    public void SetSFXVolume(float linear) =>
        SetMixerVolume("SFXVolume", linear);

    public void SetUIVolume(float linear) =>
        SetMixerVolume("UIVolume", linear);

    public float GetMasterVolume() => GetSavedVolume("MasterVolume", 1f);
    public float GetBGMVolume() => GetSavedVolume("BGMVolume", 0.8f);
    public float GetSFXVolume() => GetSavedVolume("SFXVolume", 1f);
    public float GetUIVolume() => GetSavedVolume("UIVolume", 0.8f);

    /// <summary>儲存音量設定至 PlayerPrefs</summary>
    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", GetMasterVolume());
        PlayerPrefs.SetFloat("BGMVolume", GetBGMVolume());
        PlayerPrefs.SetFloat("SFXVolume", GetSFXVolume());
        PlayerPrefs.SetFloat("UIVolume", GetUIVolume());
        PlayerPrefs.Save();
    }

    // --- Public API: Mixer Snapshots ---

    public void TransitionToSnapshot(string snapshotName, float duration = 0.5f)
    {
        var snapshot = _mixer.FindSnapshot(snapshotName);
        if (snapshot != null) snapshot.TransitionTo(duration);
    }

    // --- Private ---

    private void SetMixerVolume(string param, float linear)
    {
        float dB = linear > 0.0001f ? 20f * Mathf.Log10(linear) : -80f;
        _mixer.SetFloat(param, dB);
        PlayerPrefs.SetFloat(param, linear);
    }

    private float GetSavedVolume(string key, float defaultValue) =>
        PlayerPrefs.GetFloat(key, defaultValue);

    private void LoadVolumeSettings()
    {
        SetMasterVolume(GetSavedVolume("MasterVolume", 1f));
        SetBGMVolume(GetSavedVolume("BGMVolume", 0.8f));
        SetSFXVolume(GetSavedVolume("SFXVolume", 1f));
        SetUIVolume(GetSavedVolume("UIVolume", 0.8f));
    }

    private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
    {
        float start = source.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(start, 0, t / duration);
            yield return null;
        }
        source.Stop();
        source.volume = start;
    }
}
```

## BGMController.cs

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// BGM 播放控制器。管理雙 AudioSource CrossFade。
/// </summary>
public class BGMController : MonoBehaviour
{
    [SerializeField] private AudioSource _sourceA;
    [SerializeField] private AudioSource _sourceB;
    [SerializeField] private AnimationCurve _fadeCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    private AudioSource _active;
    private AudioSource _inactive;
    private Coroutine _fadeCoroutine;

    // Loop point support
    private float _loopStart;
    private float _loopEnd;
    private bool _hasCustomLoop;

    private void Awake()
    {
        _active = _sourceA;
        _inactive = _sourceB;
    }

    private void Update()
    {
        // Custom loop point handling
        if (_hasCustomLoop && _active.isPlaying && _active.time >= _loopEnd)
            _active.time = _loopStart;
    }

    public void Play(AudioClip clip, bool loop, float loopStart, float loopEnd,
                     float crossfadeDuration)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        _inactive.clip = clip;
        _inactive.loop = loop;
        _inactive.volume = 0;
        _inactive.Play();

        _hasCustomLoop = loopStart > 0 || loopEnd < clip.length;
        _loopStart = loopStart;
        _loopEnd = loopEnd > 0 ? loopEnd : clip.length;

        _fadeCoroutine = StartCoroutine(CrossFade(crossfadeDuration));
    }

    public void HardCut(AudioClip clip, bool loop)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _active.Stop();
        _hasCustomLoop = false;

        (_active, _inactive) = (_inactive, _active);
        _active.clip = clip;
        _active.loop = loop;
        _active.volume = 1f;
        _active.Play();
    }

    public void Stop(float fadeDuration)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOutActive(fadeDuration));
    }

    public void StopImmediate()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _active.Stop();
        _inactive.Stop();
    }

    private IEnumerator CrossFade(float duration)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float v = _fadeCurve.Evaluate(t / duration);
            _active.volume = 1f - v;
            _inactive.volume = v;
            yield return null;
        }
        _active.Stop();
        _active.volume = 0;
        _inactive.volume = 1f;
        (_active, _inactive) = (_inactive, _active);
        _fadeCoroutine = null;
    }

    private IEnumerator FadeOutActive(float duration)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            _active.volume = 1f - (t / duration);
            yield return null;
        }
        _active.Stop();
        _fadeCoroutine = null;
    }
}
```

## SFXPool.cs

```csharp
using UnityEngine;

/// <summary>
/// AudioSource Object Pool。管理多個 AudioSource 的並行播放。
/// </summary>
public class SFXPool : MonoBehaviour
{
    [SerializeField] private int _poolSize = 8;
    [SerializeField] private AudioMixerGroup _outputGroup;

    private AudioSource[] _sources;
    private int _nextIndex;

    private void Awake()
    {
        _sources = new AudioSource[_poolSize];
        for (int i = 0; i < _poolSize; i++)
        {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0;
            src.outputAudioMixerGroup = _outputGroup;
            _sources[i] = src;
        }
    }

    public void Play(AudioClip clip, float volume, float pitch, int priority)
    {
        var source = GetAvailableSource(priority);
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.priority = priority;
        source.Play();
    }

    public void StopAll()
    {
        foreach (var s in _sources) s.Stop();
    }

    private AudioSource GetAvailableSource(int priority)
    {
        // 1. 找空閒的
        foreach (var s in _sources)
            if (!s.isPlaying) return s;

        // 2. 找優先級最低的（數值最大）
        AudioSource lowest = _sources[0];
        foreach (var s in _sources)
            if (s.priority > lowest.priority) lowest = s;

        // 3. 只有新音效優先級更高時才搶佔
        if (priority < lowest.priority)
        {
            lowest.Stop();
            return lowest;
        }

        // 4. 同優先級，用 round-robin
        var src = _sources[_nextIndex];
        _nextIndex = (_nextIndex + 1) % _poolSize;
        src.Stop();
        return src;
    }
}
```

## AudioData.cs（ScriptableObject）

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AudioData", menuName = "Audio/AudioData")]
public class AudioData : ScriptableObject
{
    [Serializable]
    public class AudioEntry
    {
        public string id;
        public AudioClip clip;
        public bool loop;
        public float loopStart;
        public float loopEnd;
        public float volume = 1f;
        [Range(0, 256)] public int priority = 128;
        public string category;
    }

    [SerializeField] private List<AudioEntry> _entries = new();
    private Dictionary<string, AudioEntry> _lookup;

    public AudioClip GetClip(string id) => GetEntry(id)?.clip;

    public AudioEntry GetEntry(string id)
    {
        if (_lookup == null) BuildLookup();
        return _lookup.TryGetValue(id, out var e) ? e : null;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, AudioEntry>(_entries.Count);
        foreach (var e in _entries) _lookup[e.id] = e;
    }
}
```

## AudioConstants.cs

```csharp
public static class AudioConstants
{
    // BGM IDs
    public const string BGM_MAIN_MENU = "bgm_main_menu";
    public const string BGM_BATTLE_NORMAL = "bgm_battle_normal";
    public const string BGM_BATTLE_TENSE = "bgm_battle_tense";
    public const string BGM_BATTLE_BOSS = "bgm_battle_boss";
    public const string BGM_DECK_EDIT = "bgm_deck_edit";
    public const string BGM_VICTORY = "bgm_victory";
    public const string BGM_DEFEAT = "bgm_defeat";

    // SFX IDs — Card Ops
    public const string SFX_DRAW_CARD = "sfx_draw_card";
    public const string SFX_SET_CARD = "sfx_set_card";
    public const string SFX_FLIP_CARD = "sfx_flip_card";
    public const string SFX_CARD_SELECT = "sfx_card_select";
    public const string SFX_DISCARD = "sfx_discard";

    // SFX IDs — Summon
    public const string SFX_NORMAL_SUMMON = "sfx_normal_summon";
    public const string SFX_TRIBUTE_SUMMON = "sfx_tribute_summon";
    public const string SFX_SPECIAL_SUMMON = "sfx_special_summon";
    public const string SFX_FUSION_SUMMON = "sfx_fusion_summon";
    public const string SFX_TRIBUTE_RELEASE = "sfx_tribute_release";

    // SFX IDs — Battle
    public const string SFX_ATTACK_DECLARE = "sfx_attack_declare";
    public const string SFX_ATTACK_HIT = "sfx_attack_hit";
    public const string SFX_MONSTER_DESTROY = "sfx_monster_destroy";
    public const string SFX_DIRECT_ATTACK = "sfx_direct_attack";
    public const string SFX_DAMAGE_SMALL = "sfx_damage_small";
    public const string SFX_DAMAGE_LARGE = "sfx_damage_large";
    public const string SFX_ATTACK_REFLECT = "sfx_attack_reflect";

    // SFX IDs — Spell/Trap
    public const string SFX_SPELL_ACTIVATE = "sfx_spell_activate";
    public const string SFX_TRAP_ACTIVATE = "sfx_trap_activate";
    public const string SFX_CHAIN_START = "sfx_chain_start";
    public const string SFX_CHAIN_STACK = "sfx_chain_stack";
    public const string SFX_CHAIN_RESOLVE = "sfx_chain_resolve";
    public const string SFX_NEGATE = "sfx_negate";
    public const string SFX_CONTINUOUS_ACTIVATE = "sfx_continuous_activate";

    // SFX IDs — Turn
    public const string SFX_TURN_START_MINE = "sfx_turn_start_mine";
    public const string SFX_TURN_START_OPPONENT = "sfx_turn_start_opponent";
    public const string SFX_PHASE_CHANGE = "sfx_phase_change";
    public const string SFX_TURN_END = "sfx_turn_end";

    // SFX IDs — LP
    public const string SFX_LP_DAMAGE = "sfx_lp_damage";
    public const string SFX_LP_HEAL = "sfx_lp_heal";
    public const string SFX_LP_WARNING = "sfx_lp_warning";

    // SFX IDs — Result
    public const string SFX_VICTORY = "sfx_victory";
    public const string SFX_DEFEAT = "sfx_defeat";

    // SFX IDs — UI
    public const string SFX_UI_CLICK = "sfx_ui_click";
    public const string SFX_UI_HOVER = "sfx_ui_hover";
    public const string SFX_UI_POPUP_OPEN = "sfx_ui_popup_open";
    public const string SFX_UI_POPUP_CLOSE = "sfx_ui_popup_close";
    public const string SFX_MATCH_FOUND = "sfx_match_found";
    public const string SFX_COUNTDOWN_TICK = "sfx_countdown_tick";
    public const string SFX_COUNTDOWN_END = "sfx_countdown_end";
    public const string SFX_MATCHING_LOOP = "sfx_matching_loop";

    // CrossFade
    public const float CROSSFADE_DURATION = 2.0f;
    public const float BOSS_MIN_DURATION = 30f;
    public const float RESULT_SFX_DELAY = 1.0f;
    public const float RESULT_SILENCE_GAP = 2.0f;

    // Mixer Parameters
    public const string PARAM_MASTER = "MasterVolume";
    public const string PARAM_BGM = "BGMVolume";
    public const string PARAM_SFX = "SFXVolume";
    public const string PARAM_UI = "UIVolume";
    public const string PARAM_AMBIENT = "AmbientVolume";
    public const string PARAM_BGM_LOWPASS = "BGMLowpass";
}
```

## GameObject 階層結構

```
[AudioManager] (DontDestroyOnLoad)
├── BGMController
│   ├── BGM_SourceA (AudioSource → BGM Group)
│   └── BGM_SourceB (AudioSource → BGM Group)
├── SFXPool
│   ├── SFX_0 ~ SFX_7 (AudioSource × 8 → SFX Group)
├── UIPool
│   ├── UI_0 ~ UI_3 (AudioSource × 4 → UI Group)
└── AmbientSource (AudioSource → Ambient Group)
```

## 使用範例

```csharp
// 播放 BGM
AudioManager.Instance.PlayBGM(AudioConstants.BGM_BATTLE_NORMAL);

// 切換緊張 BGM
AudioManager.Instance.PlayBGM(AudioConstants.BGM_BATTLE_TENSE);

// 播放音效
AudioManager.Instance.PlaySFX(AudioConstants.SFX_ATTACK_HIT);

// 連鎖疊加（每層 +2 半音）
AudioManager.Instance.PlaySFX(AudioConstants.SFX_CHAIN_STACK, pitch: 1.122f * chainLayer);

// 勝利序列
AudioManager.Instance.StopAll();
AudioManager.Instance.PlaySFX(AudioConstants.SFX_VICTORY);
yield return new WaitForSeconds(1f);
AudioManager.Instance.PlayBGMHardCut(AudioConstants.BGM_VICTORY);

// LP 心跳警告
AudioManager.Instance.PlayAmbient(AudioConstants.SFX_LP_WARNING);
AudioManager.Instance.SetAmbientPitch(1.5f); // LP 1000-500

// 音量控制
AudioManager.Instance.SetBGMVolume(0.7f); // 70%
AudioManager.Instance.SaveVolumeSettings();

// Snapshot 切換
AudioManager.Instance.TransitionToSnapshot("BattleIntense", 0.5f);
```
