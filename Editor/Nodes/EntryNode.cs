using System;
using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class EntryNode : BaseNode
{
    public EntryNode(NodeArguments args) : base(args) 
    {
        IsEntryPoint = true;
    }

    public const string ENTRY_NODE_NAME = "Entry";
    protected override string NodeName => ENTRY_NODE_NAME;
    protected override int MaxOutputNumber => 1;

    protected override void AddFields()
    {
        capabilities &= ~Capabilities.Deletable;
        AddOutputPort(NEXT_KEY);
    }

    protected override string StyleSheetName => "EntryNode";

    public override NodeData SaveAsNodeData()
    {
        return ScriptableObject.CreateInstance<EntryNodeData>();
    }
}

