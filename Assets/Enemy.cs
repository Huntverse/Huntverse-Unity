using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{

    public class Enemy : MonoBehaviour, IDamageable
    {
        private Collider2D collider;
        [SerializeField] private GameObject enemyNameField;
        [SerializeField] private GameObject model;

        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float jumpInterval = 2f;
        [SerializeField] private float edgeLookAhead = 0.3f;
        private Animator animator;
        [SerializeField] private string jumpTriggerName = "Jump";

        [Header("COMBAT")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float hitStunDuration = 0.3f;
        [SerializeField] private float flashInterval = 0.1f;
        private float currentHp;
        private bool isHitStunned;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;

        private int moveDir = -1;
        private float jumpTimer;
        private Rigidbody2D rb;
        private bool wasGrounded;

        private void Start()
        {
            rb = model.GetComponent<Rigidbody2D>();

            collider = model.GetComponent<Collider2D>();
            animator = model.GetComponent<Animator>();
            spriteRenderer = model.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            enemyNameField.transform.SetParent(model.transform);
            var position = new Vector3(
                0,
                -collider.bounds.max.y + 1.5f,
                0
            );
            enemyNameField.transform.localPosition = position;

            currentHp = maxHp;
        }

        private void Update()
        {
            if (model == null || collider == null) return;
            if (isHitStunned) return;

            transform.Translate(Vector2.right * moveDir * moveSpeed * Time.deltaTime);

            var bounds = collider.bounds;

            var groundCheckOrigin = bounds.center;
            float groundCheckDist = bounds.extents.y + 0.05f;
            bool isGrounded = Physics2D.Raycast(groundCheckOrigin, Vector2.down, groundCheckDist, groundLayer);

            // 착지 감지 (이전 프레임에 공중이었다가 지금 땅에 착지)
            bool justLanded = !wasGrounded && isGrounded;

            // 콜라이더 끝에서 진행 방향으로 조금 앞에서 체크 (플랫폼 끝 도달 전에 방향 전환)
            float colliderEdgeX = moveDir > 0 ? bounds.max.x : bounds.min.x;
            float edgeX = colliderEdgeX + moveDir * edgeLookAhead;
            float edgeY = bounds.min.y + 0.05f;
            Vector2 edgeOrigin = new Vector2(edgeX, edgeY);

            float checkDistance = 0.3f;
            bool hasGroundAhead = Physics2D.Raycast(edgeOrigin, Vector2.down, checkDistance, groundLayer);
            
            // 디버그: 앞쪽 땅 체크 레이 그리기
            Debug.DrawRay(edgeOrigin, Vector2.down * checkDistance, hasGroundAhead ? Color.green : Color.red, 0.1f);

            Vector2 wallOrigin = new Vector2(edgeX, bounds.center.y);
            bool hasWallAhead = Physics2D.Raycast(wallOrigin, new Vector2(moveDir, 0f), 1.1f, groundLayer);


            if (isGrounded)
            {
                if (justLanded)
                {
                    // 착지 직후에는 앞쪽 땅이 없으면 무조건 방향 전환
                    if (!hasGroundAhead)
                    {
                        Flip();
                    }
                }
                else
                {
                    // 평소에는 앞쪽 땅이 없거나 벽이 있으면 방향 전환
                    if (!hasGroundAhead || hasWallAhead)
                    {
                        Flip();
                    }
                }
            }

            wasGrounded = isGrounded;
            var origin = model.transform.localPosition;
            var dir = Vector2.down;
            float distance = 1f;

            if (rb != null && jumpInterval > 0f)
            {
                jumpTimer += Time.deltaTime;
                if (jumpTimer >= jumpInterval)
                {
                    jumpTimer = 0f;


                    Debug.DrawRay(origin, dir * distance, Color.green, 0.1f);

                    var groundHit = Physics2D.Raycast(origin, dir, distance, groundLayer);

                    if (groundHit && rb != null)
                    {
                        $"jump!".DLog();
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

                        if (animator != null && !string.IsNullOrEmpty(jumpTriggerName))
                        {
                            animator.SetTrigger(jumpTriggerName);
                        }
                    }
                }
            }
        }

        private void Flip()
        {
            moveDir *= -1;
            if (model != null)
            {
                var sr = model.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.flipX = !sr.flipX;
                }
            }
        }

        public void TakeDamage(float damage, Vector3 hitPosition)
        {
            currentHp -= damage;
            if (currentHp <= 0f)
            {
                currentHp = 0f;
                OnDeath();
                return;
            }

            OnHit().Forget();
        }

        private async UniTaskVoid OnHit()
        {
            isHitStunned = true;
            int flashCount = Mathf.RoundToInt(hitStunDuration / flashInterval);

            for (int i = 0; i < flashCount; i++)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = i % 2 == 0 ? Color.white : Color.black;
                }
                await UniTask.Delay(System.TimeSpan.FromSeconds(flashInterval));
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            isHitStunned = false;
        }

        public Transform GetTransform()
        {
            return transform;
        }

        private void OnDeath()
        {
            Destroy(gameObject);
        }

    }

}