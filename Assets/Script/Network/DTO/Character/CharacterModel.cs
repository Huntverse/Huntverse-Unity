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

    public class CharacterModel
    {
        [JsonProperty("profession")] public ProfessionType profession;
        [JsonProperty("name")] public string name;
        [JsonProperty("level")] public int level;
        [JsonProperty("savepoint")] public string savepoint;
        [JsonProperty("stats")] public float[] stats;

        [JsonIgnore] public Sprite icon;
        [JsonIgnore] public bool IsCreated => !string.IsNullOrEmpty(name);

        public static CharacterModel FromPayload(CharacterInfoPayload p)
        {
            return new CharacterModel
            {
                level = p.level,
                name = p.name,
                profession = p.professiontype,
                icon = p.icon,
                savepoint = p.savepoint,
                stats = p.stats
            };


        }

    }
}