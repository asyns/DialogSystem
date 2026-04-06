using UnityEngine;
using UnityEngine.UIElements;

public class ChoiceNode : BaseNode
{
    public ChoiceNode() { }
    public ChoiceNode(NodeArguments args) : base(args) { }

    protected override string NodeName => "Choice";
    protected override string StyleSheetName => "ChoiceNode";

    protected override void AddFields()
    {
        AddInputPort();
        AddNewChoiceButton();
    }

    public override DialogOutputPort AddOutputPort(string portName = "")
    {
        int outputPortCount = outputContainer.Query(DEFAULT_CONNECTOR_NAME).ToList().Count; // how many outputs are we at on this node
        string outputPortName = string.IsNullOrEmpty(portName) ? $"Choice {outputPortCount}" : portName;
        
        DialogOutputPort port = base.AddOutputPort(outputPortName);

        var oldLabel = port.contentContainer.Q<Label>(OUTPUT_LABEL_NAME);
        port.contentContainer.Remove(oldLabel);
        
        port.AddDeletePortButton((o) => RemovePort(o));
        port.AddAnswerField(outputPortName);
        
        RefreshExpandedState();
        RefreshPorts();

        return port;
    }

    private void AddNewChoiceButton()
    {
        Button button = new Button(() => AddOutputPort())
        {
            text = "+"
        };
        button.AddToClassList("add-button");
        titleContainer.Add(button);
    }

    public override NodeData SaveAsNodeData()
    {
        return ScriptableObject.CreateInstance<ChoiceNodeData>();
    }
}
