using System;
using UnityEngine;
using System.Collections.Generic;
namespace hunt
{
    [Serializable]
    public class CharacterInfoPayload
    {
        public Sprite icon;
        public ProfessionType professiontype;
        public string name;
        public int level;
        public string savepoint;
    }
    [Serializable]
    public class CharacterFieldListRequst
    {
        public List<CharacterInfoPayload> chfields;
    }

}