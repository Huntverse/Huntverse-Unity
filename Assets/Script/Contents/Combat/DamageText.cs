using TMPro;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 데미지 텍스트 표시 및 페이드 아웃 처리
    /// </summary>
    public class DamageText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMesh;

        public void Initialize(float damage, Vector3 worldPosition)
        {
            worldPosition.y += 0.5f;
            transform.localPosition = worldPosition;
            textMesh.text = Mathf.RoundToInt(damage).ToString();

        }

        public void OnAnimationEnd()
        {
            Destroy(gameObject);
        }
    }
}
