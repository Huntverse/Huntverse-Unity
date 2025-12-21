using UnityEditor;
using UnityEngine;

namespace Hunt
{
    [CustomEditor(typeof(VfxObject))]
    public class VfxObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            VfxObject vfxObject = (VfxObject)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("EndFrame ReturnPool"))
            {
                RegisterAnimationEvents(vfxObject);
            }
        }

        private void RegisterAnimationEvents(VfxObject vfxObject)
        {
            var animator = vfxObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = vfxObject.GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                $"Animator가 없습니다!".DError();
                return;
            }

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                $"AnimatorController가 없습니다!".DError();
                return;
            }

            var returnClipName = vfxObject.returnOnClipName;

            foreach (var clip in controller.animationClips)
            {
                if (!string.IsNullOrEmpty(returnClipName) && clip.name != returnClipName)
                {
                    continue;
                }

                var events = AnimationUtility.GetAnimationEvents(clip);
                var newEvents = new System.Collections.Generic.List<AnimationEvent>();

                foreach (var evt in events)
                {
                    if (evt.functionName != "OnAnimationEnd")
                    {
                        newEvents.Add(evt);
                    }
                }

                var endEvent = new AnimationEvent
                {
                    time = clip.length - 0.01f,
                    functionName = "OnAnimationEnd",
                    stringParameter = clip.name
                };
                newEvents.Add(endEvent);

                AnimationUtility.SetAnimationEvents(clip, newEvents.ToArray());
                $"✅ {clip.name} 애니메이션에 ReturnToPool 이벤트 등록 완료".DLog();
            }

            EditorUtility.SetDirty(controller);
        }
    }
}
