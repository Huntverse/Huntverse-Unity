using System.Collections.Generic;
using UnityEngine;

namespace hunt
{
    public enum AudioType
    {
        SFX_HOVER,
        SFX_CHANNEL_SELECT,
        BGM_MAIN,
    }

    public static class AudioConst 
    {
        private static readonly Dictionary<AudioType, string> sfxKeys = new Dictionary<AudioType, string>
        {
            //{ AudioType.SFX_HOVER, "hover@audio" },
            { AudioType.SFX_CHANNEL_SELECT, "channel_sfx@audio" },
            { AudioType.BGM_MAIN, "main_bgm@audio" }
        };

        public static string GetSfxKey(AudioType sfxType)
        {
            return sfxKeys.TryGetValue(sfxType, out var key) ? key : string.Empty;
        }

        public static IEnumerable<string> GetAllSfxKeys()
        {
            return sfxKeys.Values;
        }
    }
}
