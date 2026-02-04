using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Hunt.Tool;

namespace Hunt
{
    public class WeaponSpriteController : MonoBehaviour
    {
        [SerializeField] private string weaponPrefabKey;
        [SerializeField] private List<SpriteHandPositionData> handPositionDataList = new List<SpriteHandPositionData>();

        private Animator animator;
        private SpriteRenderer characterRenderer;
        private Dictionary<string, List<HandPositionData>> animationHandData = new Dictionary<string, List<HandPositionData>>();
        private GameObject currentWeaponInstance;
        private string currentAnimationKey;
        
        private void Awake()
        {
            animator = GetComponentInParent<Animator>();
            characterRenderer = GetComponentInParent<SpriteRenderer>();
        }
        
        private async void Start()
        {
            if (handPositionDataList != null && handPositionDataList.Count > 0)
            {
                BuildAnimationData();
            }
            
            if (!string.IsNullOrEmpty(weaponPrefabKey))
            {
                await LoadWeaponPrefab();
            }
        }
        
        private async UniTask LoadWeaponPrefab()
        {
            if (AbLoader.Shared == null)
            {
                $"[WeaponSpriteController] AbLoader가 없습니다".DError();
                return;
            }
            
            var weaponPrefab = await AbLoader.Shared.LoadAssetAsync<GameObject>(weaponPrefabKey);
            if (weaponPrefab == null)
            {
                $"[WeaponSpriteController] 무기 Prefab 로드 실패: {weaponPrefabKey}".DError();
                return;
            }
            
            if (currentWeaponInstance != null)
            {
                Destroy(currentWeaponInstance);
            }
            
            currentWeaponInstance = Instantiate(weaponPrefab, transform);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            if (currentWeaponInstance.GetComponent<SwordTrailEffect>() == null)
                currentWeaponInstance.AddComponent<SwordTrailEffect>();

            SpriteRenderer[] weaponRenderers = currentWeaponInstance.GetComponentsInChildren<SpriteRenderer>();
            if (characterRenderer != null)
            {
                foreach (SpriteRenderer weaponRenderer in weaponRenderers)
                {
                    if (weaponRenderer != null)
                    {
                        weaponRenderer.sortingOrder = characterRenderer.sortingOrder + 1;
                    }
                }
            }
        }
        
        private void LateUpdate()
        {
            UpdateWeaponPosition();
        }
        
        private void BuildAnimationData()
        {
            animationHandData.Clear();
            
            foreach (var dataAsset in handPositionDataList)
            {
                if (dataAsset == null) continue;
                
                if (dataAsset.animationDataList != null && dataAsset.animationDataList.Count > 0)
                {
                    foreach (var animData in dataAsset.animationDataList)
                    {
                        if (string.IsNullOrEmpty(animData.animationName)) continue;
                        
                        if (!animationHandData.ContainsKey(animData.animationName))
                        {
                            animationHandData[animData.animationName] = new List<HandPositionData>();
                        }
                        
                        foreach (var frameData in animData.framePositions)
                        {
                            animationHandData[animData.animationName].Add(frameData);
                        }
                    }
                }
                else if (dataAsset.handPositions != null && dataAsset.handPositions.Count > 0)
                {
                    foreach (var handData in dataAsset.handPositions)
                    {
                        string animationName = ExtractAnimationName(handData.spriteName);
                        
                        if (!animationHandData.ContainsKey(animationName))
                        {
                            animationHandData[animationName] = new List<HandPositionData>();
                        }
                        
                        animationHandData[animationName].Add(handData);
                    }
                }
            }
            
            foreach (var key in animationHandData.Keys.ToList())
            {
                animationHandData[key] = animationHandData[key]
                    .OrderBy(x => ExtractFrameIndex(x.spriteName))
                    .ToList();
            }
            
            $"[WeaponSpriteController] 애니메이션별 손 위치 데이터 로드 완료: {animationHandData.Count}개 애니메이션".DLog();
            foreach (var kvp in animationHandData)
            {
                $"[WeaponSpriteController] - {kvp.Key}: {kvp.Value.Count}프레임".DLog();
            }
        }
        
        private string ExtractAnimationName(string spriteName)
        {
            string nameToCheck = spriteName;
            int atIndex = spriteName.IndexOf('@');
            if (atIndex >= 0)
            {
                nameToCheck = spriteName.Substring(0, atIndex);
            }
            
            int lastUnderscore = nameToCheck.LastIndexOf('_');
            if (lastUnderscore >= 0)
            {
                return nameToCheck.Substring(0, lastUnderscore);
            }
            return nameToCheck;
        }
        
        private int ExtractFrameIndex(string spriteName)
        {
            int atIndex = spriteName.IndexOf('@');
            string nameToCheck = spriteName;
            
            if (atIndex >= 0)
            {
                nameToCheck = spriteName.Substring(0, atIndex);
            }
            
            int lastUnderscore = nameToCheck.LastIndexOf('_');
            if (lastUnderscore >= 0 && int.TryParse(nameToCheck.Substring(lastUnderscore + 1), out int frameIndex))
            {
                return frameIndex;
            }
            return 0;
        }
        
        private void UpdateWeaponPosition()
        {
            if (animator == null || characterRenderer == null || currentWeaponInstance == null) return;
            if (handPositionDataList == null || handPositionDataList.Count == 0 || animationHandData.Count == 0) return;
            if (characterRenderer.sprite == null) return;
            
            Sprite currentSprite = characterRenderer.sprite;
            string currentSpriteName = currentSprite.name;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            AnimationClip currentClip = GetCurrentAnimationClip();
            
            if (currentClip == null) return;
            
            string clipName = currentClip.name;
            string animationKey = FindMatchingAnimationKey(clipName, currentSpriteName);
            
            if (string.IsNullOrEmpty(animationKey))
            {
                if (currentWeaponInstance != null)
                {
                    currentWeaponInstance.SetActive(false);
                }
                return;
            }
            
            if (!animationHandData.ContainsKey(animationKey))
            {
                if (currentWeaponInstance != null)
                {
                    currentWeaponInstance.SetActive(false);
                }
                return;
            }
            
            if (currentAnimationKey != animationKey)
            {
                currentAnimationKey = animationKey;
            }
            
            if (currentWeaponInstance != null)
            {
                currentWeaponInstance.SetActive(true);
            }
            
            var frameData = animationHandData[animationKey];
            if (frameData.Count == 0) return;
            
            int frameIndex = FindFrameIndexBySpriteName(currentSpriteName, frameData);
            
            if (frameIndex < 0)
            {
                float normalizedTime = stateInfo.normalizedTime % 1f;
                frameIndex = Mathf.FloorToInt(normalizedTime * frameData.Count);
            }
            
            frameIndex = Mathf.Clamp(frameIndex, 0, frameData.Count - 1);
            
            HandPositionData currentFrameData = frameData[frameIndex];
            UpdateWeaponTransform(currentFrameData);
        }
        
        private int FindFrameIndexBySpriteName(string spriteName, List<HandPositionData> frameData)
        {
            for (int i = 0; i < frameData.Count; i++)
            {
                if (frameData[i].spriteName.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }
        
        private AnimationClip GetCurrentAnimationClip()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return null;
            
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                return clipInfo[0].clip;
            }
            return null;
        }
        
        private string FindMatchingAnimationKey(string clipName, string currentSpriteName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            
            if (animationHandData.ContainsKey(clipName))
            {
                return clipName;
            }
            
            string spriteAnimationName = ExtractAnimationName(currentSpriteName);
            if (!string.IsNullOrEmpty(spriteAnimationName) && animationHandData.ContainsKey(spriteAnimationName))
            {
                return spriteAnimationName;
            }
            
            string baseName = ExtractBaseAnimationName(clipName);
            
            if (string.IsNullOrEmpty(baseName)) return null;
            
            foreach (var key in animationHandData.Keys)
            {
                string keyBaseName = ExtractBaseAnimationName(key);
                if (baseName.Equals(keyBaseName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }
            
            return null;
        }
        
        private string ExtractBaseAnimationName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            int atIndex = name.IndexOf('@');
            if (atIndex >= 0)
            {
                return name.Substring(0, atIndex);
            }
            
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore >= 0)
            {
                string beforeUnderscore = name.Substring(0, lastUnderscore);
                if (int.TryParse(name.Substring(lastUnderscore + 1), out _))
                {
                    return ExtractBaseAnimationName(beforeUnderscore);
                }
            }
            
            return name;
        }
        
        private void UpdateWeaponTransform(HandPositionData handData)
        {
            if (characterRenderer == null || characterRenderer.sprite == null || currentWeaponInstance == null) return;
            
            Sprite currentSprite = characterRenderer.sprite;
            Rect spriteRect = currentSprite.rect;
            Vector2 pivot = currentSprite.pivot;
            float pixelsPerUnit = currentSprite.pixelsPerUnit;
            
            Vector2 normalizedPos = handData.normalizedPosition;
            
            float spriteWidth = spriteRect.width / pixelsPerUnit;
            float spriteHeight = spriteRect.height / pixelsPerUnit;
            
            Vector2 pivotOffset = new Vector2(
                (pivot.x - spriteRect.width * 0.5f) / pixelsPerUnit,
                (pivot.y - spriteRect.height * 0.5f) / pixelsPerUnit
            );
            
            Vector2 handLocalPos = new Vector2(
                (normalizedPos.x - 0.5f) * spriteWidth,
                (normalizedPos.y - 0.5f) * spriteHeight
            );
            
            Vector2 localPosition = handLocalPos - pivotOffset;
            
            bool flipX = characterRenderer.flipX;
            if (flipX)
            {
                localPosition.x = -localPosition.x;
            }
            
            currentWeaponInstance.transform.localPosition = localPosition;
            
            // 회전: 추출기(에디터)에서 설정한 값에 -90도 오프셋을 주어
            // 실제 프리팹 자식 Transform 의 local Z 회전값을 맞춘다.
            // (무기 스프라이트 기본 방향이 위를 향해 있고, Unity 0도가 오른쪽 기준인 경우)
            float rotation = (handData.rotation - 90f + 360f) % 360f;
            
            SpriteRenderer[] weaponRenderers = currentWeaponInstance.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer weaponRenderer in weaponRenderers)
            {
                if (weaponRenderer != null)
                {
                    weaponRenderer.flipX = flipX;
                    // ScriptableObject 에 저장된 값 그대로 사용 (절대 sortingOrder)
                    weaponRenderer.sortingOrder = handData.sortingOrder;
                }
            }
            
            Vector3 eulerAngles = currentWeaponInstance.transform.localEulerAngles;
            eulerAngles.z = rotation;
            currentWeaponInstance.transform.localEulerAngles = eulerAngles;
        }
        
        public async UniTask SetWeaponPrefab(string prefabKey)
        {
            weaponPrefabKey = prefabKey;
            await LoadWeaponPrefab();
        }
        
        public void SetHandPositionData(SpriteHandPositionData data)
        {
            if (data != null && !handPositionDataList.Contains(data))
            {
                handPositionDataList.Add(data);
                BuildAnimationData();
            }
        }
        
        public void SetHandPositionDataList(List<SpriteHandPositionData> dataList)
        {
            handPositionDataList = dataList ?? new List<SpriteHandPositionData>();
            BuildAnimationData();
        }

        /// <summary>런타임에 붙은 무기 기준 검날 시작(그립) 월드 좌표.</summary>
        public bool TryGetBladeStartWorld(out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            var weapon = currentWeaponInstance != null ? currentWeaponInstance.transform : (transform.childCount > 0 ? transform.GetChild(0) : null);
            if (weapon == null) return false;
            var sr = weapon.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return false;
            float extentY = sr.sprite.bounds.extents.y;
            worldPos = weapon.TransformPoint(0f, -extentY, 0f);
            return true;
        }

        /// <summary>런타임에 붙은 무기 기준 검날 끝(선단) 월드 좌표.</summary>
        public bool TryGetBladeEndWorld(out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            var weapon = currentWeaponInstance != null ? currentWeaponInstance.transform : (transform.childCount > 0 ? transform.GetChild(0) : null);
            if (weapon == null) return false;
            var sr = weapon.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return false;
            float extentY = sr.sprite.bounds.extents.y;
            worldPos = weapon.TransformPoint(0f, extentY, 0f);
            return true;
        }

        /// <summary>애니메이션 이벤트용. 무기에 붙은 SwordTrailEffect로 전달.</summary>
        public void OnTrailBegin()
        {
            if (currentWeaponInstance != null)
                currentWeaponInstance.GetComponent<SwordTrailEffect>()?.OnTrailBegin();
        }

        /// <summary>애니메이션 이벤트용. 무기에 붙은 SwordTrailEffect로 전달.</summary>
        public void OnTrailEnd()
        {
            if (currentWeaponInstance != null)
                currentWeaponInstance.GetComponent<SwordTrailEffect>()?.OnTrailEnd();
        }

        private void OnDestroy()
        {
            if (currentWeaponInstance != null)
            {
                Destroy(currentWeaponInstance);
            }
            
            if (!string.IsNullOrEmpty(weaponPrefabKey) && AbLoader.Shared != null)
            {
                AbLoader.Shared.ReleaseAsset(weaponPrefabKey);
            }
        }
    }
}
