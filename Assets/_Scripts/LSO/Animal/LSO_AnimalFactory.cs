using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LSO.Animal
{
    /// <summary>
    /// 카드로 보드 기물을 만드는 유일한 입구.
    /// "스탯은 동물SO가, 유언은 카드가 정한다"는 규칙을 여기 한 곳에서만 지키면 된다.
    ///
    /// 코스트는 여기서 건드리지 않는다. 만드는 일과 값을 치르는 일은 다른 책임이고,
    /// 스테이지 초기 배치(LDY_BoardUnitSpawner)처럼 공짜로 세워야 하는 경우도 있다.
    /// 차감은 부르는 쪽(LDY_CardPlacer)이 한다.
    /// </summary>
    public static class LSO_AnimalFactory
    {
        public static LDY_Animal Create(LSO_CardSO card, LDY_Team team, Transform parent = null)
        {
            if (card == null || !card.IsValid)
            {
                Debug.LogWarning("LSO_AnimalFactory: 카드 또는 동물 데이터가 비어 있습니다.");
                return null;
            }

            GameObject prefab = card.Animal.unitPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"LSO_AnimalFactory: {card.AnimalName}의 unitPrefab이 비어 있습니다.", card);
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, parent);

            LDY_Animal animal = instance.GetComponent<LDY_Animal>();
            if (animal == null)
            {
                Debug.LogError($"LSO_AnimalFactory: {prefab.name}에 LDY_Animal 컴포넌트가 없습니다.", prefab);
                Object.Destroy(instance);
                return null;
            }

            animal.Setup(card, team);
            return animal;
        }
    }
}
