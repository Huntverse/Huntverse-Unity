using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Hunt
{

    public class UserCharacter : MonoBehaviour
    {
        private UserCharLoco characterAction;
        private UserDisplay charDisplay;
        private GameObject model;

        private bool isSetupComplete = false;
        public bool IsSetupComplete => isSetupComplete;
        
        public ulong CharId { get; private set; }
        private void Start()
        {
            characterAction = GetComponent<UserCharLoco>();
            charDisplay = GetComponent<UserDisplay>();
            if (characterAction != null)
            {
                characterAction.enabled = false;
            }

            var myChar = GameSession.Shared?.SelectedCharacter;
            if (myChar != null)
            {
                SetUserId(myChar.CharId);
                charDisplay.SetCharName(myChar.Name);
            }
            string modelKey;
            Vector3 spawnpos = Vector3.zero;

            if (myChar != null)
            {
                var classType = BindKeyConst.GetClassTypeByJobId(myChar.ClassType);
                modelKey = BindKeyConst.GetModelKeyByProfession(classType);
                $"[UserCharacter] 캐릭터 스폰 {myChar}: {myChar.Name} (Lv.{myChar.Level}, ClassType:{myChar.ClassType}".DLog();

            }
            else if (GameSession.Shared.SelectedCharacterModel != null)
            {
                var model = GameSession.Shared.SelectedCharacterModel;
                var classType = model.classtype;
                modelKey = BindKeyConst.GetModelKeyByProfession(classType);
                $"[UserCharacter] 캐릭터 스폰 (CharacterModel/Dev): {model.name}".DLog();
            }
            else
            {
                var classType = ClassType.Archer;
                modelKey = BindKeyConst.GetModelKeyByProfession(classType);
                $"[UserCharacter] ⚠ 선택된 캐릭터 없음".DError();

            }

            SetUp(modelKey,spawnpos).Forget();
            if (IsLocalPlayer())
            {
                GameSession.Shared?.NotifyLocalPlayerSpawned(this);
            }
        }

        public void SetUserId(ulong charId)
        {
            CharId = charId;
        }
        
        private void InitializeWeaponController()
        {
            if (model == null) return;
            
            WeaponSpriteController weaponController = model.GetComponent<WeaponSpriteController>();
            if (weaponController == null)
                weaponController = model.AddComponent<WeaponSpriteController>();
        }
        public bool IsLocalPlayer()
        {

            //var myChar = GameSession.Shared?.SelectedCharacter;
            //return myChar != null /*&& CharId == myChar.CharId*/;

            return true;
        }
        private async UniTask SetUp(string modelKey, Vector3 spawnPos)
        {
            try
            {
                if(string.IsNullOrEmpty(modelKey))
                {
                    $"ModelKey is empty".DError();
                    return;
                }

                if(AbLoader.Shared == null)
                {
                    $"Abloader not set".DError();
                    return;
                }

                var go = await AbLoader.Shared.LoadAssetAsync<GameObject>(modelKey);
                if (go == null)
                {
                    $"Abloader Error : {modelKey}".DError();
                    return;
                }

                model = Instantiate<GameObject>(go);
                model.transform.SetParent(transform);
                model.transform.position = Vector3.zero;
                model.transform.rotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                model.transform.position = spawnPos;

                if (characterAction != null)
                {
                    characterAction.enabled = true;
                    characterAction.Initialize(model);
                }

                InitializeWeaponController();

                // VFX/SFX Controller 추가
                var fxController = model.GetComponent<ActorFxController>();
                if (fxController == null) fxController = model.AddComponent<ActorFxController>();
                
                // 프리셋 로드 (Addressable Key 규칙: "Preset_모델키" 가정)
                // 규칙은 프로젝트 상황에 맞춰 변경 필요. 
                // 예: astera@model -> Preset_astera@model or Preset_Sword
                // 여기서는 모델키 앞에 "Preset_" 접두어 사용
                string presetKey = $"Preset_{modelKey}"; 
                $"[UserCharacter] Trying to load preset with key: {presetKey}".DLog();

                var preset = await AbLoader.Shared.LoadAssetAsync<CharacterFxPreset>(presetKey);
                
                if (preset != null)
                {
                    fxController.Initialize(preset);
                    $"[UserCharacter] Preset loaded and controller initialized: {presetKey}".DLog();
                }
                else
                {
                    // 실패 시 클래스 타입으로 시도해보기 (예비책)
                    // var fallbackKey = $"Preset_{myChar.ClassType}"; ...
                    $"[UserCharacter] FxPreset Load Fail: {presetKey}".DError(); 
                }

                isSetupComplete = true;
            }
            catch(Exception e) 
            {
                $"User Character Setup Fail! {e.Message}".DError();
            }
        }

    }

}