using System;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    public sealed class LSO_RewardClaim
    {
        private readonly LSO_UnlockState _unlocks;
        
        public LSO_UnlockState Unlocks => _unlocks;
        
        public event Action<LSO_RewardOption> OnClaimed;

        public LSO_RewardClaim(LSO_UnlockState unlocks = null)
        {
            // 세이브에서 되돌린 해금 목록을 그대로 이어받을 수 있게 밖에서도 넣을 수 있다.
            _unlocks = unlocks ?? new LSO_UnlockState();
        }
        
        /// <summary>
        /// 보상을 지급한다.
        ///
        /// includeAttachedWill을 끄면 카드에 딸린 유언은 건너뛴다.
        /// 보상 상자가 메모장을 보여준 뒤에 유언을 풀기 때문이다 — 지급이 먼저면
        /// 메모장이 올라오기도 전에 해금이 끝나 순서가 어긋난다.
        /// 그때는 부르는 쪽이 ClaimWill로 마저 풀어줘야 한다.
        /// </summary>
        public bool Claim(LSO_RewardOption option, bool includeAttachedWill = true)
        {
            if (option == null)
            {
                Debug.LogError("[LSO_RewardClaim] 지급할 보상이 비어 있습니다.");
                return false;
            }

            bool claimed = option.type switch
            {
                LSO_RewardType.Piece => ClaimPiece(option, includeAttachedWill),
                LSO_RewardType.Will => ClaimWill(option.will),
                _ => false
            };

            if (!claimed) return false;

            OnClaimed?.Invoke(option);
            return true;
        }

        private bool ClaimPiece(LSO_RewardOption option, bool includeAttachedWill)
        {
            LSO_CardSO card = option.piece;

            if (card == null)
            {
                Debug.LogError("[LSO_RewardClaim] 기물 보상인데 카드가 비어 있습니다.");
                return false;
            }

            if (includeAttachedWill) ClaimAttachedWill(option);

            // 해금은 동물 단위다. 같은 동물을 담은 카드가 여럿이어도 도감에는 하나로 남는다.
            LSO_AnimalSO animal = card.Animal;

            if (animal != null)
                _unlocks.UnlockPiece(animal);
            else
                Debug.LogWarning($"[LSO_RewardClaim] {card.name}에 동물 데이터가 없어 해금 기록을 남기지 못했습니다.", card);

            Library?.AddPieceToLibrary(card);

            return true;
        }

        /// <summary>
        /// 카드에 딸린 유언을 함께 푼다. 없으면 아무 일도 하지 않는다.
        ///
        /// 카드 지급이 실패하면 여기까지 오지 않는다. 유언만 풀리고 기물은 못 받는
        /// 어중간한 상태를 만들지 않기 위해서다.
        /// </summary>
        private void ClaimAttachedWill(LSO_RewardOption option)
        {
            if (option.will == null) return;

            ClaimWill(option.will);
        }

        /// <summary>
        /// 유언 하나만 푼다. 상자가 메모장을 다 보여준 뒤에 부른다.
        ///
        /// 공개해 둔 이유는 지급 시점을 밖에서 정해야 해서다.
        /// 같은 유언을 두 번 풀어도 해금 목록은 집합이라 하나로 남는다.
        /// </summary>
        public bool ClaimWill(DLJ_WillDataSO will)
        {
            if (will == null)
            {
                Debug.LogError("[LSO_RewardClaim] 유언 보상인데 유언 데이터가 비어 있습니다.");
                return false;
            }

            _unlocks.UnlockWill(will);

            Library?.AddWillToLibrary(will);

            return true;
        }
        
        private LSO_ItemLibraryManager Library
        {
            get
            {
                LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

                if (library == null)
                    Debug.LogWarning("[LSO_RewardClaim] LSO_ItemLibraryManager가 없어 재고에 담지 못했습니다.");

                return library;
            }
        }
    }
}
