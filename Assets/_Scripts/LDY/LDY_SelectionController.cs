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

        private LDY_Animal _selected;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
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
            if (_selected == null) return;
            if (!moveSystem.GetMovableTiles(_selected).Contains(gridPos)) return;

            moveSystem.MoveTo(_selected, gridPos);
            Deselect();
        }

        private void HandleSelectOrAttackClick(LDY_Animal occupant)
        {
            if (_selected == null)
            {
                if (IsSelectable(occupant))
                    Select(occupant);
                return;
            }

            if (occupant == _selected)
            {
                Deselect();
                return;
            }

            if (occupant != null && attackSystem.GetAttackTargets(_selected).Contains(occupant))
            {
                attackSystem.Attack(_selected, occupant);
                Deselect();
                return;
            }

            if (IsSelectable(occupant))
                Select(occupant);
        }

        // 내 팀(Player) 기물만 선택 가능. 이게 없으면 좌클릭으로 상대 기물을 직접 조작하게 되는 버그가 생긴다.
        // 행동력이 남아있는 한 같은 기물도 여러 번 선택해서 행동할 수 있다.
        private static bool IsSelectable(LDY_Animal animal)
        {
            return animal != null && animal.team == LDY_Team.Player;
        }

        private void Select(LDY_Animal animal)
        {
            _selected = animal;
            highlighter.ClearHighlights(this);
            highlighter.ShowMoveHighlights(this, moveSystem.GetMovableTiles(animal));
            highlighter.ShowAttackHighlights(this, attackSystem.GetAttackableTiles(animal));
        }

        private void Deselect()
        {
            _selected = null;
            highlighter.ClearHighlights(this);
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
