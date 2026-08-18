using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LDY
{
    // 씬 배선: BoardManager/MoveSystem/AttackSystem/TileHighlighter를 연결하고,
    // boardLayerMask에는 타일(바닥) 콜라이더가 속한 레이어를 지정할 것.
    // targetCamera를 비워두면 Camera.main을 사용한다.
    // 프로젝트의 Active Input Handling이 New Input System 전용이므로 UnityEngine.InputSystem을 사용한다.
    // 조작: 좌클릭 = 내 기물 선택/공격, 우클릭 = 선택된 기물 이동.
    public class LDY_SelectionController : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_MoveSystem moveSystem;
        [SerializeField] private LDY_AttackSystem attackSystem;
        [SerializeField] private LDY_TileHighlighter highlighter;
        [SerializeField] private LayerMask boardLayerMask;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LDY_TurnManager turnManager;
        [SerializeField] private LDY_CardPlacer cardPlacer;

        /// <summary>지금 선택된 기물. 선택된 것이 없으면 null.</summary>
        public LDY_Animal Selected { get; private set; }

        /// <summary>
        /// 내 기물 선택이 바뀔 때마다 발생한다. 선택되면 그 기물이, 해제되면 null이 넘어온다.
        /// 아군 정보창이 구독하면 된다(적 정보창은 OnEnemyInspectedChanged를 쓴다).
        /// </summary>
        public event Action<LDY_Animal> OnSelectionChanged;

        /// <summary>정보만 들여다보고 있는 적 기물. 없으면 null. 조작 대상인 Selected와는 별개다.</summary>
        public LDY_Animal InspectedEnemy { get; private set; }

        /// <summary>
        /// 적 기물을 클릭해 정보를 볼 때 발생한다. 볼 대상이 없어지면 null이 넘어온다.
        /// 적 전용 정보창이 구독하면 된다(내 기물 정보창은 OnSelectionChanged를 쓴다).
        /// </summary>
        public event Action<LDY_Animal> OnEnemyInspectedChanged;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void OnEnable()
        {
            _Scripts.LSO.Will.LSO_WillSelection.BoardInteractionLockChanged += HandleBoardInteractionLockChanged;
        }

        private void OnDisable()
        {
            _Scripts.LSO.Will.LSO_WillSelection.BoardInteractionLockChanged -= HandleBoardInteractionLockChanged;
        }

        private void HandleBoardInteractionLockChanged(bool locked)
        {
            if (!locked) return;

            Deselect();
            InspectEnemy(null);
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            // 계승 대기 중에는 좌클릭이 계승 대상 선택으로만 쓰이고, 그 외 조작은 전부 막힌다.
            // 계승은 주로 적 턴에(내 기물이 맞아 죽을 때) 발동하므로 턴 가드보다 앞에 둬야 클릭을 받을 수 있다.
            // 대기 중이 아닐 때는 곧바로 아래 기존 흐름으로 떨어지므로, 적 턴 클릭이 새어나가지 않는다.
            if (DLJ_SuccessionSystem.IsWaitingForSuccessionTarget)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    HandleSuccessionClick();
                return;
            }

            if (turnManager != null && turnManager.CurrentTurn != LDY_Team.Player) return;
            if (cardPlacer != null && cardPlacer.IsPlacing) return; // 카드 배치 위치 선택 중엔 이동/공격 클릭을 막는다.
            if (_Scripts.LSO.Will.LSO_WillSelection.IsBoardInteractionLocked) return;

            bool leftClicked = Mouse.current.leftButton.wasPressedThisFrame;
            bool rightClicked = Mouse.current.rightButton.wasPressedThisFrame;
            if (!leftClicked && !rightClicked) return;
            if (!TryRaycastToGrid(out var gridPos)) return;

            if (rightClicked)
            {
                HandleMoveClick(gridPos);
                return;
            }

            HandleSelectOrAttackClick(board.Get(gridPos));
        }

        // 유효한 대상인지는 DLJ_SuccessionSystem이 판단한다. 여기서는 어느 칸을 클릭했는지만 넘긴다.
        // 거부되면 대기 상태가 그대로 유지되므로 다시 클릭하면 된다.
        private void HandleSuccessionClick()
        {
            if (!TryRaycastToGrid(out var gridPos)) return;

            DLJ_SuccessionSystem.TrySelectSuccessionTarget(board.Get(gridPos));
        }

        private void HandleMoveClick(Vector3Int gridPos)
        {
            if (Selected == null) return;
            if (!moveSystem.GetMovableTiles(Selected).Contains(gridPos)) return;

            moveSystem.MoveTo(Selected, gridPos);
            Deselect();
        }

        private void HandleSelectOrAttackClick(LDY_Animal occupant)
        {
            if (Selected == null)
            {
                if (IsSelectable(occupant))
                    Select(occupant);
                else
                    InspectEnemy(occupant); // 적이면 정보만 보여주고, 빈 칸이면 보던 정보를 닫는다.
                return;
            }

            if (occupant == Selected)
            {
                Deselect();
                return;
            }

            // 사거리 안의 적을 클릭한 것은 "공격"이 우선이다. 정보를 보려면 선택을 푼 뒤 클릭하면 된다.
            if (occupant != null && attackSystem.GetAttackTargets(Selected).Contains(occupant))
            {
                attackSystem.Attack(Selected, occupant);
                Deselect();
                return;
            }

            if (IsSelectable(occupant))
                Select(occupant);
            else
                InspectEnemy(occupant);
        }

        // 적 기물(또는 빈 칸=null)을 정보 조회 대상으로 삼는다. 실제로 바뀐 경우에만 알린다.
        private void InspectEnemy(LDY_Animal animal)
        {
            LDY_Animal next = (animal != null && animal.team == LDY_Team.Enemy) ? animal : null;
            if (InspectedEnemy == next) return;

            InspectedEnemy = next;
            OnEnemyInspectedChanged?.Invoke(next);
        }

        // 내 팀(Player) 기물만 선택 가능. 이게 없으면 좌클릭으로 상대 기물을 직접 조작하게 되는 버그가 생긴다.
        // 행동력이 남아있는 한 같은 기물도 여러 번 선택해서 행동할 수 있다.
        private static bool IsSelectable(LDY_Animal animal)
        {
            return animal != null && animal.team == LDY_Team.Player;
        }

        private void Select(LDY_Animal animal)
        {
            InspectEnemy(null); // 내 기물을 고르면 적 정보창은 닫는다(둘이 동시에 떠 있지 않게).

            Selected = animal;
            highlighter.ClearHighlights(this);
            highlighter.ShowMoveHighlights(this, moveSystem.GetMovableTiles(animal));
            highlighter.ShowAttackHighlights(this, attackSystem.GetAttackableTiles(animal));
            OnSelectionChanged?.Invoke(animal);
        }

        private void Deselect()
        {
            bool hadSelection = Selected != null;

            Selected = null;
            highlighter.ClearHighlights(this);

            // 선택이 없던 상태에서 또 호출돼도 UI가 헛돌지 않게 실제로 바뀐 경우에만 알린다.
            if (hadSelection)
                OnSelectionChanged?.Invoke(null);
        }

        private bool TryRaycastToGrid(out Vector3Int gridPos)
        {
            gridPos = default;
            if (targetCamera == null) return false;

            var ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 100f, boardLayerMask)) return false;

            gridPos = board.WorldToGrid(hit.point);
            return board.IsInside(gridPos);
        }
    }
}
