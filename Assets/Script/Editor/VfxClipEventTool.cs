using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hunt.EditorTools
{
    /// <summary>
    /// 공격/스킬 애니메이션 클립에 VfxSpawn 이벤트를 일괄 추가/관리하는 에디터 툴.
    /// - AnimationClip 선택 후 메뉴에서 실행
    /// - 정규화 시간(0~1) + VFX 키를 입력하면, 모든 선택된 클립에 동일 이벤트 추가
    /// </summary>
    public class VfxClipEventTool : EditorWindow
    {
        private float _normalizedTime = 0.25f;
        private string _vfxKey = "astera_planhit@vfx";
        private string _functionName = "VfxSpawn";

        [MenuItem("Tools/VFX/VFX Clip Event Tool")]
        public static void OpenWindow()
        {
            var window = GetWindow<VfxClipEventTool>("VFX Clip Events");
            window.minSize = new Vector2(320, 140);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("선택된 AnimationClip들에 VfxSpawn 이벤트 일괄 추가", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _normalizedTime = EditorGUILayout.Slider("정규화 시간 (0~1)", _normalizedTime, 0f, 1f);
            _vfxKey = EditorGUILayout.TextField("VFX Key", _vfxKey);
            _functionName = EditorGUILayout.TextField("이벤트 함수명", _functionName);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_vfxKey)))
            {
                if (GUILayout.Button("선택된 AnimationClip에 이벤트 추가"))
                {
                    AddEventsToSelectedClips();
                }
            }

            if (GUILayout.Button("선택된 AnimationClip에서 모든 VFX 이벤트 제거"))
            {
                RemoveEventsFromSelectedClips();
            }
        }

        private void AddEventsToSelectedClips()
        {
            var clips = GetSelectedClips();
            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("VFX Clip Events", "선택된 AnimationClip이 없습니다.", "확인");
                return;
            }

            foreach (var clip in clips)
            {
                var events = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(clip));
                float time = Mathf.Clamp01(_normalizedTime) * clip.length;

                var ev = new AnimationEvent
                {
                    time = time,
                    functionName = _functionName,
                    stringParameter = _vfxKey
                };

                events.Add(ev);
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                EditorUtility.SetDirty(clip);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[VfxClipEventTool] {clips.Count}개 클립에 VFX 이벤트를 추가했습니다.");
        }

        private void RemoveEventsFromSelectedClips()
        {
            var clips = GetSelectedClips();
            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("VFX Clip Events", "선택된 AnimationClip이 없습니다.", "확인");
                return;
            }

            foreach (var clip in clips)
            {
                var events = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(clip));
                events.RemoveAll(e => e.functionName == _functionName || e.functionName == "OnVfxSpawn");
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                EditorUtility.SetDirty(clip);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[VfxClipEventTool] {clips.Count}개 클립에서 VFX 이벤트를 제거했습니다.");
        }

        private List<AnimationClip> GetSelectedClips()
        {
            var result = new List<AnimationClip>();
            foreach (var obj in Selection.objects)
            {
                if (obj is AnimationClip clip)
                {
                    result.Add(clip);
                }
            }
            return result;
        }
    }
}

