using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LDY.Effect
{
    /// <summary>
    /// 보드에 남아 있는 기물을 줄여서 없앤다. 보드를 뒤집기 직전에 부른다.
    ///
    /// 왜 기물을 치워야 하나:
    /// 기물의 부모가 두 군데로 갈려 있다(씬 배치는 LDY_Pieces, 런타임 소환은 BoardManager의 GameObject).
    /// 보드 루트만 돌리면 타일만 돌고 기물은 원래 자리에 그대로 떠 있는다.
    /// 부모를 바꿔서 같이 돌리는 방법도 있지만, 그러면 연출이 게임 상태(계층 구조)를 건드리게 된다.
    ///
    /// 왜 스케일을 줄여서 없애나:
    ///   · 머티리얼을 건드리지 않는다. URP Lit을 투명으로 바꾸면 셰이더 변형 조합에 따라 마젠타가 뜬 전례가 있다
    ///     (LDY_SceneBuilder.CreateHighlightMaterial의 주석 참고).
    ///   · 오브젝트를 파괴하지 않는다. LDY_DissolveEffect는 연출이 끝나면 GameObject를 지우고
    ///     ActiveCount를 올려 LDY_TurnManager.IsAnimating()을 true로 만든다. 우리는 그 값이 내려가기를
    ///     기다린 뒤에 연출을 시작하므로, 여기서 다시 올리면 서로 물린다.
    ///   · 되돌릴 수 있다. 연출이 중간에 끊겨도 Restore로 원래 크기와 활성 상태가 그대로 돌아온다.
    /// </summary>
    public sealed class LDY_BoardPieceHider
    {
        private readonly struct Snapshot
        {
            public readonly Transform Target;
            public readonly Vector3 LocalScale;
            public readonly bool WasActive;

            public Snapshot(Transform target, Vector3 localScale, bool wasActive)
            {
                Target = target;
                LocalScale = localScale;
                WasActive = wasActive;
            }
        }

        private readonly List<Snapshot> _hidden = new();

        /// <summary>되돌릴 대상이 남아 있는지.</summary>
        public bool HasHidden => _hidden.Count > 0;

        public IEnumerator Hide(IReadOnlyList<LDY_Animal> pieces, float duration, Ease ease)
        {
            if (pieces == null || pieces.Count == 0) yield break;

            foreach (LDY_Animal piece in pieces)
            {
                if (piece == null) continue;

                Transform target = piece.transform;
                if (!target.gameObject.activeSelf) continue;

                _hidden.Add(new Snapshot(target, target.localScale, true));

                if (duration > 0f)
                {
                    // 트윈은 자기가 움직이는 오브젝트에 묶는다.
                    // 기물이 먼저 파괴돼도 파괴된 Transform을 붙든 트윈이 남지 않는다.
                    target.DOScale(Vector3.zero, duration)
                        .SetEase(ease)
                        .SetUpdate(true)
                        .SetLink(target.gameObject);
                }
            }

            if (_hidden.Count == 0) yield break;

            if (duration > 0f)
                yield return new WaitForSecondsRealtime(duration);

            // 트윈이 끊겼을 수도 있으므로 끝 상태를 못 박고 끈다.
            foreach (Snapshot snapshot in _hidden)
            {
                if (snapshot.Target == null) continue;

                snapshot.Target.DOKill();
                snapshot.Target.localScale = Vector3.zero;
                snapshot.Target.gameObject.SetActive(false);
            }
        }

        /// <summary>숨기기 전 상태로 되돌린다.</summary>
        public void Restore()
        {
            foreach (Snapshot snapshot in _hidden)
            {
                if (snapshot.Target == null) continue;

                snapshot.Target.DOKill();
                snapshot.Target.localScale = snapshot.LocalScale;
                snapshot.Target.gameObject.SetActive(snapshot.WasActive);
            }

            _hidden.Clear();
        }
    }
}
