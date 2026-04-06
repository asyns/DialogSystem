using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogGraph : EditorWindow
{
    private DialogGraphView _graphView;
    private string _fileName = "New Narrative";

    private Toolbar _toolbar;
    private TextField _filenameTextField;
    private Blackboard _blackboard;

    [MenuItem("Graph/Dialogue Graph")]
    public static void OpenDialogGraphWindow()
    {
        var window = GetWindow<DialogGraph>();
        window.titleContent = new GUIContent("Dialog Graph");
    }

    private void OnEnable()
    {
        ResetGraph();
    }

    private void ResetGraph()
    {
        ConstructGraphView();
        // GenerateBlackBoard();
        GenerateToolbar();
        //GenerateMinimap();
    }

    // private void GenerateBlackBoard()
    // {
    //     _blackboard = new Blackboard(_graphView);
    //     _graphView.blackboard = _blackboard;

    //     _blackboard.Add(new BlackboardSection { title = "Exposed Properties" });

    //     _blackboard.addItemRequested = blackboard =>
    //     {
    //         _graphView.AddPropertyToBlackboard(new ExposedProperty());
    //     };

    //     _blackboard.editTextRequested = (blackboard,element,newName) =>
    //     {
    //         string oldName =  ((BlackboardField)element).text;
    //         if(_graphView.exposedProperties.FindAll(x => x.propertyName.Contains(newName)).Count > 0)
    //         {
    //             EditorUtility.DisplayDialog("Error", "This property already exists", "OK");
    //             return;
    //         }

    //         var propertyIndex = _graphView.exposedProperties.FindIndex(x => x.propertyName == oldName);
    //         _graphView.exposedProperties[propertyIndex].propertyName = newName;
    //         ((BlackboardField)element).text = newName;
    //     };

    //     var coords = _graphView.contentViewContainer.WorldToLocal(new Vector2(10, maxSize.y - 30));
    //     _blackboard.SetPosition(new Rect(30,30, 500, 200)); 

    //     _graphView.Add(_blackboard);
    // }

    private void GenerateMinimap()
    {
        var miniMap = new MiniMap
        {
            anchored = true
        };

        miniMap.SetPosition(new Rect(10, 30, 200, 140));
        _graphView.Add(miniMap);
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogGraphView(this) { name = "Dialog Graph" };
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        _toolbar = new Toolbar();
        var toolbarmenu = new ToolbarMenu
        {
            text = "Files"
        };
        
        _filenameTextField = new TextField("File Name:");
        _filenameTextField.SetValueWithoutNotify(_fileName);
        _filenameTextField.MarkDirtyRepaint();

        _filenameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);

        var files = Resources.LoadAll("NPCs");
        var saveUtility = GraphSaveUtility.GetInstance(_graphView);

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.name))
            {
                continue;
            }
            toolbarmenu.menu.AppendAction(file.name, (x) => 
            { 
                saveUtility.LoadGraph(file.name);
                _filenameTextField.SetValueWithoutNotify(file.name);
                _fileName = file.name;
            });
        }

        _toolbar.Add(toolbarmenu);
        _toolbar.Add(_filenameTextField);
        _toolbar.Add(new Button(() => RequestData(true)) { text = "Save Data"});
        _toolbar.Add(new Button(() => NewGraph()) { text = "New Graph" });
        _toolbar.Add(new Button(() => DeleteGraph()) { text = "Delete Graph" });
        
        rootVisualElement.Add(_toolbar);

        // if(!string.IsNullOrEmpty(_fileName))
        // {
        //     saveUtility.LoadGraph(_fileName);
        // }
    }

    private void DeleteGraph()
    {
        if(_graphView.nodes.ToList().Count > 1)
        {
            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.DeleteGraph(_fileName);
            NewGraph();
        }
    }

    private void NewGraph()
    {
        rootVisualElement.Remove(_toolbar);
        rootVisualElement.Remove(_graphView);
        ResetGraph();
        _fileName = "New Narrative";
        _filenameTextField.SetValueWithoutNotify(_fileName);
    }

    private void RequestData(bool isSaving)
    {
        if (string.IsNullOrEmpty(_fileName))
        {
            EditorUtility.DisplayDialog("Invalid file name!", "please enter a valid file name", "OK");
        }

        var saveUtility = GraphSaveUtility.GetInstance(_graphView);

        if (isSaving)
        {
            saveUtility.SaveGraph(_fileName);
            rootVisualElement.Remove(_toolbar);
            GenerateToolbar();
        }
        else
        {
            saveUtility.LoadGraph(_fileName);
        }

    }
}
