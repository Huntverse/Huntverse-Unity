using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class ToggleGameObjectNode : UINode
    {
        public GameObject[] targetGameObjects;
        
        public ToggleGameObjectNode()
        {
            nodeName = "Toggle GameObject";
        }
        
        public override UINodeType GetNodeType() => UINodeType.ToggleGameObject;
        
        public override UIGraphExecutionStep CreateExecutionStep()
        {
            var step = new UIGraphExecutionStep
            {
                nodeType = UINodeType.ToggleGameObject,
                nodeGuid = guid
            };
            
            if (targetGameObjects != null && targetGameObjects.Length > 0)
            {
                step.gameObjectIds = new string[targetGameObjects.Length];
                for (int i = 0; i < targetGameObjects.Length; i++)
                {
                    if (targetGameObjects[i] != null)
                    {
                        step.gameObjectIds[i] = GetOrCreateTargetId(targetGameObjects[i]);
                    }
                }
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

