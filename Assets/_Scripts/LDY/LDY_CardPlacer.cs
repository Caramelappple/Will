using _Scripts.LSO.Animal;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LDY
{
    // 카드 UI(KTH_DeckManager 등) 쪽에서 카드를 실제 그리드 보드 기물로 소환할 때 쓰는 다리 역할.
    // 카드 데이터 → LSO_AnimalFactory.Create → BoardManager.Place 순서로 이어주는 것 외의 로직은 없다.
    // 씬 배선: BoardManager를 연결할 것.
    public class LDY_CardPlacer : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;

        // 배치할 칸을 직접 정할 때 사용.
        public LDY_Animal PlaceCard(LSO_CardSO card, LDY_Team team, Vector3Int pos)
        {
            if (board == null || card == null || !card.IsValid) return null;
            if (!board.IsEmpty(pos)) return null;

            LDY_Animal animal = LSO_AnimalFactory.Create(card, team, board.transform);
            if (animal == null) return null;

            board.Place(animal, pos);
            return animal;
        }

        // 아직 칸을 직접 클릭해서 고르는 UI가 없는 카드 시스템(KTH_DeckManager)을 위한 자동 배치.
        // Player는 z가 작은 줄부터, Enemy는 z가 큰 줄부터 빈 칸을 찾아 채운다.
        public LDY_Animal PlaceCardAtNextAvailable(LSO_CardSO card, LDY_Team team)
        {
            Vector3Int? pos = FindNextAvailableSlot(team);
            return pos.HasValue ? PlaceCard(card, team, pos.Value) : null;
        }

        private Vector3Int? FindNextAvailableSlot(LDY_Team team)
        {
            if (board == null) return null;

            int startZ = team == LDY_Team.Player ? 0 : LDY_BoardManager.Size - 1;
            int stepZ = team == LDY_Team.Player ? 1 : -1;

            for (int i = 0; i < LDY_BoardManager.Size; i++)
            {
                int z = startZ + stepZ * i;
                for (int x = 0; x < LDY_BoardManager.Size; x++)
                {
                    var pos = new Vector3Int(x, 0, z);
                    if (board.IsEmpty(pos)) return pos;
                }
            }
            return null;
        }
    }
}
