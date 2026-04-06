using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using InteractionBehavior;
using UnityEngine;

public class DialogController : MonoBehaviour
{
    public event EventHandler<DialogEvent> OnStartDialog = delegate {};
    public event EventHandler<DialogEvent> OnNodeEnabled = delegate {};
    public static event EventHandler<DialogEvent> OnStartDialogStatic = delegate {};
    public static event EventHandler<DialogEvent> OnEndDialog = delegate {};

    public Dialogable CurrentDialogable { get; private set; }
    public CharacterSystem.CharacterController CharacterController { get; private set; }
    protected const string NEXT_KEY = "Next";

    private DialogContainer _dialogGraph;
    public DialogContainer DialogGraph
    {
        get => _dialogGraph;
        set 
        {
            if(_dialogGraph == value)
            {
                return; // avoid doing anything if we're mistakenly starting the same conversation we're already a part in
            }

            _dialogGraph = value;

            if(_dialogGraph != null)
            {
                // DialogEvent dialogEvent = CurrentDialogable == null ? null : new DialogEvent(CurrentDialogable.transform);

                OnStartDialog(this, null);
                OnStartDialogStatic(this, null);

                NetworkSessionBase.Instance.LocalPlayer.Input.Controls.Default.LeftClick.Disable();
                NodeLinkData data = _dialogGraph.NodeLinks.Find(x => x.PortName == NEXT_KEY); // find link between start node and first node
                MoveToNode(data.TargetNodeGuid); // move to first node
            }
            else
            {
                NetworkSessionBase.Instance.LocalPlayer.Input.Controls.Default.LeftClick.Enable();
            }
        }
    }

    void Awake()
    {
        DialogUIController.OnDialogUIInstantiated += OnDialogUIInstantiated;

        Talkable.OnTalkableInteractedWith += OnTalkableInteractedWith;
        Readable.OnReadableInteractedWith += OnReadableInteractedWith;

        Talkable.OnJoinConversation += OnStartListeningToConversation;

        CutSceneDirector.OnCutsceneStarted += OnCutsceneStarted;
    }

    void OnDestroy()
    {
        DialogUIController.OnDialogUIInstantiated -= OnDialogUIInstantiated;
        CutSceneDirector.OnCutsceneStarted -= OnCutsceneStarted;
        CutSceneDirector.OnCutsceneEnded -= OnCutsceneEnded;
        
        StopAllCoroutines();
        Talkable.OnTalkableInteractedWith -= OnTalkableInteractedWith;
        Readable.OnReadableInteractedWith -= OnReadableInteractedWith;
        Talkable.OnJoinConversation -= OnStartListeningToConversation;
    }

    private void OnCutsceneStarted(object sender, EventArgs e)
    {
        CutSceneDirector cutSceneDirector = sender as CutSceneDirector;
        PlayerNavAgentController groupLeader = NetworkSessionBase.Instance.LocalPlayer.Group.Leader;
        StartConversation(null, cutSceneDirector.currentCutscene.dialog, groupLeader.CharacterController);
        CutSceneDirector.OnCutsceneEnded += OnCutsceneEnded;
    }

    private void OnCutsceneEnded(object sender, EventArgs e)
    {
        CutSceneDirector.OnCutsceneEnded -= OnCutsceneEnded;
        CloseDialog();
    }

    private void StartConversation(Dialogable dialogable, DialogContainer dialogGraph, CharacterSystem.CharacterController characterController)
    {
        CurrentDialogable = dialogable;
        CharacterController = characterController;
        DialogGraph = dialogGraph;
    }

    private void JoinConversation(Dialogable dialogable, DialogContainer dialogGraph, string nodeID)
    {
        CurrentDialogable = dialogable;
        int clientID = dialogable.interactingClientID.Value;
        NetworkObject firstObject = InstanceFinder.ClientManager.Clients[clientID].FirstObject;
        if(firstObject.TryGetComponent<NetworkedGroup>(out var networkedGroup))
        {
            CharacterController = networkedGroup.Group.Leader.CharacterController;            
        }
        DialogGraph = dialogGraph;
        MoveToNode(nodeID);

        CurrentDialogable.OnNodeIDChanged += OnNodeIDChanged;
    }

    private void OnNodeIDChanged(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OnDialogUIInstantiated(object sender, EventArgs e)
    {
        DialogUIController dialogUIController = sender as DialogUIController; 
        dialogUIController.DialogController = this;
    }

    private void OnReadableInteractedWith(object sender, DialogEvent e)
    {
        Readable readable = sender as Readable;

        PlayerNavAgentController groupLeader = NetworkSessionBase.Instance.LocalPlayer.Group.Leader;
        StartConversation(readable, e.dialogGraph, groupLeader.CharacterController);
        StartCoroutine(TurnTowardsTarget(groupLeader.transform, readable.transform.parent));
    }

    private void OnTalkableInteractedWith(object sender, DialogEvent e)
    {
        Talkable talkable = sender as Talkable;

        PlayerNavAgentController groupLeader = NetworkSessionBase.Instance.LocalPlayer.Group.Leader;
        StartConversation(talkable, e.dialogGraph, groupLeader.CharacterController);
        StartCoroutine(TurnTowardsTarget(groupLeader.transform, talkable.transform.parent));
        StartCoroutine(TurnTowardsTarget(talkable.transform.parent.parent, groupLeader.transform));
    }

    private void OnStartListeningToConversation(object sender, DialogEvent e)
    {
        Talkable talkable = sender as Talkable;
        JoinConversation(talkable, e.dialogGraph, e.nodeID);
    }

    private IEnumerator TurnTowardsTarget(Transform leader, Transform target)
    {
        if (target == null)
        {
            yield break; // Exit if there is no target
        }
        
        float duration = .25f;
        float time = 0;
        Quaternion startRotation = leader.rotation;
        Quaternion endRotation = Quaternion.LookRotation(target.position - leader.position);

        while (time < duration)
        {
            time += Time.deltaTime;
            float fraction = time / duration;
            leader.rotation = Quaternion.Slerp(startRotation, endRotation, fraction);
            yield return null;
        }
        leader.rotation = endRotation;
    }

    /// <summary>
    /// We'll -never- MoveToNode when it's a ChoiceNode. ChoiceNodes are evaluated when MoveToNode is called for the previous DialogNode
    /// </summary>
    /// <param name="targetNodeGuid"></param>
    public void MoveToNode(string targetNodeGuid)
    {
        CurrentDialogable.SetDialogNodeID(targetNodeGuid);    
        NodeData targetNode = _dialogGraph.FindNode(targetNodeGuid);
        List<NodeLinkData> nextLinks = _dialogGraph.FindNextLinksForNode(targetNode); // links from the current node
        targetNode.HandleEvent(CurrentDialogable);
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
                CloseDialog();
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
            else // else next link is "Continue" to another type of node
            {
                choices.Add(nextLinks[0]);
            }
        }
        // or only show Close button if there's 0 nextLinks

        OnNodeEnabled(this, new DialogEvent(targetNode, choices));
    }

    private int HandleBranch(BranchNodeData branchNodeData)
    {
        bool result = false;
        switch (branchNodeData.Condition)
        {
            case BranchCondition.QuestDone:
                result = NetworkSessionBase.Instance.LocalPlayer.Group.QuestManager.IsQuestDone(branchNodeData.Value);
                break;
            case BranchCondition.QuestPickedUp:
                result = NetworkSessionBase.Instance.LocalPlayer.Group.QuestManager.IsQuestActive(branchNodeData.Value);
                break;
            case BranchCondition.HasItem:
                result = CharacterController.inventory.HasNItems(branchNodeData.Value, branchNodeData.Quantity);
                break;
            case BranchCondition.LevelReached:
                result = CharacterController.Experience.HasLevel(branchNodeData.Value);
                break;
            case BranchCondition.StatReached:
                result = CharacterController.Stats.HasScore(branchNodeData.Value, branchNodeData.Stat);
                break;
        }

        int linkIndex = result ? 0 : 1; // First output of node == "true"
                                    // Second output of node == "false"
        return linkIndex;
    }

    public void ConfirmTextPrompt(string text)
    {
        Debug.Log("ConfirmTextPrompt");
    }

    public void CloseDialog()
    {
        if(CurrentDialogable != null)
        {
            CurrentDialogable.SetIsInteracting(false);
        }
        CurrentDialogable = null;
        CharacterController = null;
        DialogGraph = null;
        
        OnEndDialog(this, null);
    }

    public void HandleExposedProperty(NodeLinkData choice, string exposedRelatedProperty)
    {
        Debug.Log("HandleExposedProperty:" + exposedRelatedProperty);
    }
}
