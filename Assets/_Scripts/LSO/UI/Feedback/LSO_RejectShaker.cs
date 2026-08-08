using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.UI.Feedback
{
    /// <summary>
    /// 거부 신호를 받아 흔들기를 재생한다.
    ///
    /// 어떤 이유에 반응할지 골라둘 수 있다.
    /// 코스트 표시는 코스트 부족에만, 턴 표시는 상대 턴에만 흔들리게 하는 식이다.
    /// </summary>
    [RequireComponent(typeof(LSO_ShakeEffect))]
    public class LSO_RejectShaker : MonoBehaviour
    {
        [Tooltip("비워두면 모든 거부 신호에 반응한다.")]
        [SerializeField] private List<LSO_RejectReason> reasons = new();

        [Tooltip("연달아 거부돼도 이 간격 안에는 다시 흔들지 않는다.")]
        [SerializeField, Min(0f)] private float cooldown = 0.15f;

        private LSO_ShakeEffect _shake;
        private float _lastPlayTime = float.NegativeInfinity;

        private void Awake()
        {
            _shake = GetComponent<LSO_ShakeEffect>();
        }

        private void OnEnable()
        {
            LSO_RejectSignal.Rejected += HandleRejected;
        }

        private void OnDisable()
        {
            LSO_RejectSignal.Rejected -= HandleRejected;
        }

        private void HandleRejected(LSO_RejectReason reason)
        {
            if (reasons.Count > 0 && !reasons.Contains(reason)) return;

            // 클릭을 연타하면 흔들기가 매번 처음으로 되감겨 오히려 안 움직이는 것처럼 보인다.
            if (Time.unscaledTime - _lastPlayTime < cooldown) return;

            _lastPlayTime = Time.unscaledTime;
            _shake.Play();
        }
    }
}
