using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using Hunt;

namespace Hunt
{
    public class UINodeGraphEditor : EditorWindow
    {
        private UINodeGraph currentGraph;
        private Vector2 panOffset;
        private Vector2 dragOffset;
        private UINode selectedNode;
        private UINodeConnection currentConnection;
        private bool isConnecting;
        private string connectingFromNodeGuid;
        private int connectingFromPort;
        
        private const float NODE_WIDTH = 250f;
        private const float NODE_HEIGHT = 150f;
        private const float GRID_SIZE = 20f;
        
        [MenuItem("Tools/Hunt/UI Node Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<UINodeGraphEditor>("UI Node Graph");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            if (currentGraph == null)
            {
                EditorGUILayout.HelpBox("노드 그래프를 선택하거나 생성하세요.", MessageType.Info);
                return;
            }
            
            // 노드가 있는지 확인
            if (currentGraph.nodes == null || currentGraph.nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("노드가 없습니다. Add Node 버튼을 눌러 노드를 추가하세요.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"노드 개수: {currentGraph.nodes.Count}개", MessageType.Info);
            }
            
            // Bake 상태 표시
            var bakedData = currentGraph.GetBakedData();
            if (bakedData != null && bakedData.executionSteps != null && bakedData.executionSteps.Count > 0)
            {
                EditorGUILayout.HelpBox($"Bake 완료: {bakedData.executionSteps.Count}개의 실행 단계", MessageType.Info);
            }
            else if (currentGraph.nodes != null && currentGraph.nodes.Count > 0)
            {
                EditorGUILayout.HelpBox("Bake되지 않았습니다. Bake 버튼을 눌러주세요.", MessageType.Warning);
            }
            
            HandleEvents();
            DrawGrid();
            DrawConnections();
            DrawNodes();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(currentGraph);
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            currentGraph = EditorGUILayout.ObjectField(
                currentGraph, 
                typeof(UINodeGraph), 
                false,
                GUILayout.Width(200)
            ) as UINodeGraph;
            
            if (GUILayout.Button("New Graph", EditorStyles.toolbarButton))
            {
                CreateNewGraph();
            }
            
            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton))
            {
                ShowNodeMenu();
            }
            
            if (GUILayout.Button("Bake", EditorStyles.toolbarButton))
            {
                if (currentGraph != null)
                {
                    if (currentGraph.nodes == null || currentGraph.nodes.Count == 0)
                    {
                        EditorUtility.DisplayDialog("Bake 실패", "노드가 없습니다. 노드를 추가한 후 Bake해주세요.", "OK");
                        return;
                    }
                    
                    currentGraph.Bake();
                    EditorUtility.SetDirty(currentGraph);
                    AssetDatabase.SaveAssets();
                    Debug.Log("노드 그래프가 Bake되었습니다.");
                    Repaint();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateNewGraph()
        {
            string path = null;
            try
            {
                path = EditorUtility.SaveFilePanelInProject(
                    "새 노드 그래프 생성",
                    "NewUIGraph",
                    "asset",
                    "노드 그래프를 저장할 위치를 선택하세요."
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveFilePanelInProject 오류: {ex.Message}");
                // 대체 방법: 기본 경로 사용
                path = "Assets/NewUIGraph.asset";
            }
            
            if (!string.IsNullOrEmpty(path))
            {
                var graph = CreateInstance<UINodeGraph>();
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                currentGraph = graph;
            }
        }
        
        private void DrawGrid()
        {
            int widthDivs = Mathf.CeilToInt(position.width / GRID_SIZE);
            int heightDivs = Mathf.CeilToInt(position.height / GRID_SIZE);
            
            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            
            Vector3 offset = new Vector3(panOffset.x % GRID_SIZE, panOffset.y % GRID_SIZE, 0);
            
            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(GRID_SIZE * i, -GRID_SIZE, 0) + offset,
                    new Vector3(GRID_SIZE * i, position.height, 0) + offset
                );
            }
            
            for (int i = 0; i < heightDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(-GRID_SIZE, GRID_SIZE * i, 0) + offset,
                    new Vector3(position.width, GRID_SIZE * i, 0) + offset
                );
            }
            
            Handles.color = Color.white;
            Handles.EndGUI();
        }
        
        private void DrawNodes()
        {
            if (currentGraph == null || currentGraph.nodes == null) return;
            
            BeginWindows();
            
            for (int i = 0; i < currentGraph.nodes.Count; i++)
            {
                var node = currentGraph.nodes[i];
                if (node == null) continue;
                
                Rect nodeRect = new Rect(
                    node.position.x + panOffset.x,
                    node.position.y + panOffset.y,
                    NODE_WIDTH,
                    NODE_HEIGHT
                );
                
                nodeRect = GUI.Window(i, nodeRect, (id) => DrawNodeWindow(id, node), node.nodeName);
                var newPosition = new Vector2(nodeRect.x - panOffset.x, nodeRect.y - panOffset.y);
                if (node.position != newPosition)
                {
                    node.position = newPosition;
                    EditorUtility.SetDirty(currentGraph);
                }
            }
            
            EndWindows();
        }
        
        private void DrawNodeWindow(int id, UINode node)
        {
            if (node == null) return;
            
            GUILayout.BeginVertical();
            
            DrawNodeContent(node);
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Connect"))
            {
                isConnecting = true;
                connectingFromNodeGuid = node.guid;
                connectingFromPort = 0;
                Repaint();
            }
            
            if (GUILayout.Button("Edit"))
            {
                Selection.activeObject = currentGraph;
                selectedNode = node;
                EditorGUIUtility.PingObject(currentGraph);
                Repaint();
            }
            
            if (GUILayout.Button("Delete"))
            {
                DeleteNode(node);
                Repaint();
            }
            
            GUILayout.EndVertical();
            
            GUI.DragWindow();
        }
        
        private void DrawNodeContent(UINode node)
        {
            if (node == null) return;
            
            EditorGUI.BeginChangeCheck();
            
            switch (node.GetNodeType())
            {
                case UINodeType.ButtonClick:
                    var btnNode = node as ButtonClickNode;
                    if (btnNode != null)
                    {
                        GUILayout.Label("Button Click", EditorStyles.boldLabel);
                        var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                        btnNode.targetButton = EditorGUI.ObjectField(rect, "Button", btnNode.targetButton, typeof(GameObject), true) as GameObject;
                    }
                    break;
                    
                case UINodeType.HideLayer:
                case UINodeType.ShowLayer:
                case UINodeType.ToggleLayer:
                    DrawLayerArrayInNode(node);
                    break;
                    
                case UINodeType.HideGameObject:
                case UINodeType.ShowGameObject:
                case UINodeType.ToggleGameObject:
                    DrawGameObjectArrayInNode(node);
                    break;
                    
                case UINodeType.Delay:
                    var delayNode = node as DelayNode;
                    if (delayNode != null)
                    {
                        GUILayout.Label("Delay", EditorStyles.boldLabel);
                        var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                        delayNode.delaySeconds = EditorGUI.FloatField(rect, "Seconds", delayNode.delaySeconds);
                    }
                    break;
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentGraph);
            }
        }
        
        private void DrawLayerArrayInNode(UINode node)
        {
            UILayer[] layers = null;
            string title = "";
            HideLayerNode hideNode = null;
            ShowLayerNode showNode = null;
            ToggleLayerNode toggleNode = null;
            
            if (node is HideLayerNode h)
            {
                hideNode = h;
                layers = hideNode.targetLayers;
                title = "Hide Layer";
            }
            else if (node is ShowLayerNode s)
            {
                showNode = s;
                layers = showNode.targetLayers;
                title = "Show Layer";
            }
            else if (node is ToggleLayerNode t)
            {
                toggleNode = t;
                layers = toggleNode.targetLayers;
                title = "Toggle Layer";
            }
            
            // 배열이 null이면 초기화
            if (layers == null)
            {
                layers = new UILayer[0];
                if (hideNode != null) hideNode.targetLayers = layers;
                else if (showNode != null) showNode.targetLayers = layers;
                else if (toggleNode != null) toggleNode.targetLayers = layers;
                EditorUtility.SetDirty(currentGraph);
            }
            
            GUILayout.Label(title, EditorStyles.boldLabel);
            
            int size = layers.Length;
            var sizeRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            int newSize = EditorGUI.IntField(sizeRect, "Size", size);
            
            if (newSize != size)
            {
                System.Array.Resize(ref layers, newSize);
                if (hideNode != null) hideNode.targetLayers = layers;
                else if (showNode != null) showNode.targetLayers = layers;
                else if (toggleNode != null) toggleNode.targetLayers = layers;
                EditorUtility.SetDirty(currentGraph);
            }
            
            // 현재 노드의 배열을 다시 가져옴 (크기 변경 후)
            if (hideNode != null) layers = hideNode.targetLayers;
            else if (showNode != null) layers = showNode.targetLayers;
            else if (toggleNode != null) layers = toggleNode.targetLayers;
            
            for (int i = 0; i < layers.Length; i++)
            {
                var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                var newValue = (UILayer)EditorGUI.EnumPopup(rect, $"Layer {i}", layers[i]);
                if (newValue != layers[i])
                {
                    layers[i] = newValue;
                    if (hideNode != null) hideNode.targetLayers = layers;
                    else if (showNode != null) showNode.targetLayers = layers;
                    else if (toggleNode != null) toggleNode.targetLayers = layers;
                    EditorUtility.SetDirty(currentGraph);
                }
            }
        }
        
        private void DrawGameObjectArrayInNode(UINode node)
        {
            GameObject[] gameObjects = null;
            string title = "";
            
            if (node is HideGameObjectNode hideNode)
            {
                gameObjects = hideNode.targetGameObjects;
                title = "Hide GameObject";
            }
            else if (node is ShowGameObjectNode showNode)
            {
                gameObjects = showNode.targetGameObjects;
                title = "Show GameObject";
            }
            else if (node is ToggleGameObjectNode toggleNode)
            {
                gameObjects = toggleNode.targetGameObjects;
                title = "Toggle GameObject";
            }
            
            if (gameObjects == null) return;
            
            GUILayout.Label(title, EditorStyles.boldLabel);
            
            int size = gameObjects.Length;
            var sizeRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            int newSize = EditorGUI.IntField(sizeRect, "Size", size);
            
            if (newSize != size)
            {
                System.Array.Resize(ref gameObjects, newSize);
                if (node is HideGameObjectNode h) h.targetGameObjects = gameObjects;
                else if (node is ShowGameObjectNode s) s.targetGameObjects = gameObjects;
                else if (node is ToggleGameObjectNode t) t.targetGameObjects = gameObjects;
                EditorUtility.SetDirty(currentGraph);
            }
            
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                var newValue = EditorGUI.ObjectField(rect, $"Object {i}", gameObjects[i], typeof(GameObject), true) as GameObject;
                if (newValue != gameObjects[i])
                {
                    gameObjects[i] = newValue;
                    if (node is HideGameObjectNode h) h.targetGameObjects = gameObjects;
                    else if (node is ShowGameObjectNode s) s.targetGameObjects = gameObjects;
                    else if (node is ToggleGameObjectNode t) t.targetGameObjects = gameObjects;
                    EditorUtility.SetDirty(currentGraph);
                }
            }
        }
        
        
        private void DrawConnections()
        {
            if (currentGraph == null || currentGraph.connections == null) return;
            
            Handles.BeginGUI();
            
            foreach (var connection in currentGraph.connections)
            {
                DrawConnection(connection);
            }
            
            if (isConnecting)
            {
                var fromNode = currentGraph.nodes.Find(n => n.guid == connectingFromNodeGuid);
                if (fromNode != null)
                {
                    Vector2 fromPos = new Vector2(
                        fromNode.position.x + panOffset.x + NODE_WIDTH,
                        fromNode.position.y + panOffset.y + NODE_HEIGHT / 2
                    );
                    Vector2 toPos = Event.current.mousePosition;
                    DrawConnectionLine(fromPos, toPos, Color.yellow);
                }
            }
            
            Handles.EndGUI();
        }
        
        private void DrawConnection(UINodeConnection connection)
        {
            var fromNode = currentGraph.nodes.Find(n => n.guid == connection.fromNodeGuid);
            var toNode = currentGraph.nodes.Find(n => n.guid == connection.toNodeGuid);
            
            if (fromNode == null || toNode == null) return;
            
            Vector2 fromPos = new Vector2(
                fromNode.position.x + panOffset.x + NODE_WIDTH,
                fromNode.position.y + panOffset.y + NODE_HEIGHT / 2
            );
            Vector2 toPos = new Vector2(
                toNode.position.x + panOffset.x,
                toNode.position.y + panOffset.y + NODE_HEIGHT / 2
            );
            
            DrawConnectionLine(fromPos, toPos, Color.white);
        }
        
        private void DrawConnectionLine(Vector2 from, Vector2 to, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(new Vector3(from.x, from.y, 0), new Vector3(to.x, to.y, 0));
            Handles.color = Color.white;
        }
        
        private void HandleEvents()
        {
            Event e = Event.current;
            
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 1)
                    {
                        ShowContextMenu(e.mousePosition);
                    }
                    else if (e.button == 0 && isConnecting)
                    {
                        CompleteConnection(e.mousePosition);
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (e.button == 2)
                    {
                        panOffset += e.delta;
                        Repaint();
                    }
                    break;
                    
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    HandleDragAndDrop(e);
                    break;
            }
        }
        
        private void HandleDragAndDrop(Event e)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                var obj = DragAndDrop.objectReferences[0];
                if (obj is GameObject gameObject)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        CreateNodeFromGameObject(gameObject, e.mousePosition - panOffset);
                    }
                }
            }
        }
        
        private void CreateNodeFromGameObject(GameObject obj, Vector2 position)
        {
            if (currentGraph == null) return;
            
            if (obj.GetComponent<UIButtonControlBase>() != null)
            {
                if (currentGraph.nodes == null)
                {
                    currentGraph.nodes = new List<UINode>();
                }
                
                var node = new ButtonClickNode
                {
                    guid = System.Guid.NewGuid().ToString(),
                    position = position,
                    nodeName = obj.name,
                    targetButton = obj
                };
                currentGraph.nodes.Add(node);
                EditorUtility.SetDirty(currentGraph);
                AssetDatabase.SaveAssets();
                Repaint();
            }
        }
        
        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Button Click Node"), false, () => 
                CreateNode(UINodeType.ButtonClick, mousePosition - panOffset));
            menu.AddItem(new GUIContent("Add Hide Layer Node"), false, () => 
                CreateNode(UINodeType.HideLayer, mousePosition - panOffset));
            menu.AddItem(new GUIContent("Add Show Layer Node"), false, () => 
                CreateNode(UINodeType.ShowLayer, mousePosition - panOffset));
            menu.AddItem(new GUIContent("Add Toggle Layer Node"), false, () => 
                CreateNode(UINodeType.ToggleLayer, mousePosition - panOffset));
            menu.AddItem(new GUIContent("Add Hide GameObject Node"), false, () => 
                CreateNode(UINodeType.HideGameObject, mousePosition - panOffset));
            menu.AddItem(new GUIContent("Add Show GameObject Node"), false, () => 
                CreateNode(UINodeType.ShowGameObject, mousePosition - panOffset));
            menu.AddItem(new GUIContent("Add Toggle GameObject Node"), false, () => 
                CreateNode(UINodeType.ToggleGameObject, mousePosition - panOffset));
            menu.AddItem(new GUIContent("Add Delay Node"), false, () => 
                CreateNode(UINodeType.Delay, mousePosition - panOffset));
            menu.ShowAsContext();
        }
        
        private void ShowNodeMenu()
        {
            ShowContextMenu(new Vector2(position.width / 2, position.height / 2));
        }
        
        private void CreateNode(UINodeType type, Vector2 position)
        {
            if (currentGraph == null) return;
            
            UINode node = null;
            
            switch (type)
            {
                case UINodeType.ButtonClick:
                    node = new ButtonClickNode();
                    break;
                case UINodeType.HideLayer:
                    var hideLayerNode = new HideLayerNode();
                    hideLayerNode.targetLayers = new UILayer[0];
                    node = hideLayerNode;
                    break;
                case UINodeType.ShowLayer:
                    var showLayerNode = new ShowLayerNode();
                    showLayerNode.targetLayers = new UILayer[0];
                    node = showLayerNode;
                    break;
                case UINodeType.ToggleLayer:
                    var toggleLayerNode = new ToggleLayerNode();
                    toggleLayerNode.targetLayers = new UILayer[0];
                    node = toggleLayerNode;
                    break;
                case UINodeType.HideGameObject:
                    var hideGoNode = new HideGameObjectNode();
                    hideGoNode.targetGameObjects = new GameObject[0];
                    node = hideGoNode;
                    break;
                case UINodeType.ShowGameObject:
                    var showGoNode = new ShowGameObjectNode();
                    showGoNode.targetGameObjects = new GameObject[0];
                    node = showGoNode;
                    break;
                case UINodeType.ToggleGameObject:
                    var toggleGoNode = new ToggleGameObjectNode();
                    toggleGoNode.targetGameObjects = new GameObject[0];
                    node = toggleGoNode;
                    break;
                case UINodeType.Delay:
                    var delayNode = new DelayNode();
                    delayNode.delaySeconds = 1f;
                    node = delayNode;
                    break;
            }
            
            if (node != null)
            {
                node.guid = System.Guid.NewGuid().ToString();
                node.position = position;
                
                if (string.IsNullOrEmpty(node.nodeName))
                {
                    node.nodeName = type.ToString();
                }
                
                if (currentGraph.nodes == null)
                {
                    currentGraph.nodes = new List<UINode>();
                }
                
                currentGraph.nodes.Add(node);
                EditorUtility.SetDirty(currentGraph);
                AssetDatabase.SaveAssets();
                Repaint();
            }
        }
        
        private void DeleteNode(UINode node)
        {
            if (currentGraph == null || node == null) return;
            
            currentGraph.nodes.Remove(node);
            
            if (currentGraph.connections != null)
            {
                currentGraph.connections.RemoveAll(c => 
                    c.fromNodeGuid == node.guid || c.toNodeGuid == node.guid);
            }
            
            EditorUtility.SetDirty(currentGraph);
        }
        
        private void CompleteConnection(Vector2 mousePosition)
        {
            if (!isConnecting) return;
            
            // 마우스 위치에 있는 노드 찾기
            var toNode = currentGraph.nodes.FirstOrDefault(n =>
            {
                Rect nodeRect = new Rect(
                    n.position.x + panOffset.x,
                    n.position.y + panOffset.y,
                    NODE_WIDTH,
                    NODE_HEIGHT
                );
                return nodeRect.Contains(mousePosition);
            });
            
            if (toNode != null && toNode.guid != connectingFromNodeGuid)
            {
                var connection = new UINodeConnection
                {
                    fromNodeGuid = connectingFromNodeGuid,
                    toNodeGuid = toNode.guid,
                    fromPortIndex = 0,
                    toPortIndex = 0
                };
                
                if (currentGraph.connections == null)
                {
                    currentGraph.connections = new List<UINodeConnection>();
                }
                
                currentGraph.connections.Add(connection);
                EditorUtility.SetDirty(currentGraph);
            }
            
            isConnecting = false;
            connectingFromNodeGuid = null;
        }
    }
}

