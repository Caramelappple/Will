using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;

public class ItemLibraryManager : MonoBehaviour
{
    public static ItemLibraryManager Instance { get; private set; }

    [Header("해금된 카드 및 유언 데이터")]
    [SerializeField] private List<LSO_CardSO> unlockedPieces = new List<LSO_CardSO>();
    [SerializeField] private List<DLJ_WillDataSO> unlockedWills = new List<DLJ_WillDataSO>();

    public event Action onItemLibraryUpdated;

    // 외부에서 접근할 수 있는 프로퍼티
    public List<LSO_CardSO> UnlockedPieces => unlockedPieces;
    public List<DLJ_WillDataSO> UnlockedWills => unlockedWills;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[ItemLibrary] Instance 생성");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // 카드 리스트 추가 (중복 허용)
    // =========================================================
    public void AddPiecesToLibrary(List<LSO_CardSO> newCards)
    {
        if (newCards == null || newCards.Count == 0)
        {
            Debug.LogWarning("[ItemLibrary] 추가할 카드가 없습니다.");
            return;
        }

        foreach (LSO_CardSO card in newCards)
        {
            if (card == null) continue;

            // ⭐ 중복 체크 제거: 동일 카드라도 리스트에 계속 추가됨
            unlockedPieces.Add(card);

            Debug.Log($"[ItemLibrary] 카드 추가 완료: {card.name} / 현재 총 카드 개수: {unlockedPieces.Count}");
        }

        onItemLibraryUpdated?.Invoke();
    }

    // =========================================================
    // 유언 리스트 추가 (중복 허용)
    // =========================================================
    public void AddWillsToLibrary(List<DLJ_WillDataSO> newWills)
    {
        if (newWills == null || newWills.Count == 0)
        {
            Debug.LogWarning("[ItemLibrary] 추가할 유언이 없습니다.");
            return;
        }

        foreach (DLJ_WillDataSO will in newWills)
        {
            if (will == null) continue;

            // ⭐ 중복 체크 제거: 동일 유언이라도 리스트에 계속 추가됨
            unlockedWills.Add(will);

            Debug.Log($"[ItemLibrary] 유언 추가 완료: {will.name} / 현재 총 유언 개수: {unlockedWills.Count}");
        }

        onItemLibraryUpdated?.Invoke();
    }

    // =========================================================
    // 카드 하나만 추가 (중복 허용)
    // =========================================================
    public void AddPieceToLibrary(LSO_CardSO card)
    {
        if (card == null)
        {
            Debug.LogWarning("[ItemLibrary] 추가하려는 CardSO가 NULL입니다.");
            return;
        }

        // ⭐ 중복 체크(Contains) 제거
        unlockedPieces.Add(card);

        Debug.Log($"[ItemLibrary] 카드 1개 추가: {card.name} / 현재 총 개수: {unlockedPieces.Count}");

        onItemLibraryUpdated?.Invoke();
    }

    // =========================================================
    // 유언 하나만 추가 (중복 허용)
    // =========================================================
    public void AddWillToLibrary(DLJ_WillDataSO will)
    {
        if (will == null)
        {
            Debug.LogWarning("[ItemLibrary] 추가하려는 WillSO가 NULL입니다.");
            return;
        }

        // ⭐ 중복 체크(Contains) 제거
        unlockedWills.Add(will);

        Debug.Log($"[ItemLibrary] 유언 1개 추가: {will.name} / 현재 총 개수: {unlockedWills.Count}");

        onItemLibraryUpdated?.Invoke();
    }
}