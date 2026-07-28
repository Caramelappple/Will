using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    // 기획자용 배치 에셋. 프로젝트 창에서 우클릭 > Create > LDY > Encounter로 만들고,
    // Units 리스트에 유닛 프리팹/팀/좌표를 채우면 코드 없이 전투 배치를 구성할 수 있다.
    [CreateAssetMenu(fileName = "NewEncounter", menuName = "LDY/Encounter")]
    public class LDY_EncounterSO : ScriptableObject
    {
        public List<LDY_UnitSpawnEntry> units = new List<LDY_UnitSpawnEntry>();
    }
}
