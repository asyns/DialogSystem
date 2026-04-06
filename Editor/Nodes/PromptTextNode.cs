using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PromptTextNode : BaseNode
{
    public PromptTextNode() { }
    public PromptTextNode(NodeArguments args) : base(args) 
    {
        Prompt = args.name;
    }
    public string Prompt { get; set; }
    protected override string NodeName => "Prompt";
    protected override string StyleSheetName => "PromptNode";
    protected override int MaxOutputNumber => 2;

    private const string SUCCESS_KEY = "success";
    private const string ERROR_KEY = "error";

    protected override void AddFields()
    {
        AddInputPort();
        AddOutputPort(SUCCESS_KEY);
        AddOutputPort(ERROR_KEY);
    }

    public override NodeData SaveAsNodeData()
    {
        PromptNodeData data = ScriptableObject.CreateInstance<PromptNodeData>();
        return data;
    }
}
