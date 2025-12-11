using UnityEngine;

public class IsAttackPointer : MonoBehaviour
{
    public float radius = 0.5f;
    public void SetT(Vector3 pos, Vector2 size)
    {
        transform.position = pos;
        transform.localScale = size;
    }
    public Transform GetT()
    {
        return transform;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
