using System.Collections.Generic;
using UnityEngine;

public class KTH_DeckDataPersistent : MonoBehaviour
{
    public static KTH_DeckDataPersistent Instance { get; private set; }

    [Header("유저가 인벤토리에 담은 최종 카드 리스트")]
    public List<KTH_CardData> savedInventory = new List<KTH_CardData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>인벤토리 카드 데이터 저장</summary>
    public void SaveInventory(List<KTH_CardData> newInventory)
    {
        savedInventory = new List<KTH_CardData>(newInventory);
    }
}