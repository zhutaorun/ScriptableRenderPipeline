using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphs;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Rendering;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using Node = UnityEditor.Graphs.Node;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    [Serializable]
    class FloatingWindowsLayout
    {
        public WindowDockingLayout previewLayout = new WindowDockingLayout();
        public WindowDockingLayout blackboardLayout = new WindowDockingLayout();
        public Vector2 masterPreviewSize = new Vector2(400, 400);
    }



    public class GraphEditorView : VisualElement, IDisposable
    {
        MaterialGraphView m_GraphView;
        MasterPreviewView m_MasterPreviewView;

        AbstractMaterialGraph m_Graph;
        PreviewManager m_PreviewManager;
        SearchWindowProvider m_SearchWindowProvider;
        EdgeConnectorListener m_EdgeConnectorListener;
        BlackboardProvider m_BlackboardProvider;

        System.Collections.Generic.List<MaterialGraphGroup> m_GraphGroups = new System.Collections.Generic.List<MaterialGraphGroup>();

        const string k_FloatingWindowsLayoutKey = "UnityEditor.ShaderGraph.FloatingWindowsLayout";
        FloatingWindowsLayout m_FloatingWindowsLayout;

        public Action saveRequested { get; set; }

        public Action convertToSubgraphRequested
        {
            get { return m_GraphView.onConvertToSubgraphClick; }
            set { m_GraphView.onConvertToSubgraphClick = value; }
        }

        public Action showInProjectRequested { get; set; }

        public MaterialGraphView graphView
        {
            get { return m_GraphView; }
        }

        PreviewManager previewManager
        {
            get { return m_PreviewManager; }
            set { m_PreviewManager = value; }
        }

        public string assetName
        {
            get { return m_BlackboardProvider.assetName; }
            set
            {
                m_BlackboardProvider.assetName = value;
                m_MasterPreviewView.assetName = value;
            }
        }

        public GraphEditorView(EditorWindow editorWindow, AbstractMaterialGraph graph)
        {
            m_Graph = graph;
            AddStyleSheetPath("Styles/GraphEditorView");
            previewManager = new PreviewManager(graph);

            string serializedWindowLayout = EditorUserSettings.GetConfigValue(k_FloatingWindowsLayoutKey);
            if (!string.IsNullOrEmpty(serializedWindowLayout))
            {
                m_FloatingWindowsLayout = JsonUtility.FromJson<FloatingWindowsLayout>(serializedWindowLayout);
                if (m_FloatingWindowsLayout.masterPreviewSize.x > 0f && m_FloatingWindowsLayout.masterPreviewSize.y > 0f)
                    previewManager.ResizeMasterPreview(m_FloatingWindowsLayout.masterPreviewSize);
            }

            previewManager.RenderPreviews();

            var toolbar = new IMGUIContainer(() =>
                {
                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
                    {
                        if (saveRequested != null)
                            saveRequested();
                    }
                    GUILayout.Space(6);
                    if (GUILayout.Button("Show In Project", EditorStyles.toolbarButton))
                    {
                        if (showInProjectRequested != null)
                            showInProjectRequested();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                });
            Add(toolbar);

            var content = new VisualElement { name = "content" };
            {
                m_GraphView = new MaterialGraphView(graph) { name = "GraphView", persistenceKey = "MaterialGraphView" };
                m_GraphView.SetupZoom(0.05f, ContentZoomer.DefaultMaxScale);
                m_GraphView.AddManipulator(new ContentDragger());
                m_GraphView.AddManipulator(new SelectionDragger());
                m_GraphView.AddManipulator(new RectangleSelector());
                m_GraphView.AddManipulator(new ClickSelector());
                m_GraphView.RegisterCallback<KeyDownEvent>(OnSpaceDown);
                m_GraphView.groupTitleChanged = OnGroupTitleChanged;
                m_GraphView.elementsAddedToGroup = OnElementsAddedToGroup;
                m_GraphView.elementsRemovedFromGroup = OnElementsRemovedFromGroup;
                content.Add(m_GraphView);

                m_BlackboardProvider = new BlackboardProvider(graph);
                m_GraphView.Add(m_BlackboardProvider.blackboard);
                Rect blackboardLayout = m_BlackboardProvider.blackboard.layout;
                blackboardLayout.x = 10f;
                blackboardLayout.y = 10f;
                m_BlackboardProvider.blackboard.layout = blackboardLayout;

                m_MasterPreviewView = new MasterPreviewView(previewManager, graph) { name = "masterPreview" };

                WindowDraggable masterPreviewViewDraggable = new WindowDraggable(null, this);
                m_MasterPreviewView.AddManipulator(masterPreviewViewDraggable);
                m_GraphView.Add(m_MasterPreviewView);

                //m_BlackboardProvider.onDragFinished += UpdateSerializedWindowLayout;
                //m_BlackboardProvider.onResizeFinished += UpdateSerializedWindowLayout;
                masterPreviewViewDraggable.OnDragFinished += UpdateSerializedWindowLayout;
                m_MasterPreviewView.previewResizeBorderFrame.OnResizeFinished += UpdateSerializedWindowLayout;

                m_GraphView.graphViewChanged = GraphViewChanged;

                RegisterCallback<GeometryChangedEvent>(ApplySerializewindowLayouts);
            }

            m_SearchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
            m_SearchWindowProvider.Initialize(editorWindow, m_Graph, m_GraphView);
            m_GraphView.nodeCreationRequest = (c) =>
                {
                    m_SearchWindowProvider.connectedPort = null;
                    SearchWindow.Open(new SearchWindowContext(c.screenMousePosition), m_SearchWindowProvider);
                };

            m_EdgeConnectorListener = new EdgeConnectorListener(m_Graph, m_SearchWindowProvider);

            foreach (var node in graph.GetNodes<INode>())
                AddNode(node);

            foreach (var graphGroup in graph.groups)
            {
                AddNewGroupNode(graphGroup);
            }
            // TODO Adding the groups here as well.

            foreach (var edge in graph.edges)
                AddEdge(edge);

            Add(content);
        }

        void OnSpaceDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.F1)
            {
                if (m_GraphView.selection.OfType<MaterialNodeView>().Count() == 1)
                {
                    var nodeView = (MaterialNodeView)graphView.selection.First();
                    if (nodeView.node.documentationURL != null)
                        System.Diagnostics.Process.Start(nodeView.node.documentationURL);
                }
            }
        }

        GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var leftSlot = edge.output.GetSlot();
                    var rightSlot = edge.input.GetSlot();
                    if (leftSlot != null && rightSlot != null)
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Connect Edge");
                        m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
                    }
                }
                graphViewChange.edgesToCreate.Clear();
            }

            if (graphViewChange.movedElements != null)
            {
                List<GraphElement> nodesInsideGroup = new List<GraphElement>();
                foreach (var element in graphViewChange.movedElements)
                {
                    var groupNode = element as MaterialGraphGroup;
                    if (groupNode == null)
                        continue;

                    foreach (GraphElement graphElement in groupNode.containedElements)
                    {
                        nodesInsideGroup.Add(graphElement);
                    }
                }

                if(nodesInsideGroup.Any())
                    graphViewChange.movedElements.AddRange(nodesInsideGroup);

                foreach (var element in graphViewChange.movedElements)
                {
                    var node = element.userData as INode;
                    if (node == null)
                        continue;

                    var drawState = node.drawState;
                    drawState.position = element.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, element.GetPosition());
                    node.drawState = drawState;
                }
            }

            var nodesToUpdate = m_NodeViewHashSet;
            nodesToUpdate.Clear();

            if (graphViewChange.elementsToRemove != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Remove Elements");
                m_Graph.RemoveElements(graphViewChange.elementsToRemove.OfType<MaterialNodeView>().Select(v => (INode)v.node),
                    graphViewChange.elementsToRemove.OfType<Edge>().Select(e => (IEdge)e.userData),
                    graphViewChange.elementsToRemove.OfType<MaterialGraphGroup>().Select(g => (GroupData)g.userData));
                foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                {
                    if (edge.input != null)
                    {
                        var materialNodeView = edge.input.node as MaterialNodeView;
                        if (materialNodeView != null)
                            nodesToUpdate.Add(materialNodeView);
                    }
                    if (edge.output != null)
                    {
                        var materialNodeView = edge.output.node as MaterialNodeView;
                        if (materialNodeView != null)
                            nodesToUpdate.Add(materialNodeView);
                    }
                }
            }

            foreach (var node in nodesToUpdate)
                node.UpdatePortInputVisibilities();

            UpdateEdgeColors(nodesToUpdate);

            return graphViewChange;
        }

        void OnGroupTitleChanged(Group graphGroup, string title)
        {
            var groupData = graphGroup.userData as GroupData;
            if (groupData != null)
            {
                groupData.title = graphGroup.title;
            }
        }

        void OnElementsAddedToGroup(Group graphGroup, IEnumerable<GraphElement> element)
        {
            var groupData = graphGroup.userData as GroupData;
            if (groupData != null)
            {
                var anyChanged = false;
                foreach (var materialNodeView in element.Select(e => e).OfType<MaterialNodeView>())
                {
                    if (materialNodeView.node.groupGuid != groupData.guid)
                    {
                        anyChanged = true;
                        break;
                    }
                }

                if (!anyChanged)
                    return;

                m_Graph.owner.RegisterCompleteObjectUndo(":: " + groupData.title);

                foreach (var materialNodeView in element.Select(e => e).OfType<MaterialNodeView>())
                {
                    m_Graph.SetNodeGroup(materialNodeView.node, groupData);
                    Debug.Log(materialNodeView.node.name);
                }
            }
        }

        void OnElementsRemovedFromGroup(Group graphGroup, IEnumerable<GraphElement> element)
        {
            //bool actuallyRemovedANode = false;
            var groupData = graphGroup.userData as GroupData;
            if (groupData != null)
            {
//                foreach (var materialNodeView in element.Select(e => e).OfType<MaterialNodeView>())
//                {
//                    if (materialNodeView.node != null)
//                    {
//                        //materialNodeView.node.groupGuid = Guid.Empty;
//                        m_Graph.SetNodeGroup(materialNodeView.node, null);
//                        //actuallyRemovedANode = true;
//                    }
//                }

                var anyChanged = false;
                foreach (var materialNodeView in element.Select(e => e).OfType<MaterialNodeView>())
                {
                    if (materialNodeView.node.groupGuid == groupData.guid)
                    {
                        anyChanged = true;
                        break;
                    }
                }

                if (!anyChanged)
                    return;

                m_Graph.owner.RegisterCompleteObjectUndo("Removing from node :: " + groupData.title);

                foreach (var materialNodeView in element.Select(e => e).OfType<MaterialNodeView>())
                {
                    m_Graph.SetNodeGroup(materialNodeView.node, null);
                }
            }

            // Check if there are actually any empty groups before calling the Undo method
            //m_Graph.owner.RegisterCompleteObjectUndo("Deleting Empty Group Node");
            //if(actuallyRemovedANode)
            //    RemoveEmptyGroups();
        }

        void OnNodeChanged(INode inNode, ModificationScope scope)
        {
            if (m_GraphView == null)
                return;

            var dependentNodes = new System.Collections.Generic.List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theViews = m_GraphView.nodes.ToList().OfType<MaterialNodeView>();
                var viewsFound = theViews.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in viewsFound)
                    drawableNodeData.OnModified(scope);
            }
        }


        public void RemoveEmptyGroups()
        {
            foreach (GraphElement element in graphView.graphElements.ToList())
            {
                Group group = element as Group;

                if (group != null && !group.containedElements.Any())
                {
                    graphView.RemoveElement(group);
                }
            }
        }

        HashSet<MaterialNodeView> m_NodeViewHashSet = new HashSet<MaterialNodeView>();
        HashSet<Guid> m_PotentialEmptyGroupNodes = new HashSet<Guid>();
        public void HandleGraphChanges()
        {

            previewManager.HandleGraphChanges();
            previewManager.RenderPreviews();
            m_BlackboardProvider.HandleGraphChanges();


            // Group Handling
            foreach (GroupData groupData in m_Graph.removedGroups)
            {
                // Need to get all the Groups in the scene
                var groups = m_GraphView.graphElements.ToList().OfType<MaterialGraphGroup>();
                foreach (var group in groups)
                {
                    if (group.userData == groupData)
                    {
                        System.Collections.Generic.List<INode> removeNodes = new System.Collections.Generic.List<INode>();
                        foreach (GraphElement element in group.containedElements)
                        {
                            //Debug.Log("DELETING NODE::: " + (element.userData as INode).name);
                            var node = element.userData as INode;
                            if (node == null)
                                continue;
                            removeNodes.Add(node);
                            // TODO: this doesnt work, when undoing due to temp id and previewmanager
                            Debug.Log("Removing::" + node.name);
                        }

                        m_GraphView.RemoveElement(group);
                    }
                }
            }

            //RemoveEmptyGroups();
            foreach (var node in m_Graph.removedNodes)
            {
                //m_GraphView.selection.Clear();
                node.UnregisterCallback(OnNodeChanged);
                var nodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(p => p.node != null && p.node.guid == node.guid);
                if (nodeView != null)
                {
                    nodeView.Dispose();
                    nodeView.userData = null;
                    m_GraphView.RemoveElement(nodeView);
                }
            }

            foreach (var node in m_Graph.addedNodes)
            {
                Debug.Log("ADDING::" + node.name);
                AddNode(node);
            }



            foreach (var groupNodeStruct in m_Graph.groupNodeStruct)
            {
                var nodeView = (MaterialNodeView)m_GraphView.GetNodeByGuid(groupNodeStruct.nodeGuid.ToString());
                if (nodeView != null)
                {
                    nodeView.node.groupGuid = groupNodeStruct.newGroupGuid;
                }

                if (groupNodeStruct.oldGroupGuid != Guid.Empty)
                {
                    Debug.Log("OLD ID:: " + groupNodeStruct.oldGroupGuid);
                    m_PotentialEmptyGroupNodes.Add(groupNodeStruct.oldGroupGuid);
                }
                //materialNodeView.node.groupGuid = groupData.guid;

            }



            foreach (var guid in m_PotentialEmptyGroupNodes)
            {
                //var node = m_GraphView.graphElements.ToList().FirstOrDefault((e => e.persistenceKey == guid.ToString()));

                //var node = m_GraphView.GetElementByGuid(guid.ToString());
                //var nod = m_GraphView.GetNodeByGuid(guid.ToString());

                List<GraphElement> groups = m_GraphView.graphElements.ToList().FindAll(p => p.userData is GroupData);
                foreach (GraphElement element in groups)
                {
                    Group group = element as Group;
                    if (group != null)
                    {
                        GroupData groupData = (GroupData)group.userData;
                        if(groupData.guid == guid && !group.containedElements.Any())
                        {
                            graphView.RemoveElement(group);
                        }
                    }
                }
            }
            m_PotentialEmptyGroupNodes.Clear();

            foreach (GroupData groupData in m_Graph.addedGroups)
            {
                AddNewGroupNode(groupData);
            }

            foreach (var node in m_Graph.pastedNodes)
            {
                var nodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(p => p.node != null && p.node.guid == node.guid);
                m_GraphView.AddToSelection(nodeView);
            }

            var nodesToUpdate = m_NodeViewHashSet;
            nodesToUpdate.Clear();

            foreach (var edge in m_Graph.removedEdges)
            {
                var edgeView = m_GraphView.graphElements.ToList().OfType<Edge>().FirstOrDefault(p => p.userData is IEdge && Equals((IEdge)p.userData, edge));
                if (edgeView != null)
                {
                    var nodeView = edgeView.input.node as MaterialNodeView;
                    if (nodeView != null && nodeView.node != null)
                    {
                        nodesToUpdate.Add(nodeView);
                    }
                    edgeView.output.Disconnect(edgeView);
                    edgeView.input.Disconnect(edgeView);

                    edgeView.output = null;
                    edgeView.input = null;

                    m_GraphView.RemoveElement(edgeView);
                }
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var edgeView = AddEdge(edge);
                if (edgeView != null)
                    nodesToUpdate.Add((MaterialNodeView)edgeView.input.node);
            }

            foreach (var node in nodesToUpdate)
                node.UpdatePortInputVisibilities();

            UpdateEdgeColors(nodesToUpdate);

        }

        void AddNode(INode node)
        {
            var nodeView = new MaterialNodeView { userData = node };

            m_GraphView.AddElement(nodeView);
            nodeView.Initialize(node as AbstractMaterialNode, m_PreviewManager, m_EdgeConnectorListener);
            node.RegisterCallback(OnNodeChanged);

            nodeView.MarkDirtyRepaint();

            if (m_SearchWindowProvider.nodeNeedsRepositioning && m_SearchWindowProvider.targetSlotReference.nodeGuid.Equals(node.guid))
            {
                m_SearchWindowProvider.nodeNeedsRepositioning = false;
                foreach (var element in nodeView.inputContainer.Union(nodeView.outputContainer))
                {
                    var port = element as ShaderPort;
                    if (port == null)
                        continue;
                    if (port.slot.slotReference.Equals(m_SearchWindowProvider.targetSlotReference))
                    {
                        port.RegisterCallback<GeometryChangedEvent>(RepositionNode);
                        return;
                    }
                }
            }
        }

        void AddNewGroupNode(GroupData groupData)
        {
            MaterialGraphGroup graphGroup = new MaterialGraphGroup();

            graphGroup.userData = groupData;
//            graphGroup.SetPosition(new Rect(groupData.position.x, groupData.position.y, 100, 100));
            graphGroup.title = groupData.title;

            m_GraphView.AddElement(graphGroup);
            m_GraphGroups.Add(graphGroup);

            // Need to figure out why I used this one before
            List<MaterialNodeView> allNodesInThisGroup = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().Where(x => x.node.groupGuid == groupData.guid).ToList();
//            MaterialGraphGroup graphGroup = m_GraphView.graphElements.ToList().OfType<MaterialGraphGroup>().ToList().First(g => g.userData == groupData);

            foreach (MaterialNodeView materialNodeView in allNodesInThisGroup)
            {
                graphGroup.AddElement(materialNodeView);
            }
        }

        static void RepositionNode(GeometryChangedEvent evt)
        {
            var port = evt.target as ShaderPort;
            if (port == null)
                return;
            port.UnregisterCallback<GeometryChangedEvent>(RepositionNode);
            var nodeView = port.node as MaterialNodeView;
            if (nodeView == null)
                return;
            var offset = nodeView.mainContainer.WorldToLocal(port.GetGlobalCenter() + new Vector3(3f, 3f, 0f));
            var position = nodeView.GetPosition();
            position.position -= offset;
            nodeView.SetPosition(position);
            var drawState = nodeView.node.drawState;
            drawState.position = position;
            nodeView.node.drawState = drawState;
            nodeView.MarkDirtyRepaint();
            port.MarkDirtyRepaint();
        }

        Edge AddEdge(IEdge edge)
        {
            var sourceNode = m_Graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            if (sourceNode == null)
            {
                Debug.LogWarning("Source node is null");
                return null;
            }
            var sourceSlot = sourceNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);

            var targetNode = m_Graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            if (targetNode == null)
            {
                Debug.LogWarning("Target node is null");
                return null;
            }
            var targetSlot = targetNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);

            var sourceNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == sourceNode);
            if (sourceNodeView != null)
            {
                var sourceAnchor = sourceNodeView.outputContainer.Children().OfType<ShaderPort>().FirstOrDefault(x => x.slot.Equals(sourceSlot));

                var targetNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == targetNode);
                var targetAnchor = targetNodeView.inputContainer.Children().OfType<ShaderPort>().FirstOrDefault(x => x.slot.Equals(targetSlot));

                var edgeView = new Edge
                {
                    userData = edge,
                    output = sourceAnchor,
                    input = targetAnchor
                };
                edgeView.output.Connect(edgeView);
                edgeView.input.Connect(edgeView);
                m_GraphView.AddElement(edgeView);
                sourceNodeView.RefreshPorts();
                targetNodeView.RefreshPorts();
                sourceNodeView.UpdatePortInputTypes();
                targetNodeView.UpdatePortInputTypes();

                return edgeView;
            }

            return null;
        }

        Stack<MaterialNodeView> m_NodeStack = new Stack<MaterialNodeView>();

        void UpdateEdgeColors(HashSet<MaterialNodeView> nodeViews)
        {
            var nodeStack = m_NodeStack;
            nodeStack.Clear();
            foreach (var nodeView in nodeViews)
                nodeStack.Push(nodeView);
            while (nodeStack.Any())
            {
                var nodeView = nodeStack.Pop();
                nodeView.UpdatePortInputTypes();
                foreach (var anchorView in nodeView.outputContainer.Children().OfType<Port>())
                {
                    foreach (var edgeView in anchorView.connections.OfType<Edge>())
                    {
                        var targetSlot = edgeView.input.GetSlot();
                        if (targetSlot.valueType == SlotValueType.DynamicVector || targetSlot.valueType == SlotValueType.DynamicMatrix || targetSlot.valueType == SlotValueType.Dynamic)
                        {
                            var connectedNodeView = edgeView.input.node as MaterialNodeView;
                            if (connectedNodeView != null && !nodeViews.Contains(connectedNodeView))
                            {
                                nodeStack.Push(connectedNodeView);
                                nodeViews.Add(connectedNodeView);
                            }
                        }
                    }
                }
                foreach (var anchorView in nodeView.inputContainer.Children().OfType<Port>())
                {
                    var targetSlot = anchorView.GetSlot();
                    if (targetSlot.valueType != SlotValueType.DynamicVector)
                        continue;
                    foreach (var edgeView in anchorView.connections.OfType<Edge>())
                    {
                        var connectedNodeView = edgeView.output.node as MaterialNodeView;
                        if (connectedNodeView != null && !nodeViews.Contains(connectedNodeView))
                        {
                            nodeStack.Push(connectedNodeView);
                            nodeViews.Add(connectedNodeView);
                        }
                    }
                }
            }
        }

        void HandleEditorViewChanged(GeometryChangedEvent evt)
        {
            m_BlackboardProvider.blackboard.layout = m_FloatingWindowsLayout.blackboardLayout.GetLayout(m_GraphView.layout);
        }

        void StoreBlackboardLayoutOnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateSerializedWindowLayout();
        }

        void ApplySerializewindowLayouts(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(ApplySerializewindowLayouts);

            if (m_FloatingWindowsLayout != null)
            {
                // Restore master preview layout
                m_FloatingWindowsLayout.previewLayout.ApplyPosition(m_MasterPreviewView);
                m_MasterPreviewView.previewTextureView.style.width = StyleValue<float>.Create(m_FloatingWindowsLayout.masterPreviewSize.x);
                m_MasterPreviewView.previewTextureView.style.height = StyleValue<float>.Create(m_FloatingWindowsLayout.masterPreviewSize.y);

                // Restore blackboard layout, and make sure that it remains in the view.
                Rect blackboardRect = m_FloatingWindowsLayout.blackboardLayout.GetLayout(this.layout);

                // Make sure the dimensions are sufficiently large.
                blackboardRect.width = Mathf.Clamp(blackboardRect.width, 160f, m_GraphView.contentContainer.layout.width);
                blackboardRect.height = Mathf.Clamp(blackboardRect.height, 160f, m_GraphView.contentContainer.layout.height);

                // Make sure that the positionining is on screen.
                blackboardRect.x = Mathf.Clamp(blackboardRect.x, 0f, Mathf.Max(1f, m_GraphView.contentContainer.layout.width - blackboardRect.width - blackboardRect.width));
                blackboardRect.y = Mathf.Clamp(blackboardRect.y, 0f, Mathf.Max(1f, m_GraphView.contentContainer.layout.height - blackboardRect.height - blackboardRect.height));

                // Set the processed blackboard layout.
                m_BlackboardProvider.blackboard.layout = blackboardRect;

                previewManager.ResizeMasterPreview(m_FloatingWindowsLayout.masterPreviewSize);
            }
            else
            {
                m_FloatingWindowsLayout = new FloatingWindowsLayout();
            }

            // After the layout is restored from the previous session, start tracking layout changes in the blackboard.
            m_BlackboardProvider.blackboard.RegisterCallback<GeometryChangedEvent>(StoreBlackboardLayoutOnGeometryChanged);

            // After the layout is restored, track changes in layout and make the blackboard have the same behavior as the preview w.r.t. docking.
            RegisterCallback<GeometryChangedEvent>(HandleEditorViewChanged);
        }

        void UpdateSerializedWindowLayout()
        {
            if (m_FloatingWindowsLayout == null)
                m_FloatingWindowsLayout = new FloatingWindowsLayout();

            m_FloatingWindowsLayout.previewLayout.CalculateDockingCornerAndOffset(m_MasterPreviewView.layout, m_GraphView.layout);
            m_FloatingWindowsLayout.previewLayout.ClampToParentWindow();

            m_FloatingWindowsLayout.blackboardLayout.CalculateDockingCornerAndOffset(m_BlackboardProvider.blackboard.layout, m_GraphView.layout);
            m_FloatingWindowsLayout.blackboardLayout.ClampToParentWindow();

            if (m_MasterPreviewView.expanded)
            {
                m_FloatingWindowsLayout.masterPreviewSize = m_MasterPreviewView.previewTextureView.layout.size;
            }

            string serializedWindowLayout = JsonUtility.ToJson(m_FloatingWindowsLayout);
            EditorUserSettings.SetConfigValue(k_FloatingWindowsLayoutKey, serializedWindowLayout);
        }

        public void Dispose()
        {
            if (m_GraphView != null)
            {
                saveRequested = null;
                convertToSubgraphRequested = null;
                showInProjectRequested = null;
                foreach (var node in m_GraphView.Children().OfType<MaterialNodeView>())
                    node.Dispose();
                m_GraphView = null;
            }
            if (previewManager != null)
            {
                previewManager.Dispose();
                previewManager = null;
            }
            if (m_SearchWindowProvider != null)
            {
                Object.DestroyImmediate(m_SearchWindowProvider);
                m_SearchWindowProvider = null;
            }
        }
    }
}
