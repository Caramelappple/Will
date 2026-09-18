using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI.Turn
{
    /// <summary>
    /// 누를 수 있는 동안 표식을 띄워둔다.
    ///
    /// ── 퍼지지 않는다 ─────────────────────────────────────────
    /// 자리에 그대로 떠 있다가 못 누르게 되면 사라진다. 퍼져나가는 연출은
    /// "방금 무슨 일이 일어났다"를 알리는 것이고, 여기서 알려야 하는 것은
    /// **지금 이 상태다** — 한 번 퍼지고 마는 것으로는 놓친 사람이 다시
    /// 알 방법이 없다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 켜고 끌 때만 아주 짧게 크기가 붙는다. 없이 툭 나타나면 눈에 안 띄고,
    /// 길게 끌면 그것 자체가 연출이 되어 판을 가린다.
    ///
    /// 프리팹은 한 번만 만들고 그 뒤로는 껐다 켠다. 턴마다 만들고 부수면
    /// 그만큼 쓰레기가 쌓인다.
    ///
    /// 씬 배선: 표식을 띄울 자리에 붙이고, LSO_EndTurnReady 의 On Changed 에
    /// 이 컴포넌트의 SetShown 을 건다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_ReadyMarker : MonoBehaviour
    {
        [Tooltip("띄울 표식. 프리팹이든 씬에 이미 있는 오브젝트든 된다.\n" +
                 "\n" +
                 "프리팹을 꽂으면 처음 한 번 만들어 두고 껐다 켠다.\n" +
                 "씬 오브젝트를 꽂으면 그것을 그대로 껐다 켠다.")]
        [SerializeField] private GameObject marker;

        [Tooltip("표식을 놓을 자리. 비워두면 이 오브젝트 자리다.\n" +
                 "\n" +
                 "씬에 이미 있는 오브젝트를 꽂았다면 이 값은 무시된다 —\n" +
                 "이미 놓인 자리를 옮기면 손으로 맞춘 것이 틀어진다.")]
        [SerializeField] private Transform anchor;

        [Header("나타나고 사라지기")]
        [Tooltip("나타날 때 커지는 시간(초). 0이면 툭 켜진다.")]
        [SerializeField, Min(0f)] private float showDuration = 0.14f;

        [Tooltip("사라질 때 작아지는 시간(초). 0이면 툭 꺼진다.")]
        [SerializeField, Min(0f)] private float hideDuration = 0.1f;

        [Tooltip("나타나기 전의 크기 배율. 1이면 크기가 안 변하고 그냥 켜진다.")]
        [SerializeField, Min(0f)] private float startScale = 0.85f;

        private GameObject _instance;
        private Vector3 _restScale = Vector3.one;
        private Tween _tween;
        private bool _shown;

        /// <summary>
        /// 표식을 띄우거나 치운다. LSO_EndTurnReady 의 On Changed 에 그대로 건다.
        /// </summary>
        public void SetShown(bool shown)
        {
            if (!EnsureInstance()) return;

            if (_shown == shown) return;

            _shown = shown;

            _tween?.Kill();

            if (shown)
            {
                _instance.SetActive(true);

                if (showDuration <= 0f || Mathf.Approximately(startScale, 1f))
                {
                    _instance.transform.localScale = _restScale;
                    return;
                }

                _instance.transform.localScale = _restScale * startScale;

                _tween = _instance.transform
                    .DOScale(_restScale, showDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .SetLink(_instance);

                return;
            }

            if (hideDuration <= 0f)
            {
                _instance.transform.localScale = _restScale;
                _instance.SetActive(false);
                return;
            }

            _tween = _instance.transform
                .DOScale(_restScale * startScale, hideDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetLink(_instance)
                .OnComplete(() =>
                {
                    // 다음에 켤 때 제 크기에서 시작하도록 되돌려 둔다.
                    _instance.transform.localScale = _restScale;
                    _instance.SetActive(false);
                });
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;

            // 꺼진 채로 남으면 다음에 켰을 때 지난 상태가 그대로 보인다.
            if (_instance != null)
            {
                _instance.transform.localScale = _restScale;
                _instance.SetActive(false);
            }

            _shown = false;
        }

        /// <summary>
        /// 표식을 한 번만 만든다.
        ///
        /// 꽂힌 것이 씬에 이미 있는 오브젝트면 그대로 쓴다. 프리팹이면 만들어서
        /// 자리에 놓는다. 둘을 가르는 기준은 씬에 속해 있는지다.
        /// </summary>
        private bool EnsureInstance()
        {
            if (_instance != null) return true;

            if (marker == null)
            {
                Debug.LogWarning(
                    $"{name}: 띄울 표식이 비어 있습니다. " +
                    "누를 수 있게 돼도 아무것도 안 보입니다.", this);

                enabled = false;
                return false;
            }

            bool isSceneObject = marker.scene.IsValid();

            if (isSceneObject)
            {
                _instance = marker;
            }
            else
            {
                Transform at = anchor != null ? anchor : transform;

                _instance = Instantiate(marker, at.position, at.rotation, at);
                _instance.name = marker.name;
            }

            _restScale = _instance.transform.localScale;
            _instance.SetActive(false);
            _shown = false;

            return true;
        }
    }
}
