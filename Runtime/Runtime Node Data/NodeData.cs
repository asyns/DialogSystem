using InteractionBehavior;
using UnityEngine;

[System.Serializable]
public class NodeData : ScriptableObject
{
    public string Guid;
    public Rect rectPos;
    public virtual void HandleEvent(Dialogable currentDialogable) {}
}
