using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 애니메이션 이벤트 ID 기준으로 VFX/SFX를 정의하는 중앙 테이블.
    /// - eventId는 애니메이션 이벤트(stringParameter)와 일치해야 한다.
    /// - 직업/상태/무기 등으로 세분화가 필요하면 필드 추가해서 확장.
    /// </summary>
    [CreateAssetMenu(fileName = "FxEventTable", menuName = "Hunt/FxEventTable")]
    public class FxEventTable : ScriptableObject
    {
        [Serializable]
        public class VfxEntry
        {
            public string vfxKey;
            public Vector3 offset;
            public Vector3 rotationOffsetEuler;
            public bool attachHit;
        }

        [Serializable]
        public class SfxEntry
        {
            public AudioType audioType;
            public float volumeScale = 1f;
        }

        [Serializable]
        public class FxEventConfig
        {
            public string eventId;
            public List<VfxEntry> vfxEntries = new List<VfxEntry>();
            public List<SfxEntry> sfxEntries = new List<SfxEntry>();
        }

        [SerializeField] private List<FxEventConfig> configs = new List<FxEventConfig>();

        private Dictionary<string, FxEventConfig> _cache;

        private void OnEnable()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            _cache = new Dictionary<string, FxEventConfig>(StringComparer.Ordinal);
            if (configs == null) return;

            foreach (var c in configs)
            {
                if (c == null || string.IsNullOrEmpty(c.eventId)) continue;
                _cache[c.eventId] = c;
            }
        }

        public FxEventConfig GetConfig(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;
            if (_cache == null || _cache.Count == 0)
            {
                BuildCache();
                if (_cache == null || _cache.Count == 0) return null;
            }

            _cache.TryGetValue(eventId, out var config);
            return config;
        }
    }
}

