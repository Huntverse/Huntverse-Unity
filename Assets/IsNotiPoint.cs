using UnityEngine;

public class IsNotiPoint : MonoBehaviour
{
    private float radius = 0.5f;
    public SpriteRenderer renderer;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
