using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// Slime Enemy behavior.
    /// Jumps and smashes the ground to attack.
    /// </summary>
    public class Slime : Enemy
    {
        [Header("SLIME COMBAT")]
        [SerializeField] private float jumpPower = 5.0f;
        [SerializeField] private float attackDuration = 1.5f; // Total time for jump attack
        [SerializeField] private float damageRadius = 1.5f;
        [SerializeField] private float attackDamage = 10f;
        
        // Attack State Flag to prevent re-entry during animation
        private bool _isAttacking;

        protected override void OnAttack()
        {
            if (_isAttacking) return;
            
            // Start Attack Sequence
            PerformJumpAttack().Forget();
        }

        private async UniTaskVoid PerformJumpAttack()
        {
            _isAttacking = true;
            
            // 1. Face Player before jumping
            if (target != null)
            {
                float xDiff = target.position.x - transform.position.x;
                if ((xDiff > 0 && moveDir == -1) || (xDiff < 0 && moveDir == 1)) Flip();
            }

            // 2. Initial Jump Visual (Trigger Animation)
            animator.SetTrigger(AniKeyConst.K_tAttack);
            
            // 3. Simulate Jump Movement (Simple parabolic arc or just vertical offset)
            // Use transform based leap
            Vector3 startPos = transform.position;
            
            // Calculate landing position respect to StopDistance/AttackRange
            // Instead of landing ON the player, land slightly in front
            Vector3 targetPos = startPos;
            if (target != null)
            {
                // Move towards target but stop at 'stopDistance' (or slightly closer for impact)
                Vector3 directionToTarget = (target.position - startPos).normalized;
                float distance = Vector3.Distance(startPos, target.position);
                
                // If we are far, jump to (Target - StopDistance)
                // If we are already close, just jump in place or adjust slightly
                float landingDist = Mathf.Max(0, distance - (stopDistance * 0.8f)); // Land slightly inside stop distance to ensure hit
                targetPos = startPos + directionToTarget * landingDist;
            }
            else
            {
                targetPos = startPos + transform.right * moveDir;
            }
            
            // Simple Jump Simulation using Tween-like logic over time
            float timer = 0f;
            float flightDuration = 0.8f; // Time in air
            
            while (timer < flightDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / flightDuration;
                
                // Parabolic Height: 4 * h * x * (1-x)
                float height = 4 * jumpPower * progress * (1 - progress); 
                
                // Linear Move towards target (with limit)
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
                currentPos.y += height; // Apply Jump Height
                
                transform.position = currentPos;
                
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // 4. Land and Deal Damage (AoE)
            // Reset position to ground level (assuming 0Y relative to start, or rely on physics/ground check logic)
            // For now, snap to target Y or original Y
            transform.position = new Vector3(transform.position.x, startPos.y, transform.position.z);
            
            PlayVfx(hitVfx, transform.position); // Splash effect on landing
            CheckAoEDamage();
            
            // 5. Recovery Delay
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f));
            
            _isAttacking = false;
            
            // Return to Chase or Idle based on situation
            ChangeState(EnemyState.Chase);
        }

        private void CheckAoEDamage()
        {
            // Detect players in range
            Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, LayerMask.GetMask("Player")); // Ensure 'Player' layer is set
            
            foreach (var hit in hits)
            {
                // Can check for IDamageable interface on Player
                // var player = hit.GetComponent<IDamageable>();
                // if (player != null) player.TakeDamage(attackDamage);
                
                // Or simplified for now:
                $"Slime Hit Player: {hit.name}".DLog(); 
            }
        }
        
        // Example: Override Idle to bounce slightly?
        /*
        protected override void OnIdle()
        {
            base.OnIdle();
            // Add slime bounce animation logic here if needed
        }
        */
    }
}
