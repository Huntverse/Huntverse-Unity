using UnityEngine;
using System.Collections.Generic;
namespace hunt
{
    public class CharacterCreateController : MonoBehaviourSingleton<CharacterCreateController>
    {
        [Header("CharacterInfo Field")]
        [SerializeField] private List<CharacterInfoField> characterInfoField;

        private string currentChannelName;
        private int currentCharacterCount;

        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
        }

        public void UpdateCharacterSlots(string channelName, int characterCount)
        {
            currentChannelName = channelName;
            currentCharacterCount = characterCount;

            $"[Character] UpdateCharacterSlots - Channel: {channelName}, Count: {characterCount}".DLog();

            if (characterInfoField == null || characterInfoField.Count == 0)
            {
                "[Character] characterInfoField is null or empty".DLog();
                return;
            }

            for (int i = 0; i < characterInfoField.Count; i++)
            {
                if (characterInfoField[i] == null) continue;

                bool hasCharacter = i < characterCount;
                characterInfoField[i].gameObject.SetActive(true);
                characterInfoField[i].InitField(hasCharacter);

                if (!hasCharacter)
                {
                    characterInfoField[i].SetLevelFieldValue(0);
                    characterInfoField[i].SetNameFieldValue(string.Empty);
                }
            }
        }

        public void OnRecvCharacterFieldViewUpdate(CharacterFieldListRequst res)
        {
            if (res?.chfields == null)
            {
                "[Character] OnRecvCharacterFieldViewUpdate - res is null".DLog();
                return;
            }

            $"[Character] OnRecvCharacterFieldViewUpdate - Count: {res.chfields.Count}".DLog();

            for (int i = 0; i < res.chfields.Count && i < characterInfoField.Count; i++)
            {
                if (characterInfoField[i] == null) continue;

                var model = CharacterModel.FromPayload(res.chfields[i]);
                characterInfoField[i].Bind(model);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
