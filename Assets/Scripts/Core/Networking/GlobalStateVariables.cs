using System.Collections.Generic;
using System;
using UnityEngine.Serialization;

public enum ParticipantOrder {
    A,
    B,
    C,
    D,
    E,
    F,
    None
};

public enum Language {
    English, 
    Hebrew, 
    Chinese, 
    German
}

[Serializable]
public struct ClientOption
{
    public ParticipantOrder PO;
    public int ClientDisplay;
    public int InteractableObject;
}

[Serializable]
public struct JoinParameters
{
    public ParticipantOrder PO;
}