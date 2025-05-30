using System;

namespace Core.Networking
{
    public enum ParticipantOrder
    {
        A,
        B,
        C,
        D,
        E,
        F,
        Researcher,
        None
    };

    public enum Language
    {
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
}