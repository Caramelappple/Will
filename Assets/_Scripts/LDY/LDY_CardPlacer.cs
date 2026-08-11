using System;
using System.Collections.Generic;
using _Scripts.LSO;
using _Scripts.LSO.Animal;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI.Feedback;
using _Scripts.LSO.Will;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LDY
{
    // 카드 UI(KTH_DeckManager 등) 쪽에서 카드를 실제 그리드 보드 기물로 소환할 때 쓰는 다리 역할.
    // 카드 데이터 → 코스트 확인 → LSO_AnimalFactory.Create → BoardManager.Place 순서로 이어준다.
    // 씬 배선: BoardManager를 연결할 것. TurnManager는 선택(연결하면 플레이어 턴 시작마다 코스트가 자동으로 채워짐).
    // 칸 클릭으로 배치 위치를 고르게 하려면 boardLayerMask/highlighter도 연결할 것.
    public class LDY_CardPlacer : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_TurnManager turnManager;

        [Tooltip("소환 코스트를 낼 곳. 비우면 LDY_ActionPointManager.instance를 쓴다.\n" +
                 "이동·공격과 같은 풀이라 소환도 행동력을 소모한다.")]
        [SerializeField] private LDY_ActionPointManager actionPoints;

        [Header("유언 선택")]
        [Tooltip("고를 수 있는 유언. 비워두면 전부 고를 수 있다.\n" +
                 "해금 시스템이 붙으면 이 목록 대신 그쪽을 보게 된다.")]
        [SerializeField] private List<LSO_WillType> selectableWills = new();

        [Header("칸 클릭으로 배치 위치 선택 (BeginPlacement 사용 시 필요)")]
        [SerializeField] private LayerMask boardLayerMask;
        [SerializeField] private LDY_TileHighlighter highlighter;
        [SerializeField] private Camera targetCamera;

        /// <summary>소환 코스트 = 행동력. 값을 따로 들고 있지 않고 항상 원본을 본다.</summary>
        private LDY_ActionPointManager ActionPoints =>
            actionPoints != null ? actionPoints : LDY_ActionPointManager.instance;

        public int MaxCost => ActionPoints != null ? ActionPoints.Max : 0;
        public int CurrentCost => ActionPoints != null ? ActionPoints.Current : 0;
        public bool IsPlacing { get; private set; }

        /// <summary>턴 매니저가 연결돼 있으면 플레이어 턴에만 소환할 수 있다.</summary>
        public bool IsPlayerTurn => turnManager == null || turnManager.CurrentTurn == LDY_Team.Player;

        public event Action<int, int> OnCostChanged;

        private LSO_CardSO _pendingCard;
        private LDY_Team _pendingTeam;
        private Action<LDY_Animal> _onPlaced;
        private Action _onCancelled;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (turnManager != null)
                turnManager.OnTurnChanged += HandleTurnChanged;

            // 코스트 표시 UI가 이 컴포넌트를 보고 있으므로 행동력 변화를 그대로 넘겨준다.
            if (ActionPoints != null)
                ActionPoints.OnActionPointsChanged += HandleActionPointsChanged;
        }

        private void OnDisable()
        {
            if (turnManager != null)
                turnManager.OnTurnChanged -= HandleTurnChanged;

            if (ActionPoints != null)
                ActionPoints.OnActionPointsChanged -= HandleActionPointsChanged;
        }

        private void HandleActionPointsChanged(int current, int max)
        {
            OnCostChanged?.Invoke(current, max);
        }

        private void Update()
        {
            if (!IsPlacing || Mouse.current == null) return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            if (!TryRaycastToGrid(out var pos)) return;
            if (!IsValidPlacementTile(pos, _pendingTeam)) return;

            LSO_CardSO card = _pendingCard;
            LDY_Team team = _pendingTeam;
            Action<LDY_Animal> onPlaced = _onPlaced;

            IsPlacing = false;
            ClearPlacementHighlights();
            _pendingCard = null;
            _onCancelled = null;
            _onPlaced = null;

            LDY_Animal animal = PlaceCard(card, team, pos);
            onPlaced?.Invoke(animal);
        }

        private void HandleTurnChanged(LDY_Team team)
        {
            // 턴이 바뀔 때 행동력을 채우는 건 LDY_TurnManager가 이미 한다. 여기서 또 할 필요 없다.
            if (team == LDY_Team.Player) return;

            // 상대 턴이 시작되면 고르던 중이던 배치를 취소한다(카드는 손패에 그대로 남는다).
            CancelPlacement();

            // 유언 창이 열린 채로 적 턴이 진행되면 화면이 막힌다.
            // 이때 기물은 이미 보드에 있으므로 기본 유언으로 확정된다.
            LSO_WillSelection.Abort();
        }

        public void ResetCost()
        {
            if (ActionPoints != null)
                ActionPoints.ResetPoints();
        }

        /// <summary>스테이지마다 다른 코스트를 적용할 때 쓴다. 현재 값도 함께 채운다.</summary>
        public void SetMaxCost(int value)
        {
            if (value <= 0 || ActionPoints == null) return;

            ActionPoints.SetMax(value);
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

            if (card.Cost > CurrentCost)
            {
                LSO_RejectSignal.Raise(LSO_RejectReason.NotEnoughCost);
                return null;
            }

            if (!board.IsEmpty(pos))
            {
                LSO_RejectSignal.Raise(LSO_RejectReason.InvalidTile);
                return null;
            }

            LDY_Animal animal = LSO_AnimalFactory.Create(card, team, board.transform);

            // 프리팹이 비었다든지 해서 못 만들었으면 값을 치르지 않는다.
            if (animal == null) return null;

            board.Place(animal, pos);
            ActionPoints?.TryConsume(card.Cost);

            RequestWill(card, animal);

            return animal;
        }

        /// <summary>
        /// 소환이 끝난 뒤 플레이어에게 유언을 고르게 한다.
        ///
        /// 기물은 이미 보드에 올라가 있고 소환 시점에 동물 데이터의 기본 유언이 들어가 있다.
        /// 그래서 여기서 하는 일은 "덮어쓰기"이며 취소해도 유언 없는 기물은 생기지 않는다.
        ///
        /// 적 기물은 이 경로를 타지 않는다(LDY_BoardUnitSpawner가 팩토리를 직접 쓴다).
        /// </summary>
        private void RequestWill(LSO_CardSO card, LDY_Animal animal)
        {
            // 플레이어가 직접 놓은 기물만 고를 수 있다.
            if (animal == null || animal.team != LDY_Team.Player) return;

            LSO_WillSelection.Request(
                card,
                SelectableWills,
                card.DefaultWill,
                onSelected: selected => Apply(animal, selected));
        }

        private static void Apply(LDY_Animal animal, LSO_WillType willType)
        {
            // 고르는 사이에 기물이 죽거나 씬이 바뀔 수 있다.
            if (animal == null) return;

            animal.SetWill(willType);
        }

        /// <summary>
        /// 고를 수 있는 유언.
        ///
        /// 해금된 것만 고를 수 있다. 해금 정보가 없으면(테스트 씬 등)
        /// 인스펙터 목록을, 그것도 비어 있으면 전부를 쓴다.
        /// </summary>
        private IReadOnlyList<LSO_WillType> SelectableWills
        {
            get
            {
                IReadOnlyList<LSO_WillType> unlocked = LSO_WillSelection.UnlockedWills;
                if (unlocked is { Count: > 0 }) return unlocked;

                return FallbackWills;
            }
        }

        private IReadOnlyList<LSO_WillType> FallbackWills =>
            selectableWills is { Count: > 0 } ? selectableWills : AllWills;

        // None은 유언이 없는 기물용 값이므로 플레이어의 유언 선택지에는 노출하지 않는다.
        private static readonly LSO_WillType[] AllWills =
        {
            LSO_WillType.Curse,
            LSO_WillType.Rage,
            LSO_WillType.Succession,
            LSO_WillType.Contract,
            LSO_WillType.Sacrifice
        };

        // 아직 칸을 직접 클릭해서 고르는 UI가 없는 카드 시스템을 위한 자동 배치.
        // Player는 z가 작은 줄부터, Enemy는 z가 큰 줄부터 빈 칸을 찾아 채운다.
        public LDY_Animal PlaceCardAtNextAvailable(LSO_CardSO card, LDY_Team team)
        {
            Vector3Int? pos = FindNextAvailableSlot(team);
            return pos.HasValue ? PlaceCard(card, team, pos.Value) : null;
        }

        // 플레이어가 보드 칸을 직접 클릭해서 배치 위치를 고르게 한다.
        // 상대 턴이거나 코스트가 부족하면 바로 false를 반환하고 아무것도 시작하지 않는다.
        // onPlaced는 실제로 칸을 클릭해 소환이 끝난 뒤(성공/실패 모두) 호출되고,
        // onCancelled는 우클릭으로 취소했을 때만 호출된다.
        public bool BeginPlacement(LSO_CardSO card, LDY_Team team, Action<LDY_Animal> onPlaced, Action onCancelled = null)
        {
            if (!IsPlayerTurn)
            {
                LSO_RejectSignal.Raise(LSO_RejectReason.NotYourTurn);
                return false;
            }

            if (!CanAfford(card))
            {
                LSO_RejectSignal.Raise(LSO_RejectReason.NotEnoughCost);
                return false;
            }

            if (IsPlacing) CancelPlacement();

            _pendingCard = card;
            _pendingTeam = team;
            _onPlaced = onPlaced;
            _onCancelled = onCancelled;
            IsPlacing = true;

            ShowPlacementHighlights(team);
            return true;
        }

        public void CancelPlacement()
        {
            if (!IsPlacing) return;

            IsPlacing = false;
            ClearPlacementHighlights();
            _pendingCard = null;

            Action cancelled = _onCancelled;
            _onCancelled = null;
            _onPlaced = null;
            cancelled?.Invoke();
        }

        private void ShowPlacementHighlights(LDY_Team team)
        {
            if (highlighter == null || board == null) return;

            var tiles = new List<Vector3Int>();
            for (int x = 0; x < LDY_BoardManager.Size; x++)
            {
                for (int z = 0; z < LDY_BoardManager.Size; z++)
                {
                    var pos = new Vector3Int(x, 0, z);
                    if (IsValidPlacementTile(pos, team))
                        tiles.Add(pos);
                }
            }

            highlighter.ClearHighlights(this);
            highlighter.ShowMoveHighlights(this, tiles);
        }

        private void ClearPlacementHighlights()
        {
            if (highlighter != null)
                highlighter.ClearHighlights(this);
        }

        // 배치 가능 구역은 자기 진영 절반으로 제한한다 (Player=z 낮은 절반, Enemy=z 높은 절반).
        private bool IsValidPlacementTile(Vector3Int pos, LDY_Team team)
        {
            if (board == null || !board.IsInside(pos) || !board.IsEmpty(pos)) return false;

            int half = LDY_BoardManager.Size / 2;
            return team == LDY_Team.Player ? pos.z < half : pos.z >= half;
        }

        private bool TryRaycastToGrid(out Vector3Int pos)
        {
            pos = default;
            if (targetCamera == null || board == null) return false;

            var ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 100f, boardLayerMask)) return false;

            pos = board.WorldToGrid(hit.point);
            return board.IsInside(pos);
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
