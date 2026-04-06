using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public struct NodeArguments
{
    public string name;
    public string statName;
    public int quantity;
    public Vector2 position;
    public Vector2 size;
}

public class NodeFactory
{
    /// <summary>
    /// Creates a node by loading NodeData (ScriptableObject) from Assets
    /// </summary>
    /// <param name="nodeData"></param>
    /// <returns></returns>
    public BaseNode CreateNode(NodeData nodeData)
    {
        BaseNode node;
        NodeArguments args = new NodeArguments
        {
            position = nodeData.rectPos.position,
            size = nodeData.rectPos.size,
        };

        switch(nodeData)
        {
            case ChoiceNodeData choiceNodeData:
                node = new ChoiceNode(args);
                break;

            case GoToGraphData goToData:
                args.name = goToData.GraphName;
                node = new GoToGraphNode(args);
                break;

            case PromptNodeData promptData:
                args.name = promptData.Prompt;
                node = new PromptTextNode(args);
                break;

            case QuestNodeData questData:
                node = new StartQuestNode(args);
                break;

            case DialogNodeData dialogData:
                args.name = dialogData.DialogText;
                node = new DialogNode(args);
                break;
                
            case BranchNodeData branchData:
                args.name = branchData.Value;
                args.statName = branchData.Stat;
                args.quantity = branchData.Quantity;
                node = new BranchNode(args, branchData.Condition);
                break;

            case EntryNodeData:
                node = new EntryNode(args);
                break;

            case ExitNodeData:
                node = new ExitNode(args);
                break;

            default:
                Debug.LogError("Cannot create this type of node");
                return null;
        }
        return node;
    }

    /// <summary>
    /// Creates a node by selecting an option in the Contextual Menu 
    /// </summary>
    /// <param name="userData"></param>
    /// <param name="mousePosition"></param>
    /// <returns></returns>
    public BaseNode CreateNode(object userData, Vector2 mousePosition)
    {
        BaseNode node;
        NodeArguments args = new NodeArguments
        {
            name = string.Empty,
            statName = string.Empty,
            position = mousePosition,
            size = Vector2.zero
        };

        switch (userData)
        {
            case ChoiceNode:
                node = new ChoiceNode(args);
                break;

            case GoToGraphNode:
                node = new GoToGraphNode(args);
                break;

            case BranchNode:
                node = new BranchNode(args);
                break;

            case PromptTextNode:
                node = new PromptTextNode(args);
                break;

            case StartQuestNode:
                node = new StartQuestNode(args);
                break;

            case DialogNode: 
                node = new DialogNode(args);
                break;
                
            case ExitNode:
                node = new ExitNode(args);
                break;

            default:
                Debug.LogError("Cannot create this type of node");
                return null;
        }
        return node;
    }
}