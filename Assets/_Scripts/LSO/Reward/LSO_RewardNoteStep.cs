using System;
using System.Collections;
using _Scripts.LSO.Will;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 유언 메모장이 올라오고, 눌릴 때까지 떠 있다가, 상자로 들어가고, 유언이 풀리기까지.
    ///
    /// 상자가 시키면 처음부터 끝까지 혼자 진행한다.
    /// 상자는 "언제 시작할지" 와 "끝났나" 만 알면 된다 — 그 사이는 여기 몫이다.
    ///
    /// MonoBehaviour가 아니라 상자가 속으로 쓰는 도구다. 씬에 따로 놓을 것이 아니고,
    /// 코루틴은 상자가 대신 돌려준다. LDY_BoardFlipMotion과 같은 방식이다.
    ///
    /// ── 지급 시점 ──────────────────────────────────────────────
    /// 종이가 다 들어간 뒤에 유언을 푼다. 떠 있는 동안 이미 풀려 있으면
    /// 다른 화면(도장 목록 등)이 먼저 반응해버려 순서가 어긋나 보인다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    [Serializable]
    public sealed class LSO_RewardNoteStep
    {
        [Tooltip("켜면 처음 보는 유언일 때만 메모장이 나온다. 두 번째부터는 그냥 닫힌다.\n" +
                 "\n" +
                 "끄면 받을 때마다 나온다. 같은 유언을 여러 번 받는 것이 흔하다면 이쪽이 낫다 —\n" +
                 "재고에는 쌓이는데 화면에는 아무 반응이 없으면 받은 줄 모른다.")]
        [SerializeField] private bool onlyWhenNew = true;

        [Tooltip("메모장이 오르내리는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.4f;

        [SerializeField] private Ease riseEase = Ease.OutBack;

        private LSO_RewardCardPool _pool;
        private Transform _anchor;

        private LSO_WillNote _note;
        private DLJ_WillDataSO _pending;
        private bool _dismissed;

        /// <summary>메모장이 떠서 눌리기를 기다리는 중인지. 클릭 게이트가 본다.</summary>
        public bool AcceptsClick { get; private set; }

        /// <summary>지금 올라와 있는 메모장. 클릭 게이트가 여닫을 대상이다.</summary>
        public LSO_RewardCard Note => _note;

        /// <summary>메모장이 다 올라왔을 때. 인자는 보여주는 유언이다.</summary>
        public event Action<DLJ_WillDataSO> Shown;

        /// <summary>종이가 들어가고 유언이 풀렸을 때.</summary>
        public event Action<DLJ_WillDataSO> Unlocked;

        public void Bind(LSO_RewardCardPool pool, Transform anchor)
        {
            _pool = pool;
            _anchor = anchor;
        }

        /// <summary>
        /// 이 보상으로 보여줄 유언. 없으면 null이고, 그러면 메모장 단계를 건너뛴다.
        ///
        /// 반드시 지급 전에 부를 것. 지급하고 나면 해금 목록에 들어가
        /// 처음 보는 것인지 알 수 없게 된다.
        /// </summary>
        public DLJ_WillDataSO Resolve(LSO_RewardOption option)
        {
            _pending = null;

            if (option == null || option.will == null) return null;
            if (_pool == null || !_pool.HasNote) return null;

            if (!onlyWhenNew)
            {
                _pending = option.will;
                return _pending;
            }

            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            // 해금 목록을 못 보면 처음인지 알 수 없다. 그럴 때는 보여준다 —
            // 한 번 더 보는 것이 못 보고 넘어가는 것보다 낫다.
            bool known = library != null
                         && library.Claim != null
                         && library.Claim.Unlocks.IsWillUnlocked(option.will);

            _pending = known ? null : option.will;

            return _pending;
        }

        /// <summary>
        /// 올라오고 → 눌릴 때까지 기다리고 → 들어가고 → 유언을 푼다.
        ///
        /// Resolve가 null을 돌려줬으면 아무 일도 하지 않고 끝난다.
        /// </summary>
        public IEnumerator Run()
        {
            if (_pending == null) yield break;

            if (_pool == null)
            {
                Debug.LogWarning("[LSO_RewardNoteStep] 풀이 없어 메모장을 꺼내지 못했습니다.");
                yield break;
            }

            _note = _pool.TakeNote();

            if (_note == null)
            {
                Debug.LogError("[LSO_RewardNoteStep] Will Note Prefab이 없어 메모장을 만들지 못했습니다.");
                yield break;
            }

            _note.transform.SetParent(_anchor, false);
            _note.transform.localPosition = _pool.NoteInsideLocal;
            _note.transform.localRotation = Quaternion.identity;

            _dismissed = false;
            _note.Bind(_pending, HandleClicked);

            // 멈출 자리는 앵커의 원점이다. 앵커를 옮기면 그대로 따라온다.
            yield return _note.transform
                .DOLocalMove(Vector3.zero, riseDuration)
                .SetEase(riseEase)
                .SetLink(_note.gameObject)
                .WaitForCompletion();

            AcceptsClick = true;

            Shown?.Invoke(_pending);

            // 시간으로는 넘어가지 않는다. 다 읽고 누를 때까지 떠 있는다.
            yield return new WaitUntil(() => _dismissed);

            AcceptsClick = false;

            _note.transform.DOKill();

            yield return _note.transform
                .DOLocalMove(_pool.NoteInsideLocal, riseDuration)
                .SetEase(riseEase)
                .SetLink(_note.gameObject)
                .WaitForCompletion();

            ClaimPending();

            Release();
        }

        /// <summary>
        /// 연출이 끊겼을 때 뒷정리. 상자가 닫힐 때 반드시 부른다.
        ///
        /// 여기까지 오지 못하면 유언이 영영 안 풀린다. 두 번 풀어도 해금 목록은
        /// 집합이라 하나로 남으므로, 놓치는 쪽보다 겹치는 쪽이 낫다.
        /// </summary>
        public void Finish()
        {
            AcceptsClick = false;

            ClaimPending();
            Release();
        }

        private void HandleClicked(LSO_RewardCard note)
        {
            if (!AcceptsClick) return;

            _dismissed = true;
        }

        private void ClaimPending()
        {
            if (_pending == null) return;

            DLJ_WillDataSO will = _pending;
            _pending = null;

            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library != null && library.Claim != null)
                library.Claim.ClaimWill(will);
            else
                Debug.LogWarning("[LSO_RewardNoteStep] LSO_ItemLibraryManager가 없어 유언을 풀지 못했습니다.");

            Unlocked?.Invoke(will);
        }

        private void Release()
        {
            if (_note == null) return;

            _pool?.Return(_note);
            _note = null;
        }
    }
}
