using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hunt
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerAction : MonoBehaviour, IPlayer
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
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D rb;
        private Animator animator;

        private bool canControl;
        private Vector2 moveInput;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool wasGrounded;
        private bool isGrounded;
        private SpriteRenderer spriteRenderer;

        private GameObject model;
        private InputSystem_Actions inputKey;
        private void Awake()
        {
            inputKey = new InputSystem_Actions();
            inputKey.Player.Jump.performed += OnJumpPerformed;
            inputKey.Player.Attack.performed += OnAttackPerformed;

        }
        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        }
        private void OnEnable()
        {
            inputKey?.Player.Enable();
        }

        // ̰ ߰!!!
        private void OnDisable()
        {
            inputKey?.Player.Disable();
        }
        public void Initialize(GameObject characterModel)
        {
            model = characterModel;
            animator = model.GetComponent<Animator>();
            spriteRenderer = model.GetComponent<SpriteRenderer>();
            canControl = true;
        }
        private void Update()
        {
            if (!canControl) return;
            HandleInput();
            UpdateGroundCheck();
            UpdateTimers();
            UpdateAnimator();
        }
        
        // 추가된 부분: 자식 모델의 스케일이 멋대로 바뀌는 것을 방지
        private void LateUpdate()
        {
            if (model != null)
            {
                // 모델은 항상 (1, 1, 1)을 유지해야 부모의 스케일 방향을 그대로 따름
                if (model.transform.localScale.x != 1f)
                {
                    model.transform.localScale = Vector3.one;
                }
            }
        }

        // Sync NetWork
        private void FixedUpdate()
        {
            if (!canControl) return;

            HandleMovement();

        }

        public void HandleInput()
        {
            moveInput = inputKey.Player.Move.ReadValue<Vector2>();

            // 부모(PlayerAction)의 스케일을 변경하여 방향 제어
            if (moveInput.x > 0.1f)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (moveInput.x < -0.1f)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            jumpBufferCounter = jumpBufferTime;
        }
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (!canControl) return;
            HandleAttack();

        }

        // Sync NetWork
        public void HandleMovement()
        {

            float velx = moveInput.x * moveSpeed;
            rb.linearVelocity = new Vector2(velx, rb.linearVelocity.y);

        }

        // Sync NetWork
        public void HandleAttack()
        {
            if(!canControl) return;
            animator?.SetTrigger(AniKeyConst.K_tAttack);
        }

        public void HandleJump()
        {
            if(!canControl) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimeCounter = 0f;
            animator?.SetBool(AniKeyConst.k_bGround, true);
        }

        #region Update
        private void UpdateAnimator()
        {
            if (animator == null) return;

            var speed = Mathf.Abs(moveInput.x);
            animator.SetBool(AniKeyConst.k_bMove, speed > 0.1f && isGrounded);
        }

        private void UpdateTimers()
        {
            coyoteTimeCounter -= Time.deltaTime;
            jumpBufferCounter -= Time.deltaTime;

            if(jumpBufferCounter > 0f && coyoteTimeCounter >0f)
            {
                HandleJump();
                jumpBufferCounter = 0f;
            }
        }
        
        private void UpdateGroundCheck()
        {
            wasGrounded = isGrounded;

            RaycastHit2D hit = Physics2D.Raycast(
                groundCheckPoint.position,
                Vector2.down,
                groundCheckDistance,
                groundLayer
                );

            isGrounded = hit.collider != null;

            if(isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }

            if(!wasGrounded && isGrounded)
            {
                OnLanded();
            }
        }

        private void OnLanded()
        {
            animator?.SetBool(AniKeyConst.k_bGround, false);
        }


        #endregion

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint == null) return;

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundCheckPoint.position, groundCheckPoint.position + Vector3.down * groundCheckDistance);
            Gizmos.DrawWireSphere(groundCheckPoint.position + Vector3.down * groundCheckDistance, 0.05f);
        }

        private void OnDestroy()
        {
            if(inputKey != null)
            {
                inputKey.Player.Jump.performed -= OnJumpPerformed;
                inputKey.Player.Attack.performed -= OnAttackPerformed;
                inputKey.Dispose();
            }
        }
    }
}
