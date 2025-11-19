using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
namespace hunt
{
    public class CharacterCreateController : MonoBehaviourSingleton<CharacterCreateController>
    {
        [Header("CharacterInfo Field")]
        [SerializeField] private List<CharacterInfoField> characterInfoField;

        private string currentChannelName;
        private int currentCharacterCount;
        private List<CharacterModel> cachedCharacters = new List<CharacterModel>();

        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
        }

        public void UpdateCharacterSlots(string channelName, int characterCount)
        {
            currentChannelName = channelName;
            currentCharacterCount = characterCount;

            $"???? [Character] UpdateCharacterSlots - Channel: {channelName}, Count: {characterCount}".DLog();

            if (characterInfoField == null || characterInfoField.Count == 0)
            {
                "???? [Character] characterInfoField is null or empty".DLog();
                return;
            }

            for (int i = 0; i < characterInfoField.Count; i++)
            {
                if (characterInfoField[i] == null) continue;

                bool hasCharacter = i < characterCount;
                
                if (hasCharacter && i < cachedCharacters.Count && cachedCharacters[i] != null)
                {
                   
                    characterInfoField[i].Bind(cachedCharacters[i]);
                }
                else if (hasCharacter)
                {
                    
                    characterInfoField[i].InitField(true);
                }
                else
                {
                    
                    characterInfoField[i].InitField(false);
                    characterInfoField[i].SetLevelFieldValue(0);
                    characterInfoField[i].SetNameFieldValue(string.Empty);
                    characterInfoField[i].SetSavePointFieldValie(string.Empty);
                }
            }
        }

        public void OnRecvCharacterFieldViewUpdate(CharacterFieldListRequst res)
        {
            if (res?.chfields == null)
            {
                "???? [Character] OnRecvCharacterFieldViewUpdate - res is null".DLog();
                return;
            }

            $"???? [Character] OnRecvCharacterFieldViewUpdate - Count: {res.chfields.Count}".DLog();

            cachedCharacters.Clear();
            for (int i = 0; i < res.chfields.Count && i < characterInfoField.Count; i++)
            {
                if (characterInfoField[i] == null) continue;

                var model = CharacterModel.FromPayload(res.chfields[i]);
                cachedCharacters.Add(model);
                characterInfoField[i].Bind(model);
            }
        }

        public void OnSelectCharacterField(CharacterInfoField selected)
        {
            if (selected == null) return;

            if (characterInfoField != null)
            {
                foreach (var field in characterInfoField)
                {
                    if (field == null || field == selected) continue;
                    field.HightlightField(false);
                }
            }

            selected.HightlightField(true);
        }

        private async void OnEnable()
        {
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            ResetSelectionState();
        }

        private void OnDisable()
        {
            ResetSelectionState();
        }

        private void ResetSelectionState()
        {
            if (characterInfoField == null) return;

            foreach (var field in characterInfoField)
            {
                if (field == null) continue;
                field.HightlightField(false);
            }
        }



        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
