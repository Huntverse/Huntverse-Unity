using UnityEngine;

namespace Hunt
{
    public class NPCBase : MonoBehaviour, IInteractable
    {
        [Header("SETTINGS")]
        [SerializeField] protected NPCData npcData;
        [SerializeField] protected float interactionTriggerRange = 3f;
        [SerializeField] protected NPCNotiType currentNotification = NPCNotiType.None;

        [Header("VISUAL")]
        [SerializeField] protected GameObject notificationIcon;

        protected bool isInteracting;

        public NPCData Data => npcData;
        public bool IsInteracting => isInteracting;

        #region Life
        protected virtual void Start()
        {
            InitializeNPC();
        }

        protected virtual void OnDestroy()
        {

        }

        protected virtual void InitializeNPC()
        {
            if (npcData == null)
            {
                "[NPC] NPCData가 할당되지 않았습니다.".DError();
                return;
            }

        }

        #endregion
        private void CheckPlayerDistance()
        {


        }
        public virtual bool CanInteract(Transform player)
        {
            if (npcData == null || isInteracting) return false;
            float distance = Vector3.Distance(transform.position, player.position);
            return distance < interactionTriggerRange;
        }
        public virtual bool CanInteract()
        {
            return npcData != null && !isInteracting;
        }
        public virtual void Interact(Transform player)
        {
            if (!CanInteract(player))
            {
                return;
            }

            isInteracting = true;
            $"[NPC] {npcData?.npcName} - {player.name}".DLog();

            switch (npcData.npcType)
            {

                case NPCType.Merchant:
                    OpenMerchantMenu(player);
                    break;
                case NPCType.QuestGiver:
                    OpenQuestMenu(player);
                    break;
                case NPCType.Healer:
                    OpenHealMenu(player);
                    break;
                case NPCType.Blacksmith:
                    OpenBlacksmithMenu(player);
                    break;
                case NPCType.Banker:
                    OpenBankMenu(player);
                    break;
                case NPCType.TalkOnly:
                default:
                    StartDialog(player);
                    break;
            }
        }


        public void Interact()
        {
            $"[NPC] {npcData?.npcName} 상호작용 (플레이어 미지정)".DLog();
        }

        public string GetInteractionText()
        {
            throw new System.NotImplementedException();
        }

        public float GetInteractionTriggerRange() => interactionTriggerRange;

        public NPCData GetNPCData() => npcData;

        public NPCNotiType GetNPCNotiType() => currentNotification;

        #region Action
        protected virtual void OpenMerchantMenu(Transform player)
        {
            $"[NPC] {npcData.npcName} 상점 열기".DLog();
            // NPCMerchant 컴포넌트가 처리
        }

        protected virtual void OpenQuestMenu(Transform player)
        {
            $"[NPC] {npcData.npcName} 퀘스트 메뉴".DLog();
            // NPCQuest 컴포넌트가 처리
        }

        protected virtual void OpenHealMenu(Transform player)
        {
            $"[NPC] {npcData.npcName} 치료 메뉴".DLog();
        }

        protected virtual void OpenBlacksmithMenu(Transform player)
        {
            $"[NPC] {npcData.npcName} 대장간 메뉴".DLog();
        }

        protected virtual void OpenBankMenu(Transform player)
        {
            $"[NPC] {npcData.npcName} 은행 메뉴".DLog();
        }

        protected virtual void StartDialog(Transform player)
        {
            $"[NPC] {npcData.npcName} 대화 시작".DLog();
            // NPCDialog 컴포넌트가 처리
        }

        public virtual void EndInteraction()
        {
            isInteracting = false;
        }

        public void SetNotification(NPCNotiType type)
        {
            currentNotification = type;
        }
        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionTriggerRange);
        }

        #endregion
    }
}