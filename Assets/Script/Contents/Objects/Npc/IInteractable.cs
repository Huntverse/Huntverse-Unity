using Unity.VisualScripting;
using UnityEngine;

namespace Hunt
{
    public interface IInteractable
    {
        bool CanInteract();
        void Interact();

        string GetInteractionText();

        float GetInteractionTriggerRange();

        NPCData GetNPCData();   

        NPCNotiType GetNPCNotiType();   
    }

}