using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace Hunt
{
    [CreateAssetMenu(fileName = "NewUIGraph", menuName = "Hunt/UI Node Graph")]
    public class UINodeGraph : ScriptableObject
    {
        [SerializeReference] public List<UINode> nodes = new List<UINode>();
        public List<UINodeConnection> connections = new List<UINodeConnection>();
        [SerializeField, HideInInspector] private UIGraphRuntimeData bakedData;
        
        public UIGraphRuntimeData GetBakedData() => bakedData;
        public List<UINodeConnection> GetConnections() => connections;
        
        public void Bake()
        {
#if UNITY_EDITOR
            Debug.Log($"========== [UINodeGraph] Bake 시작 - Graph: {name} ==========");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            
            CleanupOldBakedEvents();
            
            bakedData = new UIGraphRuntimeData();
            BakeNodes();
            BakeButtonEvents();
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"========== [UINodeGraph] Bake 완료 - Graph: {name} ==========");
#endif
        }
        
        private void CleanupOldBakedEvents()
        {
#if UNITY_EDITOR
            Debug.Log($"[Cleanup] 시작 - nodes 개수: {nodes?.Count ?? 0}");
            var processedButtons = new HashSet<GameObject>();
            var processedKeyboardObjects = new HashSet<GameObject>();
            
            foreach (var node in nodes)
            {
                if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                {
                    if (!processedButtons.Add(btnNode.targetButton))
                    {
                        Debug.Log($"[Cleanup] Button 스킵 (이미 처리됨): {btnNode.targetButton.name}");
                        continue;
                    }
                    
                    var allComponents = btnNode.targetButton.GetComponents<UIGraphBakedEvent>();
                    Debug.Log($"[Cleanup] Button: {btnNode.targetButton.name}, 기존 컴포넌트 개수: {allComponents.Length}");
                    
                    foreach (var comp in allComponents)
                    {
                        if (comp != null)
                        {
                            Debug.Log($"[Cleanup]   - 컴포넌트 제거 (Graph: {comp.graph?.name ?? "null"})");
                            UnityEngine.Object.DestroyImmediate(comp);
                        }
                    }
                    
                    var afterCleanup = btnNode.targetButton.GetComponents<UIGraphBakedEvent>();
                    Debug.Log($"[Cleanup] Button: {btnNode.targetButton.name}, 제거 후 남은 개수: {afterCleanup.Length}");
                }
                else if (node is KeyboardInputNode keyNode && keyNode.targetGameObject != null)
                {
                    if (!processedKeyboardObjects.Add(keyNode.targetGameObject))
                    {
                        Debug.Log($"[Cleanup] KeyboardObject 스킵 (이미 처리됨): {keyNode.targetGameObject.name}");
                        continue;
                    }
                    
                    var allComponents = keyNode.targetGameObject.GetComponents<UIGraphBakedKeyboardEvent>();
                    Debug.Log($"[Cleanup] KeyboardObject: {keyNode.targetGameObject.name}, 기존 컴포넌트 개수: {allComponents.Length}");
                    
                    foreach (var comp in allComponents)
                    {
                        if (comp != null)
                        {
                            Debug.Log($"[Cleanup]   - 컴포넌트 제거 (Graph: {comp.graph?.name ?? "null"})");
                            UnityEngine.Object.DestroyImmediate(comp);
                        }
                    }
                    
                    var afterCleanup = keyNode.targetGameObject.GetComponents<UIGraphBakedKeyboardEvent>();
                    Debug.Log($"[Cleanup] KeyboardObject: {keyNode.targetGameObject.name}, 제거 후 남은 개수: {afterCleanup.Length}");
                }
            }
            Debug.Log($"[Cleanup] 완료");
#endif
        }
        
        private void BakeNodes()
        {
            if (nodes == null || nodes.Count == 0) return;
            
            var executionOrder = CalculateExecutionOrder();
            bakedData.executionSteps.Clear();
            
            foreach (var nodeGuid in executionOrder)
            {
                var node = nodes.Find(n => n.guid == nodeGuid);
                if (node != null)
                {
                    PreserveNodeReferences(node);
                    var step = node.CreateExecutionStep(this);
                    if (step != null) bakedData.executionSteps.Add(step);
#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }
        }
        
        private void BakeButtonEvents()
        {
#if UNITY_EDITOR
            Debug.Log($"[Bake] 시작 - nodes 개수: {nodes?.Count ?? 0}");
            var processedButtons = new HashSet<GameObject>();
            var processedKeyboardObjects = new HashSet<GameObject>();
            
            foreach (var node in nodes)
            {
                if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                {
                    if (!processedButtons.Add(btnNode.targetButton))
                    {
                        Debug.Log($"[Bake] Button 스킵 (이미 처리됨): {btnNode.targetButton.name}");
                        continue;
                    }
                    
                    var button = btnNode.targetButton.GetComponent<UnityEngine.UI.Button>();
                    if (button == null)
                    {
                        Debug.LogWarning($"[Bake] Button 컴포넌트 없음: {btnNode.targetButton.name}");
                        continue;
                    }
                    
                    var beforeAdd = btnNode.targetButton.GetComponents<UIGraphBakedEvent>();
                    Debug.Log($"[Bake] Button: {btnNode.targetButton.name}, 추가 전 컴포넌트 개수: {beforeAdd.Length}");
                    
                    var bakedEvent = btnNode.targetButton.AddComponent<UIGraphBakedEvent>();
                    bakedEvent.SetGraph(this);
                    bakedEvent.SetStartNodeGuid(btnNode.guid);
                    
                    var afterAdd = btnNode.targetButton.GetComponents<UIGraphBakedEvent>();
                    Debug.Log($"[Bake] Button: {btnNode.targetButton.name}, 추가 후 컴포넌트 개수: {afterAdd.Length}, GUID: {btnNode.guid}");
                    
                    EditorUtility.SetDirty(bakedEvent);
                    EditorUtility.SetDirty(button);
                    EditorUtility.SetDirty(btnNode.targetButton);
                }
                else if (node is KeyboardInputNode keyNode && keyNode.targetGameObject != null && keyNode.targetKeyCode != KeyCode.None)
                {
                    if (!processedKeyboardObjects.Add(keyNode.targetGameObject))
                    {
                        Debug.Log($"[Bake] KeyboardObject 스킵 (이미 처리됨): {keyNode.targetGameObject.name}");
                        continue;
                    }
                    
                    var beforeAdd = keyNode.targetGameObject.GetComponents<UIGraphBakedKeyboardEvent>();
                    Debug.Log($"[Bake] KeyboardObject: {keyNode.targetGameObject.name}, 추가 전 컴포넌트 개수: {beforeAdd.Length}");
                    
                    var bakedKeyboardEvent = keyNode.targetGameObject.AddComponent<UIGraphBakedKeyboardEvent>();
                    bakedKeyboardEvent.SetGraph(this);
                    bakedKeyboardEvent.SetStartNodeGuid(keyNode.guid);
                    bakedKeyboardEvent.SetKeyCode(keyNode.targetKeyCode);
                    
                    var afterAdd = keyNode.targetGameObject.GetComponents<UIGraphBakedKeyboardEvent>();
                    Debug.Log($"[Bake] KeyboardObject: {keyNode.targetGameObject.name}, 추가 후 컴포넌트 개수: {afterAdd.Length}, GUID: {keyNode.guid}, KeyCode: {keyNode.targetKeyCode}");
                    
                    EditorUtility.SetDirty(bakedKeyboardEvent);
                    EditorUtility.SetDirty(keyNode.targetGameObject);
                }
            }
            Debug.Log($"[Bake] 완료");
#endif
        }
        
        private void PreserveNodeReferences(UINode node)
        {
#if UNITY_EDITOR
            if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                EditorUtility.SetDirty(btnNode.targetButton);
            else if (node is KeyboardInputNode keyNode && keyNode.targetGameObject != null)
                EditorUtility.SetDirty(keyNode.targetGameObject);
            else if (node is HideGameObjectNode hideNode && hideNode.targetGameObjects != null)
            {
                foreach (var obj in hideNode.targetGameObjects) if (obj != null) EditorUtility.SetDirty(obj);
            }
            else if (node is ShowGameObjectNode showNode && showNode.targetGameObjects != null)
            {
                foreach (var obj in showNode.targetGameObjects) if (obj != null) EditorUtility.SetDirty(obj);
            }
            else if (node is ToggleGameObjectNode toggleNode && toggleNode.targetGameObjects != null)
            {
                foreach (var obj in toggleNode.targetGameObjects) if (obj != null) EditorUtility.SetDirty(obj);
            }
            else if (node is ExecuteMethodNode execNode && execNode.targetObject != null)
                EditorUtility.SetDirty(execNode.targetObject);
#endif
        }
        
        public string GetOrCreateTargetId(GameObject obj)
        {
            if (obj == null) return string.Empty;
            
#if UNITY_EDITOR
            var target = obj.GetComponent<UIGraphTarget>();
            if (target == null) { target = obj.AddComponent<UIGraphTarget>(); EditorUtility.SetDirty(obj); }
            
            if (string.IsNullOrEmpty(target.TargetId))
            {
                target.SetTargetId(Guid.NewGuid().ToString());
                EditorUtility.SetDirty(target);
                EditorUtility.SetDirty(obj);
            }
            
            return target.TargetId;
#else
            var target = obj.GetComponent<UIGraphTarget>();
            return target != null ? target.TargetId : string.Empty;
#endif
        }
        
        private List<string> CalculateExecutionOrder()
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var processing = new HashSet<string>();
            var nodesWithoutInput = new HashSet<string>();
            
            foreach (var node in nodes) nodesWithoutInput.Add(node.guid);
            if (connections != null)
                foreach (var connection in connections) nodesWithoutInput.Remove(connection.toNodeGuid);
            
            foreach (var node in nodes)
                if (nodesWithoutInput.Contains(node.guid) && !visited.Contains(node.guid))
                    TopologicalSort(node.guid, visited, processing, result);
            
            foreach (var node in nodes)
                if (!visited.Contains(node.guid)) result.Add(node.guid);
            
            return result;
        }
        
        private void TopologicalSort(string nodeGuid, HashSet<string> visited, HashSet<string> processing, List<string> result)
        {
            if (processing.Contains(nodeGuid) || visited.Contains(nodeGuid)) return;
            
            processing.Add(nodeGuid);
            visited.Add(nodeGuid);
            result.Add(nodeGuid);
            
            if (connections != null)
            {
                var outgoingConnections = connections.FindAll(c => c.fromNodeGuid == nodeGuid);
                foreach (var connection in outgoingConnections)
                    TopologicalSort(connection.toNodeGuid, visited, processing, result);
            }
            
            processing.Remove(nodeGuid);
        }
    }
}
