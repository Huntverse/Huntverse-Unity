using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{

    public class Enemy : MonoBehaviour, IDamageable
    {
        #region Common Components
        protected Collider col;
     
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        
        [SerializeField] protected GameObject enemyNameField;
        #endregion

        #region Common Stats
        [Header("BASE STATS")]
        [SerializeField] protected float moveSpeed = 2f;
        [SerializeField] protected float maxHp = 100f;
        [SerializeField] protected float hitStunDuration = 0.3f;
        [SerializeField] protected float flashInterval = 0.1f;
        protected float currentHp;
        protected bool isHitStunned;
        protected int moveDir = -1;
        protected Color originalColor;
        #endregion

        #region Physics & Check
        [Header("PHYSICS & AI")]
        [SerializeField] protected LayerMask groundLayer;
        [SerializeField] protected LayerMask playerLayer; // Added
        [SerializeField] protected float detectionRange = 8.0f;
        [SerializeField] protected float stopDistance = 1.2f;
        protected Transform target;
        
        protected float _detectionTimer;
        protected const float DETECTION_INTERVAL = 0.5f;
        #endregion

        protected virtual void Start()
        {
            InitializeCommon();
        }

        protected void InitializeCommon()
        {
            
            col = GetComponent<Collider>(); // Collider should be on Root for Logic/Sync
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) originalColor = spriteRenderer.color;

            if (enemyNameField != null)
            {
                enemyNameField.transform.SetParent(transform);
                var position = new Vector3(0, col != null ? -(col.bounds.center.y - 1.0f) : 2.0f, 0);
                enemyNameField.transform.localPosition = position;

                this.DLog($"NameField Position : {position}");
            }

            currentHp = maxHp;
            
            currentHp = maxHp;
            
            // Target acquisition moved to Periodic Update based on LayerMask
            target = null;

            spawnPosition = transform.position;
            wanderTarget = spawnPosition;
        }

        protected virtual void Update()
        {
            if (col == null) return;
            if (isHitStunned) return; // Stun state blocks behavior
            if (currentHp <= 0) return; // Dead

            // Periodic Detection Check
            _detectionTimer += Time.deltaTime;
            if (_detectionTimer >= DETECTION_INTERVAL)
            {
                _detectionTimer = 0f;
                DetectTarget();
            }

            ProcessBehavior();
        }

        /// <summary>
        /// Scans for players within detection range using OverlapSphere and LayerMask.
        /// </summary>
        protected virtual void DetectTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
            
            Transform closest = null;
            float minDst = float.MaxValue;

            foreach (var hit in hits)
            {
                float dst = Vector3.Distance(transform.position, hit.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    closest = hit.transform;
                }
            }

            target = closest;
        }

        public enum EnemyState { Idle, Chase, Attack }

        [Header("AI STATE")]
        [SerializeField] protected EnemyState currentState = EnemyState.Idle;
        [SerializeField] protected float attackRange = 1.0f;
        [SerializeField] protected float wanderRadius = 3.0f;
        [SerializeField] protected float wanderInterval = 3.0f;
        
        protected float stateTimer;
        protected Vector3 wanderTarget;
        protected Vector3 spawnPosition;

// ... (Start Update) ...

        protected virtual void ProcessBehavior()
        {
            // [Server/Client Sync Check can go here]
            
            // 1. Global Transitions (Check for Target)
            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist < attackRange) ChangeState(EnemyState.Attack);
                else if (dist < detectionRange) ChangeState(EnemyState.Chase);
                else ChangeState(EnemyState.Idle);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }

            // 2. State Execution
            switch (currentState)
            {
                case EnemyState.Idle:   OnIdle();   break;
                case EnemyState.Chase:  OnChase();  break;
                case EnemyState.Attack: OnAttack(); break;
            }
        }

        protected virtual void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            stateTimer = 0f; // Reset timer on state change

            switch (currentState)
            {
                case EnemyState.Idle:
                    animator.SetBool(AniKeyConst.K_bChase, false);
                    break;
                case EnemyState.Chase:
                    animator.SetBool(AniKeyConst.K_bChase, true);
                    break;
                case EnemyState.Attack:
                    animator.SetBool(AniKeyConst.K_bChase, false);
                    break;
            }
        }

        /// <summary>
        /// Wanders around the spawn point randomly.
        /// </summary>
        protected virtual void OnIdle()
        {
            // Simple timer-based wandering
            stateTimer += Time.deltaTime;
            
            if (stateTimer > wanderInterval)
            {
                // Pick a new random point around spawn
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                wanderTarget = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                stateTimer = 0f; // Reset to move towards it
            }

            // Move towards wander target if far enough
            if (Vector3.Distance(transform.position, wanderTarget) > 0.1f)
            {
                Vector3 dir = (wanderTarget - transform.position).normalized;
                MoveTo(dir * 0.5f); // Move slower when wandering
            }
        }

        protected virtual void OnChase()
        {
            if (target == null)
            {
                ChangeState(EnemyState.Idle);
                return;
            }
            
            Vector3 dir = (target.position - transform.position).normalized;
            MoveTo(dir);
        }

        protected virtual void OnAttack()
        {
            // Stub: Override in child classes (e.g. MeleeEnemy)
            // Face the target
            if (target != null)
            {
                animator.SetTrigger(AniKeyConst.K_tAttack);
                float xDiff = target.position.x - transform.position.x;
                if (xDiff > 0 && moveDir == -1) Flip();
                else if (xDiff < 0 && moveDir == 1) Flip();
            }
            
            // Attack logic (cooldown, animation trigger) would go here
            // Debug.Log("Attacking!"); 
        }

        /// <summary>
        /// Common Movement Method.
        /// </summary>
        protected void MoveTo(Vector3 direction)
        {
            // [Server Sync Ready]
            // Instead of Physics forces, we modify Transform directly.
            // On a real Client, this would be replacing local 'pos' with 'serverPos'.
            // On the Host/Server, this calculates the new 'pos'.

            // Simple Transform Movement (No Physics/Gravity simulated here for now)
            Vector3 moveVector = new Vector3(direction.x, 0, direction.z) * moveSpeed * Time.deltaTime;
            transform.Translate(moveVector);

            // Facing
            if (direction.x > 0 && moveDir == -1) Flip();
            else if (direction.x < 0 && moveDir == 1) Flip();
        }

        protected virtual void Flip()
        {
            moveDir *= -1;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
            }
        }

        public virtual void TakeDamage(float damage, Vector3 hitPosition)
        {
            // TODO: [Server Sync] Validate hit on server before applying damage
            currentHp -= damage;
            if (currentHp <= 0f)
            {
                currentHp = 0f;
                OnDeath();
                return;
            }

            OnHit().Forget();
        }

        [Header("FX SETTINGS")]
        [SerializeField] protected VfxType hitVfx = VfxType.Hit_Normal;
        [SerializeField] protected VfxType deathVfx = VfxType.None;
        [SerializeField] protected AudioType hitSfx = AudioType.None; // Add SfxType enum if available, assuming AudioType exists based on context
        [SerializeField] protected AudioType deathSfx = AudioType.None;

        // ... existing methods ...

        protected async UniTaskVoid OnHit()
        {
            isHitStunned = true;
            
            // Use helper with configured type
            PlayVfx(hitVfx, col.bounds.center); 
            PlaySfx(hitSfx);

            // Visual Effect only (Flash)
            int flashCount = Mathf.RoundToInt(hitStunDuration / flashInterval);
            for (int i = 0; i < flashCount; i++)
            {
                if (spriteRenderer != null) spriteRenderer.color = (i % 2 == 0) ? Color.white : Color.black;
                await UniTask.Delay(System.TimeSpan.FromSeconds(flashInterval));
            }
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            isHitStunned = false;
        }

        protected virtual void OnDeath()
        {
            // TODO: [Server Sync] Send death packet, spawn loot
            PlayVfx(deathVfx, col.bounds.center);
            PlaySfx(deathSfx);
            Destroy(gameObject);
        }
        
        #region FX Helpers
        protected void PlayVfx(VfxType type, Vector3 position)
        {
            if (type == VfxType.None) return;
            string key = VfxKeyConst.GetVfxKey(type);
            if (!string.IsNullOrEmpty(key))
            {
                VfxManager.Shared.PlayOneShot(key, position, Quaternion.identity).Forget();
            }
        }

        protected void PlaySfx(AudioType type)
        {
            if (type == AudioType.None) return;
            string key = AudioKeyConst.GetSfxKey(type); // Assuming AudioKeyConst exists similar to VfxKeyConst
            if (!string.IsNullOrEmpty(key))
            {
                AudioManager.Shared.PlaySfx(key);
            }
        }
        #endregion

        public Transform GetTransform() => transform;
    }

}