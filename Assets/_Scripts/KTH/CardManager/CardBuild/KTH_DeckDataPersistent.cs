using _Scripts.LSO.Deck.Data;
using System.Collections.Generic;
using UnityEngine;

public class KTH_DeckDataPersistent : MonoBehaviour
{
    public static KTH_DeckDataPersistent Instance { get; private set; }

    [Header("유저가 인벤토리에 담은 최종 카드 리스트")]
    [SerializeField]
    private List<LSO_CardSO> _savedInventory = new();

    public IReadOnlyList<LSO_CardSO> SavedInventory => _savedInventory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 인벤토리 카드 데이터를 저장합니다.
    /// </summary>
    public void SaveInventory(List<LSO_CardSO> newInventory)
    {
        if (newInventory == null)
        {
            Debug.LogWarning(
                $"[{nameof(KTH_DeckDataPersistent)}] 저장하려는 인벤토리가 null입니다.",
                this
            );

            _savedInventory.Clear();
            return;
        }

        _savedInventory.Clear();
        _savedInventory.AddRange(newInventory);

        Debug.Log(
            $"[{nameof(KTH_DeckDataPersistent)}] " +
            $"덱 데이터가 저장되었습니다. (총 {_savedInventory.Count}장)"
        );
    }
}