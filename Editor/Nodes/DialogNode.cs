using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


public class DialogNode : BaseNode
{
    public DialogNode() { }
    public DialogNode(NodeArguments args) : base(args) 
    { 
        DialogText = args.name;
    }

    protected override int MaxOutputNumber => 1;
    protected override string NodeName => "Dialogue";
    protected override string StyleSheetName => "DialogNode";
    
    private TextField _dialogTextField;
    private string _dialogText;
    public string DialogText
    {
        get => _dialogText;
        set
        {
            _dialogText = value;
            title = value;
            if(_dialogTextField != null)
            {
                _dialogTextField.value = value;
            }
        }
    }

    protected override void AddFields()
    {
        AddInputPort();

        AddDialogTextBox();
        
        AddOutputPort();
    }

    public override NodeData SaveAsNodeData()
    {
        DialogNodeData data = ScriptableObject.CreateInstance<DialogNodeData>();
        data.DialogText = DialogText;
        return data;
    }

    private void AddDialogTextBox()
    {
        _dialogTextField = new TextField("Text");
        _dialogTextField.RegisterValueChangedCallback(evt =>
        {
            DialogText = evt.newValue;
        });
        _dialogTextField.multiline = true;
        _dialogTextField.SetValueWithoutNotify(title);
        mainContainer.Add(_dialogTextField);
    }

    public override DialogOutputPort AddOutputPort(string portName = "")
    {
        DialogOutputPort port = base.AddOutputPort(portName);
        if(port == null)
        {
            return null;
        }

        var oldLabel = port.contentContainer.Q<Label>(OUTPUT_LABEL_NAME);
        // port.contentContainer.Remove(oldLabel);

        RefreshExpandedState();
        RefreshPorts();

        return port;
    }
}


public class DialogOutputPort : Port
{
    private const string VALUE_LABEL = "Value";
    private const string ANSWER_LABEL = "Answer";
    private const string X_LABEL = "X";
    public int PortNumber { get; set; }

    public DialogOutputPort() : base(Orientation.Horizontal, Direction.Output, Capacity.Single, typeof(float))
    {
        portName = "Out";

        DefaultEdgeConnectorListenerCopy listener = new DefaultEdgeConnectorListenerCopy(); 
        m_EdgeConnector = new EdgeConnector<Edge>(listener);
        this.AddManipulator(m_EdgeConnector);
    }

    public void AddDeletePortButton(Action<Port> onClick)
    {
        Button deleteButton = new Button(() => onClick.Invoke(this)) { text = X_LABEL };
        contentContainer.Add(deleteButton);
    }

    public void AddAnswerField(string outputPortName)
    {
        TextField textField = new TextField
        {
            name = string.Empty,
            value = outputPortName
        };
        textField.RegisterValueChangedCallback(evt => portName = evt.newValue);
        contentContainer.Add(textField);
        contentContainer.Add(new Label(ANSWER_LABEL));
    }

    public void AddValueField(string answerValue)
    {
        TextField textFieldValue = new TextField
        {
            name = string.Empty,
            value = answerValue
        };
        textFieldValue.RegisterValueChangedCallback(evt => userData = evt.newValue);
        contentContainer.Add(textFieldValue);
        contentContainer.Add(new Label(VALUE_LABEL));
    }
}

public class DialogInputPort : Port
{
    public DialogInputPort() : base(Orientation.Horizontal, Direction.Input, Capacity.Multi, typeof(float))
    {
        portName = "In";

        DefaultEdgeConnectorListenerCopy listener = new DefaultEdgeConnectorListenerCopy();
        m_EdgeConnector = new EdgeConnector<Edge>(listener);
        this.AddManipulator(m_EdgeConnector);
    }

    public int PortNumber { get; set; }
}

public class DialogNodeEventArgs : EventArgs
{
    public Port OutputPort { get; private set; }
    public DialogNodeEventArgs(Port outputPort)
    {
        OutputPort = outputPort;
    }
}

/// <summary>
/// This is a pure copy of the DefaultEdgeConnectorListener class that's _inside_ of Port, but since it's private
/// I can't use it directly (to override their InstantiatePort which does Port.Create which uses DefaultEdgeConnectorListener) so we're gonna use this one instead smh
/// ...which means we don't actually need to override anything
/// </summary>
public class DefaultEdgeConnectorListenerCopy : IEdgeConnectorListener
{
    private GraphViewChange m_GraphViewChange;

    private List<Edge> m_EdgesToCreate;

    private List<GraphElement> m_EdgesToDelete;

    public DefaultEdgeConnectorListenerCopy()
    {
        m_EdgesToCreate = new List<Edge>();
        m_EdgesToDelete = new List<GraphElement>();
        m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
    }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
    }

    public void OnDrop(GraphView graphView, Edge edge)
    {
        m_EdgesToCreate.Clear();
        m_EdgesToCreate.Add(edge);
        m_EdgesToDelete.Clear();
        if (edge.input.capacity == Port.Capacity.Single)
        {
            foreach (Edge connection in edge.input.connections)
            {
                if (connection != edge)
                {
                    m_EdgesToDelete.Add(connection);
                }
            }
        }

        if (edge.output.capacity == Port.Capacity.Single)
        {
            foreach (Edge connection2 in edge.output.connections)
            {
                if (connection2 != edge)
                {
                    m_EdgesToDelete.Add(connection2);
                }
            }
        }

        if (m_EdgesToDelete.Count > 0)
        {
            graphView.DeleteElements(m_EdgesToDelete);
        }

        List<Edge> edgesToCreate = m_EdgesToCreate;
        if (graphView.graphViewChanged != null)
        {
            edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
        }

        foreach (Edge item in edgesToCreate)
        {
            graphView.AddElement(item);
            edge.input.Connect(item);
            edge.output.Connect(item);
        }
    }
}
