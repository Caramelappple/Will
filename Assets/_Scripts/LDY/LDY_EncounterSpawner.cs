using UnityEngine;

namespace _Scripts.LDY
{
    // 씬 배선: BoardManager와 배치할 LDY_EncounterSO 에셋을 연결할 것.
    // 유닛 프리팹을 인스턴스화해서 지정된 팀/좌표로 보드에 등록한다 (BoardManager의 자동 스윕에 의존하지 않는다).
    public class LDY_EncounterSpawner : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_EncounterSO encounter;

        private void Awake()
        {
            if (board == null || encounter == null) return;

            foreach (var entry in encounter.units)
            {
                if (entry.unitPrefab == null) continue;

                var instance = Instantiate(entry.unitPrefab);
                var animal = instance.GetComponent<LDY_Animal>();
                if (animal == null)
                {
                    Debug.LogWarning($"{entry.unitPrefab.name}에 LDY_Animal 컴포넌트가 없습니다.");
                    Destroy(instance);
                    continue;
                }

                animal.team = entry.team;
                board.Place(animal, entry.pos);
            }
        }
    }
}
