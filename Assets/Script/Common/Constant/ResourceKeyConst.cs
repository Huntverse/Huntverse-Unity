using Hunt.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClassType
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
        public static readonly string Ks_Profession_Magician = "archer_pf@sprite";
        public static readonly string Ks_Profession_Tanker = "tanker_pf@sprite";

        public static readonly string Ks_Illust_Astera = "astera@sprite";
        public static readonly string Ks_Illust_Brunt = "brunt@sprite";
        public static readonly string Ks_Illust_Sable = "sable@sprite";

        // Player
        public static readonly string Kp_Model_Seible = "seible@model";
        public static readonly string Kp_Model_Astera = "astera@model";
        public static readonly string Kp_Model_Brunt = "brunt@model";

        // Prefab
        public static readonly string Kp_Portrait_Cam = "port_cam@prefab";
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
        public static readonly int k_cDancing = Animator.StringToHash("Dancing");
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

        public static string GetWorldNameByWorldId(uint worldId)
        {
            return worldId switch
            {
                1 => "그라시아",
                2 => "라비올래",
                3 => "카탄",
                _ => $"World_{worldId}",
            };
        }

        public static uint GetWorldIdByWorldName(string worldName)
        {
            return worldName switch
            {
                "그라시아" => 1,
                "라비올래" => 2,
                "카탄" => 3,
                _ => 0
            };
        }
        public static string GetProfessionMatchName(ClassType profession, bool eng = false)
        {
            return profession switch
            {
                ClassType.Sword => eng ? "ASTRA" : "아스트라",
                ClassType.Archer => eng ? "SEIBLE" : "세이블",
                ClassType.Fighter => eng ? "BRUNT" : "브런트",
                _ => string.Empty
            };
        }

        public static string GetIconKeyByProfession(ClassType profession)
        {
            return profession switch
            {
                ClassType.Sword => ResourceKeyConst.Ks_Profession_Worrior,
                ClassType.Archer => ResourceKeyConst.Ks_Profession_Magician,
                ClassType.Fighter => ResourceKeyConst.Ks_Profession_Tanker,
                _ => string.Empty
            };
        }

        public static string GetIllustKeyByProfession(ClassType profession)
        {
            return profession switch
            {
                ClassType.Sword => ResourceKeyConst.Ks_Illust_Astera,
                ClassType.Archer => ResourceKeyConst.Ks_Illust_Sable,
                ClassType.Fighter => ResourceKeyConst.Ks_Illust_Brunt,
                _ => string.Empty
            };
        }

        public static string GetModelKeyByProfession(ClassType profession)
        {
            return profession switch
            {
                ClassType.Sword => ResourceKeyConst.Kp_Model_Astera,
                ClassType.Archer => ResourceKeyConst.Kp_Model_Seible,
                ClassType.Fighter => ResourceKeyConst.Kp_Model_Brunt,

                _ => string.Empty
            };

        }
        public static Job_ID GetJobIdByClassType(ClassType type)
        {
            return type switch
            {
                ClassType.Sword => Job_ID.JobWarrior,      // 101
                ClassType.Archer => Job_ID.JobArcher,      // 102
                ClassType.Fighter => Job_ID.JobBoxer,      // 103
                _ => Job_ID.JobWarrior
            };
        }

        public static ClassType GetClassTypeByJobId(uint jobId)
        {
            return jobId switch
            {
                101 => ClassType.Sword,      // JobWarrior
                102 => ClassType.Archer,     // JobArcher
                103 => ClassType.Fighter,    // JobBoxer
                _ => ClassType.Sword
            };
        }
        public static string GetMapNameByMapId(ulong mapId)
        {
            return mapId switch
            {
                0 => "레미나의 잠경촌",
                1 => "일루네스의 상념정",
                2 => "서광잔영의 숲",
                _ => string.Empty
            };
        }

        public static string GetStatStringByType(CharStatType t)
        {
            return t switch
            {
                CharStatType.HP => "체력",
                CharStatType.MP => "마력",
                CharStatType.STR => "힘",
                CharStatType.INT => "지능",
                CharStatType.PATK => "물리공격",
                CharStatType.MATK => "마법공격",
                CharStatType.CRIT => "크리티컬",
                CharStatType.ASPD => "공격속도",
                CharStatType.MSPD => "이동속도",
                CharStatType.LUK => "저주",
                CharStatType.DEF => "방어력",
                CharStatType.EVA => "회피",

                _ => string.Empty
            };

        }
    }

    public static class VfxKetConst
    {
        public static readonly string Kp_plain_hit_astera = "astera_planhit@vfx";
    }

}
