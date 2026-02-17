using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hunt
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class UserCharLoco : MonoBehaviour, IPlayer
    {
        [Header("MOVE")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("JUMP")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.2f;

        [Header("GROUND CHECK")]
        [SerializeField] private float groundCheckRadius = 0.25f; // Slightly smaller than capsule radius
        [SerializeField] private float groundCastLength = 0.5f;   // How far to cast down from the origin
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.5f, 0); // Start from center/knees
        #region Private Field

        private Rigidbody rb;
        private Animator animator;

        private bool canControl;
        private Vector2 moveInput;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool wasGrounded;
        private bool isGrounded;
        private SpriteRenderer spriteRenderer;

        private GameObject model;
        private InputManager inputKey;
        private IsAttackPointer hitpointer;
        private IsNotiPoint notiPoint;
        private HashSet<IInteractable> nearbyInteractables = new HashSet<IInteractable>();
        private IInteractable currentInteractable;
        private UserCombat combat;
        private AnimationActionType _currentActionType;
        private float _facingScaleX = 1f;
        /// <summary>방향 전환 시 scale 적용 대상(기본: model만). VFX 등에서 facing 참조용</summary>
        public float FacingScaleX => _facingScaleX;

        #endregion
        private void Awake()
        {
            UniTask.WaitUntil(() => !InputManager.Shared);
            inputKey = InputManager.Shared;
            inputKey.Player.Jump.performed += OnJumpPerformed;
            inputKey.Player.Attack.performed += OnAttackPerformed;
            inputKey.Player.Talk.performed += OnInteractPerformed;
        }
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Constrain rotation to keep character upright (no physics rotation)
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            hitpointer = GetComponentInChildren<IsAttackPointer>();
            hitpointer.SetT(new Vector3(2.0f, 0.5f, 0f), new Vector2(1,1.25f)); // Custom
            notiPoint = GetComponentInChildren<IsNotiPoint>();
            combat = GetComponent<UserCombat>();
            if (combat == null)
            {
                combat = gameObject.AddComponent<UserCombat>();
            }
        }
        private void OnEnable()
        {
            inputKey?.Player.Enable();
        }

        private void OnDisable()
        {
            inputKey?.Player.Disable();
        }
        public void Initialize(GameObject characterModel)
        {
            model = characterModel;
            animator = model.GetComponent<Animator>();
            spriteRenderer = model.GetComponent<SpriteRenderer>();
            if (model.GetComponent<AnimationVfxEventReceiver>() == null)
                model.AddComponent<AnimationVfxEventReceiver>();
            canControl = true;
        }
        private void Update()
        {
            if (!canControl) return;
            HandleInput();
            UpdateGroundCheck();
            UpdateTimers();
            UpdateAnimator();
            HandleMovement();
        }
        public void HandleInput()
        {
            if (isAttacking) return;
            moveInput = inputKey.Player.Move.ReadValue<Vector2>();
            if (moveInput.x > 0.1f)
            {
                _facingScaleX = 1f;
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (moveInput.x < -0.1f)
            {
                _facingScaleX = -1f;
                transform.localScale = new Vector3(-1, 1, 1);

            }
            GetComponent<UserDisplay>()?.OnFacingChanged(_facingScaleX);
        }

        public bool isJumpping = true;
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            jumpBufferCounter = jumpBufferTime;
        }

        public bool isAttacking = false;
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            HandleAttack();

        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            HandleInteract();
        }

        // Sync NetWork
        public void HandleMovement()
        {
            if (isAttacking) return;
            
            // Dungeon Fighter Style: 3D Movement with Physics
            // X Axis: Lateral
            float velx = moveInput.x * moveSpeed;
            
            // Z Axis: Depth
            float velz = moveInput.y * moveSpeed;

            // Apply to Rigidbody velocity (preserving Y velocity for jump/gravity)
            rb.linearVelocity = new Vector3(velx, rb.linearVelocity.y, velz);
        }

        // Sync NetWork
        public void HandleAttack()
        {
            if (!canControl || isAttacking) return;
            isAttacking = true;
            animator?.SetTrigger(AniKeyConst.K_tAttack);
        }

        public void SetCurrentActionType(AnimationActionType type) => _currentActionType = type;

        public void HandleInteract()
        {
            if (!canControl || isAttacking) return;

            var nearest = GetNearestInteractable();

            if (nearest != null && nearest.CanInteract())
            {
                nearest.Interact(transform);
                $"[UserCharLoco] {nearest.GetTransform().name}와 상호작용".DLog();
            }
            else
            {
                "[UserCharLoco] 상호작용 가능한 대상이 없습니다".DWarnning();
            }
        }
        public void HandleJump()
        {
            if (!canControl || isAttacking) return;

            // 3D Jump: Apply force to Y axis
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            coyoteTimeCounter = 0f;
        }
        /// <summary>
        /// NPC가 "나랑 대화 가능해!" 알림
        /// </summary>
        public void RegisterInteractable(IInteractable interactable)
        {
            if (interactable == null) return;

            nearbyInteractables.Add(interactable);
            $"[UserCharLoco] {interactable.GetTransform().name} 등록 (총 {nearbyInteractables.Count}개)".DLog();
            UpdateInteractionUI().Forget();
        }

        /// <summary>
        /// NPC가 "나랑 대화 불가!" 알림
        /// </summary>
        public void UnregisterInteractable(IInteractable interactable)
        {
            if (interactable == null) return;

            nearbyInteractables.Remove(interactable);
            $"[UserCharLoco] {interactable.GetTransform().name} 해제 (남은 {nearbyInteractables.Count}개)".DLog();
            UpdateInteractionUI().Forget();
        }

        public void SetJumpEnabled(bool enabled) => isJumpping = enabled;

        private IInteractable GetNearestInteractable()
        {
            if (nearbyInteractables.Count == 0) return null;

            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var interactable in nearbyInteractables)
            {
                if (interactable == null || !interactable.CanInteract()) continue;

                float distance = Vector3.Distance(transform.position, interactable.GetTransform().position);
                if (distance < nearestDistance)
                {
                    nearest = interactable;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private async UniTask UpdateInteractionUI()
        {
            var nearest = GetNearestInteractable();

            if (nearest != null && notiPoint != null && notiPoint.renderer != null)
            {
                string text = nearest.GetInteractionText();

                var sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(NotiInteractionConst.ks_normal_noti);
                if (sprite != null)
                {
                    notiPoint.renderer.sprite = sprite;
                    $"[UserCharLoco] Noti 활성화".DLog();
                }
            }
            else
            {
                $"[UserCharLoco] UI 숨김".DLog();
                if (notiPoint != null && notiPoint.renderer != null)
                {
                    notiPoint.renderer.sprite = null;
                }
            }
        }
        #region Update
        private void UpdateAnimator()
        {
            if (animator == null) return;

            // Check if moving in either X (Horizontal) or Z (Vertical Input)
            var moveMagnitude = moveInput.magnitude;
            animator.SetBool(AniKeyConst.k_bMove, moveMagnitude > 0.1f && isGrounded);
        }

        private void UpdateTimers()
        {
            
            coyoteTimeCounter -= Time.deltaTime;
            jumpBufferCounter -= Time.deltaTime;

            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                HandleJump();
                jumpBufferCounter = 0f;
            }
        }

        private void UpdateGroundCheck()
        {
            wasGrounded = isGrounded;

            // SphereCast downwards from the offset position (e.g. knees/center)
            Vector3 origin = transform.position + groundCheckOffset;
            
            // Cast a sphere downwards to detect ground
            isGrounded = Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out RaycastHit hitInfo,
                groundCastLength,
                groundLayer,
                QueryTriggerInteraction.Ignore // Ignore triggers to avoid false positives
            );
            
            animator?.SetBool(AniKeyConst.k_bGround, !isGrounded);

            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
                
                // Optional: Check slope angle here using hitInfo.normal
                // Vector3 groundNormal = hitInfo.normal;
            }

            if (!wasGrounded && isGrounded)
            {
                OnLanded();
            }
        }

        private void OnLanded()
        {
            // 착지 시 추가 효과 (사운드, 파티클 등)
            // 애니메이터는 UpdateGroundCheck에서 처리됨
        }


        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 origin = transform.position + groundCheckOffset;
            
            // Draw SphereCast start
            Gizmos.DrawWireSphere(origin, groundCheckRadius);
            // Draw SphereCast end
            Gizmos.DrawWireSphere(origin + Vector3.down * groundCastLength, groundCheckRadius);
            // Draw centerline
            Gizmos.DrawLine(origin, origin + Vector3.down * groundCastLength);
        }

        private void OnDestroy()
        {
            if (inputKey != null)
            {
                inputKey.Player.Jump.performed -= OnJumpPerformed;
                inputKey.Player.Attack.performed -= OnAttackPerformed;
                inputKey.Player.Talk.performed -= OnInteractPerformed;
                inputKey.Action.Dispose();
            }
        }
        // ... existing code ...

        #region Combat & Damage
        
        public void TakeDamage(float damage)
        {
            if (!canControl) return; 

            // Hp Logic
            // currentHp -= damage; 

            // 1. Flash Effect
            FlashEffect().Forget();

            ShowDamageTextAsync(damage).Forget();
            
            this.DLog($"Player took {damage} damage!");
        }

        private async UniTaskVoid ShowDamageTextAsync(float damage)
        {
            var instance = await AbLoader.Shared.LoadInstantiateAsync(ResourceKeyConst.Kp_DamageText_Vfx);
            if (instance != null)
            {
                var dt = instance.GetComponent<DamageText>();
                if (dt != null) dt.Setup(damage, instance.transform.position, Color.white);
            }
        }

        private async UniTaskVoid FlashEffect()
        {
            if (spriteRenderer == null) return;
            
            Color original = Color.white; 
            // Save original if needed, but usually white is default tint
            
            int flashCount = 3;
            float duration = 0.1f;

            for (int i = 0; i < flashCount; i++)
            {
                spriteRenderer.color = Color.red;
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration));
                spriteRenderer.color = Color.white;
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration));
            }
        }
        #endregion
    }
}
