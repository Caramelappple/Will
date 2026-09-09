using System.Collections.Generic;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Will;

namespace _Scripts.LSO.Reward
{
    public sealed class LSO_UnlockState
    {
        private readonly HashSet<LSO_AnimalSO> _unlockedPieces = new();
        private readonly HashSet<DLJ_WillDataSO> _unlockedWills = new();

        public IReadOnlyCollection<LSO_AnimalSO> Pieces => _unlockedPieces;
        public IReadOnlyCollection<DLJ_WillDataSO> Wills => _unlockedWills;

        public bool IsPieceUnlocked(LSO_AnimalSO piece) => piece != null && _unlockedPieces.Contains(piece);
        public bool IsWillUnlocked(DLJ_WillDataSO will) => will != null && _unlockedWills.Contains(will);

        public bool UnlockPiece(LSO_AnimalSO piece)
        {
            if (piece == null) return false;
            return _unlockedPieces.Add(piece);
        }

        public bool UnlockWill(DLJ_WillDataSO will)
        {
            if (will == null) return false;
            return _unlockedWills.Add(will);
        }

        public void Clear()
        {
            _unlockedPieces.Clear();
            _unlockedWills.Clear();
        }

        /// <summary>
        /// 세이브에 적을 형태로 꺼낸다.
        ///
        /// 기물은 에셋 이름, 유언은 LSO_WillType이다. 둘을 한 목록에 섞지 않는 이유는
        /// 되돌릴 때 어느 쪽인지 알 수 없어지기 때문이다.
        ///
        /// 유언을 enum 그대로 넘기지만, 세이브 파일에는 문자열로 적을 것.
        /// 나중에 enum 중간에 값을 끼워 넣어도 기존 세이브가 엉뚱한 유언으로 읽히지 않는다.
        /// </summary>
        public void Export(out string[] pieceNames, out LSO_WillType[] willTypes)
        {
            pieceNames = new string[_unlockedPieces.Count];

            int i = 0;
            foreach (LSO_AnimalSO piece in _unlockedPieces)
                pieceNames[i++] = piece.name;

            willTypes = new LSO_WillType[_unlockedWills.Count];

            i = 0;
            foreach (DLJ_WillDataSO will in _unlockedWills)
                willTypes[i++] = will.WillType;
        }

        /// <summary>
        /// 세이브에서 읽은 목록으로 통째로 되돌린다.
        ///
        /// UnlockPiece를 루프로 도는 방식과 다르다. 그쪽은 기존 해금 위에 덧씌워져서
        /// 세이브에 없는 해금이 남아버린다. 여기서는 먼저 비운다.
        ///
        /// 이름이 아니라 에셋을 받는다. 이름을 에셋으로 되돌리는 일은
        /// 어떤 목록에서 찾을지 아는 쪽(세이브 게이트웨이)의 몫이다.
        /// 여기서 찾으려 들면 이 클래스가 에셋 저장소를 알아야 한다.
        /// </summary>
        public void Import(IEnumerable<LSO_AnimalSO> pieces, IEnumerable<DLJ_WillDataSO> wills)
        {
            Clear();

            if (pieces != null)
            {
                foreach (LSO_AnimalSO piece in pieces)
                    UnlockPiece(piece);
            }

            if (wills == null) return;

            foreach (DLJ_WillDataSO will in wills)
                UnlockWill(will);
        }
    }
}