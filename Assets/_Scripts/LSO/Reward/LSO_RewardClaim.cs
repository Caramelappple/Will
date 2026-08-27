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
        
        public bool Claim(LSO_RewardOption option)
        {
            if (option == null)
            {
                Debug.LogError("[LSO_RewardClaim] 지급할 보상이 비어 있습니다.");
                return false;
            }

            bool claimed = option.type switch
            {
                LSO_RewardType.Piece => ClaimPiece(option.piece),
                LSO_RewardType.Will => ClaimWill(option.will),
                _ => false
            };

            if (!claimed) return false;

            OnClaimed?.Invoke(option);
            return true;
        }

        private bool ClaimPiece(LSO_CardSO card)
        {
            if (card == null)
            {
                Debug.LogError("[LSO_RewardClaim] 기물 보상인데 카드가 비어 있습니다.");
                return false;
            }

            // 해금은 동물 단위다. 같은 동물을 담은 카드가 여럿이어도 도감에는 하나로 남는다.
            LSO_AnimalSO animal = card.Animal;

            if (animal != null)
                _unlocks.UnlockPiece(animal);
            else
                Debug.LogWarning($"[LSO_RewardClaim] {card.name}에 동물 데이터가 없어 해금 기록을 남기지 못했습니다.", card);

            Library?.AddPieceToLibrary(card);

            return true;
        }

        private bool ClaimWill(DLJ_WillDataSO will)
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
