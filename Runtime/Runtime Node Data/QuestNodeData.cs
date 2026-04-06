using System;
using System.Collections;
using System.Collections.Generic;
using InteractionBehavior;
using UnityEngine;

[Serializable]
public class QuestNodeData : NodeData
{
    public static event EventHandler<QuestEventArgs> OnQuestAccepted = delegate{};
    public Quest quest;

    public override void HandleEvent(Dialogable currentDialogable)
    {
        OnQuestAccepted(this, null);
    }
}
