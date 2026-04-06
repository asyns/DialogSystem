using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BranchNodeData : NodeData 
{
    public BranchCondition Condition;
    public string Value;
    public string Stat;
    public int Quantity;
}

public enum BranchCondition
{
    QuestDone,
    QuestPickedUp,
    HasItem,
    LevelReached,
    StatReached
}