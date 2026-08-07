using _Scripts.LSO.Deck.Data;
using System.Collections.Generic;
using UnityEngine;

public class KTH_DeckDataPersistent : MonoBehaviour
{
    public static KTH_DeckDataPersistent Instance { get; private set; }

    [Header("유저가 인벤토리에 담은 최종 카드 리스트")]
    public List<LSO_CardSO> savedInventory = new List<LSO_CardSO>();

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
    public void SaveInventory(List<LSO_CardSO> newInventory)
    {
        savedInventory = new List<LSO_CardSO>(newInventory);
        Debug.Log($"[KTH_DeckDataPersistent] 덱 데이터가 저장되었습니다. (총 {savedInventory.Count}장)");
    }
}