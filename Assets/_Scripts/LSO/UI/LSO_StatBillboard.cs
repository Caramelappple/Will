using TMPro;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    public class LSO_StatBillboard : MonoBehaviour
    {
        [SerializeField] private TextMeshPro atkText;
        [SerializeField] private TextMeshPro hpText;
    
        public void SetAtkText(int atk)
        {
            atkText.text = atk.ToString();
        }

        public void SetHpText(int hp)
        {
            hpText.text = hp.ToString();
        }
    }
}
