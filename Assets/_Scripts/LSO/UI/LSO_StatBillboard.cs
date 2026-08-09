using System.Net.NetworkInformation;
using _Scripts.LDY;
using TMPro;
using UnityEngine;

public class LSO_StatBillboard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI AtkText;
    [SerializeField] private TextMeshProUGUI HPText;

    public void SetText(int atk, int hp)
    {
        AtkText.text = atk.ToString();
        HPText.text = hp.ToString();
    }
}
