using _Scripts.LSO.CoreLib;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 카드와 메모장을 빌려주고 돌려받는다. 어디에 놓일지는 모른다.
    ///
    /// 종류마다 풀을 따로 둔다. 하나로 묶으면 꺼낼 때마다 기물인지 유언인지 확인해야 하고,
    /// 잘못 꺼낸 카드가 조용히 빈 채로 나온다.
    ///
    /// 돌려받을 때 트랜스폼을 되돌리는 것도 여기 몫이다.
    /// LSO_ObjectPool은 컴포넌트 상태만 비울 뿐 자리와 크기는 모른다 —
    /// 덱으로 날아가 작아진 카드를 그대로 돌려보내면 다음 보상에서 그 크기로 나온다.
    ///
    /// 상자 속 자리를 들고 있는 이유도 같다. 쉬는 카드가 있어야 할 곳을 아는 것은
    /// 빌려주는 쪽이 맞고, 그래야 LSO_RewardBox가 "언제 무엇을" 만 신경 쓸 수 있다.
    /// </summary>
    public sealed class LSO_RewardCardPool
    {
        private readonly LSO_RewardPieceCard _piecePrefab;
        private readonly LSO_WillNote _notePrefab;

        private readonly Transform _cardAnchor;
        private readonly Transform _cardInsideAnchor;
        private readonly Transform _noteAnchor;
        private readonly Transform _noteInsideAnchor;

        private readonly LSO_ObjectPool<LSO_RewardPieceCard> _pieces;
        private readonly LSO_ObjectPool<LSO_WillNote> _notes;

        /// <param name="piecePrefab">기물 카드 원본. 없으면 카드를 꺼낼 수 없다.</param>
        /// <param name="notePrefab">유언 메모장 원본. 없으면 메모장을 꺼낼 수 없다.</param>
        public LSO_RewardCardPool(
            LSO_RewardPieceCard piecePrefab, Transform cardAnchor, Transform cardInsideAnchor,
            LSO_WillNote notePrefab, Transform noteAnchor, Transform noteInsideAnchor)
        {
            _piecePrefab = piecePrefab;
            _notePrefab = notePrefab;

            _cardAnchor = cardAnchor;
            _cardInsideAnchor = cardInsideAnchor;
            _noteAnchor = noteAnchor;
            _noteInsideAnchor = noteInsideAnchor;

            // 후보는 보통 셋이다. 미리 만들어 두면 첫 스테이지에서 끊기지 않는다.
            if (piecePrefab != null)
                _pieces = new LSO_ObjectPool<LSO_RewardPieceCard>(piecePrefab, cardAnchor, prewarm: 3);

            // 메모장은 한 번에 한 장이지만, 연출이 끊겨 돌려받지 못한 것이 쌓일 수 있다.
            if (notePrefab != null)
                _notes = new LSO_ObjectPool<LSO_WillNote>(notePrefab, noteAnchor, prewarm: 2);
        }

        /// <summary>둘 중 하나라도 꺼낼 수 있는지. 아무것도 없으면 보상을 시작할 수 없다.</summary>
        public bool HasAny => _pieces != null || _notes != null;

        /// <summary>메모장을 꺼낼 수 있는지. 유언 보상을 보여줄지 정할 때 본다.</summary>
        public bool HasNote => _notes != null;

        /// <summary>카드가 드나드는 상자 속 자리. Card Anchor 기준의 로컬 좌표다.</summary>
        public Vector3 CardInsideLocal => LocalOf(_cardAnchor, _cardInsideAnchor);

        /// <summary>메모장이 드나드는 상자 속 자리. Note Anchor 기준의 로컬 좌표다.</summary>
        public Vector3 NoteInsideLocal => LocalOf(_noteAnchor, _noteInsideAnchor);

        /// <summary>기물 카드 한 장. 원본을 안 꽂았으면 null.</summary>
        public LSO_RewardPieceCard TakePiece()
        {
            return _pieces?.Get();
        }

        /// <summary>메모장 한 장. 원본을 안 꽂았으면 null.</summary>
        public LSO_WillNote TakeNote()
        {
            return _notes?.Get();
        }

        /// <summary>
        /// 다 쓴 것을 제 풀로 돌려보낸다.
        ///
        /// 어느 풀에서 왔는지는 실제 타입으로 정한다. 카드에 출처를 적어두는 방법도 있지만,
        /// 그러면 상태가 하나 늘고 그 값이 실제와 어긋날 수 있는 자리가 생긴다.
        /// </summary>
        public void Return(LSO_RewardCard card)
        {
            if (card == null) return;

            RestoreTransform(card);

            switch (card)
            {
                case LSO_WillNote note when _notes != null:
                    _notes.Release(note);
                    break;

                case LSO_RewardPieceCard piece when _pieces != null:
                    _pieces.Release(piece);
                    break;

                default:
                    Debug.LogWarning($"{card.name}: 돌려보낼 풀을 찾지 못해 그대로 껐습니다.", card);
                    card.gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// 쉬는 자리로 되돌린다.
        ///
        /// 덱으로 날아간 카드는 크기가 줄어 있고 상자에서 멀리 떨어져 있다.
        /// 그대로 돌려보내면 다음 보상에서 작은 카드가 엉뚱한 자리에서 나온다.
        /// </summary>
        private void RestoreTransform(LSO_RewardCard card)
        {
            Transform t = card.transform;

            t.DOKill();

            bool isNote = card is LSO_WillNote;

            t.SetParent(isNote ? _noteAnchor : _cardAnchor, false);
            t.localPosition = isNote ? NoteInsideLocal : CardInsideLocal;
            t.localRotation = Quaternion.identity;

            // Vector3.one으로 되돌리지 않는다. 프리팹 크기가 1이 아닐 수 있고,
            // 그러면 두 번째 보상부터 카드 크기가 달라진다.
            t.localScale = PrefabScaleOf(card);
        }

        /// <summary>이 카드를 꺼낸 원본의 크기. 되돌릴 기준이다.</summary>
        private Vector3 PrefabScaleOf(LSO_RewardCard card)
        {
            if (card is LSO_WillNote)
                return _notePrefab != null ? _notePrefab.transform.localScale : Vector3.one;

            return _piecePrefab != null ? _piecePrefab.transform.localScale : Vector3.one;
        }

        /// <summary>
        /// target을 parent 기준의 로컬 좌표로 옮겨 적는다.
        ///
        /// 안 꽂았으면 원점이다. 그러면 기준 자리에서 드나들게 되는데,
        /// 앵커를 따로 두기 전과 같은 동작이라 배선을 빠뜨려도 돌아간다.
        /// </summary>
        private static Vector3 LocalOf(Transform parent, Transform target)
        {
            if (parent == null || target == null) return Vector3.zero;

            return parent.InverseTransformPoint(target.position);
        }

        /// <summary>상태를 콘솔에 찍을 때 쓴다.</summary>
        public string Describe()
        {
            return
                $"기물 {(_pieces == null ? "없음" : $"대기 {_pieces.IdleCount} / 만든 것 {_pieces.CreatedCount}")}" +
                $" · 메모장 {(_notes == null ? "없음" : $"대기 {_notes.IdleCount} / 만든 것 {_notes.CreatedCount}")}";
        }
    }
}
