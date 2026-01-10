using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class ButtonClickNode : UINode
    {
        public GameObject targetButton;
        
        public ButtonClickNode()
        {
            nodeName = "Button Click";
        }
        
        public override UINodeType GetNodeType() => UINodeType.ButtonClick;
        
        public override UIGraphExecutionStep CreateExecutionStep()
        {
            var step = new UIGraphExecutionStep
            {
                nodeType = UINodeType.ButtonClick,
                nodeGuid = guid
            };
            
            if (targetButton != null)
            {
                step.gameObjectIds = new[] { GetOrCreateTargetId(targetButton) };
            }
            
            return step;
        }
        
        private string GetOrCreateTargetId(GameObject obj)
        {
            if (obj == null) return string.Empty;
            
#if UNITY_EDITOR
            var target = obj.GetComponent<UIGraphTarget>();
            if (target == null)
            {
                target = obj.AddComponent<UIGraphTarget>();
            }
            
            if (string.IsNullOrEmpty(target.TargetId))
            {
                string newId = System.Guid.NewGuid().ToString();
                target.SetTargetId(newId);
                UnityEditor.EditorUtility.SetDirty(target);
                UnityEditor.EditorUtility.SetDirty(obj);
            }
            
            return target.TargetId;
#else
            var target = obj.GetComponent<UIGraphTarget>();
            return target != null ? target.TargetId : string.Empty;
#endif
        }
    }
}

