using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.UI.Popup
{
    /// <summary>
    /// 체력 변화를 팝업 요청으로 옮겨주는 다리.
    /// 기물 프리팹에 붙인다.
    ///
    /// Health는 팝업이 있는지 모르고, 팝업은 Health가 있는지 모른다.
    /// 둘을 아는 건 이 컴포넌트뿐이라 어느 쪽을 갈아치워도 나머지는 그대로다.
    /// </summary>
    public class LSO_HealthPopupBinder : MonoBehaviour
    {
        [Tooltip("비워두면 자신과 자식에서 찾는다.")]
        [SerializeField] private Health health;

        [Tooltip("팝업이 뜰 기준 위치. 비우면 자신의 Transform.")]
        [SerializeField] private Transform anchor;

        [Tooltip("기준 위치에서 얼마나 띄울지(월드 단위). 머리 위로 올리는 용도.")]
        [SerializeField] private Vector3 worldOffset = new(0f, 1.2f, 0f);

        [Header("피해")]
        [SerializeField] private Color damageColor = new(1f, 0.35f, 0.3f);
        [SerializeField] private string damagePrefix = "-";

        [Header("회복")]
        [SerializeField] private bool showRecover = true;
        [SerializeField] private Color recoverColor = new(0.45f, 1f, 0.6f);
        [SerializeField] private string recoverPrefix = "+";

        private void Awake()
        {
            if (health == null)
                health = GetComponentInChildren<Health>(true);

            if (health == null)
                Debug.LogWarning($"{name}: Health를 찾지 못해 팝업이 뜨지 않습니다.", this);
        }

        private void OnEnable()
        {
            if (health == null) return;

            health.OnDamage += HandleDamage;

            if (showRecover)
                health.OnRecover += HandleRecover;
        }

        private void OnDisable()
        {
            if (health == null) return;

            health.OnDamage -= HandleDamage;
            health.OnRecover -= HandleRecover;
        }

        private void HandleDamage(DamageResultData data)
        {
            Show($"{damagePrefix}{data.damage}", damageColor);
        }

        private void HandleRecover(RecoverResultData data)
        {
            Show($"{recoverPrefix}{data.recoverValue}", recoverColor);
        }

        private void Show(string text, Color color)
        {
            // 씬에 스포너가 없으면 조용히 넘어간다. 연출이 없다고 게임이 멈추면 안 된다.
            LSO_IDamagePopupSpawner spawner = LSO_DamagePopupSpawner.Current;
            if (spawner == null) return;

            Transform origin = anchor != null ? anchor : transform;
            spawner.Spawn(origin.position + worldOffset, text, color);
        }
    }
}
