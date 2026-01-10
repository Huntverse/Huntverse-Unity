using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class UISpriteAutoAnimator : MonoBehaviour
{
    [Header("Target")]
    public Image image;

    [Header("Sprites")]
    public Sprite[] sprites;

    [Header("Option")]
    public float frameTime = 0.1f;
    public bool loop = true;

    Coroutine playRoutine;

    void Awake()
    {
        // 🔥 이름 기준 자동 정렬
        sprites = sprites
            .OrderBy(s => s.name)
            .ToArray();
    }

    void OnEnable()
    {
        playRoutine = StartCoroutine(Play());
    }

    void OnDisable()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);
    }

    IEnumerator Play()
    {
        int index = 0;

        while (true)
        {
            image.sprite = sprites[index];
            index++;

            if (index >= sprites.Length)
            {
                if (loop)
                    index = 0;
                else
                    yield break;
            }

            yield return new WaitForSeconds(frameTime);
        }
    }
}
