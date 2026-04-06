using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseNode : Node
{
    public BaseNode() { }
    public BaseNode(NodeArguments args)
    {
        GUID = Guid.NewGuid().ToString();
        title = NodeName;
        AddFields();
        Finalize(args.position, args.size);
        LoadStyleSheet();
    }

    public static event EventHandler<DialogNodeEventArgs> OnRemovePortFromNode = delegate { };
    protected virtual int MaxInputNumber => 1;
    protected virtual int MaxOutputNumber => -1; // Maximum number of outputs this Node can have 
                                                // -1 means unlimited number of ports
    protected virtual string NodeName => string.Empty; // Default name of the node
    protected virtual string StyleSheetName => string.Empty; // Name of the Stylesheet (without extension) for this node
    protected const string NEXT_KEY = "Next";
    protected const string DEFAULT_CONNECTOR_NAME = "connector";
    protected const string DEFAULT_IO_LABEL = "type";
    protected const string INPUT_LABEL_NAME = "myInput";
    protected const string OUTPUT_LABEL_NAME = "myOutput";

    protected void LoadStyleSheet()
    {
        if(!string.IsNullOrEmpty(StyleSheetName))
        {
            StyleSheet styleSheet = Resources.Load<StyleSheet>(StyleSheetName);
            if(styleSheet)
            {
                styleSheets.Add(styleSheet);
            }
        }
    }

    public string GUID; // unique id to distinguish between nodes
    public bool IsEntryPoint = false;

    protected virtual void AddFields() {}

    public DialogInputPort AddInputPort(string portName = "")
    {
        int outputPortCount = outputContainer.Query(DEFAULT_CONNECTOR_NAME).ToList().Count; 
        if(MaxInputNumber != -1 && outputPortCount >= MaxInputNumber)
        {
            // Debug.Log("Cannot add more ports to this node - Max ports: " + MaxOutputNumber);
            return null;
        }

        DialogInputPort port = InstantiatePort(Direction.Input) as DialogInputPort;
        port.AddToClassList("input-port");
        var label = port.contentContainer.Q<Label>(DEFAULT_IO_LABEL);
        label.name = INPUT_LABEL_NAME;
        label.AddToClassList("input-label");
        port.portName = "In";
        
        inputContainer.Add(port);
        return port;
    }

    public virtual DialogOutputPort AddOutputPort(string portName = "")
    {
        // how many outputs are we at on this node
        int outputPortCount = outputContainer.Query(DEFAULT_CONNECTOR_NAME).ToList().Count; 
        if(MaxOutputNumber != -1 && outputPortCount >= MaxOutputNumber)
        {
            return null;
        }

        DialogOutputPort port = (DialogOutputPort)InstantiatePort(Direction.Output);
        port.AddToClassList("output-port");
        var label = port.contentContainer.Q<Label>(DEFAULT_IO_LABEL);
        label.name = OUTPUT_LABEL_NAME;
        label.AddToClassList("output-label");
        port.portName = string.Empty.Equals(portName) ? "Out" : portName;
        outputContainer.Add(port);
        return port;
    }

    protected void RemovePort(Port outputPort)
    {
        OnRemovePortFromNode(this, new DialogNodeEventArgs(outputPort));
        outputContainer.Remove(outputPort);
        RefreshPorts();
        RefreshExpandedState();
    }

    public Port InstantiatePort(Direction direction)
    {
        return direction == Direction.Input ? new DialogInputPort() 
                                            : new DialogOutputPort();
    }

    private void Finalize(Vector2 mousePosition, Vector2 size)
    {
        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(mousePosition, size));
    }

    public virtual NodeData SaveAsNodeData() 
    {
        Debug.LogError($"SaveAsNodeData is not implemented for {name}"); 
        return null; 
    }

    public void SavePortNumbers() 
    {
        List<VisualElement> inputs = inputContainer.Children().ToList();
        for(int i = 0; i < inputContainer.childCount; i++)
        {
            (inputs[i] as DialogInputPort).PortNumber = i;
        }

        List<VisualElement> outputs = outputContainer.Children().ToList();
        for(int i = 0; i < outputContainer.childCount; i++)
        {
            (outputs[i] as DialogOutputPort).PortNumber = i;
        }


    }
}
