namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 해금 목록(기물/유언)을 세이브와 주고받는다.
    ///
    /// ────────────────────────────────────────────────────────────
    /// 보류 중이다. LDY 담당 파일이고 아래 두 가지가 아직 합의되지 않았다.
    ///
    ///   1. 해금 목록을 누가 들고 있을 것인가
    ///      지금 코드는 LSO_ItemLibraryManager.Claim.Unlocks 에 있다고 가정한다.
    ///
    ///   2. 이름을 에셋으로 되돌릴 때 어디서 찾을 것인가
    ///      아래 구현은 LSO_ItemLibraryManager 의 재고를 목록으로 쓴다.
    ///      "해금된 것은 재고에도 있다"를 전제로 하는데, 재고 자체가 세이브에서
    ///      복원되지 않으면(LDY_DeckSaveGateway 가 아직 주석) 되찾을 것이 없다.
    ///      전체 에셋 카탈로그가 생기면 그쪽을 보는 편이 맞다.
    ///
    /// 합의되면 아래 주석을 풀고 using 세 줄을 되살리면 된다.
    ///   using System; using System.Collections.Generic;
    ///   using _Scripts.LSO; using _Scripts.LSO.Animal.Data;
    ///   using _Scripts.LSO.Deck.Data; using _Scripts.LSO.Reward;
    ///   using _Scripts.LSO.Will; using UnityEngine;
    /// ────────────────────────────────────────────────────────────
    /// </summary>
    public sealed class LDY_UnlockSaveGateway
    {
        public void Capture(LDY_RunSaveData data)
        {
            // 적을 것이 없어도 지난 런의 값이 남지 않게 비우기는 한다.
            data.unlockedPieceNames.Clear();
            data.unlockedWillIds.Clear();

            /*
            LSO_UnlockState unlocks = Unlocks;
            if (unlocks == null) return;

            unlocks.Export(out string[] pieces, out LSO_WillType[] wills);

            data.unlockedPieceNames.AddRange(pieces);

            // 유언은 enum이지만 문자열로 적는다. 나중에 enum에 값을 끼워 넣어도
            // 기존 세이브가 엉뚱한 유언으로 읽히지 않는다.
            foreach (LSO_WillType will in wills)
                data.unlockedWillIds.Add(will.ToString());
            */
        }

        public void Restore(LDY_RunSaveData data)
        {
            /*
            LSO_UnlockState unlocks = Unlocks;
            if (unlocks == null) return;

            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            unlocks.Import(
                ResolvePieces(data.unlockedPieceNames, library),
                ResolveWills(data.unlockedWillIds, library));
            */
        }

        /*
        /// <summary>
        /// 해금 목록. 없으면 경고만 남기고 null.
        ///
        /// LSO_ItemLibraryManager가 들고 있다. 상자는 스테이지마다 사라지지만
        /// 해금 목록은 런 전체를 살아남아야 해서 DontDestroyOnLoad인 쪽에 얹혀 있다.
        /// </summary>
        private static LSO_UnlockState Unlocks
        {
            get
            {
                LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

                if (library == null || library.Claim == null)
                {
                    Debug.LogWarning(
                        "[LDY_UnlockSaveGateway] LSO_ItemLibraryManager가 없어 해금 목록을 다루지 못했습니다.");

                    return null;
                }

                return library.Claim.Unlocks;
            }
        }

        /// <summary>이름을 동물 에셋으로 되돌린다. 재고에 있는 카드들에서 찾는다.</summary>
        private static List<LSO_AnimalSO> ResolvePieces(
            List<string> names, LSO_ItemLibraryManager library)
        {
            var result = new List<LSO_AnimalSO>();

            if (names == null || names.Count == 0) return result;

            if (library == null)
            {
                Debug.LogWarning(
                    $"[LDY_UnlockSaveGateway] 재고가 없어 기물 {names.Count}개를 되돌리지 못했습니다.");

                return result;
            }

            foreach (string savedName in names)
            {
                LSO_AnimalSO found = null;

                foreach (LSO_CardSO card in library.UnlockedPieces)
                {
                    if (card == null || card.Animal == null) continue;
                    if (card.Animal.name != savedName) continue;

                    found = card.Animal;
                    break;
                }

                if (found != null)
                {
                    result.Add(found);
                    continue;
                }

                // 에셋 이름이 바뀌었거나 재고에 아직 들어오지 않았다.
                // 건너뛰되 조용히 넘기지는 않는다.
                Debug.LogWarning($"[LDY_UnlockSaveGateway] 알 수 없는 기물 '{savedName}' 입니다.");
            }

            return result;
        }

        /// <summary>문자열을 유언 에셋으로 되돌린다. enum을 거쳐 재고에서 찾는다.</summary>
        private static List<DLJ_WillDataSO> ResolveWills(
            List<string> ids, LSO_ItemLibraryManager library)
        {
            var result = new List<DLJ_WillDataSO>();

            if (ids == null || ids.Count == 0) return result;

            foreach (string willId in ids)
            {
                if (!Enum.TryParse(willId, out LSO_WillType type))
                {
                    // 이름이 바뀌었거나 사라진 유언이다.
                    Debug.LogWarning($"[LDY_UnlockSaveGateway] 알 수 없는 유언 id '{willId}' 입니다.");
                    continue;
                }

                DLJ_WillDataSO found = null;

                if (library != null)
                {
                    foreach (DLJ_WillDataSO will in library.UnlockedWills)
                    {
                        if (will == null || will.WillType != type) continue;

                        found = will;
                        break;
                    }
                }

                if (found != null)
                    result.Add(found);
                else
                    Debug.LogWarning($"[LDY_UnlockSaveGateway] 재고에서 유언 {type}를 찾지 못했습니다.");
            }

            return result;
        }
        */
    }
}
