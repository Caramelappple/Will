using _Scripts.LSO.Animal;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// "카드 → 기물 생성 → 보드 등록"만 담당한다. 코스트·턴·스테이지 같은 규칙은 전혀 알지 못한다.
    /// 씬 배선: BoardManager를 연결할 것.
    /// </summary>
    public class LDY_BoardUnitSpawner : MonoBehaviour, LDY_IUnitSpawner
    {
        [SerializeField] private LDY_BoardManager board;

        public LDY_Animal Spawn(LSO_CardSO card, LDY_Team team, Vector3Int pos)
        {
            if (board == null)
            {
                Debug.LogError($"{name}: BoardManager가 연결되어 있지 않습니다.", this);
                return null;
            }

            if (card == null || !card.IsValid)
            {
                Debug.LogWarning($"{name}: 유효하지 않은 카드로 소환을 시도했습니다.", this);
                return null;
            }

            if (!board.IsEmpty(pos))
            {
                Debug.LogWarning($"{name}: {pos} 칸이 비어 있지 않아 {card.AnimalName}을(를) 소환하지 못했습니다.", this);
                return null;
            }

            LDY_Animal animal = LSO_AnimalFactory.Create(card, team, board.transform);
            if (animal == null) return null;

            board.Place(animal, pos);
            return animal;
        }
    }
}
