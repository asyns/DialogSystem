using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using UnityEditor;


public class BranchNode : BaseNode
{
    public BranchNode() { }
    public BranchNode(NodeArguments args) : base(args) 
    {
        Condition = BranchCondition.QuestDone;
        StatsValue = "Strength";
        QuantityValue = "1";
    }
    
    public BranchNode(NodeArguments args, BranchCondition condition) : base(args) 
    {
        Condition = condition;
        StringValue = args.name;
        StatsValue = args.statName;
        QuantityValue = args.quantity.ToString();
    }

    protected override string NodeName => "Branch";
    protected override string StyleSheetName => "BranchNode";
    protected override int MaxOutputNumber => 2;

    private List<string> _conditions = new List<string>();
    private const string TRUE = "True";
    private const string FALSE = "False";
    private const string QUEST_DONE = "Quest Done";
    private const string QUEST_PICKED_UP = "Quest Picked up";
    private const string HAS_ITEM = "Has Item";
    private const string LEVEL_REACHED = "Level Reached";
    private const string STAT_REACHED = "Stat Reached";

    private DropdownField _conditionDropdown;
    private BranchCondition _condition;
    public BranchCondition Condition
    {
        get => _condition;
        set
        {
            _condition = value;
            if(_conditionDropdown != null)
            {
                SetFromEnum(_condition);
                ClearFields();
                AddSpecificConditionFields();
            }    
        }
    }

    private DropdownField _statsDropdown;
    // Used for the name of the stat we want to check
    // (add additional use cases as we go)
    private string _statsValue;
    public string StatsValue
    {
        get => _statsValue;
        set
        {
            _statsValue = value;
            if(_statsDropdown != null)
            {
                _statsDropdown.value = value;
            }
            CheckValueForCondition();
        }
    }

    // Used for item quantity, 
    private TextField _quantityField;
    private string _quantityValue;
    public string QuantityValue
    {
        get => _quantityValue;
        set
        {
            _quantityValue = value;
            if(_quantityField != null)
            {
                _quantityField.value = value;
            }
            CheckValueForCondition();
        }
    }

    // Used for quest name, item name, stat value, level value
    // (add additional use cases as we go)
    private TextField _valueTextField;
    private string _stringValue;
    public string StringValue
    {
        get => _stringValue;
        set
        {
            _stringValue = value;
            if(_valueTextField != null)
            {
                _valueTextField.value = value;
            }
            CheckValueForCondition();
        }
    }

    private void Init()
    {
        _conditions.Add(QUEST_DONE);
        _conditions.Add(QUEST_PICKED_UP);
        _conditions.Add(HAS_ITEM);
        _conditions.Add(LEVEL_REACHED);
        _conditions.Add(STAT_REACHED);
    }

    private void CheckValueForCondition()
    {
        switch (_condition)
        {
            case BranchCondition.QuestPickedUp:
            case BranchCondition.QuestDone:
                CheckQuestName();
                break;
            case BranchCondition.LevelReached:
                title = $"Check level >= {StringValue}";
                break;
            case BranchCondition.StatReached:
                title = $"Check {StatsValue} >= {StringValue}";
                break;
            case BranchCondition.HasItem:
                CheckItemName();
                break;
        }
    }

    private void CheckItemName()
    {
        if(!string.IsNullOrEmpty(StringValue))
        {
            string pathToItem = $"Assets/Inventory System/Items/Resources/{StringValue}.asset";
            Item item = (Item)AssetDatabase.LoadAssetAtPath(pathToItem, typeof(Item));
            if(item != null)
            {
                title = $"Check Item: {QuantityValue}x {item.name}";
            }
            else
            {
                title = "Check Item: not found";
            }
        }
    }

    private void CheckQuestName()
    {
        if (!string.IsNullOrEmpty(StringValue))
        {
            string pathToQuest = $"Assets/Quest System/Quest Library/Resources/{StringValue}.asset";
            Quest quest = (Quest)AssetDatabase.LoadAssetAtPath(pathToQuest, typeof(Quest));
            if (quest != null)
            {
                title = $"Check Quest: {quest.name}";
            }
            else
            {
                title = "Check Quest: not found";
            }
        }
    }

    protected override void AddFields()
    {
        Init();
        AddInputPort();
        AddOutputPort(TRUE);
        AddOutputPort(FALSE);
        AddConditionField();
    }

    private void SetFromEnum(BranchCondition value)
    {
        switch (value)
        {
            case BranchCondition.QuestDone:
                _conditionDropdown.SetValueWithoutNotify(QUEST_DONE);
                break;
            case BranchCondition.LevelReached:
                _conditionDropdown.SetValueWithoutNotify(LEVEL_REACHED);
                break;
            case BranchCondition.StatReached:
                _conditionDropdown.SetValueWithoutNotify(STAT_REACHED);
                break;
            case BranchCondition.QuestPickedUp:
                _conditionDropdown.SetValueWithoutNotify(QUEST_PICKED_UP);
                break;
            case BranchCondition.HasItem:
                _conditionDropdown.SetValueWithoutNotify(HAS_ITEM);
                break;
            default:
                break;
        }
    }
    
    private void SetFromString(string value)
    {
        switch(value)
        {
            case QUEST_PICKED_UP:
                Condition = BranchCondition.QuestPickedUp;
                break;
            case HAS_ITEM:
                Condition = BranchCondition.HasItem;
                break;
            case QUEST_DONE:
                Condition = BranchCondition.QuestDone;
                break;
            case LEVEL_REACHED:
                Condition = BranchCondition.LevelReached;
                break;
            case STAT_REACHED:
                Condition = BranchCondition.StatReached;
                break;
        }
    }

    private void AddConditionField()
    {
        _conditionDropdown = new DropdownField("Condition");
        foreach(var condition in _conditions)
        {
            _conditionDropdown.choices.Add(condition);
        }
        _conditionDropdown.RegisterValueChangedCallback(evt => 
        {
            SetFromString(evt.newValue);
            ClearFields();
            AddSpecificConditionFields();
        });
        mainContainer.Add(_conditionDropdown);
    }

    private void AddSpecificConditionFields()
    {
        switch (_condition)
        {
            default:
                break;
            case BranchCondition.QuestPickedUp:
            case BranchCondition.QuestDone:
            case BranchCondition.LevelReached:
                AddValueField();
                break;
            case BranchCondition.HasItem:
                AddQuantityField();
                AddValueField();
                break;
            case BranchCondition.StatReached:
                AddStatsDropdownField();
                AddValueField();
                break;
        }
    }

    private void AddStatsDropdownField()
    {
        _statsDropdown = new DropdownField("Stat");
        _statsDropdown.choices.Add("Strength");
        _statsDropdown.choices.Add("Dexterity");
        _statsDropdown.choices.Add("Constitution");
        _statsDropdown.choices.Add("Intelligence");
        _statsDropdown.choices.Add("Wisdom");
        _statsDropdown.choices.Add("Charisma");

        _statsDropdown.RegisterValueChangedCallback(evt => 
        {
            StatsValue = evt.newValue;
            Debug.Log("StatsValue is now " + StatsValue);
        });
        _statsDropdown.SetValueWithoutNotify(StatsValue);
        mainContainer.Add(_statsDropdown);
    }

    private void AddValueField()
    {
        _valueTextField = new TextField("Value");
        _valueTextField.RegisterValueChangedCallback(evt => 
        {
            StringValue = evt.newValue;
        });
        _valueTextField.SetValueWithoutNotify(StringValue);
        mainContainer.Add(_valueTextField);
    }

    private void AddQuantityField()
    {
        _quantityField = new TextField("Quantity");
        _quantityField.RegisterValueChangedCallback(evt => 
        {
            QuantityValue = evt.newValue;
        });
        _quantityField.SetValueWithoutNotify(QuantityValue);
        mainContainer.Add(_quantityField);
    }

    private void ClearFields()
    {
        if(_valueTextField != null)
        {
            mainContainer.Remove(_valueTextField);   
            _valueTextField = null;
        }

        if(_statsDropdown != null)
        {
            mainContainer.Remove(_statsDropdown);
            _statsDropdown = null;
        }

        if(_quantityField != null)
        {
            mainContainer.Remove(_quantityField);
            _quantityField = null;
        }
    }

    public override NodeData SaveAsNodeData()
    {
        BranchNodeData data = ScriptableObject.CreateInstance<BranchNodeData>();
        data.Condition = _condition;
        data.Value = StringValue;
        data.Stat = StatsValue;
        if(int.TryParse(QuantityValue, out int value))
        {
            data.Quantity = value;
        }
        return data;
    }
}
