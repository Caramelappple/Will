using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LDY.Boss.BullKing;
using _Scripts.LSO.UI.Effect;
using UnityEngine;

namespace _Scripts.DLJ.Boss
{
    // DLJ: 접촉 위치와 충격 전달 연출. 보드 점유/피해는 LDY_BullCollision이 담당한다.
    internal sealed class DLJ_BullImpactMotion : IDisposable
    {
        private sealed class Pose
        {
            public readonly LDY_Animal Animal;
            public readonly Transform Model;
            public readonly Vector3 Start;
            public readonly float Front;
            public readonly float Back;
            public Vector3 Landing;
            public float StartTime;
            public float TravelTime;
            public float Distance;
            public float ContactTime;
            public float ContactFraction;
            public bool Started;
            private readonly LSO_HoverMoveEffect[] _hover;

            public Pose(LDY_Animal animal, LDY_BoardManager board, Vector3 direction, float cell)
            {
                Animal = animal;
                Model = animal.modelTransform;
                Landing = RestPosition(animal, board);
                _hover = animal.GetComponentsInChildren<LSO_HoverMoveEffect>(true);
                foreach (var effect in _hover)
                    if (effect != null) effect.SetSuspended(true, restore: false);
                Start = Model != null ? Model.position : Landing;
                Bounds bounds = ModelBounds(animal, cell);
                float extent = Vector3.Dot(bounds.extents, Abs(direction));
                Front = Vector3.Dot(bounds.center, direction) + extent;
                Back = Vector3.Dot(bounds.center, direction) - extent;
            }

            public void Restore(LDY_BoardManager board)
            {
                // 중단/사망/보드 교체 중에는 살아 있고 여전히 등록된 기물만 제자리로 돌린다.
                if (Model != null && Animal != null && board != null && board.Get(Animal.pos) == Animal)
                    Model.position = RestPosition(Animal, board);
                foreach (var effect in _hover)
                {
                    if (effect == null) continue;
                    effect.ClearOffset();
                    effect.SetSuspended(false);
                }
            }
        }

        private readonly LDY_BoardManager _board;
        private readonly Vector3 _direction;
        private readonly Vector3Int _gridDirection;
        private readonly float _cell;
        private readonly Pose _bull;
        private readonly Pose[] _pieces;
        private bool _disposed;

        public DLJ_BullImpactMotion(LDY_Animal bull, LDY_BoardManager board,
            IReadOnlyList<LDY_Animal> chain, Vector3Int direction)
        {
            _board = board;
            _gridDirection = direction;
            _direction = (Vector3)direction;
            _cell = Vector3.Distance(board.GridToWorld(direction), board.GridToWorld(Vector3Int.zero));
            _bull = new Pose(bull, board, _direction, _cell);
            _pieces = new Pose[chain.Count];
            for (int i = 0; i < chain.Count; i++)
                _pieces[i] = new Pose(chain[i], board, _direction, _cell);
        }

        internal static IEnumerator ChargeToContact(LDY_Animal bull, LDY_Animal target,
            LDY_BoardManager board, Vector3Int direction, Vector3 restingPosition,
            float duration, AnimationCurve easing)
        {
            Transform model = bull != null ? bull.modelTransform : null;
            if (model == null || board == null) yield break;
            Vector3 start = model.position;
            float cell = Vector3.Distance(board.GridToWorld(direction), board.GridToWorld(Vector3Int.zero));
            float elapsed = 0f;
            while (model != null)
            {
                elapsed += Time.deltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                Vector3 contact = restingPosition;
                if (target != null && target.modelTransform != null && board.Get(target.pos) == target)
                {
                    // 회전 중인 모델의 크기도 반영한다. 모델 중심을 상대 칸 중심까지 밀지 않는다.
                    Vector3 axis = direction;
                    Bounds ownBounds = ModelBounds(bull, cell);
                    Bounds targetBounds = ModelBounds(target, cell);
                    float front = Vector3.Dot(ownBounds.center, axis) + Vector3.Dot(ownBounds.extents, Abs(axis));
                    float back = Vector3.Dot(targetBounds.center, axis) - Vector3.Dot(targetBounds.extents, Abs(axis));
                    float toCentre = Vector3.Dot(target.modelTransform.position - model.position, axis);
                    float advance = Mathf.Min(back - front, toCentre - cell * 0.01f);
                    contact = model.position + axis * advance;
                    contact.y = restingPosition.y;
                }
                float progress = t >= 1f ? 1f : Mathf.Clamp01(easing != null ? easing.Evaluate(t) : t * t);
                model.position = Vector3.Lerp(start, contact, progress);
                // 마지막 위치를 쓴 프레임에 바로 충돌을 처리해 접촉 뒤의 한 프레임 정지도 없앤다.
                if (t >= 1f) yield break;
                yield return null;
            }
        }

        public IEnumerator PlayChain(LDY_BullKingBoss boss, bool advanced, bool hitBoardEdge,
            float flightDuration, float returnDuration, float pushHeight, float blockedHeight,
            float bullReturnDuration, float chainTimeMultiplier)
        {
            if (_disposed || _board == null) yield break;
            float chainScale = Mathf.Max(0.1f, chainTimeMultiplier);
            flightDuration = Mathf.Max(0.04f, flightDuration) * chainScale;
            returnDuration = Mathf.Max(0.04f, returnDuration) * chainScale;
            bullReturnDuration = Mathf.Max(0.04f, bullReturnDuration);
            float minimumTransferTime = Mathf.Max(0.045f, Time.deltaTime * 2f) * chainScale;
            float endTime = bullReturnDuration;
            for (int i = 0; i < _pieces.Length; i++)
            {
                Pose piece = _pieces[i];
                piece.Landing = RestPosition(piece.Animal, _board);
                float gap;
                if (i + 1 < _pieces.Length)
                    gap = Mathf.Max(0f, _pieces[i + 1].Back - piece.Front);
                else if (hitBoardEdge)
                {
                    // 가장자리 칸의 바깥쪽 반 칸이 실제 보드 경계다. 기물 표면이 이 선에 닿는다.
                    Vector3 edge = _board.GridToWorld(piece.Animal.pos) + _direction * (_cell * 0.5f);
                    gap = Mathf.Max(0f, Vector3.Dot(edge, _direction) - piece.Front);
                }
                else
                {
                    LDY_Animal obstacle = _board.Get(piece.Animal.pos + _gridDirection);
                    Bounds bounds = obstacle != null ? ModelBounds(obstacle, _cell) : default;
                    float back = Vector3.Dot(bounds.center, _direction)
                        - Vector3.Dot(bounds.extents, Abs(_direction));
                    gap = obstacle != null ? Mathf.Max(0f, back - piece.Front) : _cell;
                }

                piece.Distance = advanced ? _cell : Mathf.Min(gap, _cell);
                piece.TravelTime = advanced ? flightDuration
                    : Mathf.Max(minimumTransferTime, flightDuration * piece.Distance / Mathf.Max(0.001f, _cell));
                if (i + 1 < _pieces.Length)
                {
                    // OutQuad 이동의 역함수: 앞 모델이 다음 모델에 닿는 순간을 구한다.
                    piece.ContactFraction = Mathf.Clamp01(gap / Mathf.Max(0.001f, _cell));
                    piece.ContactTime = advanced
                        ? Mathf.Max(minimumTransferTime, flightDuration * (1f - Mathf.Sqrt(1f - piece.ContactFraction)))
                        : piece.TravelTime;
                    if (advanced)
                        piece.TravelTime = Mathf.Max(piece.TravelTime, piece.ContactTime
                            + (piece.ContactFraction < 1f ? 0.04f : 0f));
                    _pieces[i + 1].StartTime = piece.StartTime + piece.ContactTime;
                }
                endTime = Mathf.Max(endTime, piece.StartTime + piece.TravelTime
                    + (advanced ? 0f : returnDuration));
            }

            Vector3 bullContact = _bull.Model != null ? _bull.Model.position : _bull.Landing;
            bool edgeShaken = false;
            float clock = 0f;
            while (!_disposed)
            {
                if (_bull.Model != null)
                    _bull.Model.position = Vector3.Lerp(bullContact, _bull.Landing,
                        OutQuad(Mathf.Clamp01(clock / bullReturnDuration)));

                for (int i = 0; i < _pieces.Length; i++)
                {
                    Pose piece = _pieces[i];
                    float age = clock - piece.StartTime;
                    if (age < 0f) continue;
                    if (!piece.Started)
                    {
                        piece.Started = true;
                        if (i == 0) boss.ShakeOnCollision();
                        else boss.ShakeOnPiecePushed();
                    }
                    if (hitBoardEdge && i == _pieces.Length - 1 && !edgeShaken && age >= piece.TravelTime)
                    {
                        edgeShaken = true;
                        boss.ShakeOnBoardEdgeCollision();
                    }
                    if (piece.Model == null) continue;

                    float t = Mathf.Clamp01(age / piece.TravelTime);
                    Vector3 position;
                    if (advanced)
                    {
                        float progress = OutQuad(t);
                        if (piece.ContactTime > 0f)
                        {
                            // 낮은 FPS에서도 다음 기물이 반응하기 전에는 접촉면을 통과하지 않는다.
                            progress = age < piece.ContactTime
                                ? piece.ContactFraction * OutQuad(Mathf.Clamp01(age / piece.ContactTime))
                                : piece.ContactFraction + (1f - piece.ContactFraction) * OutQuad(
                                    Mathf.Clamp01((age - piece.ContactTime) /
                                        Mathf.Max(0.001f, piece.TravelTime - piece.ContactTime)));
                        }
                        position = Vector3.Lerp(piece.Start, piece.Landing, progress);
                        // 꼭대기에서 정지하지 않는 짧은 포물선. 이동과 착지가 한 흐름으로 이어진다.
                        position.y += 4f * t * (1f - t) * pushHeight;
                    }
                    else
                    {
                        Vector3 contact = piece.Start + _direction * piece.Distance;
                        if (age < piece.TravelTime)
                        {
                            position = Vector3.Lerp(piece.Start, contact, OutQuad(t));
                            position.y += 4f * t * (1f - t) * blockedHeight * 0.25f;
                        }
                        else
                        {
                            float rebound = Mathf.Clamp01((age - piece.TravelTime) / returnDuration);
                            position = Vector3.Lerp(contact, piece.Landing, OutQuad(rebound));
                            // 보드 끝을 직접 때린 마지막 기물만 높게 튄다. 앞 기물과 연쇄 한도에
                            // 막힌 기물은 다른 기물에 부딪힌 것이므로 기존 반동 높이를 유지한다.
                            bool directlyHitsBoardEdge = hitBoardEdge && i == _pieces.Length - 1;
                            float reboundHeight = blockedHeight * 0.5f;
                            if (directlyHitsBoardEdge)
                                reboundHeight *= boss.BoardEdgeHopMultiplier;
                            position.y += 4f * rebound * (1f - rebound) * reboundHeight;
                        }
                    }
                    piece.Model.position = position;
                }

                if (clock >= endTime) break;
                yield return null;
                clock = Mathf.Min(endTime, clock + Time.deltaTime);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _bull.Restore(_board);
            foreach (Pose piece in _pieces) piece.Restore(_board);
        }

        private static Vector3 RestPosition(LDY_Animal animal, LDY_BoardManager board)
        {
            Vector3 position = board.GridToWorld(animal.pos);
            position.y = animal.RestWorldY ?? (position.y + animal.restHeight);
            return position;
        }

        private static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static Bounds ModelBounds(LDY_Animal animal, float cell)
        {
            Transform model = animal.modelTransform != null ? animal.modelTransform : animal.transform;
            Bounds result = new Bounds(model.position, Vector3.one * cell * 0.5f);
            bool found = false;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                // 파티클/트레일/월드 UI가 접촉 크기를 부풀리지 않게 본체 메시만 센다.
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                if (!found) result = renderer.bounds;
                else result.Encapsulate(renderer.bounds);
                found = true;
            }
            return result;
        }
    }
}
