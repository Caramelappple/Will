using System.Collections.Generic;
using System.Linq;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.Factories;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

namespace _Scripts.LDY
{
    [RequireComponent(typeof(Health))]
    public class LDY_Animal : MonoBehaviour
    {
        public LSO_AnimalSO data;

        public Health health;
        
        [Header("Board State")]
        [Tooltip("x/z는 격자 좌표(0~7), y는 모델 표시용 높이값이며 이동/공격 거리 계산에는 쓰이지 않는다.")]
        public Vector3Int pos;
        public LDY_Team team;

        [Header("Stats")]
        public int baseAtk;
        public LDY_RangeType rangeType;
        private List<LSO_IAbility> _abilities = new();
        public LSO_AbilityType abilityType;
        public LSO_WillType willType;

        [Header("3D")]
        [Tooltip("이동/공격 연출 시 실제로 움직일 3D 모델 트랜스폼. 비워두면 자기 자신의 transform을 사용한다.")]
        public Transform modelTransform;

        //public bool IsDead => hp <= 0;

        private void Awake()
        {
           Init();
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            Init();
        }
        #endif

        private void Init()
        {
            if (modelTransform == null)
                modelTransform = transform;

            if (health == null)
                health = GetComponent<Health>();

            if (data == null)
            {
                Debug.LogWarning("LDY_Animal data is null");
                return;
            }
            
            this._abilities.Clear();
            this._abilities.Add(LSO_AbilityFactory.Get(data.ability));

            this.pos = data.pos;
            this.baseAtk = data.damage;
            this.rangeType = data.range;
            this.abilityType = data.ability;

            if (health != null)
                health.Init(data.maxHealth);
       
            
        }

        // ATK는 항상 이 메서드를 통해서만 조회한다.
        // 늑대/하이에나처럼 상황에 따라 공격력이 변하는 특성은 하위 클래스에서 이 메서드를 override해서 구현한다.
        public virtual int GetAtk()
        {
            int atk = baseAtk;
            
            foreach(var mod in _abilities.OfType<IStatModifier>())
                atk= mod.ModifyAttack(this, atk);
            
            return atk;
        }
    }
}

