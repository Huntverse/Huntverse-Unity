using System;
using UnityEngine;

namespace Hunt
{
    public interface IInteractable
    {
        /// <summary> 상호작용 가능 여부 </summary>
        bool CanInteract();

        /// <summary> 상호작용 실행 </summary>
        void Interact(Transform t );
        
        /// <summary> 상호작용 UI 텍스트 </summary>
        string GetInteractionText();

        /// <summary> 상호작용 범위 </summary>
        float GetInteractionTriggerRange();

        /// <summary> Transform (위치/거리 계산용) </summary>
        Transform GetTransform();

        event Action<InteractionEventArgs> OnInteractionRequested;
    }

}