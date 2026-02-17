using UnityEngine;
using TMPro;

namespace Hunt
{
    public class DamageText : MonoBehaviour
    {
        private TextMeshProUGUI textMesh;
        [SerializeField] private float floatSpeed = 1.0f;
        [SerializeField] private float lifeTime = 1.0f;
        
        private float timer;
        private Vector3 startPos;

        public static void Spawn(GameObject prefab, float damage, Vector3 position, Color color)
        {
            if (prefab == null) return;
            var go = Instantiate(prefab, position, Quaternion.identity);
            var dt = go.GetComponent<DamageText>();
            if (dt != null)
            {
                dt.Setup(damage, position, color);
            }
        }

        public void Setup(float damage, Vector3 position, Color color)
        {
            transform.position = position;
    
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
            
            textMesh.text = damage.ToString("0"); // Integer style
            textMesh.color = color;
            startPos = position;
            timer = 0;
            
            // Random offset for flavor
            transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 1.0f), 0);
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
}

