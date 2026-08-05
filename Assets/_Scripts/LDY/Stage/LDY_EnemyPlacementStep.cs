using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지 데이터에 적힌 적들을 보드에 배치한다. 적 배치 외의 일은 하지 않으며,
    /// 기물을 어떻게 만드는지는 LDY_IUnitSpawner 구현에 맡긴다.
    /// 씬 배선: spawnerSource에 LDY_IUnitSpawner를 구현한 컴포넌트(기본 제공: LDY_BoardUnitSpawner)를 연결할 것.
    /// 비워두면 같은 오브젝트에서 자동으로 찾는다.
    /// </summary>
    public class LDY_EnemyPlacementStep : MonoBehaviour, LDY_IStageSetupStep
    {
        // Unity는 인터페이스 필드를 직렬화하지 못하므로 MonoBehaviour로 받고 실행 시 인터페이스로 확인한다.
        [SerializeField] private MonoBehaviour spawnerSource;

        private LDY_IUnitSpawner _spawner;

        private void Awake()
        {
            ResolveSpawner();
        }

        public void Setup(LDY_StageSO stage)
        {
            if (stage == null) return;
            if (_spawner == null) ResolveSpawner();
            if (_spawner == null)
            {
                Debug.LogError($"{name}: LDY_IUnitSpawner를 찾지 못해 적을 배치할 수 없습니다.", this);
                return;
            }

            foreach (LDY_StageEnemyEntry entry in stage.enemies)
            {
                if (entry == null || entry.card == null) continue;
                _spawner.Spawn(entry.card, LDY_Team.Enemy, entry.pos);
            }
        }

        private void ResolveSpawner()
        {
            _spawner = spawnerSource as LDY_IUnitSpawner;

            if (_spawner == null && spawnerSource != null)
                Debug.LogError($"{name}: {spawnerSource.GetType().Name}은(는) LDY_IUnitSpawner를 구현하지 않습니다.", this);

            if (_spawner == null)
                _spawner = GetComponent<LDY_IUnitSpawner>();
        }
    }
}
