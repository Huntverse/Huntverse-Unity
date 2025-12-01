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

        #region Field
        private string currentChannelName;
        private int currentCharacterCount;
        private List<CharacterModel> cachedCharacters = new List<CharacterModel>();
        private readonly Dictionary<string, List<CharacterModel>> channelCharacterCache = new Dictionary<string, List<CharacterModel>>();
        private int currentGenerationCharacterIndex = 0;
        #endregion
        protected override bool DontDestroy => false;

        protected override void Awake()
        {
            base.Awake();
            nextButton.onClick.AddListener(() => OnShowChracterInfo(1));
            prevButton.onClick.AddListener(() => OnShowChracterInfo(-1));
            userCharacterPanel.gameObject.SetActive(false);
        }
        private async void OnEnable()
        {
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            ResetSelectionState();
        }
        /// <summary>
        /// 채널 필드를 클릭할 때 업데이트 되는 캐릭터 슬롯 필드들의 정보입니다.
        /// </summary>
        /// <param name="channelName">채널 이름</param>
        /// <param name="characterCount">해당 채널에서 보유한 캐릭터 수</param>
        public void UpdateCharacterSlots(string channelName, int characterCount)
        {
            currentChannelName = channelName;
            currentCharacterCount = characterCount;

            $"[Character] UpdateCharacterSlots - Channel: {channelName}, Count: {characterCount}".DLog();

            if (characterInfoFields == null || characterInfoFields.Count == 0)
            {
                "[Character] characterInfoField is null or empty".DLog();
                return;
            }

            List<CharacterModel> charactersToBind = null;
            int actualCharacterCount = characterCount;
            
            if (!string.IsNullOrEmpty(channelName) && channelCharacterCache.ContainsKey(channelName))
            {
                charactersToBind = channelCharacterCache[channelName];
                actualCharacterCount = charactersToBind.Count;
                
                // 캐시의 실제 개수와 서버에서 받은 개수가 다르면 경고
                if (actualCharacterCount != characterCount)
                {
                    $"⚠️ [Character] Character count mismatch for channel '{channelName}': Server={characterCount}, Cache={actualCharacterCount}. Using cache count.".DLog();
                }
                else
                {
                    $"✅ [Character] Loaded characters from cache for channel: {channelName}, Count: {actualCharacterCount}".DLog();
                }
            }
            else if (!string.IsNullOrEmpty(channelName))
            {
                $"⚠️ [Character] No cached characters found for channel '{channelName}'. Using server count: {characterCount}".DLog();
            }

            // 캐시에서 로드한 데이터가 있으면 cachedCharacters 업데이트
            if (charactersToBind != null)
            {
                cachedCharacters.Clear();
                cachedCharacters.AddRange(charactersToBind);
            }
            else
            {
                cachedCharacters.Clear();
            }

            // 실제 바인딩할 때는 캐시의 실제 개수 또는 서버 개수 중 작은 값 사용
            int bindCount = charactersToBind != null ? actualCharacterCount : characterCount;

            // 필드를 bindCount에 맞게 업데이트를 합니다.
            // 캐릭터 데이터가 있으면 바인드된 Field를 활성화 하고
            // 데이터가 없는 필드라면 캐릭터 선택 필드로 초기화합니다.
            for (int i = 0; i < characterInfoFields.Count; i++)
            {
                if (characterInfoFields[i] == null) continue;

                bool hasCharacter = i < bindCount;

                if (hasCharacter && i < cachedCharacters.Count && cachedCharacters[i] != null)
                {
                    characterInfoFields[i].Bind(cachedCharacters[i]);
                }
                else if (hasCharacter)
                {

                    if (characterInfoFields[i].HasCharacterData)
                    {
                        $"[Character] Field {i} already has character data, keeping existing".DLog();
                    }
                    else
                    {
                        characterInfoFields[i].InitField(true);
                    }
                }
                else
                {
                    characterInfoFields[i].InitField(false);
                    characterInfoFields[i].SetLevelFieldValue(0);
                    characterInfoFields[i].SetNameFieldValue(string.Empty);
                    characterInfoFields[i].SetSavePointFieldValie(string.Empty);
                    userCharacterPanel.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 서버에서 받은 채널별 캐릭터 리스트를 캐시에 저장합니다.
        /// UI 바인딩은 채널 클릭 시 UpdateCharacterSlots에서 처리됩니다.
        /// </summary>
        /// <param name="channelName">채널 이름</param>
        /// <param name="res">캐릭터 필드 리스트</param>
        public void OnRecvCharacterFieldViewUpdate(string channelName, CharacterFieldListRequst res)
        {
            if (res?.chfields == null)
            {
                "[Character] OnRecvCharacterFieldViewUpdate - res is null".DLog();
                return;
            }

            if (string.IsNullOrEmpty(channelName))
            {
                "[Character] OnRecvCharacterFieldViewUpdate - Invalid channel name".DLog();
                return;
            }

            $"[Character] OnRecvCharacterFieldViewUpdate - Channel: {channelName}, Count: {res.chfields.Count}".DLog();

            
            var models = new List<CharacterModel>();
            for (int i = 0; i < res.chfields.Count && i < characterInfoFields.Count; i++)
            {
                if (characterInfoFields[i] == null) continue;

                var model = CharacterModel.FromPayload(res.chfields[i]);
                models.Add(model);
            }

            channelCharacterCache[channelName] = models;
            $"[Character] Cached characters for channel: {channelName}, Count: {models.Count}".DLog();
        }

        /// <summary>
        /// 선택한 캐릭터 필드 영역에 대한 처리입니다.
        /// 캐릭터가이미 존재한다면 하이라이트 효과처리와 키 값을 통해 필요한 정보를 로드합니다.
        /// 공용으로 표시되는 패널에 선택된 캐릭터의 정보를 업데이트하게됩니다.
        /// </summary>
        /// <param name="selected">선택된 캐릭터 필드 영역</param>
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
                   
                    string illustKey = BindKeyConst.GetProfessionIllustKey(model.profession);
                    string characterName = BindKeyConst.GetProfessionMatchName(model.profession);
                   
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
        
        /// <summary>
        /// 생성하게 될 새로운 캐릭터의 정보를 업데이트합니다.
        /// prev 또는 next 버튼을 통해서 업데이트 하게됩니다.
        /// </summary>
        /// <param name="index">이동할 인덱스 오프셋 (-1: 이전, 1: 다음)</param>
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

            // 필드 활성화/비활성화 처리
            for (int i = 0; i < newCharacterInfoFields.Count; i++)
            {
                var field = newCharacterInfoFields[i];
                if (field == null) continue;

                bool active = (i == currentGenerationCharacterIndex);
                if (field.gameObject.activeSelf != active)
                {
                    field.gameObject.SetActive(active);
                }
            }

            // 현재 선택된 필드의 실제 데이터로 패널 업데이트
            var currentField = newCharacterInfoFields[currentGenerationCharacterIndex];
            if (currentField != null && generationcharacterPanel != null)
            {
                // 필드의 실제 스탯 데이터 사용 (5개: ATT, DEF, SDP, LUK, AGI)
                float[] stats = currentField.stats != null && currentField.stats.Count >= 5
                    ? currentField.stats.ToArray()
                    : new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f }; // 기본값

                generationcharacterPanel.OnSetFieldValue(currentField.storyString, stats);
                
                $"[Character] Updated generation panel - Index: {currentGenerationCharacterIndex}, Name: {currentField.characterName}, Profession: {currentField.professionType}".DLog();
            }

            if (prevButton != null)
            {
                prevButton.interactable = currentGenerationCharacterIndex > 0;
            }

            if (nextButton != null)
            {
                nextButton.interactable = currentGenerationCharacterIndex < newCharacterInfoFields.Count - 1;
            }
        }
        public void OnCreateNewCharacter(ProfessionType profession)
        {
            $"캐릭터 생성".DLog();
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


        private void OnDisable()
        {
            ResetSelectionState();
        }
        protected override void OnDestroy()
        {
            nextButton.onClick.RemoveAllListeners();
            prevButton.onClick.RemoveAllListeners();
            base.OnDestroy();
        }
    }
}
