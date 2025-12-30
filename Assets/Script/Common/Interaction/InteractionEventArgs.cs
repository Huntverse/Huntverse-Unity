using UnityEngine;

namespace Hunt
{
    public class InteractionEventArgs
    {
        public Transform Interactor { get; private set; }
        public GameObject InteractorObject { get; private set; }
        public Vector3 InteractionPoint { get; private set; }
        public float Timestamp { get; private set; }

        // public int ActorId{get;set}
        // public IntereactionType Type{get;set;}

        public InteractionEventArgs(Transform interactor, Vector3 point)
        {
            Interactor = interactor;
            InteractorObject= interactor?.gameObject;
            InteractionPoint = point;
            Timestamp = Time.time;
        }

        //public class InteractionCommand
        //{
        //    public int ActorId { get; set; }
        //    public int TargetId { get; set; }
        //    public Vector3 Position { get; set; }
        //    public float Timestamp { get; set; }
        //    public InteractionType Type { get; set; }

        //    public InteractionCommand(int actorId, int targetId, Vector3 position, float timestamp, InteractionType type)
        //    {
        //        ActorId = actorId;
        //        TargetId = targetId;
        //        Position = position;
        //        Timestamp = timestamp;
        //        Type = type;
        //    }
        //}

        //public enum InteractionType
        //{
        //    None = 0,
        //    Dialog = 1,
        //    Trade = 2,
        //    Quest = 3,
        //    Heal = 4,
        //    Craft = 5,
        //    Bank = 6
        //}

    }
}
