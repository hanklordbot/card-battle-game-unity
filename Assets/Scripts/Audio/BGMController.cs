using UnityEngine;
using System.Collections;

namespace CardBattle.Audio
{
    /// <summary>
    /// BGM controller with dual AudioSource crossfade and custom loop points.
    /// </summary>
    public class BGMController : MonoBehaviour
    {
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _active;
        private AudioSource _inactive;
        private Coroutine _fadeCoroutine;
        private float _loopStart, _loopEnd;
        private bool _hasCustomLoop;
        private string _currentClipName;

        public string CurrentClipName => _currentClipName;

        private void Awake()
        {
            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceA.playOnAwake = false;
            _sourceA.spatialBlend = 0;

            _sourceB = gameObject.AddComponent<AudioSource>();
            _sourceB.playOnAwake = false;
            _sourceB.spatialBlend = 0;

            _active = _sourceA;
            _inactive = _sourceB;
        }

        private void Update()
        {
            if (_hasCustomLoop && _active.isPlaying && _active.time >= _loopEnd)
                _active.time = _loopStart;
        }

        public void Play(AudioClip clip, bool loop, float loopStart, float loopEnd, float crossfadeDuration)
        {
            if (_active.clip == clip && _active.isPlaying) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            _inactive.clip = clip;
            _inactive.loop = loop;
            _inactive.volume = 0;
            _inactive.Play();

            _hasCustomLoop = loopStart > 0 || (loopEnd > 0 && loopEnd < clip.length);
            _loopStart = loopStart;
            _loopEnd = loopEnd > 0 ? loopEnd : clip.length;
            _currentClipName = clip.name;

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
            _currentClipName = clip.name;
        }

        public void Stop(float fadeDuration)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutActive(fadeDuration));
            _currentClipName = null;
        }

        public void StopImmediate()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _active.Stop();
            _inactive.Stop();
            _currentClipName = null;
        }

        public void SetVolume(float vol)
        {
            if (_active.isPlaying) _active.volume = vol;
        }

        private IEnumerator CrossFade(float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float v = Mathf.SmoothStep(0, 1, t / duration);
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
            float start = _active.volume;
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _active.volume = Mathf.Lerp(start, 0, t / duration);
                yield return null;
            }
            _active.Stop();
            _fadeCoroutine = null;
        }
    }
}
