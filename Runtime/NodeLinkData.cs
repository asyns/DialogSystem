using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeLinkData : ScriptableObject
{
    public string BaseNodeGuid;
    public string TargetNodeGuid;
    public string PortName;
    public int PortNumber;
}
