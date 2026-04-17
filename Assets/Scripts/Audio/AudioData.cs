using UnityEngine;
using System;
using System.Collections.Generic;

namespace CardBattle.Audio
{
    /// <summary>
    /// ScriptableObject holding all audio clip references and metadata.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioData", menuName = "CardBattle/AudioData")]
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
            [Range(0f, 1f)] public float volume = 1f;
        }

        [SerializeField] private List<AudioEntry> entries = new();
        private Dictionary<string, AudioEntry> _lookup;

        public AudioClip GetClip(string id)
        {
            var entry = GetEntry(id);
            return entry?.clip;
        }

        public AudioEntry GetEntry(string id)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(id, out var e) ? e : null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, AudioEntry>(entries.Count);
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.id))
                    _lookup[e.id] = e;
        }

        private void OnEnable() => _lookup = null; // rebuild on load
    }
}
