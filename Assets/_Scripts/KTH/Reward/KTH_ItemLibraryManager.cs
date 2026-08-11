using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.LSO.Deck.Data; // LSO_CardSO
using _Scripts.LSO.Will;

public class ItemLibraryManager : MonoBehaviour
{
    public static ItemLibraryManager Instance { get; private set; }

    [Header("해금된 카드 및 유언 데이터")]
    public List<LSO_CardSO> unlockedPieces = new List<LSO_CardSO>();
    public List<DLJ_WillDataSO> unlockedWills = new List<DLJ_WillDataSO>();

    public event Action onItemLibraryUpdated;

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

    public void AddPiecesToLibrary(List<LSO_CardSO> newCards)
    {
        if (newCards == null) return;

        foreach (var card in newCards)
        {
            if (card != null && !unlockedPieces.Contains(card))
            {
                unlockedPieces.Add(card);
            }
        }

        onItemLibraryUpdated?.Invoke();
    }

    public void AddWillsToLibrary(List<DLJ_WillDataSO> newWills)
    {
        if (newWills == null) return;

        foreach (var will in newWills)
        {
            if (will != null && !unlockedWills.Contains(will))
            {
                unlockedWills.Add(will);
            }
        }

        onItemLibraryUpdated?.Invoke();
    }
}