using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.Stage
{
    /// <summary>
    /// 챕터를 깨면 아직 꺼지지 않은 양초를 최대까지 채운다.
    ///
    /// ── 왜 챕터 단위인가 ─────────────────────────────────────
    /// 스테이지마다 채우면 기물을 잃어도 다음 판이면 없던 일이 된다.
    /// 반대로 한 번도 안 채우면 런 후반에는 남은 양초로 버티는 것 말고는
    /// 할 수 있는 것이 없다.
    ///
    /// 챕터는 그 사이다. 한 챕터 안에서 잃은 것은 그 챕터 동안 지고 가고,
    /// 넘어가면 숨을 돌린다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// **꺼진 초는 되살리지 않는다.** 그 판단은 DLJ_PlayerHealth 가 한다
    /// (RecoverForNextStage). 여기서 초를 하나하나 세지 않는다 —
    /// 세는 곳이 둘이 되면 언젠가 서로 다른 말을 한다.
    ///
    /// 씬 배선: 아무 관리 오브젝트에나 하나만. 둘 붙이면 두 번 부르는데,
    /// 채우는 일이라 결과는 같지만 On Recovered 가 두 번 울린다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_ChapterCandleRecovery : MonoBehaviour
    {
        [Header("반응")]
        [Tooltip("양초를 채웠을 때. 연출이나 소리를 걸면 된다.\n" +
                 "\n" +
                 "채울 것이 없었어도 울린다 — 챕터를 넘었다는 사실은 같다.")]
        [SerializeField] private UnityEvent onRecovered;

        [Header("진단")]
        [SerializeField] private bool logSteps = true;

        private LSO_StageProgression _progression;

        private void OnEnable()
        {
            if (!LSO_StageProgression.HasInstance)
            {
                Debug.LogWarning(
                    $"{name}: LSO_StageProgression이 없어 챕터 클리어를 들을 수 없습니다. " +
                    "챕터를 넘어도 양초가 채워지지 않습니다.", this);

                return;
            }

            _progression = LSO_StageProgression.Instance;
            _progression.ChapterChanged += HandleChapterChanged;
        }

        private void OnDisable()
        {
            if (_progression == null) return;

            _progression.ChapterChanged -= HandleChapterChanged;
            _progression = null;
        }

        /// <summary>
        /// 다음 챕터로 넘어갔다.
        ///
        /// 이 신호는 LSO_StageProgression.Advance 안에서만 울린다. 게임을 켤 때나
        /// Restart·SetPosition 으로 자리를 옮길 때는 안 울리므로, "챕터를 깼다"와
        /// 뜻이 정확히 같다. 첫 울림을 걸러낼 것이 없다.
        /// </summary>
        private void HandleChapterChanged(LSO_ChapterSO chapter)
        {
            DLJ_PlayerHealth health = DLJ_PlayerHealth.Instance;

            if (health == null)
            {
                Debug.LogWarning(
                    $"{name}: DLJ_PlayerHealth가 없어 양초를 채우지 못했습니다.", this);

                return;
            }

            int before = health.TotalHealth;

            health.RecoverForNextStage();

            if (logSteps)
                Debug.Log(
                    $"[{name}] 챕터 {chapter?.chapter} 시작 — " +
                    $"양초 {before} → {health.TotalHealth} (꺼진 초는 그대로)",
                    this);

            onRecovered?.Invoke();
        }
    }
}
