using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProfessionType
    {
        Sword,
        Archer,
        Fighter,
        Unknwon

    }
    public static class ResourceKeyConst
    {
        public static readonly string Ks_Mainmenu = "mainmenu@scene";
        public static readonly string Ks_Village = "village@scene";

        public static readonly string Ka_isActive = "IsActive";

        // Sprite
        public static readonly string Ks_Profession_Worrior = "worrior_pf@sprite";
        public static readonly string Ks_Profession_Magician = "magician_pf@sprite";
        public static readonly string Ks_Profession_Tanker = "tanker_pf@sprite";

        public static readonly string Ks_Illust_Astera = "astera@sprite";
        public static readonly string Ks_Illust_Brunt = "brunt@sprite";
        public static readonly string Ks_Illust_Sable = "sable@sprite";

        // Player
        public static readonly string Kp_Model_Seible = "seible@model";
        public static readonly string Kp_Model_Astera = "astera@model";
        public static readonly string Kp_Model_Brunt = "brunt@model";
    }
    public static class AniKeyConst
    {
        // Animate
        public static readonly int K_tAttack = Animator.StringToHash("IsAttack");
        public static readonly int k_bMove = Animator.StringToHash("IsMove");
        public static readonly int k_bGround = Animator.StringToHash("IsGround");
        public static readonly int k_bSelect = Animator.StringToHash("IsSelect");
        public static readonly int k_bValid = Animator.StringToHash("IsVaild");
        public static readonly int k_tFail = Animator.StringToHash("tFail");
    }
    public enum AudioType
    {
        SFX_HOVER,
        SFX_CHANNEL_SELECT,
        BGM_MAIN,
        BGM_VILLAGE,
    }

    public static class AudioKeyConst
    {
        private static readonly Dictionary<AudioType, string> sfxKeys = new Dictionary<AudioType, string>
        {
            //{ AudioType.SFX_HOVER, "hover@audio" },
            { AudioType.SFX_CHANNEL_SELECT, "channel_sfx@audio" },
            { AudioType.BGM_MAIN, "main_bgm@audio" },
            { AudioType.BGM_VILLAGE, "village_bgm@audio" }
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
    public static class BindKeyConst
    {
        public static string GetProfessionMatchName(ProfessionType profession)
        {
            return profession switch
            {
                ProfessionType.Sword => "아스트라",
                ProfessionType.Archer => "세이블",
                ProfessionType.Fighter => "브런트",
                _ => string.Empty
            };
        }

        public static string GetProfessionIconKey(ProfessionType profession)
        {
            return profession switch
            {
                ProfessionType.Sword => ResourceKeyConst.Ks_Profession_Worrior,
                ProfessionType.Archer => ResourceKeyConst.Ks_Profession_Magician,
                ProfessionType.Fighter => ResourceKeyConst.Ks_Profession_Tanker,
                _ => string.Empty
            };
        }

        public static string GetProfessionIllustKey(ProfessionType profession)
        {
            return profession switch
            {
                ProfessionType.Sword => ResourceKeyConst.Ks_Illust_Astera,
                ProfessionType.Archer => ResourceKeyConst.Ks_Illust_Sable,
                ProfessionType.Fighter => ResourceKeyConst.Ks_Illust_Brunt,
                _ => string.Empty
            };
        }

        public static string GetProfessionModelkey(ProfessionType profession)
        {
            return profession switch
            {
                ProfessionType.Sword => ResourceKeyConst.Kp_Model_Astera,
                ProfessionType.Archer => ResourceKeyConst.Kp_Model_Seible,
                ProfessionType.Fighter => ResourceKeyConst.Kp_Model_Brunt,

                _ => string.Empty
            };

        }

    }
}
