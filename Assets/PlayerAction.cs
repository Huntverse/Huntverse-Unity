using Cysharp.Threading.Tasks;
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
        #region Private Field

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
        private InputManager inputKey;
        #endregion
        private void Awake()
        {
            UniTask.WaitUntil(() => !InputManager.Shared);
            inputKey = InputManager.Shared;
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
            HandleMovement();
        }



        public void HandleInput()
        {
            if (isAttacking) return;
            moveInput = inputKey.Player.Move.ReadValue<Vector2>();
            if (moveInput.x > 0.1f)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (moveInput.x < -0.1f)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        public bool isJumpping = false;
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
      

        // Sync NetWork
        public void HandleMovement()
        {
            if (isAttacking) return;
            float velx = moveInput.x * moveSpeed;
            rb.linearVelocity = new Vector2(velx, rb.linearVelocity.y);

        }

        // Sync NetWork
        public void HandleAttack()
        {
            if(!canControl || isAttacking) return;
            isAttacking = true;
            animator?.SetTrigger(AniKeyConst.K_tAttack);
        }

        public void HandleJump()
        {
            if(!canControl || isAttacking) return;

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
                inputKey.Action.Dispose();
            }
        }
    }
}
