using System;
using _Scripts.LSO.Animal;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LDY
{
    // 카드 UI(KTH_DeckManager 등) 쪽에서 카드를 실제 그리드 보드 기물로 소환할 때 쓰는 다리 역할.
    // 카드 데이터 → 코스트 확인 → LSO_AnimalFactory.Create → BoardManager.Place 순서로 이어준다.
    // 씬 배선: BoardManager를 연결할 것. TurnManager는 선택(연결하면 플레이어 턴 시작마다 코스트가 자동으로 채워짐).
    public class LDY_CardPlacer : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_TurnManager turnManager;
        [SerializeField] private int maxCost = 3;

        public int MaxCost => maxCost;
        public int CurrentCost { get; private set; }

        public event Action<int, int> OnCostChanged;

        private void Awake()
        {
            CurrentCost = maxCost;
        }

        private void OnEnable()
        {
            if (turnManager != null)
                turnManager.OnTurnChanged += HandleTurnChanged;
        }

        private void OnDisable()
        {
            if (turnManager != null)
                turnManager.OnTurnChanged -= HandleTurnChanged;
        }

        private void HandleTurnChanged(LDY_Team team)
        {
            if (team == LDY_Team.Player)
                ResetCost();
        }

        public void ResetCost()
        {
            CurrentCost = maxCost;
            OnCostChanged?.Invoke(CurrentCost, maxCost);
        }

        // 카드를 손패에서 실제로 빼기 전에 미리 코스트가 되는지 확인할 때 사용.
        public bool CanAfford(LSO_CardSO card)
        {
            return card != null && card.IsValid && card.Cost <= CurrentCost;
        }

        // 배치할 칸을 직접 정할 때 사용.
        public LDY_Animal PlaceCard(LSO_CardSO card, LDY_Team team, Vector3Int pos)
        {
            if (board == null || card == null || !card.IsValid) return null;
            if (card.Cost > CurrentCost) return null;
            if (!board.IsEmpty(pos)) return null;

            LDY_Animal animal = LSO_AnimalFactory.Create(card, team, board.transform);
            if (animal == null) return null;

            board.Place(animal, pos);
            SpendCost(card.Cost);
            return animal;
        }

        // 아직 칸을 직접 클릭해서 고르는 UI가 없는 카드 시스템(KTH_DeckManager)을 위한 자동 배치.
        // Player는 z가 작은 줄부터, Enemy는 z가 큰 줄부터 빈 칸을 찾아 채운다.
        public LDY_Animal PlaceCardAtNextAvailable(LSO_CardSO card, LDY_Team team)
        {
            Vector3Int? pos = FindNextAvailableSlot(team);
            return pos.HasValue ? PlaceCard(card, team, pos.Value) : null;
        }

        private void SpendCost(int amount)
        {
            CurrentCost -= amount;
            OnCostChanged?.Invoke(CurrentCost, maxCost);
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
