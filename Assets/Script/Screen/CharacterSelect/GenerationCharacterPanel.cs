
using TMPro;
using UnityEngine;

namespace hunt
{
    public class GenerationCharacterPanel : MonoBehaviour
    {
        [SerializeField] private PentagonBalanceUI pentagonBalanceUI;
        [SerializeField] private TextMeshProUGUI characterStoryText;
 
        public void SetStats(float attack, float defense, float movespeed, float hp, float agility)
        {
            if (attack <= 0 || defense <= 0 || movespeed <= 0 || hp <= 0 || agility <= 0)
            {
                pentagonBalanceUI.AnimateStatsFromZero(0.5f, 0.5f, 0.5f, 0.5f, 0.5f,1f);
            }
            pentagonBalanceUI.AnimateStatsFromZero(attack, defense, movespeed, hp, agility, 1f);
        }
        public float[] GetStats(float attack, float defense, float movespeed, float hp, float agility)
        {
            return new float[] { attack, defense, movespeed, hp, agility };
        }
        public string GetStoryText(string s)
        {
            return s;
        }
        public void SetStroyText(string s)
        {
            characterStoryText.text = s;
        }

        public (string, float[] f) OnSetFieldValue(string stroy, float[] f)
        {
            
            SetStroyText(GetStoryText(stroy));
            SetStats(0.8f, 0.6f, 0.2f, 0.6f, 0.5f);
            return (GetStoryText(stroy), GetStats(0.8f, 0.6f, 0.2f, 0.6f, 0.5f));
        }
    }
}