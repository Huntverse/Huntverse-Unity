using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace hunt
{
    public class CharacterCreateController : MonoBehaviourSingleton<CharacterCreateController>
    {
        [Header("CharacterInfo Field")]
        [SerializeField] private List<CharacterInfoField> characterInfoFields;
        [SerializeField] private List<GenerationCharacterInfoField> newCharacterInfoFields;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private GenerationCharacterPanel generationcharacterPanel;
        [SerializeField] private GameObject loadCreatedCharacterPanel;

        private string currentChannelName;
        private int currentCharacterCount;
        private List<CharacterModel> cachedCharacters = new List<CharacterModel>();

        private int currentGenerationCharacterIndex = 0;
        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
            nextButton.onClick.AddListener(() => OnShowChracterInfo(1));
            prevButton.onClick.AddListener(() => OnShowChracterInfo(-1));
        }


        public void UpdateCharacterSlots(string channelName, int characterCount)
        {
            currentChannelName = channelName;
            currentCharacterCount = characterCount;

            $"???? [Character] UpdateCharacterSlots - Channel: {channelName}, Count: {characterCount}".DLog();

            if (characterInfoFields == null || characterInfoFields.Count == 0)
            {
                "???? [Character] characterInfoField is null or empty".DLog();
                return;
            }

            for (int i = 0; i < characterInfoFields.Count; i++)
            {
                if (characterInfoFields[i] == null) continue;

                bool hasCharacter = i < characterCount;

                if (hasCharacter && i < cachedCharacters.Count && cachedCharacters[i] != null)
                {

                    characterInfoFields[i].Bind(cachedCharacters[i]);
                }
                else if (hasCharacter)
                {

                    characterInfoFields[i].InitField(true);
                }
                else
                {

                    characterInfoFields[i].InitField(false);
                    characterInfoFields[i].SetLevelFieldValue(0);
                    characterInfoFields[i].SetNameFieldValue(string.Empty);
                    characterInfoFields[i].SetSavePointFieldValie(string.Empty);
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
            for (int i = 0; i < res.chfields.Count && i < characterInfoFields.Count; i++)
            {
                if (characterInfoFields[i] == null) continue;

                var model = CharacterModel.FromPayload(res.chfields[i]);
                cachedCharacters.Add(model);
                characterInfoFields[i].Bind(model);
            }
        }

        public void OnSelectCharacterField(CharacterInfoField selected)
        {
            if (selected == null) return;

            if (characterInfoFields != null)
            {
                foreach (var field in characterInfoFields)
                {
                    if (field == null || field == selected) continue;
                    field.HightlightField(false);
                }
            }

            selected.HightlightField(true);
        }
        public void OnShowChracterInfo(int index)
        {
            if (newCharacterInfoFields == null || newCharacterInfoFields.Count <= 0)
            {
                return;
            }

            currentGenerationCharacterIndex += index;

            if (currentGenerationCharacterIndex < 0) currentGenerationCharacterIndex = 0;
            else if (currentGenerationCharacterIndex >= newCharacterInfoFields.Count)
            {
                currentGenerationCharacterIndex = newCharacterInfoFields.Count - 1;
            }

            for (int i = 0; i < newCharacterInfoFields.Count; i++)
            {
                var field = newCharacterInfoFields[i];
                if (field == null) continue;

                bool active = (i == currentGenerationCharacterIndex);
                if (field.gameObject.activeSelf != active)
                {
                    field.gameObject.SetActive(active);
                }

                generationcharacterPanel.OnSetFieldValue(field.storyString, new float[] { 0.8f, 0.6f, 0.2f, 0.6f, 0.5f });
            }
            if (prevButton != null)
            {
                prevButton.interactable = currentGenerationCharacterIndex > 0;
            }

            if (nextButton != null)
            {
                nextButton.interactable = currentGenerationCharacterIndex < newCharacterInfoFields.Count-1;
            }

      
            // next, prev버튼을 누를때마다 
            // newCharacterInfoFields의 인덱스가 바뀌어 활성화 되며  NewCharacterInfoPannel 의 내용이 바뀜
            // 활성화된 나머지 인덱스는 비활성화한다.


        }
        public void OnCreateNewCharacter(ProfessionType profession)
        {
            $"캐릭터 생성".DLog();
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
            if (characterInfoFields == null) return;

            foreach (var field in characterInfoFields)
            {
                if (field == null) continue;
                field.HightlightField(false);
            }

        }


        protected override void OnDestroy()
        {

            nextButton.onClick.RemoveAllListeners();
            prevButton.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}
