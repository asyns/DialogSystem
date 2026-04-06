using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class StartQuestNode : BaseNode
{
    public StartQuestNode() { }
    public StartQuestNode(NodeArguments args) : base(args) 
    {
        QuestName = args.name;
    }
    
    protected override string NodeName => "Start Quest: ";
    private const string QUEST_NAME_LABEL = "Quest name";
    protected override string StyleSheetName => "QuestNode";
    protected override int MaxOutputNumber => 1;

    private TextField _questNameField;
    private string _questName;
    public string QuestName
    {
        get => _questName;
        set 
        {
            _questName = value;
            if(_questNameField != null)
            {
                _questNameField.value = _questName;
            }

            if(!string.IsNullOrEmpty(_questName))
            {
                string pathToQuest = "Assets/Quest System/Quest Library/Resources/" + _questName + ".asset";
            }
        }
    }


    protected override void AddFields()
    {
        AddInputPort();
        AddOutputPort();
        AddQuestNameField();
    }

    //make special field which will match the name of the quest started by reaching this dialog node
    private void AddQuestNameField()
    {
        _questNameField = new TextField(string.Empty);
        _questNameField.label = QUEST_NAME_LABEL;
        _questNameField.RegisterValueChangedCallback(evt =>
        {
            QuestName = evt.newValue;
        });

        _questNameField.SetValueWithoutNotify(QuestName);
        mainContainer.Add(_questNameField);
    }

    public override NodeData SaveAsNodeData()
    {
        QuestNodeData data = ScriptableObject.CreateInstance<QuestNodeData>();
        return data;
    }
}
