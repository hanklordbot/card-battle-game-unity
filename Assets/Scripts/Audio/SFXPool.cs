using UnityEngine;

namespace CardBattle.Audio
{
    /// <summary>
    /// AudioSource object pool for concurrent SFX playback.
    /// </summary>
    public class SFXPool : MonoBehaviour
    {
        [SerializeField] private int _poolSize = 8;
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
                _sources[i] = src;
            }
        }

        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            var source = GetAvailable();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        public void StopAll()
        {
            foreach (var s in _sources)
                if (s != null) s.Stop();
        }

        public void SetVolume(float vol)
        {
            foreach (var s in _sources)
                if (s != null) s.volume = vol;
        }

        private AudioSource GetAvailable()
        {
            // Find idle source
            foreach (var s in _sources)
                if (!s.isPlaying) return s;

            // Round-robin evict
            var src = _sources[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _poolSize;
            src.Stop();
            return src;
        }
    }
}
