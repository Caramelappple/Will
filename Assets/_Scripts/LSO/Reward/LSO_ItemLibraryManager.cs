using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    public class LSO_ItemLibraryManager : MonoBehaviour
    {
        public static LSO_ItemLibraryManager Instance { get; private set; }

        [Header("해금된 카드 및 유언 데이터")]
        [SerializeField] private List<LSO_CardSO> unlockedPieces = new List<LSO_CardSO>();
        [SerializeField] private List<DLJ_WillDataSO> unlockedWills = new List<DLJ_WillDataSO>();

        public event Action OnItemLibraryUpdated;

        // 외부에서 접근할 수 있는 프로퍼티
        public List<LSO_CardSO> UnlockedPieces => unlockedPieces;
        public List<DLJ_WillDataSO> UnlockedWills => unlockedWills;

        /// <summary>
        /// 보상 지급 담당. 해금 목록도 이쪽이 들고 있다.
        ///
        /// 여기 얹은 이유는 수명이 같아서다. 상자는 스테이지마다 사라지지만
        /// 해금 목록은 런 전체를 살아남아야 하고, 이 매니저가 이미 DontDestroyOnLoad다.
        /// 홀더를 하나 더 만들면 "둘 중 어느 것이 먼저 사라지나"를 따져야 한다.
        ///
        /// 재고(unlockedPieces)와 해금 목록(Claim.Unlocks)은 다른 것이다.
        /// 재고는 같은 카드를 두 장 받으면 두 장 쌓이고, 해금 목록은 한 번만 남는다.
        /// </summary>
        public LSO_RewardClaim Claim { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Instance보다 먼저 만들면 안 된다. Claim이 지급할 때 Instance를 통해
                // 이 매니저를 되찾는데, 그 시점에 아직 비어 있으면 재고에 담지 못한다.
                Claim = new LSO_RewardClaim();

                Debug.Log("[ItemLibrary] Instance 생성");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // 씬을 넘길 때 살아남은 쪽이 아니라 버려진 쪽이 여기 들어올 수 있다.
            // 자기가 Instance일 때만 지운다.
            if (Instance == this)
                Instance = null;
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

            OnItemLibraryUpdated?.Invoke();
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

            OnItemLibraryUpdated?.Invoke();
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

            OnItemLibraryUpdated?.Invoke();
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

            OnItemLibraryUpdated?.Invoke();
        }
    }
}