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
        [SerializeField] private UserCharacterPanel userCharacterPanel;

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
            userCharacterPanel.gameObject.SetActive(false);
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
                    // 캐시된 캐릭터 데이터가 있으면 바인딩
                    characterInfoFields[i].Bind(cachedCharacters[i]);
                }
                else if (hasCharacter)
                {
                    // 캐릭터 슬롯은 있지만 데이터가 없는 경우
                    // 이미 필드에 데이터가 있는지 확인
                    if (characterInfoFields[i].HasCharacterData)
                    {
                        // 기존 데이터가 있으면 유지 (재바인딩하지 않음)
                        $"???? [Character] Field {i} already has character data, keeping existing".DLog();
                    }
                    else
                    {
                        // 데이터가 없으면 빈 필드로 초기화
                        characterInfoFields[i].InitField(true);
                    }
                }
                else
                {
                    // 캐릭터 슬롯이 없는 경우
                    characterInfoFields[i].InitField(false);
                    characterInfoFields[i].SetLevelFieldValue(0);
                    characterInfoFields[i].SetNameFieldValue(string.Empty);
                    characterInfoFields[i].SetSavePointFieldValie(string.Empty);
                }
            }
        }

        /// <summary>
        /// 특정 인덱스의 CharacterInfoField가 이미 캐릭터 데이터를 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasCharacterDataAt(int index)
        {
            if (index < 0 || index >= characterInfoFields.Count) return false;
            if (characterInfoFields[index] == null) return false;
            return characterInfoFields[index].HasCharacterData;
        }

        /// <summary>
        /// 특정 인덱스의 CharacterInfoField에서 현재 바인딩된 CharacterModel을 가져옵니다.
        /// </summary>
        public CharacterModel GetCharacterModelAt(int index)
        {
            if (index < 0 || index >= characterInfoFields.Count) return null;
            if (characterInfoFields[index] == null) return null;
            return characterInfoFields[index].CurrentModel;
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

        public async void OnSelectCharacterField(CharacterInfoField selected)
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

   
            if (selected.HasCharacterData && userCharacterPanel != null)
            {
                
                var model = selected.CurrentModel;
                if (model != null)
                {
                   
                    string illustKey = selected.GetProfessionIllustKey(model.profession);
                    string characterName = selected.GetProfessionMatchName(model.profession);
                   
                    await userCharacterPanel.HandleUpdateConfig(
                        level: model.level,
                        name: model.name,
                        stats: model.stats,
                        illustKey: illustKey,
                        savepoint: model.savepoint,
                        characterProfession: characterName
                    );

                    $"✅ [Character] Updated userCharacterPanel with character: {model.name} (Level: {model.level})".DLog();
                }
            }
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
        }
        public void OnCreateNewCharacter(ProfessionType profession)
        {
            $"ĳ���� ����".DLog();
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
