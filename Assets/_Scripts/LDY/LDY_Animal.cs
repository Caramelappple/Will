using UnityEngine;

namespace _Scripts.LDY
{
    public class LDY_Animal : MonoBehaviour
    {
        [Header("Board State")]
        [Tooltip("x/z는 격자 좌표(0~7), y는 모델 표시용 높이값이며 이동/공격 거리 계산에는 쓰이지 않는다.")]
        public Vector3Int pos;
        public LDY_Team team;

        [Header("Stats")]
        public int baseAtk;
        public int hp;
        public LDY_RangeType rangeType;

        [Header("3D")]
        [Tooltip("이동/공격 연출 시 실제로 움직일 3D 모델 트랜스폼. 비워두면 자기 자신의 transform을 사용한다.")]
        public Transform modelTransform;

        public bool IsDead => hp <= 0;

        private void Awake()
        {
            if (modelTransform == null)
                modelTransform = transform;
        }

        // ATK는 항상 이 메서드를 통해서만 조회한다.
        // 늑대/하이에나처럼 상황에 따라 공격력이 변하는 특성은 하위 클래스에서 이 메서드를 override해서 구현한다.
        public virtual int GetAtk()
        {
            return baseAtk;
        }
    }
}
