using TMPro;
using UnityEngine;

public class UserDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI userCharNameText;

    public void SetCharName(string name)
    {
        userCharNameText.text = name;
    }
    public void OnFacingChanged(float parentScaleX)
    {
        if (userCharNameText == null) return;
        var ls = userCharNameText.transform.localScale;
        float sign = parentScaleX < 0f ? -1f : 1f;
        userCharNameText.transform.localScale = new Vector3(sign * Mathf.Abs(ls.x), ls.y, ls.z);
    }
}