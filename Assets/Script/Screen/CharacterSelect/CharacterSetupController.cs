using Cysharp.Threading.Tasks;
using Hunt.Game;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class CharacterSetupController : MonoBehaviourSingleton<CharacterSetupController>
    {

        [Header("Character Slots")]
        [SerializeField] private List<CharInfoField> charInfoFields;
        [SerializeField] private CreateCharProfile createCharProfile;

        [Header("UI Panels")]
        [SerializeField] private CreateCharDocuPanel createCharDocuPanel;
        [SerializeField] private UserCharProfilePanel userCharProfilePanel;
        #region Fields
        private string currentWorldName;
        private int currentMyCharCount;
        private List<CharModel> cachedChars = new List<CharModel>();
        private readonly Dictionary<string, List<CharModel>> channelCharacterCache = new Dictionary<string, List<CharModel>>();
        #endregion

        protected override bool DontDestroy => false;

        #region Life
        protected override void Awake()
        {
            base.Awake();            
        }

        private async void OnEnable()
        {
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            userCharProfilePanel.gameObject.SetActive(false);
            ResetSelectionState();
        }

        private void OnDisable()
        {
            ResetSelectionState();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
   
        #endregion

        /// <summary>
        /// 채널 필드를 클릭할 때 업데이트 되는 캐릭터 슬롯 필드들의 정보입니다.
        /// </summary>
        /// <param name="channelName">채널 이름</param>
        /// <param name="characterCount">해당 채널에서 보유한 캐릭터 수</param>
        public void UpdateCharacterSlots(string channelName, int characterCount)
        {
            UpdateWorldContext(channelName, characterCount);

            if (!ValidateCharacterFields()) return;

            var charactersToBind = LoadCharactersFromCache(channelName, characterCount);
            UpdateCachedCharacters(charactersToBind);

            int bindCount = charactersToBind?.Count ?? characterCount;
            BindCharactersToFields(bindCount);
        }

        /// <summary>
        /// 서버에서 받은 채널별 캐릭터 리스트를 캐시에 저장합니다.
        /// UI 바인딩은 채널 클릭 시 UpdateCharacterSlots에서 처리됩니다.
        /// </summary>
        /// <param name="worldName">채널 이름</param>
        /// <param name="models">캐릭터 모델 리스트</param>
        public void OnRecvCharacterList(string worldName, List<CharModel> models)
        {
            if (models == null)
            {
                this.DError("OnRecvCharacterList - characters is null");
                return;
            }

            if (string.IsNullOrEmpty(worldName))
            {
                return;
            }

            channelCharacterCache[worldName] = models;
            this.DLog($"Cached characters - Channel: {worldName}, Count: {models.Count}");
        }

        /// <summary>
        /// 선택한 캐릭터 필드 영역에 대한 처리입니다.
        /// 캐릭터가 이미 존재한다면 하이라이트 효과 처리와 키 값을 통해 필요한 정보를 로드합니다.
        /// 공용으로 표시되는 패널에 선택된 캐릭터의 정보를 업데이트하게 됩니다.
        /// </summary>
        /// <param name="selected">선택된 캐릭터 필드 영역</param>
        public async void OnSelectCharacterField(CharInfoField selected)
        {
            if (selected == null) return;

            DeselectAllFields();
            selected.HightlightField(true);

            if (selected.HasCharacterData)
            {
                await UpdateUserCharacterPanel(selected.CurrentModel);
            }
        }

        #region Create
        /// <summary>
        /// 생성할 캐릭터 정보를 업데이트합니다.
        /// </summary>
        public void OnShowCharInfo(ClassType profession)
        {
            if (createCharProfile == null)
            {
                this.DError($"❌ newCharacterInfoFields가 null입니다");
                return;
            }

            createCharProfile.SetProfession(profession);
            
            // stats가 비어있거나 5개 미만이면 기본값으로 초기화
            if (createCharProfile.stats == null || createCharProfile.stats.Count < 5)
            {
                createCharProfile.stats = CreateDefaultStatInfos();
            }

            UpdateCharInfoField();
            UpdateDocuPanel();

            this.DLog($"[Character] Show character info - Profession: {profession}, Name: {createCharProfile.characterName}");
        }

        /// <summary>
        /// 새로운 캐릭터를 생성합니다.
        /// </summary>
        /// <param name="profession">생성할 캐릭터의 직업</param>
        public void OnCreateNewCharacter(ClassType profession)
        {
            $"[Create-Character] 캐릭터 생성 시작 - 직업: {profession}".DLog();

            CreateCharProfile generationField = FindGenFieldByProfession(profession);
            if (generationField == null)
            {
                this.DError($"❌ {profession}의 생성 필드를 찾을 수 없습니다");
                return;
            }

            int emptySlotIndex = FindEmptyCharSlot();
            if (emptySlotIndex == -1)
            {
                this.DError("❌ 사용 가능한 슬롯이 없습니다");
                return;
            }

            $"[Create-Character] 빈 슬롯 찾음: {emptySlotIndex}".DLog();

            // TODO: 서버 통신으로 교체
            // var response = await NetworkManager.Shared.CreateCharacterAsync(...)

            CharModel newModel = CreateCharacterModel(generationField);
            charInfoFields[emptySlotIndex].Bind(newModel);
            UpdateCharacterCache(newModel);
            OnSelectCharacterField(charInfoFields[emptySlotIndex]);

            this.DLog($"캐릭터 생성 완료: {newModel.name}");
        }


        #endregion

        #region Character Slots
        private void UpdateWorldContext(string worldName, int myCharCount)
        {
            currentWorldName = worldName;
            currentMyCharCount = myCharCount;
            this.DLog($"UpdateCharacterSlots - Channel: {worldName}, Count: {myCharCount}");
        }

        private bool ValidateCharacterFields()
        {
            if (charInfoFields == null || charInfoFields.Count == 0)
            {
                this.DLog("characterInfoField is null or empty");
                return false;
            }
            return true;
        }

        private List<CharModel> LoadCharactersFromCache(string worldName, int expectedCount)
        {
            if (string.IsNullOrEmpty(worldName)) return null;

            if (channelCharacterCache.TryGetValue(worldName, out var cached))
            {
                if (cached.Count != expectedCount)
                {
                    this.DWarnning($"Count mismatch - Server: {expectedCount}, Cache: {cached.Count}");
                }
                else
                {
                    this.DLog($"Loaded from cache: {worldName}, Count: {cached.Count}");
                }
                return cached;
            }

            this.DError($"No cache for '{worldName}'. Using server count: {expectedCount}");
            return null;
        }

        private void UpdateCachedCharacters(List<CharModel> characters)
        {
            cachedChars.Clear();
            if (characters != null)
            {
                cachedChars.AddRange(characters);
            }
        }

        private void BindCharactersToFields(int bindCount)
        {
            for (int i = 0; i < charInfoFields.Count; i++)
            {
                var field = charInfoFields[i];
                if (field == null) continue;

                if (i < bindCount && i < cachedChars.Count && cachedChars[i] != null)
                {
                    field.Bind(cachedChars[i]);
                }
                else if (i < bindCount)
                {
                    if (!field.HasCharacterData)
                    {
                        field.InitField(true);
                    }
                }
                else
                {
                    ResetEmptySlot(field);
                }
            }
        }

        private void ResetEmptySlot(CharInfoField field)
        {
            field.InitField(false);
            field.SetLevelFieldValue(0);
            field.SetNameFieldValue(string.Empty);
            field.SetSavePointFieldValie(string.Empty);
            userCharProfilePanel.gameObject.SetActive(false);
        }
        #endregion

        #region Character Selection
        private void DeselectAllFields()
        {
            if (charInfoFields == null) return;

            foreach (var field in charInfoFields)
            {
                if (field == null) continue;
                field.HightlightField(false);
            }
        }

        private async UniTask UpdateUserCharacterPanel(CharModel model)
        {
            if (model == null || userCharProfilePanel == null) return;

            SaveCharacterToGameSession(model);

            string illustKey = BindKeyConst.GetIllustKeyByProfession(model.classtype);

            await userCharProfilePanel.HandleUpdateConfig(
                level: model.level,
                name: model.name,
                stats: model.stats,
                illustKey: illustKey,
                mapId: model.mapId,
                characterProfession: model.classtype
            );

            this.DLog($"Updated panel: {model.name} (Level: {model.level})");
        }

        private void SaveCharacterToGameSession(CharModel model)
        {
            var simpleChar = GameSession.Shared.CharacterInfos?.Find(c => c.Name == model.name);

            if (simpleChar != null)
            {
                GameSession.Shared.SelectCharacter(simpleChar);
                this.DLog($"Saved to GameSession: {simpleChar.Name} (CharId: {simpleChar.CharId})");
            }
            else
            {
                GameSession.Shared.SelectCharacterModel(model);
                this.DError($"SimpleCharacterInfo not found: {model.name}");
            }
        }
        #endregion

        #region Character Creation
        private int FindEmptyCharSlot()
        {
            if (charInfoFields == null || charInfoFields.Count == 0)
            {
                this.DError($"characterInfoField is null or empty");
                return -1;
            }

            for (int i = 0; i < charInfoFields.Count; i++)
            {
                if (charInfoFields[i] != null && !charInfoFields[i].HasCharacterData)
                {
                    return i;
                }
            }

            return -1;
        }
        private CreateCharProfile FindGenFieldByProfession(ClassType profession)
        {
            if (createCharProfile == null) return null;
            
            if (createCharProfile.ProfessionType == profession)
            {
                return createCharProfile;
            }
            
            return null;
        }

        private CharModel CreateCharacterModel(CreateCharProfile generationField)
        {
            List<Hunt.Game.StatInfo> statInfoList = generationField.stats;
            
            if (statInfoList == null || statInfoList.Count < 5)
            {
                this.DWarnning($"StatInfo가 null이거나 5개 미만입니다. 기본값 사용");
                statInfoList = CreateDefaultStatInfos();
            }

            return new CharModel
            {
                name = generationField.characterName,
                classtype = generationField.ProfessionType,
                level = 1,
                mapId = 1,
                stats = new List<Hunt.Game.StatInfo>(statInfoList),
                charId = 0,
                worldId = 0
            };
        }

        /// <summary>
        /// 기본 StatInfo 리스트를 생성합니다.
        /// </summary>
        private List<StatInfo> CreateDefaultStatInfos()
        {
            return new List<StatInfo>
            {
                new StatInfo { Type = 0, Point = 50 },  // ATT
                new StatInfo { Type = 1, Point = 50 },  // DEF
                new StatInfo { Type = 2, Point = 50 },  // SPD
                new StatInfo { Type = 3, Point = 50 },  // LUK
                new StatInfo { Type = 4, Point = 50 }   // AGI
            };
        }
        #endregion

        /// <summary>
        /// GenerateCharInfoField의 내용을 업데이트합니다.
        /// </summary>
        private async void UpdateCharInfoField()
        {
            if (createCharProfile == null) return;
            
            // characterName UI 업데이트
            createCharProfile.UpdateCharacterNameUI();
            
            await LoadPortraitImage(createCharProfile);
        }

        /// <summary>
        /// GenerateCharDocuPanel의 내용을 업데이트합니다.
        /// </summary>
        private void UpdateDocuPanel()
        {
            if (createCharProfile == null || createCharDocuPanel == null) return;
            
            createCharDocuPanel.OnSetFieldValue(createCharProfile);
            this.DLog($"DocuPanel 업데이트 완료 - Profession: {createCharProfile.ProfessionType}");
        }

        /// <summary>
        /// GenerateCharInfoField에 일러스트를 로드합니다.
        /// </summary>
        private async UniTask LoadPortraitImage(CreateCharProfile genField)
        {
            if (genField == null || AbLoader.Shared == null) return;

            string illustKey = BindKeyConst.GetIllustKeyByProfession(genField.ProfessionType);
            if (string.IsNullOrEmpty(illustKey))
            {
                this.DError($"일러스트 키를 찾을 수 없습니다: {genField.ProfessionType}");
                return;
            }

            try
            {
                var sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(illustKey);
                if (sprite != null && genField.PortraitImage != null)
                {
                    genField.PortraitImage.sprite = sprite;
                    genField.PortraitImage.enabled = true;
                }
            }
            catch (System.Exception ex)
            {
                this.DError($"일러스트 로드 오류: {ex.Message}");
            }
        }

     
        private void UpdateCharacterCache(CharModel newModel)
        {
            if (newModel == null)
            {
                this.DError("newModel이 null");
                return;
            }

            if (string.IsNullOrEmpty(currentWorldName))
            {
                this.DError("채널명이 없음");
                return;
            }

            if (!channelCharacterCache.ContainsKey(currentWorldName))
            {
                channelCharacterCache[currentWorldName] = new List<CharModel>();
            }

            channelCharacterCache[currentWorldName].Add(newModel);
            cachedChars.Add(newModel);

            this.DLog($"캐시 업데이트 완료 - Channel: {currentWorldName}, Total: {channelCharacterCache[currentWorldName].Count}");
        }

        private void ResetSelectionState()
        {
            if (charInfoFields == null) return;

            foreach (var field in charInfoFields)
            {
                if (field == null) continue;
                field.HightlightField(false);
            }
        }
    }
}
