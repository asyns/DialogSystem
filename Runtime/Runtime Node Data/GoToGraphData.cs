using System;
using InteractionBehavior;

[System.Serializable]
public class GoToGraphData : NodeData 
{
    public string GraphName;
    public override void HandleEvent(Dialogable currentDialogable)
    {
        if(currentDialogable is Talkable talkable)
        {
            talkable.MoveToGraph(GraphName);
        }
    }
}
