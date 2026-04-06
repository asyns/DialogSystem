using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialogGraphView _targetGraphView;
    private DialogContainer _containerCache;
    private List<ScriptableObject> createdObjects;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<BaseNode> Nodes => _targetGraphView.nodes.ToList().Cast<BaseNode>().ToList();

    public static GraphSaveUtility GetInstance(DialogGraphView targetGraphView)
    {
        return new GraphSaveUtility
        {
            _targetGraphView = targetGraphView
        };
    }

    public void SaveGraph(string fileName)
    {
        List<Edge> edges = Edges.Where(x => x.output.node.title == EntryNode.ENTRY_NODE_NAME).ToList();

        var entryPoint = Edges.Find(x => x.output.node.title == EntryNode.ENTRY_NODE_NAME);
        if(entryPoint == null)
        {
            EditorUtility.DisplayDialog("Error", "Start Node must be connected before saving", "OK");
            return;
        }
        
        string path = $"Assets/Dialog Resources/Resources/NPCs/{fileName}.asset";

        var dialogContainer = ScriptableObject.CreateInstance<DialogContainer>();
        createdObjects = new List<ScriptableObject>();

        DialogContainer dialogAsset = AssetDatabase.LoadAssetAtPath(path, typeof(DialogContainer)) as DialogContainer;
        if(dialogAsset != null)
        {
            //asset at this path already exists
            //remove children objects and readd updated ones instead of recreating the entire scriptable object asset,
            //in order to preserve dialogcontainer properties which are set in the editor
            EditorUtility.SetDirty(dialogAsset); //set dirty to modify
            dialogAsset.NodeLinks.Clear();
            dialogAsset.NodeData.Clear();
            dialogAsset.ExposedProperties.Clear();
            
            UnityEngine.Object[] children = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            foreach(var child in children)
            {
                AssetDatabase.RemoveObjectFromAsset(child);
            }
            if (!SaveNodes(dialogAsset)) return;
            SaveExposedProperties(dialogAsset);
        }
        else
        {
            if (!SaveNodes(dialogContainer)) return;
            SaveExposedProperties(dialogContainer);

            //asset doesn' exist, create new
            AssetDatabase.CreateAsset(dialogContainer, path);
        }

        SaveAllScriptableObjects(path);
        AssetDatabase.SaveAssets();
        Debug.Log("Graph successfully saved!");
    }

    private void SaveAllScriptableObjects(string path)
    {
        foreach(ScriptableObject obj in createdObjects)
        {
            obj.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(obj, path);
        }
    }

    private void SaveExposedProperties(DialogContainer dialogContainer)
    {
        dialogContainer.ExposedProperties.AddRange(_targetGraphView.exposedProperties);
    }

    private bool SaveNodes(DialogContainer dialogContainer)
    {
        if (!Edges.Any())
        {
            return false;
        }

        var connected = Edges.Where(x => x.input.node != null).ToArray();
        Nodes.ForEach(node => node.SavePortNumbers());
        for (int i = 0; i < connected.Length; i++)
        {
            var outputNode = connected[i].output.node as BaseNode;
            var inputNode = connected[i].input.node as BaseNode;

            NodeLinkData nodeLinkData = ScriptableObject.CreateInstance<NodeLinkData>();
            nodeLinkData.PortName = connected[i].output.portName;
            nodeLinkData.BaseNodeGuid = outputNode.GUID;
            nodeLinkData.TargetNodeGuid = inputNode.GUID;
            nodeLinkData.PortNumber = (connected[i].output as DialogOutputPort).PortNumber;
            Debug.Log($"{nodeLinkData.PortName} is port {nodeLinkData.PortNumber}");

            createdObjects.Add(nodeLinkData);
            dialogContainer.NodeLinks.Add(nodeLinkData);
        }

        foreach (var dialogNode in Nodes)
        {
            NodeData dataInstance = MakeNodeData(dialogNode);
            createdObjects.Add(dataInstance);
            dialogContainer.NodeData.Add(dataInstance);
        }
        return true;
    }

    // Create NodeData instances when saving
    private NodeData MakeNodeData(BaseNode node)
    {
        NodeData data = node.SaveAsNodeData();
        if(data != null)
        {
            data.Guid = node.GUID;
            data.rectPos = node.GetPosition();
        }
        return data;
    }

    public void LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<DialogContainer>("NPCs/"+fileName);

        if (_containerCache == null)
        {
            EditorUtility.DisplayDialog("File not found", "Target graph file does not exist. Make sure it is in the NPCs folder (inside a Resources folder)", "OK");
            return;
        }
        else
        {
            Debug.Log("Loaded " + fileName);
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
        // CreateExposedProperties();
    }

    private void CreateExposedProperties()
    {
        //Clear existing proeprties on hot reload
        _targetGraphView.ClearBlackboardAndProperties();   

        //Add properties from data
        foreach(var exposedProperty in _containerCache.ExposedProperties)
        {
            _targetGraphView.AddPropertyToBlackboard(exposedProperty);
        }
    }

    public void DeleteGraph(string fileName)
    {
        AssetDatabase.DeleteAsset($"Assets/Dialog Resources/Resources/NPCs/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }

    private void DebugConnections()
    {
        for(int i = 0; i < Nodes.Count; i++)
        {
            var connections = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[i].GUID).ToList();
            Debug.Log($"Looking at node {Nodes[i].title} with guid {Nodes[i].GUID}");
            for(var j = 0; j < connections.Count; j++)
            {
                var targetNodeGuid = connections[j].TargetNodeGuid;
                var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);

                Debug.Log($"Make connection from " + Nodes[i].title + " to " + targetNode.title);
            }
        }
    }

    private void ConnectNodes()
    {
        for(int i = 0; i < Nodes.Count; i++)
        {
            var connections = _containerCache.NodeLinks
                                .Where(x => x.BaseNodeGuid == Nodes[i].GUID)
                                .OrderBy(x => x.PortNumber)
                                .ToList();
            
            for(var j = 0; j < connections.Count; j++)
            {
                if(i >= Nodes.Count)
                {
                    Debug.LogError($"i ({i}) is not in bounds, there are {Nodes.Count} nodes");
                    continue;
                }
                if(j >= connections.Count)
                {
                    Debug.LogError($"j ({j}) is not in bounds, there are {connections.Count} connections");
                    continue;
                }

                if(j >= Nodes[i].outputContainer.childCount)
                {
                    Debug.LogError($"Trying to make more than one edge out of this output , which is not allowed (there are {Nodes[i].outputContainer.childCount} outputs)");
                    continue;
                }


                VisualElement outputContainer = Nodes[i].outputContainer[j];
                var targetNodeGuid = connections[j].TargetNodeGuid;
                var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);

                if(targetNode.inputContainer.childCount > 0)
                {
                    LinkNodes(outputContainer.Q<Port>(), (Port)targetNode.inputContainer[0]);
                }
            }
        }
    }

    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge
        {
            output = output,
            input = input
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);

        _targetGraphView.Add(tempEdge);
    }

    private void CreateNodes()
    {
        foreach(var nodeData in _containerCache.NodeData)
        {
            BaseNode tempNode = _targetGraphView.NodeFactory.CreateNode(nodeData);
            if(tempNode == null) 
            {
                Debug.LogError("Cannot Load undefined node ");
                continue;
            }

            tempNode.GUID = nodeData.Guid;
            _targetGraphView.AddElement(tempNode);

            var nodePorts = _containerCache.NodeLinks
                                    .Where(x => x.BaseNodeGuid == nodeData.Guid)
                                    .OrderBy(x => x.PortNumber)
                                    .ToList();
            nodePorts.ForEach(x => tempNode.AddOutputPort(x.PortName));            
        }
    }

    private void ClearGraph()
    {
        foreach(var node in Nodes)
        {
            Edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));
            _targetGraphView.RemoveElement(node);
        }
    }
}
