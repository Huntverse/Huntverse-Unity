using Hunt.Game;
using Hunt.Login;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    public class CharModel
    {
        public uint worldId;
        public ulong charId;
        public string name;
        public ClassType classtype;
        public ulong level;
        public ulong mapId;
        public List<StatInfo> stats;
        
        public Sprite icon;

        public bool IsCreated => !string.IsNullOrEmpty(name);

        public static CharModel FromCharacterInfo(SimpleCharacterInfo inp)
        {
            return new CharModel
            {
                worldId = inp.WorldId,
                charId = inp.CharId,
                name = inp.Name,
                classtype = BindKeyConst.GetClassTypeByJobId(inp.ClassType),
                level = inp.Level,
                mapId = inp.MapId,
                stats = new List<StatInfo>(inp.StatInfos)

            };


        }

    }
}