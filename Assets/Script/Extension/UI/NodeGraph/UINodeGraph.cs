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
        [SerializeReference]
        public List<UINode> nodes = new List<UINode>();
        public List<UINodeConnection> connections = new List<UINodeConnection>();
        
        [SerializeField, HideInInspector]
        private UIGraphRuntimeData bakedData;
        
        public UIGraphRuntimeData GetBakedData() => bakedData;
        public List<UINodeConnection> GetConnections() => connections;
        
        public void Bake()
        {
            bakedData = new UIGraphRuntimeData();
            BakeNodes();
            BakeButtonEvents();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
        
        private void BakeNodes()
        {
            if (nodes == null || nodes.Count == 0)
            {
                Debug.LogWarning("Bake: 노드가 없습니다.");
                return;
            }
            
            var executionOrder = CalculateExecutionOrder();
            bakedData.executionSteps.Clear();
            
            Debug.Log($"Bake: 실행 순서 계산 완료 - {executionOrder.Count}개 노드");
            
            foreach (var nodeGuid in executionOrder)
            {
                var node = nodes.Find(n => n.guid == nodeGuid);
                if (node != null)
                {
                    var step = node.CreateExecutionStep();
                    if (step != null)
                    {
                        bakedData.executionSteps.Add(step);
                        Debug.Log($"Bake: {node.nodeName} ({nodeGuid}) 추가됨");
                    }
                    else
                    {
                        Debug.LogWarning($"Bake: {node.nodeName} ({nodeGuid})의 ExecutionStep이 null입니다.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Bake: 노드를 찾을 수 없습니다: {nodeGuid}");
                }
            }
            
            Debug.Log($"Bake 완료: {bakedData.executionSteps.Count}개의 실행 단계 생성");
        }
        
        private void BakeButtonEvents()
        {
#if UNITY_EDITOR
            // 모든 ButtonClickNode 찾기
            foreach (var node in nodes)
            {
                if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                {
                    var button = btnNode.targetButton.GetComponent<UnityEngine.UI.Button>();
                    if (button == null) continue;
                    
                    // 기존 UIGraphBakedEvent 제거
                    var existingEvent = btnNode.targetButton.GetComponent<UIGraphBakedEvent>();
                    if (existingEvent != null)
                    {
                        // 기존 이벤트 리스너 제거
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, existingEvent.OnButtonClick);
                        Undo.DestroyObjectImmediate(existingEvent);
                    }
                    
                    // 새 UIGraphBakedEvent 추가
                    var bakedEvent = btnNode.targetButton.AddComponent<UIGraphBakedEvent>();
                    bakedEvent.SetGraph(this);
                    bakedEvent.SetStartNodeGuid(btnNode.guid);
                    
                    // UnityEvent에 PersistentCall 추가 (Inspector에 보이게)
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, bakedEvent.OnButtonClick);
                    
                    EditorUtility.SetDirty(bakedEvent);
                    EditorUtility.SetDirty(button);
                    EditorUtility.SetDirty(btnNode.targetButton);
                    
                    Debug.Log($"Bake: {btnNode.targetButton.name} 버튼의 OnClick에 이벤트 등록됨 (시작 노드: {btnNode.guid})");
                }
            }
#endif
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
        
        private List<string> CalculateExecutionOrder()
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var processing = new HashSet<string>();
            
            // 연결이 없는 노드들 (입력이 없는 노드들) 찾기
            var nodesWithoutInput = new HashSet<string>();
            foreach (var node in nodes)
            {
                nodesWithoutInput.Add(node.guid);
            }
            
            if (connections != null)
            {
                foreach (var connection in connections)
                {
                    nodesWithoutInput.Remove(connection.toNodeGuid);
                }
            }
            
            // 입력이 없는 노드들부터 시작하여 위상 정렬
            foreach (var node in nodes)
            {
                if (nodesWithoutInput.Contains(node.guid) && !visited.Contains(node.guid))
                {
                    TopologicalSort(node.guid, visited, processing, result);
                }
            }
            
            // 연결이 없는 노드들도 추가 (독립적으로 실행)
            foreach (var node in nodes)
            {
                if (!visited.Contains(node.guid))
                {
                    result.Add(node.guid);
                }
            }
            
            Debug.Log($"실행 순서 계산 완료: {result.Count}개 노드");
            
            return result;
        }
        
        private void TopologicalSort(string nodeGuid, HashSet<string> visited, HashSet<string> processing, List<string> result)
        {
            if (processing.Contains(nodeGuid))
            {
                Debug.LogWarning($"순환 참조 감지: {nodeGuid}");
                return;
            }
            
            if (visited.Contains(nodeGuid))
            {
                return;
            }
            
            processing.Add(nodeGuid);
            
            // 현재 노드를 먼저 결과에 추가 (연결의 시작점이 먼저 실행되도록)
            visited.Add(nodeGuid);
            result.Add(nodeGuid);
            
            // 그 다음 연결된 노드들을 재귀적으로 처리
            if (connections != null)
            {
                var outgoingConnections = connections.FindAll(c => c.fromNodeGuid == nodeGuid);
                foreach (var connection in outgoingConnections)
                {
                    TopologicalSort(connection.toNodeGuid, visited, processing, result);
                }
            }
            
            processing.Remove(nodeGuid);
        }
    }
}

