using Hunt.Login;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    public class GameSession : MonoBehaviourSingleton<GameSession>
    {
        protected override bool DontDestroy => true;
        public List<SimpleCharacterInfo> CharacterInfos { get; protected set; }
        public SimpleCharacterInfo SelectedCharacter { get; protected set; }

        // Dev
        public CharacterModel SelectedCharacterModel { get; protected set; }
        
        // Login
        public void SetCharacterList(List<SimpleCharacterInfo> characters)
        {
            CharacterInfos = new List<SimpleCharacterInfo>(characters);
            $"[GameSession] 캐릭터 리스트 저장 : {characters.Count}개".DLog();
        }


        public void SelectCharacter(SimpleCharacterInfo character)
        {
            SelectedCharacter = character;
            $"[GameSession] 선택된 캐릭터 : 이름->{character.Name} , 직업->{character.ClassType}".DLog();
        }

        public void SelectCharacterById(ulong charId)
        {
            SelectedCharacter = CharacterInfos?.Find(c => c.CharId == charId);
            if(SelectedCharacter != null)
            {
                $"[GameSession] 캐릭터 선택 : {SelectedCharacter.Name}".DLog();
            }
        }

        // Dev
        public void SelectCharacterModel(CharacterModel model)
        {
            SelectedCharacterModel = model;
            $"[GameSession] 선택된 캐릭터 (Model): {model.name} (ClassType: {model.classtype})".DLog();
        }
        protected override void Awake()
        {
            base.Awake();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
