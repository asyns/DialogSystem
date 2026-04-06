using System;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class DialogEvent : EventArgs
{
    public DialogContainer dialogGraph;
    public NodeData node;
    public List<NodeLinkData> choices;
    public string nodeID;

    public DialogEvent(NodeData node, List<NodeLinkData> choices)
    {
        this.node = node;
        this.choices = choices;
    }

    public DialogEvent(DialogContainer dialogGraph)
    {
        this.dialogGraph = dialogGraph;
    }

    public DialogEvent(DialogContainer dialogGraph, string nodeID) : this(dialogGraph)
    {
        this.nodeID = nodeID;       
    }
}