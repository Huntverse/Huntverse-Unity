using UnityEngine;
using TMPro;
using DG.Tweening; // Assuming DOTween or similar, but will use simple Coroutine if not sure. Let's stick to Coroutine/Update for dependency-free.

namespace Hunt
{
    public class DamageText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float floatSpeed = 1.0f;
        [SerializeField] private float lifeTime = 1.0f;
        
        private float timer;
        private Vector3 startPos;

        public void Setup(float damage, Vector3 position, Color color)
        {
            transform.position = position;
            if(textMesh == null) textMesh = GetComponent<TextMeshPro>();
            if(textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();
            
            textMesh.text = damage.ToString("0");
            textMesh.color = color;
            startPos = position;
            timer = 0;
            
            // Random offset for flavor
            transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0, 0.5f), 0);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            
            float alpha = Mathf.Lerp(1, 0, timer / lifeTime);
            if (textMesh != null) textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);

            if (timer >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
    }

    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [SerializeField] private GameObject damageTextPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ShowDamage(float damage, Vector3 position, bool isPlayerDamage = false)
        {
            if (damageTextPrefab == null) return;
            
            var go = Instantiate(damageTextPrefab, position, Quaternion.identity);
            var dt = go.GetComponent<DamageText>();
            if (dt != null)
            {
                Color color = isPlayerDamage ? Color.red : Color.yellow; // Red for Player hurt, Yellow for Enemy hurt
                dt.Setup(damage, position, color);
            }
        }
    }
}
