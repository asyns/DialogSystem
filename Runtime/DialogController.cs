using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IDialogController
{
    event EventHandler<DialogEvent> OnStartDialog;
    event EventHandler<DialogEvent> OnNodeEnabled;
    void StartConversation(DialogContainer dialogGraph);
    void StopConversation();
}

public class DialogController : MonoBehaviour, IDialogController
{
    public event EventHandler<DialogEvent> OnStartDialog = delegate {};
    public event EventHandler<DialogEvent> OnNodeEnabled = delegate {};
    public static event EventHandler<DialogEvent> OnStartDialogStatic = delegate {};
    public static event EventHandler<DialogEvent> OnEndDialog = delegate {};

    protected const string NEXT_KEY = "Next";

    private DialogContainer _dialogGraph;
    public DialogContainer DialogGraph
    {
        get => _dialogGraph;
        private set 
        {
            if(_dialogGraph == value)
            {
                return; // avoid doing anything if we're mistakenly starting the same conversation we're already a part in
            }

            _dialogGraph = value;

            if(_dialogGraph != null)
            {
                OnStartDialog(this, null);
                OnStartDialogStatic(this, null);

                NodeLinkData data = _dialogGraph.NodeLinks.Find(x => x.PortName == NEXT_KEY); // find link between start node and first node
                MoveToNode(data.TargetNodeGuid); // move to first node
            }
        }
    }

    protected virtual void Awake()
    {
        DialogUIController.OnDialogUIInstantiated += OnDialogUIInstantiated;
    }

    protected virtual void OnDestroy()
    {
        DialogUIController.OnDialogUIInstantiated -= OnDialogUIInstantiated;
        StopAllCoroutines();
    }

    public void StartConversation(DialogContainer dialogGraph)
    {
        DialogGraph = dialogGraph;
    }

    private void OnDialogUIInstantiated(object sender, EventArgs e)
    {
        DialogUIController dialogUIController = sender as DialogUIController; 
        dialogUIController.DialogController = this;
    }

    /// <summary>
    /// We'll -never- MoveToNode when it's a ChoiceNode. ChoiceNodes are evaluated when MoveToNode is called for the previous DialogNode
    /// </summary>
    /// <param name="targetNodeGuid"></param>
    public void MoveToNode(string targetNodeGuid)
    {
        NodeData targetNode = _dialogGraph.FindNode(targetNodeGuid);
        List<NodeLinkData> nextLinks = _dialogGraph.FindNextLinksForNode(targetNode); // links from the current node
        switch(targetNode)
        {
            case DialogNodeData:
                HandleDialogNode(targetNode, nextLinks);
                break;

            case BranchNodeData branchNodeData:
                int linkIndex = HandleBranch(branchNodeData);
                MoveToNode(nextLinks[linkIndex].TargetNodeGuid);
                break;
            
            case ExitNodeData:
                StopConversation();
                break;
            
            default:
                // Probably only just one next possible node, move to that node
                if(nextLinks.Count == 1)
                {
                    MoveToNode(nextLinks[0].TargetNodeGuid);
                }
                else
                {
                    Debug.LogError("Unimplemented case?");
                }
                break;
        }
    }

    private void HandleDialogNode(NodeData targetNode, List<NodeLinkData> nextLinks)
    {
        List<NodeLinkData> choices = new List<NodeLinkData>();
        if(nextLinks.Count == 1)
        {
            NodeData nextNode = _dialogGraph.FindNode(nextLinks[0].TargetNodeGuid);
            // We'll have dialog choices only if the nextLink is a ChoiceNode
            if(nextNode is ChoiceNodeData)
            {
                // Outputs from the choice node are the different possible choices
                choices = _dialogGraph.FindNextLinksForNode(nextNode);
            }
            else if (nextNode is ExitNodeData)
            {
                // No choices, but we want to show the Close button, so we return empty choices
            }
            else // else next link is "Continue" to another type of node
            {
                choices.Add(nextLinks[0]);
            }
        }
        OnNodeEnabled(this, new DialogEvent(targetNode, choices));
    }

    protected virtual int HandleBranch(BranchNodeData branchNodeData)
    {
        bool result = false;
        int linkIndex = result ? 0 : 1; // First output of node == "true"
                                    // Second output of node == "false"
        return linkIndex;
    }

    public void ConfirmTextPrompt(string text)
    {
        Debug.Log("ConfirmTextPrompt");
    }

    public void StopConversation()
    {
        DialogGraph = null;
        OnEndDialog(this, null);
    }
}
