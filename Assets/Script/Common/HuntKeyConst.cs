using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using UnityEngine;

namespace hunt
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
    public static class HuntKeyConst
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
    }
    public static class AniKeyConst
    {
        // Animate
        public static readonly int K_tAttack = Animator.StringToHash("IsAttack");
        public static readonly int k_bMove = Animator.StringToHash("IsMove");
        public static readonly int k_bGround = Animator.StringToHash("IsGround");
        public static readonly int k_bSelect = Animator.StringToHash("IsSelect");
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
                ProfessionType.Sword => HuntKeyConst.Ks_Profession_Worrior,
                ProfessionType.Archer => HuntKeyConst.Ks_Profession_Magician,
                ProfessionType.Fighter => HuntKeyConst.Ks_Profession_Tanker,
                _ => string.Empty
            };
        }

        public static string GetProfessionIllustKey(ProfessionType profession)
        {
            return profession switch
            {
                ProfessionType.Sword => HuntKeyConst.Ks_Illust_Astera,
                ProfessionType.Archer => HuntKeyConst.Ks_Illust_Sable,
                ProfessionType.Fighter => HuntKeyConst.Ks_Illust_Brunt,
                _ => string.Empty
            };
        }
    }
}
