using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GoToGraphNode : BaseNode
{
    public GoToGraphNode() { }
    public GoToGraphNode(NodeArguments args) : base(args) 
    {
        GraphName = args.name;
    }

    protected override string NodeName => "Go To Graph";
    protected override string StyleSheetName => "GoToGraphNode";
    protected override int MaxOutputNumber => 1;

    public DialogContainer Graph { get; private set; }
    private TextField _graphNameField;
    private string _graphName;
    public string GraphName
    {
        get => _graphName;
        set
        {
            _graphName = value;
            if(_graphNameField != null)
            {
                _graphNameField.value = value;
            }

            if(!string.IsNullOrEmpty(_graphName))
            {
                string pathToGraph = $"Assets/Dialog Resources/Resources/NPCs/{_graphName}.asset";
                Graph = (DialogContainer)AssetDatabase.LoadAssetAtPath(pathToGraph, typeof(DialogContainer));
                if(Graph != null)
                {
                    title = "Go To graph: " + Graph.name;
                }
                else
                {
                    title = "Go To graph: graph not found";
                }
            }
        }
    }

    protected override void AddFields()
    {
        AddInputPort();
        AddOutputPort();
        AddGraphNameTextBox();
    }

    private void AddGraphNameTextBox()
    {
        _graphNameField = new TextField("Graph Name");
        _graphNameField.RegisterValueChangedCallback(evt =>
        {
            GraphName = evt.newValue;
        });
        _graphNameField.multiline = true;
        _graphNameField.SetValueWithoutNotify(title);
        mainContainer.Add(_graphNameField);
    }

    public override NodeData SaveAsNodeData()
    {
        GoToGraphData data = ScriptableObject.CreateInstance<GoToGraphData>();
        data.GraphName = GraphName;
        return data;
    }
}
