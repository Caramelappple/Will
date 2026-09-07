using System.Collections.Generic;
using _Scripts.LSO.UI.Input;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LDY.Effect
{
    /// <summary>
    /// 보드가 도는 동안 기물을 판에 태운다. 다 돌면 내려놓는다.
    ///
    /// ── 왜 필요한가 ───────────────────────────────────────────
    /// 기물의 부모가 보드와 갈려 있다.
    ///
    ///   LDY_Board          타일. 회전하는 것은 이것뿐이다
    ///   LDY_GameSystems    런타임에 소환된 기물이 여기 자식으로 붙는다
    ///   LDY_Pieces         씬에 미리 놓아둔 기물
    ///
    /// 셋이 형제라서 보드만 돌리면 기물은 제자리에 그대로 떠 있는다.
    /// 예전에는 그래서 돌리기 전에 기물을 줄여서 없앴다(LDY_BoardPieceHider).
    /// 지금은 잠깐 보드 밑으로 옮겨서 같이 돌게 한다.
    ///
    /// 계층을 아예 바꾸지 않는 이유는 boardOrigin에 스케일이 걸려 있기 때문이다
    /// (LDY_BoardManager.UniformWorldScale이 그 값을 읽는다).
    /// 기물을 그 밑에 상주시키면 크기가 딸려 변한다.
    /// 여기서는 SetParent(.., worldPositionStays: true) 로 옮기므로 겉보기가 변하지 않는다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 연출이 계층을 건드리는 것은 맞다. 그래서 되돌리는 길을 둘 다 열어둔다.
    ///   Settle   정상적으로 다 돌았을 때. 감추고 부모를 되돌린다.
    ///   Restore  중간에 끊겼을 때. 감추지 않고 있던 그대로 되돌린다.
    /// 어느 쪽으로 끝나든 부모와 자세는 태우기 전으로 돌아온다.
    /// </summary>
    public sealed class LSO_BoardRiders
    {
        /// <summary>태우기 직전의 자리. 부모만이 아니라 형제 순서와 로컬 자세까지 적어둔다.</summary>
        private readonly struct Rider
        {
            public readonly Transform Target;
            public readonly Transform Parent;
            public readonly int SiblingIndex;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;
            public readonly Vector3 LocalScale;
            public readonly bool WasActive;

            /// <summary>도는 동안 꺼둘 호버 핸들러. 없을 수도 있다.</summary>
            public readonly LSO_ButtonHoverHandler Hover;
            public readonly bool HoverWasEnabled;

            public Rider(Transform target)
            {
                Target = target;
                Parent = target.parent;
                SiblingIndex = target.GetSiblingIndex();
                LocalPosition = target.localPosition;
                LocalRotation = target.localRotation;
                LocalScale = target.localScale;
                WasActive = target.gameObject.activeSelf;

                Hover = target.GetComponent<LSO_ButtonHoverHandler>();
                HoverWasEnabled = Hover != null && Hover.enabled;
            }
        }

        private readonly List<Rider> _riders = new();

        /// <summary>내려놓을 것이 남아 있는지.</summary>
        public bool HasRiders => _riders.Count > 0;

        /// <summary>
        /// 기물을 판에 태운다. 겉보기는 그대로고 부모만 바뀐다.
        ///
        /// 이미 태운 것이 있으면 아무것도 하지 않는다. 두 번 태우면
        /// 두 번째가 적어두는 "원래 자리"가 이미 보드 밑이라, 내려놓을 곳을 잃는다.
        /// </summary>
        public void Attach(IReadOnlyList<LDY_Animal> pieces, Transform board)
        {
            if (board == null || pieces == null || pieces.Count == 0) return;

            if (HasRiders)
            {
                Debug.LogWarning(
                    "LSO_BoardRiders: 이미 기물이 판에 타 있습니다. 이번 요청을 무시합니다.", board);
                return;
            }

            foreach (LDY_Animal piece in pieces)
            {
                if (piece == null) continue;

                Transform target = piece.transform;
                if (!target.gameObject.activeSelf) continue;

                // 트윈이 돌던 중이면 끊는다. 회전 중에 로컬 좌표를 밀어대면 판 위에서 미끄러진다.
                target.DOKill();

                Board(target, board);
            }
        }

        /// <summary>
        /// 지금 자세 그대로 태운다. 적어두고, 호버를 끄고, 부모를 바꾼다.
        ///
        /// 도는 동안 커서가 얹히면 호버가 로컬 좌표를 밀어서 기물이 판 밖으로 흘러나간다.
        /// 예전에는 기물을 아예 꺼버려서 이런 일이 없었다.
        ///
        /// 끄는 순간 LSO_ButtonHoverHandler가 스스로 Exit를 보내므로,
        /// 이미 떠올라 있던 기물도 제자리로 돌아온 뒤에 실린다.
        /// </summary>
        private Rider Board(Transform target, Transform board)
        {
            var rider = new Rider(target);
            _riders.Add(rider);

            if (rider.Hover != null)
                rider.Hover.enabled = false;

            target.SetParent(board, worldPositionStays: true);

            return rider;
        }

        /// <summary>
        /// **도착할 자리를 기준으로** 태운다. 보상 상자처럼 회전이 끝난 뒤에
        /// 특정 자리에 놓여 있어야 하는 것에 쓴다.
        ///
        /// 지금 놓인 자세를 "도착점"으로 보고, 회전을 거꾸로 한 번 먹여
        /// 뒷면에 해당하는 자리로 옮긴 뒤 태운다. 그러면 판이 돌면서
        /// 원래 놓여 있던 그 자세로 정확히 올라온다.
        ///
        /// 덕분에 씬에서는 **보이고 싶은 자리에 그냥 놓으면 된다.**
        /// 뒷면 좌표를 손으로 계산할 필요가 없다.
        /// </summary>
        public void AttachForArrival(
            Transform target, Transform board, Vector3 pivot, Vector3 axis, float angle)
        {
            if (target == null || board == null) return;

            if (Contains(target))
            {
                Debug.LogWarning(
                    $"LSO_BoardRiders: '{target.name}'은 이미 판에 타 있습니다. 이번 요청을 무시합니다.", target);
                return;
            }

            target.DOKill();

            // 지금 자세를 적어둔 뒤 태운다. 적어둔 자세가 도착점이자,
            // 내려놓을 때 돌아갈 자리다.
            Board(target, board);

            MoveToBackFace(target, pivot, axis, angle);
        }

        /// <summary>
        /// 여러 개를 한꺼번에. 다음 스테이지 기물이 판과 함께 올라올 때 쓴다.
        ///
        /// 기물을 먼저 놓고 이걸 부르면, 놓인 자리를 도착점으로 삼아
        /// 뒷면에서 실려 올라온다.
        ///
        /// **놓는 쪽이 앞면 좌표로 놓아야 한다.** 뒤집힌 채로 격자 계산을 하면
        /// 원점이 어긋난다(LDY_BoardFlipDirector.RunAtHomePose 참고).
        /// 여기서는 놓인 자리를 그대로 도착점으로 믿는다.
        /// </summary>
        public void AttachForArrival(
            IReadOnlyList<LDY_Animal> pieces, Transform board, Vector3 pivot, Vector3 axis, float angle)
        {
            if (board == null || pieces == null || pieces.Count == 0) return;

            foreach (LDY_Animal piece in pieces)
            {
                if (piece == null) continue;

                Transform target = piece.transform;
                if (!target.gameObject.activeSelf) continue;
                if (Contains(target)) continue;

                target.DOKill();

                Board(target, board);

                MoveToBackFace(target, pivot, axis, angle);
            }
        }

        /// <summary>
        /// 회전의 반대를 먹여 뒷면에 해당하는 자리로 옮긴다.
        /// 판이 angle 만큼 돌면 여기서 출발한 것이 옮기기 전 자세에 정확히 닿는다.
        /// </summary>
        private static void MoveToBackFace(Transform target, Vector3 pivot, Vector3 axis, float angle)
        {
            Vector3 unitAxis = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized : Vector3.right;

            Quaternion back = Quaternion.AngleAxis(-angle, unitAxis);

            target.SetPositionAndRotation(
                pivot + back * (target.position - pivot),
                back * target.rotation);
        }

        private bool Contains(Transform target)
        {
            foreach (Rider rider in _riders)
            {
                if (rider.Target == target) return true;
            }

            return false;
        }

        /// <summary>
        /// 다 돌았다. 태운 기록을 지운다. 어느 쪽이든 목록은 반드시 비워야 한다 —
        /// 안 비우면 다음번에 "이미 타 있습니다"로 태우기를 거부한다.
        /// </summary>
        /// <param name="hide">
        /// <b>true</b> — 감추고 부모·자세를 태우기 전으로 되돌린다.
        /// 판 뒤편이라 어차피 안 보이므로 연출 없이 즉시 처리한다.
        /// 보상 화면에 지난 판의 흔적이 남지 않는다.
        ///
        /// <b>false</b> — 판에 매달린 채로 둔다. 판이 가려준다면 이쪽이 자연스럽다.
        /// 이때 부모를 되돌리면 안 된다. 되돌리는 순간 앞면 자리로 순간이동해서
        /// 뒤집힌 판 위 허공에 뜬다. 정리는 LDY_BoardClearStep 이 파괴하며 끝낸다.
        /// </param>
        public void Settle(bool hide)
        {
            if (!hide)
            {
                // 판에 실린 채로 둔다. 손대지 않고 기록만 지운다.
                _riders.Clear();
                return;
            }

            foreach (Rider rider in _riders)
            {
                if (rider.Target == null) continue;

                rider.Target.DOKill();

                // 먼저 끈다. 자리를 되돌리는 한 프레임이 화면에 비치지 않게.
                rider.Target.gameObject.SetActive(false);

                PutBack(rider);
            }

            _riders.Clear();
        }

        /// <summary>
        /// 태우기 전으로 되돌린다. 연출이 중단됐을 때 쓴다.
        ///
        /// Settle과 달리 감추지 않는다. 중단은 "없던 일로 한다"는 뜻이므로
        /// 기물이 판 위에 그대로 서 있어야 한다.
        /// </summary>
        public void Restore()
        {
            foreach (Rider rider in _riders)
            {
                if (rider.Target == null) continue;

                rider.Target.DOKill();

                PutBack(rider);

                // 호버를 되살린다. 중단은 "없던 일로 한다"는 뜻이므로
                // 기물을 다시 고를 수 있어야 한다.
                if (rider.Hover != null)
                    rider.Hover.enabled = rider.HoverWasEnabled;

                rider.Target.gameObject.SetActive(rider.WasActive);
            }

            _riders.Clear();
        }

        /// <summary>
        /// 적어둔 자리로 정확히 돌려놓는다.
        ///
        /// worldPositionStays를 false로 두는 것이 핵심이다. true로 두면 유니티가
        /// 지금 월드 자세를 유지하려고 로컬 값을 다시 계산하는데, 보드가 뒤집힌
        /// 상태라 그 값이 원래와 달라진다. 우리는 적어둔 로컬 값을 그대로 쓴다.
        /// </summary>
        private static void PutBack(Rider rider)
        {
            rider.Target.SetParent(rider.Parent, worldPositionStays: false);

            rider.Target.localPosition = rider.LocalPosition;
            rider.Target.localRotation = rider.LocalRotation;
            rider.Target.localScale = rider.LocalScale;

            if (rider.Parent != null)
                rider.Target.SetSiblingIndex(Mathf.Min(rider.SiblingIndex, rider.Parent.childCount - 1));
        }
    }
}
