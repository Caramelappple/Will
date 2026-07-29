using UnityEngine;

namespace _Scripts.LDY
{
    // 기획자가 인스펙터에서 채우는 배치 한 줄: 어떤 유닛 프리팹을, 어느 팀으로, 어느 칸에 놓을지.
    [System.Serializable]
    public class LDY_UnitSpawnEntry
    {
        [Tooltip("LDY_Animal이 붙어있는 유닛 프리팹")]
        public GameObject unitPrefab;
        public LDY_Team team;
        [Tooltip("x/z는 격자 좌표(0~7), y는 모델 표시용 높이값")]
        public Vector3Int pos;
    }
}
