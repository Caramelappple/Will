using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;
using _Scripts.LSO.Will;

namespace _Scripts.LSO.Animal.Data
{
    [CreateAssetMenu(fileName = "NewAnimalSO", menuName = "SO/AnimalSO")]
    public class LSO_AnimalSO : ScriptableObject
    {
        [Header("Tool Tip")]
        public string animalName;
        [TextArea(3, 10)]
        public string description;
        //public Vector3Int pos;
    
        [Header("Stats")]
        public int maxHealth;
        public int damage;
        public int cost;
        public int playerHealthPoints;

        [Tooltip("한 번에 움직일 수 있는 칸 수. 2를 넣으면 2칸까지 이동한다.\n" +
                 "0으로 두면 기본값 1칸으로 취급한다(예전에 만든 에셋 호환용).")]
        [Min(0)]
        public int moveRange = 1;

        /// <summary>실제로 쓰이는 이동 칸 수. 값을 채우지 않은 옛 에셋은 1칸으로 본다.</summary>
        public int MoveRange => moveRange > 0 ? moveRange : 1;

        [Header("Types")]
        public LDY_RangeType range;

        [Tooltip("이 기물이 가진 특성들. 위에서부터 순서대로 등록된다.\n" +
                 "같은 특성을 두 번 넣으면 뒤쪽은 무시된다.")]
        public List<LSO_AbilityType> abilities = new();

        /// <summary>
        /// 이 기물이 가진 특성 종류.
        ///
        /// 이름이 Abilities가 아닌 것은 LDY_Animal.Abilities(실제 특성 인스턴스)와 구분하기 위해서다.
        ///   AbilityTypes = 무엇을 가졌는지(enum), Abilities = 실제로 동작 중인 객체.
        /// </summary>
        public IReadOnlyList<LSO_AbilityType> AbilityTypes =>
            abilities ?? (IReadOnlyList<LSO_AbilityType>)Array.Empty<LSO_AbilityType>();

        [Tooltip("플레이어가 고르지 않고 소환될 때 쓸 유언.\n" +
                 "적 기물과 스테이지 초기 배치가 이 값을 쓴다.\n" +
                 "플레이어가 직접 소환할 때는 선택 UI 결과가 우선한다.")]
        public LSO_WillType defaultWill;

        [Header("Prefab")]
        [Tooltip("보드에 소환될 기물 프리팹. LDY_Animal 컴포넌트가 붙어 있어야 한다.")]
        public GameObject unitPrefab;
    }
}
