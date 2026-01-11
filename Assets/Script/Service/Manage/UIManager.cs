using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hunt
{
    public class UIManager : MonoBehaviourSingleton<UIManager>
    {
        private Dictionary<UILayer,HashSet<UILayerGroup>> layerGroupMap = new Dictionary<UILayer,HashSet<UILayerGroup>>();
        private Dictionary<InputAction,UILayer> inputLayerMap = new Dictionary<InputAction,UILayer>();
        private Dictionary<string, UIGraphTarget> graphTargetMap = new Dictionary<string, UIGraphTarget>();

        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
            RegisterAllGraphTargetsInScene();
            RegisterAllLayerGroupsInScene();
        }
        
        private void RegisterAllGraphTargetsInScene()
        {
            // 씬에 있는 모든 UIGraphTarget 찾아서 등록
            var allTargets = FindObjectsOfType<UIGraphTarget>(true); // 비활성화된 것도 포함
            Debug.Log($"[UIManager] 씬에서 {allTargets.Length}개의 UIGraphTarget 발견");
            
            foreach (var target in allTargets)
            {
                if (target != null && !string.IsNullOrEmpty(target.TargetId))
                {
                    RegisterGraphTarget(target);
                }
            }
        }
        
        private void RegisterAllLayerGroupsInScene()
        {
            // 씬에 있는 모든 UILayerGroup 찾아서 등록
            var allGroups = FindObjectsOfType<UILayerGroup>(true); // 비활성화된 것도 포함
            Debug.Log($"[UIManager] 씬에서 {allGroups.Length}개의 UILayerGroup 발견");
            
            if (allGroups.Length == 0)
            {
                Debug.LogWarning($"[UIManager] 씬에 UILayerGroup이 없습니다. GameObject에 UILayerGroup 컴포넌트를 추가해주세요.");
                return;
            }
            
            foreach (var group in allGroups)
            {
                if (group == null)
                {
                    Debug.LogWarning($"[UIManager] null인 UILayerGroup 발견");
                    continue;
                }
                
                Debug.Log($"[UIManager] UILayerGroup 처리 중: {group.gameObject.name}, Layer: {group.Layer}");
                
                if (group.Layer != UILayer.None)
                {
                    RegisterGroup(group);
                }
                else
                {
                    Debug.LogWarning($"[UIManager] {group.gameObject.name}의 Layer가 UILayer.None입니다. Inspector에서 Layer를 설정해주세요.");
                }
            }
            
            Debug.Log($"[UIManager] layerGroupMap 등록 완료. 총 {layerGroupMap.Count}개 레이어에 그룹 등록됨");
            foreach (var kvp in layerGroupMap)
            {
                Debug.Log($"[UIManager]   - {kvp.Key}: {kvp.Value.Count}개 그룹");
            }
        }

        protected override void OnDestroy()
        {
            UnregisterAllInputEvents();
            graphTargetMap.Clear();
            base.OnDestroy();
        }

        private void RegisterInputEvent(InputAction inputAction,UILayer targetLayer)
        {
            if (inputAction == null) return;

            if(inputLayerMap.ContainsKey(inputAction))
            {
                inputAction.performed -= OnInputPerformed;
            }

            inputLayerMap[inputAction] = targetLayer;
            inputAction.performed += OnInputPerformed;
        }

        public void UnregisterInputEvent(InputAction inputAction)
        {
            if(inputAction == null || !inputLayerMap.ContainsKey(inputAction)) return;

            inputAction.performed -= OnInputPerformed;
            inputLayerMap.Remove(inputAction);
        }

        private void OnInputPerformed(InputAction.CallbackContext context)
        {
            if (!inputLayerMap.TryGetValue(context.action, out var targetLayer)) return;
            HideLayer(targetLayer);
        }

        public void RegisterGroup(UILayerGroup group)
        {
            if (group == null)
            {
                Debug.LogWarning($"[UIManager] RegisterGroup: group이 null입니다.");
                return;
            }
            
            if (group.Layer == UILayer.None)
            {
                Debug.LogWarning($"[UIManager] RegisterGroup: {group.gameObject.name}의 Layer가 UILayer.None입니다.");
                return;
            }

            if (!layerGroupMap.ContainsKey(group.Layer))
            {
                layerGroupMap[group.Layer] = new HashSet<UILayerGroup>();
            }
            
            layerGroupMap[group.Layer].Add(group);
            Debug.Log($"[UIManager] RegisterGroup: {group.gameObject.name} (Layer: {group.Layer}) 등록 완료. 현재 {group.Layer} 레이어에 {layerGroupMap[group.Layer].Count}개 그룹 등록됨");
        }

        public void UnregisterGroup(UILayerGroup group)
        {
            if (group == null || group.Layer == UILayer.None) return;   

            if(layerGroupMap.TryGetValue(group.Layer, out var groupSet))
            {
                groupSet.Remove(group);
            }
        }

        public void HideLayer(UILayer layer)
        {
            if (layer == UILayer.None)
            {
                Debug.LogWarning($"[UIManager] HideLayer: UILayer.None은 처리할 수 없습니다.");
                return;
            }
            
            if (!layerGroupMap.TryGetValue(layer, out var groups))
            {
                Debug.LogWarning($"[UIManager] HideLayer: {layer} 레이어에 등록된 그룹이 없습니다. UILayerGroup 컴포넌트가 씬에 있는지 확인해주세요.");
                return;
            }

            Debug.Log($"[UIManager] HideLayer: {layer} 레이어의 {groups.Count}개 그룹 비활성화 중...");
            
            foreach (var group in groups)
            {
                if (group != null && group.gameObject != null)
                {
                    Debug.Log($"[UIManager] HideLayer: {group.gameObject.name} 및 모든 자식 비활성화");
                    // 부모를 비활성화하면 자식들도 자동으로 비활성화됨
                    group.gameObject.SetActive(false);
                }
            }
        }

        public void ShowLayer(UILayer layer)
        {
            if (layer == UILayer.None)
            {
                Debug.LogWarning($"[UIManager] ShowLayer: UILayer.None은 처리할 수 없습니다.");
                return;
            }
            
            if (!layerGroupMap.TryGetValue(layer, out var groups))
            {
                Debug.LogWarning($"[UIManager] ShowLayer: {layer} 레이어에 등록된 그룹이 없습니다. UILayerGroup 컴포넌트가 씬에 있는지 확인해주세요.");
                return;
            }

            Debug.Log($"[UIManager] ShowLayer: {layer} 레이어의 {groups.Count}개 그룹 활성화 중...");
            
            foreach (var group in groups)
            {
                if (group != null && group.gameObject != null)
                {
                    Debug.Log($"[UIManager] ShowLayer: {group.gameObject.name} 및 모든 자식 활성화");
                    // 부모를 활성화하면 자식들도 자동으로 활성화됨 (단, 자식이 별도로 비활성화되어 있지 않은 경우)
                    group.gameObject.SetActive(true);
                }
            }
        }

        public void HideLayers(params UILayer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                HideLayer(layer);
            }
        }

        public void ShowLayers(params UILayer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                ShowLayer(layer);
            }
        }

        public void ToggleLayers(params UILayer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                if (layer == UILayer.None || !layerGroupMap.TryGetValue(layer, out var groups)) continue;
                
                bool anyActive = false;
                foreach (var group in groups)
                {
                    if (group != null && group.gameObject != null && group.gameObject.activeSelf)
                    {
                        anyActive = true;
                        break;
                    }
                }
                
                if (anyActive)
                {
                    HideLayer(layer);
                }
                else
                {
                    ShowLayer(layer);
                }
            }
        }

        public void HideGameObjects(params GameObject[] targets)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(false);
                }
            }
        }

        public void ShowGameObjects(params GameObject[] targets)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            }
        }

        public void ToggleGameObjects(params GameObject[] targets)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(!target.activeSelf);
                }
            }
        }

        public void RegisterGraphTarget(UIGraphTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId))
            {
                Debug.LogWarning($"[UIManager] RegisterGraphTarget: target이 null이거나 TargetId가 비어있습니다.");
                return;
            }
            
            Debug.Log($"[UIManager] RegisterGraphTarget: {target.gameObject.name} (ID: {target.TargetId}) 등록 중...");
            graphTargetMap[target.TargetId] = target;
            Debug.Log($"[UIManager] RegisterGraphTarget 완료. 현재 등록된 개수: {graphTargetMap.Count}");
        }
        
        public void UnregisterGraphTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            
            graphTargetMap.Remove(targetId);
        }
        
        public GameObject FindGameObjectById(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                Debug.LogWarning($"[UIManager] FindGameObjectById: targetId가 비어있습니다.");
                return null;
            }
         
            if (graphTargetMap.TryGetValue(targetId, out var target) && target != null)
            {
                return target.gameObject;
            }
            
            Debug.LogWarning($"[UIManager] FindGameObjectById: ID {targetId}를 찾을 수 없습니다.");
            return null;
        }

        public async UniTask ExecuteGraph(UINodeGraph graph)
        {
            try
            {
                if (graph == null)
                {
                    $"UINodeGraph가 null입니다.".DError();
                    return;
                }
                
                var runtimeData = graph.GetBakedData();
                if (runtimeData == null)
                {
                    $"Bake된 데이터가 없습니다. 노드 그래프를 Bake해주세요.".DError();
                    return;
                }
                
                if (runtimeData.executionSteps == null || runtimeData.executionSteps.Count == 0)
                {
                    $"실행할 단계가 없습니다. 노드 그래프에 노드를 추가하고 Bake해주세요.".DWarnning();
                    return;
                }
                
                for (int i = 0; i < runtimeData.executionSteps.Count; i++)
                {
                    var step = runtimeData.executionSteps[i];
                    if (step == null) continue;
                    
                    try
                    {
                        await ExecuteStep(step);
                    }
                    catch (System.Exception e)
                    {
                        $"ExecuteStep 오류 (단계 {i + 1}): {e.Message}".DError();
                        Debug.LogException(e);
                    }
                }
            }
            catch (System.Exception e)
            {
                $"ExecuteGraph 오류: {e.Message}".DError();
                Debug.LogException(e);
            }
        }
        
        public async UniTask ExecuteGraphFromNode(UINodeGraph graph, string startNodeGuid)
        {
            try
            {
                Debug.Log($"[UIManager] ExecuteGraphFromNode 호출됨 - graph: {graph?.name}, startNodeGuid: {startNodeGuid}");
                
                if (graph == null || string.IsNullOrEmpty(startNodeGuid))
                {
                    Debug.LogWarning($"[UIManager] ExecuteGraphFromNode: graph 또는 startNodeGuid가 null입니다.");
                    return;
                }
                
                var runtimeData = graph.GetBakedData();
                if (runtimeData == null || runtimeData.executionSteps == null)
                {
                    Debug.LogWarning($"[UIManager] ExecuteGraphFromNode: runtimeData가 null이거나 executionSteps가 없습니다.");
                    return;
                }
                
                Debug.Log($"[UIManager] executionSteps 개수: {runtimeData.executionSteps.Count}, connections 개수: {graph.GetConnections()?.Count ?? 0}");
                
                // 시작 노드부터 연결된 노드들만 실행
                var stepsToExecute = GetStepsFromNode(runtimeData.executionSteps, startNodeGuid, graph.GetConnections());
                
                Debug.Log($"[UIManager] 실행할 step 개수: {stepsToExecute.Count}");
                
                if (stepsToExecute.Count == 0)
                {
                    Debug.LogWarning($"[UIManager] 실행할 step이 없습니다. ButtonClickNode에서 연결된 노드가 있는지 확인해주세요.");
                    return;
                }
                
                foreach (var step in stepsToExecute)
                {
                    if (step == null) continue;
                    
                    try
                    {
                        Debug.Log($"[UIManager] Step 실행 중: {step.nodeType}, nodeGuid: {step.nodeGuid}");
                        await ExecuteStep(step);
                    }
                    catch (System.Exception e)
                    {
                        $"ExecuteStep 오류: {e.Message}".DError();
                        Debug.LogException(e);
                    }
                }
            }
            catch (System.Exception e)
            {
                $"ExecuteGraphFromNode 오류: {e.Message}".DError();
                Debug.LogException(e);
            }
        }
        
        private List<UIGraphExecutionStep> GetStepsFromNode(List<UIGraphExecutionStep> allSteps, string startGuid, List<UINodeConnection> connections)
        {
            var result = new List<UIGraphExecutionStep>();
            var visited = new HashSet<string>();
            var stepMap = new Dictionary<string, UIGraphExecutionStep>();
            
            Debug.Log($"[UIManager] GetStepsFromNode 시작 - startGuid: {startGuid}, allSteps: {allSteps?.Count ?? 0}, connections: {connections?.Count ?? 0}");
            
            // Step을 GUID로 매핑
            foreach (var step in allSteps)
            {
                if (step != null && !string.IsNullOrEmpty(step.nodeGuid))
                {
                    stepMap[step.nodeGuid] = step;
                    Debug.Log($"[UIManager] Step 매핑: {step.nodeGuid} -> {step.nodeType}");
                }
            }
            
            // 시작 노드(ButtonClickNode)는 제외하고, 연결된 노드들만 찾기
            var queue = new Queue<string>();
            visited.Add(startGuid); // 시작 노드는 방문 처리하지만 결과에 추가하지 않음
            
            // 시작 노드에서 연결된 노드들부터 시작
            if (connections != null)
            {
                int connectionCount = 0;
                foreach (var connection in connections)
                {
                    Debug.Log($"[UIManager] Connection 확인: {connection.fromNodeGuid} -> {connection.toNodeGuid}");
                    if (connection.fromNodeGuid == startGuid && !visited.Contains(connection.toNodeGuid))
                    {
                        visited.Add(connection.toNodeGuid);
                        queue.Enqueue(connection.toNodeGuid);
                        connectionCount++;
                        Debug.Log($"[UIGraphBakedEvent] 시작 노드에서 연결된 노드 발견: {connection.toNodeGuid}");
                    }
                }
                
                if (connectionCount == 0)
                {
                    Debug.LogWarning($"[UIManager] 시작 노드({startGuid})에서 연결된 노드가 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[UIManager] connections가 null입니다.");
            }
            
            while (queue.Count > 0)
            {
                var currentGuid = queue.Dequeue();
                
                // ButtonClickNode는 실행하지 않음
                if (stepMap.TryGetValue(currentGuid, out var step) && step.nodeType != UINodeType.ButtonClick)
                {
                    result.Add(step);
                    Debug.Log($"[UIManager] 실행할 step 추가: {step.nodeType} ({currentGuid})");
                }
                else
                {
                    Debug.LogWarning($"[UIManager] Step을 찾을 수 없거나 ButtonClick 타입입니다: {currentGuid}");
                }
                
                // 연결된 노드들 찾기
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (connection.fromNodeGuid == currentGuid && !visited.Contains(connection.toNodeGuid))
                        {
                            visited.Add(connection.toNodeGuid);
                            queue.Enqueue(connection.toNodeGuid);
                        }
                    }
                }
            }
            
            Debug.Log($"[UIManager] GetStepsFromNode 완료 - 결과 개수: {result.Count}");
            return result;
        }

        private async UniTask ExecuteStep(UIGraphExecutionStep step)
        {
            if (step == null) return;
            
            try
            {
                switch (step.nodeType)
            {
                case UINodeType.HideLayer:
                    if (step.layerParams != null && step.layerParams.Length > 0)
                    {
                        foreach (var layerStr in step.layerParams)
                        {
                            if (System.Enum.TryParse<UILayer>(layerStr, out var layer) && layer != UILayer.None)
                            {
                                HideLayer(layer);
                            }
                        }
                    }
                    break;
                    
                case UINodeType.ShowLayer:
                    if (step.layerParams != null && step.layerParams.Length > 0)
                    {
                        foreach (var layerStr in step.layerParams)
                        {
                            if (System.Enum.TryParse<UILayer>(layerStr, out var layer) && layer != UILayer.None)
                            {
                                ShowLayer(layer);
                            }
                        }
                    }
                    break;
                    
                case UINodeType.ToggleLayer:
                    if (step.layerParams != null && step.layerParams.Length > 0)
                    {
                        foreach (var layerStr in step.layerParams)
                        {
                            if (System.Enum.TryParse<UILayer>(layerStr, out var layer) && layer != UILayer.None)
                            {
                                ToggleLayers(layer);
                            }
                        }
                    }
                    break;
                    
                case UINodeType.HideGameObject:
                    if (step.gameObjectIds == null || step.gameObjectIds.Length == 0)
                    {
                        Debug.LogWarning($"[UIManager] HideGameObject: gameObjectIds가 null이거나 비어있습니다.");
                        break;
                    }
                    
                    Debug.Log($"[UIManager] HideGameObject: {step.gameObjectIds.Length}개 GameObject 처리 시작");
                    
                    foreach (var id in step.gameObjectIds)
                    {
                        Debug.Log($"[UIManager] HideGameObject: ID {id}로 GameObject 찾는 중...");
                        var go = FindGameObjectById(id);
                        if (go != null)
                        {
                            Debug.Log($"[UIManager] HideGameObject: {go.name} 찾음, 비활성화 중...");
                            go.SetActive(false);
                            Debug.Log($"[UIManager] HideGameObject: {go.name} 비활성화 완료 (activeSelf: {go.activeSelf})");
                        }
                        else
                        {
                            Debug.LogWarning($"[UIManager] HideGameObject: ID {id}로 GameObject를 찾을 수 없습니다.");
                        }
                    }
                    break;
                    
                case UINodeType.ShowGameObject:
                    if (step.gameObjectIds != null && step.gameObjectIds.Length > 0)
                    {
                        foreach (var id in step.gameObjectIds)
                        {
                            var go = FindGameObjectById(id);
                            if (go != null)
                            {
                                go.SetActive(true);
                            }
                        }
                    }
                    break;
                    
                case UINodeType.ToggleGameObject:
                    if (step.gameObjectIds != null && step.gameObjectIds.Length > 0)
                    {
                        foreach (var id in step.gameObjectIds)
                        {
                            var go = FindGameObjectById(id);
                            if (go != null)
                            {
                                go.SetActive(!go.activeSelf);
                            }
                        }
                    }
                    break;
                    
                case UINodeType.Delay:
                    if (step.floatParams.TryGetValue("delaySeconds", out var delay))
                    {
                        await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
                    }
                    break;
                    
                case UINodeType.ButtonClick:
                    // ButtonClickNode는 실행하지 않음 (이미 버튼이 클릭되었으므로)
                    break;
                    
                case UINodeType.ExecuteMethod:
                    if (step.gameObjectIds != null && step.gameObjectIds.Length > 0 && 
                        step.stringParams.TryGetValue("componentType", out var compType) &&
                        step.stringParams.TryGetValue("methodName", out var methodName) &&
                        !string.IsNullOrEmpty(compType) && !string.IsNullOrEmpty(methodName))
                    {
                        var go = FindGameObjectById(step.gameObjectIds[0]);
                        if (go != null)
                        {
                            var componentType = System.Type.GetType(compType);
                            if (componentType == null) componentType = System.Reflection.Assembly.GetExecutingAssembly().GetType(compType);
                            if (componentType == null)
                            {
                                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    componentType = asm.GetType(compType);
                                    if (componentType != null) break;
                                }
                            }
                            
                            if (componentType != null)
                            {
                                var component = go.GetComponent(componentType);
                                if (component != null)
                                {
                                    var method = componentType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                    if (method != null && method.GetParameters().Length == 0)
                                        method.Invoke(component, null);
                                }
                            }
                        }
                    }
                    break;
            }
            }
            catch (System.Exception e)
            {
                $"ExecuteStep 오류 ({step?.nodeType}): {e.Message}".DError();
                Debug.LogException(e);
            }
        }

        private void UnregisterAllInputEvents()
        {
            foreach(var kvp in inputLayerMap)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.performed -= OnInputPerformed;
                }
            }

            inputLayerMap.Clear();
        }


    }
}
