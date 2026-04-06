using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogGraphView : GraphView
{
    public readonly Vector2 defaultNodeSize = new Vector2(150, 200);
    private NodeSearchWindow _searchWindow;
    public Blackboard blackboard;
    public List<ExposedProperty> exposedProperties = new List<ExposedProperty>();

    public NodeFactory NodeFactory { get; private set; }

    public DialogGraphView(EditorWindow editorWindow)
    {
        NodeFactory = new NodeFactory();
        
        styleSheets.Add(Resources.Load<StyleSheet>("DialogGraph"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        GridBackground grid = new GridBackground();

        DialogNode.OnRemovePortFromNode += OnRemovePortFromNode;

        Insert(0, grid);
        grid.StretchToParentSize();

        NodeArguments args = new NodeArguments()
        {
            position = Vector2.zero,
            size = defaultNodeSize 
        };
        AddElement(new EntryNode(args));
        AddSearchWindow(editorWindow);
    }

    ~DialogGraphView()
    {
        DialogNode.OnRemovePortFromNode -= OnRemovePortFromNode;
    }

    private void AddSearchWindow(EditorWindow editorWindow)
    {
        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.Init(this, editorWindow);
        nodeCreationRequest = ctx => SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);
    }

    public void ClearBlackboardAndProperties()
    {
        exposedProperties.Clear();
        blackboard.Clear();
    }

    internal void AddPropertyToBlackboard(ExposedProperty exposedProperty, bool isRefreshing = false)
    {
        string localPropertyName = exposedProperty.propertyName;
        string localPropertyValue = exposedProperty.propertyValue;

        int nPropertiesWithName = exposedProperties.FindAll(x => x.propertyName.Contains(localPropertyName)).Count;

        if (nPropertiesWithName > 0 && !isRefreshing)
        {
            localPropertyName = $"{localPropertyName} {nPropertiesWithName}";
        }

        if (!isRefreshing)
        {
            ExposedProperty property = new ExposedProperty
            {
                propertyName = localPropertyName,
                propertyValue = localPropertyValue
            };
            exposedProperties.Add(property);
        }

        VisualElement container = new VisualElement();
        BlackboardField blackboardField = new BlackboardField { text = localPropertyName, typeText = "string" };

        blackboardField.Add(new Button(() => { RemovePropertyFromBlackboard(localPropertyName); }) { text = "X" });
        container.Add(blackboardField);

        TextField propertyValueTextField = new TextField("Value:")
        {
            value = localPropertyValue
        };

        propertyValueTextField.RegisterValueChangedCallback(evt =>
        {
            int propertyIndex = exposedProperties.FindIndex(x => x.propertyName == localPropertyName);
            if(propertyIndex >= 0 && propertyIndex < exposedProperties.Count) 
            {
                exposedProperties[propertyIndex].propertyValue = evt.newValue;
            }
        });

        BlackboardRow blackboardValueRow = new BlackboardRow(blackboardField, propertyValueTextField);
        container.Add(blackboardValueRow); 
        blackboard.Add(container);
    }

    private void RemovePropertyFromBlackboard(string localPropertyName)
    {
        ExposedProperty propertyToRemove = exposedProperties.Find(prop => prop.propertyName == localPropertyName);
        exposedProperties.Remove(propertyToRemove);

        blackboard.Clear();
        //Add properties from data
        foreach (ExposedProperty exposedProperty in exposedProperties.ToList())
        {
            AddPropertyToBlackboard(exposedProperty, true);
        }
    }

    /// <summary>
    /// Removes the specified port from the sender node and any associated edges
    /// </summary>
    /// <param name="sender">A node</param>
    /// <param name="args">args.Output port is the port to remove</param>
    private void OnRemovePortFromNode(object sender, DialogNodeEventArgs args)
    {
        var targetEdge = edges.ToList().Where(x => x.output.portName == args.OutputPort.portName && x.output.node == args.OutputPort.node);
        if (targetEdge.Any())
        {
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(targetEdge.First());
        }
    }

    /// <summary>
    /// You'd think this is useless, but it actually allows connecting ports together
    /// Removing this method will make it so that nothing can be connected manually anymore
    /// </summary>
    /// <param name="startPort"></param>
    /// <param name="nodeAdapter"></param>
    /// <returns></returns>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        List<Port> compatiblePorts = new List<Port>();
        ports.ForEach((port) =>
        {
            if(startPort != port && startPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    public void MakeNewNode(object userData, Vector2 localMousePos)
    {
        BaseNode node = NodeFactory.CreateNode(userData, localMousePos);
        if(node != null)
        {
            AddElement(node);
        }
    }
}