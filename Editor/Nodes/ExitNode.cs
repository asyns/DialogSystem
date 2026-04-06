using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ExitNode : BaseNode
{
    public ExitNode() { }
    public ExitNode(NodeArguments args) : base(args) { }
    protected override string NodeName => EXIT_NODE_NAME;
    public const string EXIT_NODE_NAME = "Exit";
    protected override string StyleSheetName => "ExitNode";
    protected override int MaxOutputNumber => 0;
    protected override void AddFields()
    {
        AddInputPort();
    }

    public override NodeData SaveAsNodeData()
    {
        return ScriptableObject.CreateInstance<ExitNodeData>();
    }
}
