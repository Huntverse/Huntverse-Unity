using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 데미지를 받을 수 있는 객체 인터페이스
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage, Vector3 hitPosition);
        Transform GetTransform();
    }
}
